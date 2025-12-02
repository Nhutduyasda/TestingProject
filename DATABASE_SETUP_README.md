# MyProject - Database Models Documentation

## 📁 Cấu Trúc Thư Mục

Dự án được tổ chức theo kiến trúc Areas để phân chia rõ ràng giữa quản trị và người dùng:

```
MyProject/
├── Data/
│   └── ApplicationDbContext.cs          # DbContext chính với tất cả cấu hình
│
├── Models/
│   └── Shared/                          # Models dùng chung (Core/Product)
│       ├── Categories.cs                # Danh mục sản phẩm
│       ├── Supplier.cs                  # Nhà cung cấp
│       ├── Product.cs                   # Sản phẩm
│       ├── Variant.cs                   # Biến thể sản phẩm (size, color, etc.)
│       ├── AttributeType.cs             # Loại thuộc tính (Size, Color, Weight...)
│       ├── VariantAttribute.cs          # Thuộc tính cụ thể của variant
│       ├── ProductImage.cs              # Hình ảnh sản phẩm/variant
│       ├── Combo.cs                     # Combo sản phẩm
│       └── ComboProduct.cs              # Chi tiết sản phẩm trong combo
│
├── Areas/
│   ├── Admin/
│   │   └── Models/                      # Models quản trị
│   │       ├── User.cs                  # Người dùng (Customer/Staff/Admin)
│   │       ├── InventoryLog.cs          # Lịch sử nhập/xuất kho
│   │       ├── OrderAuditLog.cs         # Audit log đơn hàng
│   │       └── OrderStatusExtensions.cs # Extension methods cho OrderStatus
│   │
│   └── User/
│       └── Models/                      # Models người dùng
│           ├── Cart.cs                  # Giỏ hàng
│           ├── CartDetail.cs            # Chi tiết giỏ hàng
│           ├── Invoice.cs               # Hóa đơn/Đơn hàng
│           ├── InvoiceDetail.cs         # Chi tiết hóa đơn
│           ├── ProductReview.cs         # Đánh giá sản phẩm
│           ├── Wishlist.cs              # Danh sách yêu thích
│           └── Notification.cs          # Thông báo người dùng
```

## 🔧 Cài Đặt & Cấu Hình

### Bước 1: Cài đặt các NuGet Packages cần thiết

Mở Terminal trong Visual Studio và chạy các lệnh sau:

```powershell
# Entity Framework Core
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0

# ASP.NET Core MVC Validation
dotnet add package Microsoft.AspNetCore.Mvc.ViewFeatures --version 8.0.0
```

### Bước 2: Cấu hình Connection String

Thêm connection string vào `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyProjectDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Bước 3: Đăng ký DbContext trong Program.cs

Thêm vào file `Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using MyProject.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Đăng ký ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Cấu hình Areas
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### Bước 4: Chạy Migrations

```powershell
# Tạo migration đầu tiên
dotnet ef migrations add InitialCreate

# Tạo database
dotnet ef database update
```

## 📊 Giải Thích Cấu Trúc Database

### 1. **Models/Shared** - Core Product Management
Chứa các models liên quan đến sản phẩm và quản lý cốt lõi, được dùng chung bởi cả Admin và User:

- **Categories**: Danh mục sản phẩm (Thức ăn, Đồ chơi, Phụ kiện...)
- **Supplier**: Nhà cung cấp sản phẩm
- **Product**: Thông tin sản phẩm chính
- **Variant**: Các biến thể của sản phẩm (Size S/M/L, Màu đỏ/xanh...)
- **AttributeType**: Định nghĩa loại thuộc tính (Size, Color, Weight...)
- **VariantAttribute**: Giá trị cụ thể của thuộc tính cho từng variant
- **ProductImage**: Hình ảnh sản phẩm/variant
- **Combo**: Gói combo sản phẩm với giá ưu đãi
- **ComboProduct**: Sản phẩm trong combo (junction table)

### 2. **Areas/Admin/Models** - Administration & Management
Chứa các models dành cho quản trị viên:

- **User**: Quản lý người dùng với 3 roles (Customer, Staff, Admin)
- **InventoryLog**: Lịch sử nhập/xuất/điều chỉnh tồn kho
- **OrderAuditLog**: Ghi lại toàn bộ thay đổi trạng thái đơn hàng
- **OrderStatus**: Enum định nghĩa trạng thái đơn hàng
  - Pending → Confirmed → Shipped → Completed
  - CancelRequested → Cancelled
