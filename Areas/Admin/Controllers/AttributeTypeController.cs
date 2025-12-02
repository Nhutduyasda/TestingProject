using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyProject.Data;
using MyProject.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class AttributeTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttributeTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AttributeType
        public async Task<IActionResult> Index()
        {
            var attributeTypes = await _context.AttributeTypes
                .OrderBy(at => at.DisplayOrder)
                .ToListAsync();
            return View(attributeTypes);
        }

        // GET: Admin/AttributeType/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AttributeType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttributeType attributeType)
        {
            if (ModelState.IsValid)
            {
                _context.AttributeTypes.Add(attributeType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo loại thuộc tính '{attributeType.Name}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(attributeType);
        }

        // GET: Admin/AttributeType/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var attributeType = await _context.AttributeTypes.FindAsync(id);
            if (attributeType == null)
            {
                return NotFound();
            }
            return View(attributeType);
        }

        // POST: Admin/AttributeType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AttributeType attributeType)
        {
            if (id != attributeType.AttributeTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(attributeType);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật loại thuộc tính '{attributeType.Name}' thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await AttributeTypeExists(attributeType.AttributeTypeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(attributeType);
        }

        // GET: Admin/AttributeType/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var attributeType = await _context.AttributeTypes
                .Include(at => at.VariantAttributes)
                .FirstOrDefaultAsync(at => at.AttributeTypeId == id);
            
            if (attributeType == null)
            {
                return NotFound();
            }

            return View(attributeType);
        }

        // POST: Admin/AttributeType/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attributeType = await _context.AttributeTypes
                .Include(at => at.VariantAttributes)
                .FirstOrDefaultAsync(at => at.AttributeTypeId == id);
            
            if (attributeType != null)
            {
                if (attributeType.VariantAttributes.Any())
                {
                    TempData["ErrorMessage"] = $"Không thể xóa '{attributeType.Name}' vì đang được sử dụng bởi {attributeType.VariantAttributes.Count} biến thể!";
                    return RedirectToAction(nameof(Index));
                }

                _context.AttributeTypes.Remove(attributeType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa loại thuộc tính '{attributeType.Name}' thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> AttributeTypeExists(int id)
        {
            return await _context.AttributeTypes.AnyAsync(e => e.AttributeTypeId == id);
        }
    }
}
