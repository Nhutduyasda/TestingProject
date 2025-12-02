using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Interface;
using MyProject.Areas.User.Models;
using MyProject.Areas.Admin.Models;

namespace MyProject.Service
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public InvoiceService(ApplicationDbContext context, IHttpContextAccessor? httpContextAccessor = null)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Log status changes to audit trail
        /// </summary>
        private async Task LogStatusChangeAsync(int invoiceId, OrderStatus oldStatus, OrderStatus newStatus, 
            string? reason = null, int? userId = null, string? role = null)
        {
            var auditLog = new OrderAuditLog
            {
                InvoiceId = invoiceId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                ChangedByRole = role,
                Reason = reason,
                IpAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                IsAutomated = false
            };

            _context.OrderAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            return await _context.Invoices
                .Where(i => !i.IsDeleted)
                .Include(i => i.User)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Variant)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByUserIdAsync(int userId)
        {
            return await _context.Invoices
                .Where(i => i.UserId == userId && !i.IsDeleted)
                .Include(i => i.User)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Variant)
                .ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.User)
                .Include(i => i.InvoiceDetails!)
                    .ThenInclude(d => d.Variant)
                .FirstOrDefaultAsync(i => i.InvoiceId == id && !i.IsDeleted);
        }

        public async Task Add(Invoice invoice)
        {
            if (invoice.InvoiceDetails?.Any() == true)
            {
                invoice.TotalAmount = invoice.InvoiceDetails.Sum(d => d.UnitPrice * d.Quantity);
            }
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                invoice.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Invoice> CreateFromCartAsync(int cartId, PayMethod? payMethod)
        {
            var cart = await _context.Carts
                .Include(c => c.CartDetails!)
                    .ThenInclude(cd => cd.Variant)
                    .ThenInclude(v => v!.Product)
                .FirstOrDefaultAsync(c => c.CartId == cartId);

            if (cart == null) throw new InvalidOperationException("Cart not found");
            if (cart.CartDetails == null || cart.CartDetails.Count == 0)
                throw new InvalidOperationException("Cart is empty");

            // Validate and load variants
            foreach (var cartDetail in cart.CartDetails)
            {
                if (cartDetail.Variant == null)
                {
                    var variant = await _context.Variants.FindAsync(cartDetail.VariantId);
                    if (variant == null)
                    {
                        throw new InvalidOperationException($"Variant with ID {cartDetail.VariantId} not found");
                    }
                    cartDetail.Variant = variant;
                }
            }

            var invoice = new Invoice
            {
                UserId = cart.UserId,
                PayMethod = payMethod,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                InvoiceDetails = new List<InvoiceDetail>()
            };

            foreach (var item in cart.CartDetails)
            {
                if (item.Variant == null) continue;

                var detail = new InvoiceDetail
                {
                    VariantId = item.VariantId,
                    ComboId = null,
                    Quantity = item.Quantity,
                    UnitPrice = item.Variant.Price
                };
                invoice.InvoiceDetails.Add(detail);
            }

            if (!invoice.InvoiceDetails.Any())
            {
                throw new InvalidOperationException("No valid items to checkout");
            }

            decimal totalAmount = invoice.InvoiceDetails.Sum(d => d.UnitPrice * d.Quantity);
            invoice.TotalAmount = totalAmount;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Invoices.Add(invoice);
                
                // Update variant stock with concurrency check
                foreach (var detail in invoice.InvoiceDetails)
                {
                    var variant = await _context.Variants.FindAsync(detail.VariantId);
                    if (variant == null) throw new InvalidOperationException($"Variant {detail.VariantId} not found");

                    if (variant.Quanlity < detail.Quantity)
                    {
                        throw new InvalidOperationException($"Không đủ số lượng cho sản phẩm {variant.VariantName}. Còn lại: {variant.Quanlity}");
                    }

                    variant.Quanlity -= detail.Quantity;
                    _context.Entry(variant).OriginalValues["RowVersion"] = variant.RowVersion;
                }

                // Clear cart
                _context.CartDetails.RemoveRange(cart.CartDetails);
                _context.Carts.Remove(cart);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return invoice;
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                throw new InvalidOperationException("Sản phẩm đã bị thay đổi (hoặc hết hàng) trong quá trình thanh toán. Vui lòng thử lại.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task RecalculateTotalAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null) return;

            invoice.TotalAmount = invoice.InvoiceDetails.Sum(d => d.UnitPrice * d.Quantity);
            await _context.SaveChangesAsync();
        }

        #region Order Workflow Methods

        public async Task<bool> ConfirmAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && !i.IsDeleted);
            if (invoice == null) return false;

            var oldStatus = invoice.Status;

            if (!oldStatus.CanTransitionTo(OrderStatus.Confirmed)) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.Status = OrderStatus.Confirmed;
                invoice.ConfirmedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogStatusChangeAsync(invoiceId, oldStatus, OrderStatus.Confirmed,
                    "Đơn hàng đã được xác nhận", null, "Admin/Staff");

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> MarkShippedAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && !i.IsDeleted);
            if (invoice == null) return false;

            var oldStatus = invoice.Status;

            if (!oldStatus.CanTransitionTo(OrderStatus.Shipped)) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.Status = OrderStatus.Shipped;
                invoice.ShippedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogStatusChangeAsync(invoiceId, oldStatus, OrderStatus.Shipped,
                    "Đơn hàng đã được giao cho đơn vị vận chuyển", null, "Admin/Staff");

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> MarkCompletedByUserAsync(int invoiceId, int userId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.UserId == userId && !i.IsDeleted);
            if (invoice == null) return false;

            var oldStatus = invoice.Status;

            if (!oldStatus.CanTransitionTo(OrderStatus.Completed)) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.Status = OrderStatus.Completed;
                invoice.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogStatusChangeAsync(invoiceId, oldStatus, OrderStatus.Completed,
                    "Khách hàng xác nhận đã nhận hàng", userId, "Customer");

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RequestCancelAsync(int invoiceId, int userId, string? reason)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.UserId == userId && !i.IsDeleted);
            if (invoice == null) return false;

            var oldStatus = invoice.Status;

            if (!oldStatus.CanCustomerCancel()) return false;
            if (oldStatus.IsFinalStatus()) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.Status = OrderStatus.CancelRequested;
                invoice.CancelReason = reason ?? "Khách hàng yêu cầu hủy đơn hàng";
                await _context.SaveChangesAsync();

                await LogStatusChangeAsync(invoiceId, oldStatus, OrderStatus.CancelRequested,
                    invoice.CancelReason, userId, "Customer");

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> AdminCancelAsync(int invoiceId, string? reason)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && !i.IsDeleted);
            if (invoice == null) return false;

            var oldStatus = invoice.Status;

            if (!oldStatus.CanTransitionTo(OrderStatus.Cancelled)) return false;
            if (oldStatus == OrderStatus.Completed) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.Status = OrderStatus.Cancelled;
                invoice.CancelReason = reason ?? "Hủy bởi quản trị viên";
                invoice.CancelledAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogStatusChangeAsync(invoiceId, oldStatus, OrderStatus.Cancelled,
                    invoice.CancelReason, null, "Admin");

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ApproveCancelAsync(int invoiceId, string? reason)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && !i.IsDeleted);
            if (invoice == null || invoice.Status != OrderStatus.CancelRequested) return false;

            var oldStatus = invoice.Status;

            if (!oldStatus.CanTransitionTo(OrderStatus.Cancelled)) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.Status = OrderStatus.Cancelled;
                invoice.CancelReason = reason ?? invoice.CancelReason ?? "Đã chấp nhận yêu cầu hủy";
                invoice.CancelledAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogStatusChangeAsync(invoiceId, oldStatus, OrderStatus.Cancelled,
                    "Admin chấp nhận yêu cầu hủy", null, "Admin");

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RejectCancelAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && !i.IsDeleted);
            if (invoice == null || invoice.Status != OrderStatus.CancelRequested) return false;

            var oldStatus = invoice.Status;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                OrderStatus revertStatus;

                if (invoice.ShippedAt.HasValue)
                    revertStatus = OrderStatus.Shipped;
                else if (invoice.ConfirmedAt.HasValue)
                    revertStatus = OrderStatus.Confirmed;
                else
                    revertStatus = OrderStatus.Pending;

                if (!oldStatus.CanTransitionTo(revertStatus)) return false;

                invoice.Status = revertStatus;
                invoice.CancelReason = null;
                await _context.SaveChangesAsync();

                await LogStatusChangeAsync(invoiceId, oldStatus, revertStatus,
                    "Admin từ chối yêu cầu hủy", null, "Admin");

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<Invoice>> GetCancelRequestsAsync()
        {
            return await _context.Invoices
                .Include(i => i.User)
                .Include(i => i.InvoiceDetails!)
                    .ThenInclude(id => id.Variant)
                    .ThenInclude(v => v!.Product)
                .Where(i => i.Status == OrderStatus.CancelRequested && !i.IsDeleted)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CanUserCancelAsync(int invoiceId, int userId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.UserId == userId && !i.IsDeleted);
            if (invoice == null) return false;

            return invoice.Status == OrderStatus.Pending || invoice.Status == OrderStatus.Confirmed;
        }

        public async Task<IEnumerable<Invoice>> GetUserCancelRequestsAsync(int userId)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceDetails!)
                    .ThenInclude(id => id.Variant)
                    .ThenInclude(v => v!.Product)
                .Where(i => i.UserId == userId && i.Status == OrderStatus.CancelRequested && !i.IsDeleted)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        #endregion
    }
}