- **OrderStatusExtensions**: Helper methods hiển thị trạng thái

### 3. **Areas/User/Models** - Customer Shopping Experience
Chứa các models dành cho người dùng/khách hàng:

- **Cart**: Giỏ hàng của người dùng
- **CartDetail**: Chi tiết sản phẩm trong giỏ (liên kết với Variant)
- **Invoice**: Đơn hàng/Hóa đơn
- **InvoiceDetail**: Chi tiết từng item trong đơn (hỗ trợ cả Variant và Combo)
- **ProductReview**: Đánh giá sản phẩm (1-5 sao + comment)
- **Wishlist**: Danh sách yêu thích
- **Notification**: Thông báo cho người dùng

## 🎯 Tính Năng Nổi Bật

### ✅ Quản Lý Sản Phẩm Đa Biến Thể
- Mỗi Product có thể có nhiều Variants (VD: áo có S/M/L)
- Mỗi Variant có giá và số lượng riêng
- Hỗ trợ nhiều loại thuộc tính tùy chỉnh (Size, Color, Weight, Age...)

### ✅ Hệ Thống Combo
- Tạo combo với nhiều sản phẩm
- Tính toán giá gốc và giá khuyến mãi tự động
- Quản lý số lượng combo khả dụng

### ✅ Quản Lý Đơn Hàng
- Workflow rõ ràng: Pending → Confirmed → Shipped → Completed
- Hỗ trợ yêu cầu hủy đơn (CancelRequested)
- Audit log đầy đủ cho mọi thay đổi trạng thái

### ✅ Quản Lý Kho
- Ghi lại lịch sử Import/Export/Adjust/Return/Damaged
- Lưu số lượng trước và sau mỗi giao dịch
- Liên kết với Invoice khi xuất hàng

### ✅ Tương Tác Người Dùng
- Giỏ hàng persistent
- Hệ thống đánh giá sản phẩm
- Wishlist
- Notification system

## 🔐 User Roles

```csharp
public enum UserRole
{
    Customer = 0,  // Khách hàng thông thường
    Staff = 1,     // Nhân viên (xử lý đơn hàng, quản lý kho)
    Admin = 2      // Quản trị viên (full quyền)
}
```

## 💾 Relationships Summary

### Core Relationships:
- Product → Variants (1:Many, Cascade Delete)
- Product → Category (Many:1)
- Product → Supplier (Many:1)
- Variant → VariantAttributes (1:Many, Cascade Delete)
- Variant → ProductImages (1:Many)

### Shopping Relationships:
- User → Carts (1:Many)
- Cart → CartDetails (1:Many, Cascade Delete)
- CartDetail → Variant (Many:1)
- User → Invoices (1:Many)
- Invoice → InvoiceDetails (1:Many, Cascade Delete)
- InvoiceDetail → Variant OR Combo (Many:1, Optional)

### Management Relationships:
- Variant → InventoryLogs (1:Many)
- Invoice → OrderAuditLogs (1:Many, Cascade Delete)

## 📝 Notes

1. **Decimal Precision**: Tất cả giá trị tiền tệ dùng `decimal(18,2)`
2. **Soft Delete**: Invoice và Combo hỗ trợ soft delete (IsDeleted flag)
3. **Audit Trail**: OrderAuditLog ghi lại toàn bộ lịch sử thay đổi đơn hàng
4. **Flexible Attributes**: Hệ thống thuộc tính động cho Variants
5. **Combo Support**: InvoiceDetail hỗ trợ cả sản phẩm đơn lẻ và combo

## 🚀 Nâng Cấp So Với Phiên Bản Gốc

1. ✨ **Phân chia Areas rõ ràng** thay vì để tất cả trong Models/
2. ✨ **Namespace chuẩn hóa** theo cấu trúc folder
3. ✨ **Tài liệu đầy đủ** với comments và README
4. ✨ **Sử dụng .NET 8.0** với các best practices mới nhất
5. ✨ **Relationships configuration** tối ưu và rõ ràng

## ⚠️ Lưu Ý Khi Sử Dụng

- Đảm bảo đã cài đặt SQL Server hoặc LocalDB
- Kiểm tra connection string phù hợp với môi trường của bạn
- Chạy migrations trước khi sử dụng
- Areas phải được cấu hình đúng trong Program.cs

---

**Created Date**: November 22, 2025  
**Version**: 1.0  
**Framework**: ASP.NET Core 8.0 MVC
