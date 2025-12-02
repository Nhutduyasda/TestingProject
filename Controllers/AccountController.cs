using System.Threading.Tasks;
using MyProject.Data;
using MyProject.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MyProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        
        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            RoleManager<IdentityRole> roleManager, 
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _db = db;
        }

        #region Register

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            // Validation
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Username, email và password (tối thiểu 6 ký tự) là bắt buộc.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
                return View();
            }

            // Ensure required roles exist
            await EnsureRolesExist();

            // Create Identity user
            var identityUser = new ApplicationUser 
            { 
                UserName = username,
                Email = email
            };
            
            var result = await _userManager.CreateAsync(identityUser, password);
            
            if (result.Succeeded)
            {
                // Assign default Customer role
                await _userManager.AddToRoleAsync(identityUser, "Customer");
                
                // Create corresponding domain User record
                await EnsureUserRecordExists(identityUser, email);
                
                // Sign in the user
                await _signInManager.SignInAsync(identityUser, isPersistent: false);
                
                TempData["SuccessMessage"] = "Đăng ký thành công! Chào mừng bạn đến với MyProject.";
                return RedirectToAction("Index", "Home");
            }
            
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
                
            return View();
        }

        #endregion

        #region Login

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
                
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập username và password.");
                return View();
            }
            
            // Ensure required roles exist
            await EnsureRolesExist();
            
            var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                // Ensure User record exists for this Identity user
                var identityUser = await _userManager.FindByNameAsync(username);
                if (identityUser != null)
                {
                    await EnsureUserRecordExists(identityUser, identityUser.Email ?? "");
                    
                    // Check if user is banned
                    if (identityUser.DomainUserId.HasValue)
                    {
                        var user = await _db.Users.FindAsync(identityUser.DomainUserId.Value);
                        if (user != null && !user.IsActive)
                        {
                            // User is banned - sign them out and show error
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ với quản trị viên.");
                            return View();
                        }
                        
                        // Update last login date
                        user!.LastLoginDate = DateTime.Now;
                        await _db.SaveChangesAsync();
                    }
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                    
                return RedirectToAction("Index", "Home");
            }
            
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                // Check if user is banned
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null && user.DomainUserId.HasValue)
                {
                    var domainUser = await _db.Users.FindAsync(user.DomainUserId.Value);
                    if (domainUser != null && !domainUser.IsActive)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa.");
                        return View("Login");
                    }
                    
                    // Update last login
                    if (domainUser != null)
                    {
                        domainUser.LastLoginDate = DateTime.Now;
                        await _db.SaveChangesAsync();
                    }
                }

                return LocalRedirect(returnUrl ?? "/");
            }
            
            // If the user does not have an account, then ask the user to create an account.
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var avatarUrl = info.Principal.FindFirstValue("urn:google:picture") ?? 
                            info.Principal.FindFirstValue("picture") ?? 
                            info.Principal.FindFirstValue("avatar");

            if (email != null)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email
                    };
                    var createResult = await _userManager.CreateAsync(user);
                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Customer");
                        await EnsureUserRecordExists(user, email, avatarUrl);
                        
                        var addLoginResult = await _userManager.AddLoginAsync(user, info);
                        if (addLoginResult.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            return LocalRedirect(returnUrl ?? "/");
                        }
                    }
                }
                else
                {
                    // Update avatar if it exists and user doesn't have one or we want to sync it
                    if (!string.IsNullOrEmpty(avatarUrl) && user.DomainUserId.HasValue)
                    {
                        var domainUser = await _db.Users.FindAsync(user.DomainUserId.Value);
                        if (domainUser != null && string.IsNullOrEmpty(domainUser.Avatar))
                        {
                            domainUser.Avatar = avatarUrl;
                            await _db.SaveChangesAsync();
                        }
                    }

                    // Link the existing user
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl ?? "/");
                    }
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            ModelState.AddModelError(string.Empty, "Error loading external login information.");
            return View("Login");
        }

        #endregion

        #region Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["InfoMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Access Denied

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #endregion

        #region Admin - Create Staff

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateStaff()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Username, email và password (tối thiểu 6 ký tự) là bắt buộc.");
                return View();
            }
            
            var identityUser = new ApplicationUser 
            { 
                UserName = username,
                Email = email
            };
            
            var result = await _userManager.CreateAsync(identityUser, password);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(identityUser, "Staff");
                await EnsureUserRecordExists(identityUser, email);
                
                TempData["SuccessMessage"] = "Tạo tài khoản Staff thành công!";
                return RedirectToAction("Index", "Home");
            }
            
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
                
            return View();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Ensure User domain record exists and is linked to Identity user
        /// </summary>
        private async Task EnsureUserRecordExists(ApplicationUser identityUser, string email, string? avatarUrl = null)
        {
            // Check if this Identity user already has a linked User record
            if (identityUser.DomainUserId.HasValue)
            {
                var linkedUser = await _db.Users.FindAsync(identityUser.DomainUserId.Value);
                if (linkedUser != null)
                {
                    return; // User record already exists and is linked
                }
            }

            // Get user role
            var roles = await _userManager.GetRolesAsync(identityUser);
            var role = UserRole.Customer; // Default
            
            if (roles.Contains("Admin"))
                role = UserRole.Admin;
            else if (roles.Contains("Staff"))
                role = UserRole.Staff;

            // Create new User domain record
            var userRecord = new User
            {
                FirstMidName = identityUser.UserName ?? "User",
                LastName = "",
                Email = email,
                PasswordHash = "", // Not used, Identity handles authentication
                Address = "",
                Avatar = avatarUrl,
                Role = role,
                RegistrationDate = DateTime.Now,
                IsActive = true
            };

            _db.Users.Add(userRecord);
            await _db.SaveChangesAsync();

            // Link the Identity user to the User record
            identityUser.DomainUserId = userRecord.UserID;
            await _userManager.UpdateAsync(identityUser);
        }

        /// <summary>
        /// Ensure all required roles exist in the system
        /// </summary>
        private async Task EnsureRolesExist()
        {
            var roles = new[] { "Admin", "Staff", "Customer" };
            
            foreach (var roleName in roles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        #endregion
    }
}
