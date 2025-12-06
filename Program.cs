using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MyProject.Data;
using MyProject.Service;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add logging providers
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<MyProject.Interface.IInvoiceService, MyProject.Service.InvoiceService>();
builder.Services.AddScoped<MyProject.Interface.IInvoiceDetailService, MyProject.Service.InvoiceDetailService>();
builder.Services.AddScoped<MyProject.Interface.IUserService, MyProject.Service.UserService>();
builder.Services.AddScoped<MyProject.Interface.ICartService, MyProject.Service.CartService>();
builder.Services.AddScoped<MyProject.Interface.ICartDetailService, MyProject.Service.CartDetailService>();
builder.Services.AddScoped<MyProject.Interface.IVariantService, MyProject.Service.VariantService>();
builder.Services.AddScoped<MyProject.Interface.IInventoryService, MyProject.Service.InventoryService>();
builder.Services.AddScoped<MyProject.Interface.IOrderAuditService, MyProject.Service.OrderAuditService>();
builder.Services.AddScoped<MyProject.Interface.IReviewService, MyProject.Service.ReviewService>();
builder.Services.AddScoped<MyProject.Interface.IWishlistService, MyProject.Service.WishlistService>();
builder.Services.AddScoped<MyProject.Interface.INotificationService, MyProject.Service.NotificationService>();

// ASP.NET Core Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings - relaxed for development (needs strengthening)
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = false; 
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure authentication cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// Add HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession();
// Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Google";
    })
    .AddCookie()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        googleOptions.CallbackPath = "/signin-google";

        // L?y th�m th�ng tin user
        googleOptions.Scope.Add("profile");
        googleOptions.Scope.Add("email");

        // Map Google "sub" th�nh ClaimTypes.NameIdentifier
        googleOptions.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        googleOptions.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
        googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
        googleOptions.ClaimActions.MapJsonKey("picture", "picture");
    });

var app = builder.Build();

// Ensure database & seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        await IdentitySeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCookiePolicy();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
