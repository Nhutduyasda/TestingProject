using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MyProject.Data
{
    /// <summary>
    /// Seeds default roles and admin user for the application
    /// </summary>
    public static class IdentitySeeder
    {
        private static readonly string[] Roles = new[] { "Admin", "Staff", "Customer" };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure all roles exist
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed default admin user
            // Username: admin
            // Password: Admin@123
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@myproject.com",
                    EmailConfirmed = true,
                    PhoneNumber = "0123456789"
                };
                
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create default admin: {errors}");
                }
            }

            // Ensure admin has Admin role only
            var roles = await userManager.GetRolesAsync(adminUser);
            if (!roles.Contains("Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            
            // Remove other roles to keep single role assignment
            foreach (var r in roles.Where(r => r != "Admin"))
            {
                await userManager.RemoveFromRoleAsync(adminUser, r);
            }

            // Seed default customer user
            // Username: user
            // Password: User@123
            var customerUser = await userManager.FindByNameAsync("user");
            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = "user",
                    Email = "user@myproject.com",
                    EmailConfirmed = true,
                    PhoneNumber = "0987654321"
                };
                
                var result = await userManager.CreateAsync(customerUser, "User@123");
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create default customer user: {errors}");
                }
            }

            // Ensure customer has Customer role only
            var customerRoles = await userManager.GetRolesAsync(customerUser);
            if (!customerRoles.Contains("Customer"))
            {
                await userManager.AddToRoleAsync(customerUser, "Customer");
            }
            
            // Remove other roles to keep single role assignment
            foreach (var r in customerRoles.Where(r => r != "Customer"))
            {
                await userManager.RemoveFromRoleAsync(customerUser, r);
            }
        }
    }
}
