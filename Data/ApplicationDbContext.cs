using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MyProject.Models.Shared;
using MyProject.Areas.Admin.Models;
using MyProject.Areas.User.Models;

namespace MyProject.Data
{
    /// <summary>
    /// Main database context inheriting from IdentityDbContext for authentication
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // ===== Shared Models - Core Product Management =====
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<AttributeType> AttributeTypes { get; set; }
        public DbSet<VariantAttribute> VariantAttributes { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboProduct> ComboProducts { get; set; }

        // ===== Admin Models - User & Management =====
        public new DbSet<User> Users { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }
        public DbSet<OrderAuditLog> OrderAuditLogs { get; set; }

        // ===== User Models - Shopping & Orders =====
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // ===== TABLE NAMES CONFIGURATION =====
            // Configure table name for custom User table (separate from Identity)
            modelBuilder.Entity<User>().ToTable("AppUsers");
            
            // ===== DECIMAL PRECISION FOR MONETARY VALUES =====
            modelBuilder.Entity<Variant>()
                .Property(v => v.Price)
                .HasColumnType("decimal(18,2)");
                
            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasColumnType("decimal(18,2)");
                
            modelBuilder.Entity<InvoiceDetail>()
                .Property(id => id.UnitPrice)
                .HasColumnType("decimal(18,2)");
                
            modelBuilder.Entity<Combo>()
                .Property(c => c.Price)
                .HasColumnType("decimal(18,2)");
            
            modelBuilder.Entity<Combo>()
                .Property(c => c.OriginalPrice)
                .HasColumnType("decimal(18,2)");
                
            modelBuilder.Entity<ComboProduct>()
                .Property(cp => cp.UnitPrice)
                .HasColumnType("decimal(18,2)");
            
            // ===== SHARED MODELS RELATIONSHIPS =====
            
            // Product relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Variant relationships
            modelBuilder.Entity<Variant>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // VariantAttribute relationships
            modelBuilder.Entity<VariantAttribute>()
                .HasOne(va => va.Variant)
                .WithMany(v => v.VariantAttributes)
                .HasForeignKey(va => va.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<VariantAttribute>()
                .HasOne(va => va.AttributeType)
                .WithMany(at => at.VariantAttributes)
                .HasForeignKey(va => va.AttributeTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // ProductImage relationships
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Variant)
                .WithMany(v => v.Images)
                .HasForeignKey(pi => pi.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Combo relationships
            modelBuilder.Entity<Combo>()
                .HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // ComboProduct relationships (junction table)
            modelBuilder.Entity<ComboProduct>()
                .HasOne(cp => cp.Combo)
                .WithMany(c => c.ComboProducts)
                .HasForeignKey(cp => cp.ComboId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<ComboProduct>()
                .HasOne(cp => cp.Product)
                .WithMany()
                .HasForeignKey(cp => cp.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // ===== USER MODELS RELATIONSHIPS =====
            
            // Cart relationships
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<CartDetail>()
                .HasOne(cd => cd.Cart)
                .WithMany(c => c.CartDetails)
                .HasForeignKey(cd => cd.CartId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<CartDetail>()
                .HasOne(cd => cd.Variant)
                .WithMany(v => v.CartDetails)
                .HasForeignKey(cd => cd.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartDetail>()
                .HasOne(cd => cd.Combo)
                .WithMany()
                .HasForeignKey(cd => cd.ComboId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            
            // Invoice relationships
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.User)
                .WithMany(u => u.Invoices)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Invoice)
                .WithMany(i => i.InvoiceDetails)
                .HasForeignKey(id => id.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Variant)
                .WithMany(v => v.InvoiceDetails)
                .HasForeignKey(id => id.VariantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Combo)
                .WithMany(c => c.InvoiceDetails)
                .HasForeignKey(id => id.ComboId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            
            // ProductReview relationships
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.User)
                .WithMany(u => u.ProductReviews)
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Wishlist relationships
            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.RelatedInvoice)
                .WithMany()
                .HasForeignKey(n => n.RelatedInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
            
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.RelatedProduct)
                .WithMany()
                .HasForeignKey(n => n.RelatedProductId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // ===== ADMIN MODELS RELATIONSHIPS =====
            
            // InventoryLog relationships
            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.Variant)
                .WithMany(v => v.InventoryLogs)
                .HasForeignKey(il => il.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.Invoice)
                .WithMany()
                .HasForeignKey(il => il.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
            
            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.User)
                .WithMany()
                .HasForeignKey(il => il.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // OrderAuditLog relationships
            modelBuilder.Entity<OrderAuditLog>()
                .HasOne(a => a.Invoice)
                .WithMany()
                .HasForeignKey(a => a.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
