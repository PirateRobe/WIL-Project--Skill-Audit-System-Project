using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using WebApplication2.Models;
using WebApplication2.Services;
using System;
using System.Collections.Generic;

namespace WebApplication2.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly AdminService _adminService;

        public AccountController(AuthService authService, AdminService adminService)
        {
            _authService = authService;
            _adminService = adminService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> DebugAdmin(string userId)
        {
            var debugInfo = await _adminService.CheckAdminExists(userId);
            return Content(debugInfo, "text/plain");
        }

        // In your AccountController Login method
        //[HttpPost]
        //public async Task<IActionResult> Login(string idToken)
        //{
        //    try
        //    {
        //        Console.WriteLine("=== SIMPLE LOGIN PROCESS STARTED ===");

        //        if (string.IsNullOrEmpty(idToken))
        //        {
        //            return Json(new { success = false, message = "ID token is required" });
        //        }

        //        // Verify Firebase token
        //        var decodedToken = await _authService.VerifyIdTokenAsync(idToken);
        //        var uid = decodedToken.Uid;
        //        var user = await _authService.GetUserAsync(uid);

        //        Console.WriteLine($"✅ Firebase user authenticated: {user.Email}");

        //        // Check if user is admin
        //        var isAdmin = await _adminService.IsAdminAsync(uid);

        //        if (!isAdmin)
        //        {
        //            // Auto-create admin for @admin.com emails
        //            if (user.Email.EndsWith("@admin.com"))
        //            {
        //                Console.WriteLine($"🔄 Auto-creating admin for: {user.Email}");
        //                await _adminService.CreateDefaultAdminProfileAsync(user.Email, uid, user.DisplayName);
        //                isAdmin = true;
        //            }
        //        }

        //        if (!isAdmin)
        //        {
        //            return Json(new { success = false, message = "Admin access required" });
        //        }

        //        // Create session
        //        var claims = new[]
        //        {
        //            new Claim(ClaimTypes.NameIdentifier, uid),
        //            new Claim(ClaimTypes.Email, user.Email),
        //            new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
        //            new Claim(ClaimTypes.Role, "Admin")
        //        };

        //        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        //        var principal = new ClaimsPrincipal(identity);

        //        await HttpContext.SignInAsync(
        //            CookieAuthenticationDefaults.AuthenticationScheme,
        //            principal,
        //            new AuthenticationProperties
        //            {
        //                IsPersistent = true,
        //                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
        //            });

        //        // Update last login
        //        await _adminService.UpdateLastLoginAsync(user.Email);

        //        Console.WriteLine("✅ Login successful - returning redirect");
        //        return Json(new { success = true, redirectUrl = Url.Action("Index", "Dashboard") });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Login error: {ex.Message}");
        //        return Json(new { success = false, message = $"Login failed: {ex.Message}" });
        //    }
        //}
        [HttpPost]
        public async Task<IActionResult> Login(string idToken)
        {
            try
            {
                Console.WriteLine("=== LOGIN PROCESS STARTED ===");

                if (string.IsNullOrEmpty(idToken))
                {
                    return Json(new { success = false, message = "ID token is required" });
                }

                var decodedToken = await _authService.VerifyIdTokenAsync(idToken);
                var uid = decodedToken.Uid;
                var user = await _authService.GetUserAsync(uid);

                Console.WriteLine($"✅ Firebase user authenticated: {user.Email}");

                // Check if user is admin
                var isAdmin = await _adminService.IsAdminAsync(uid);

                if (!isAdmin)
                {
                    // Auto-create admin for @admin.com emails
                    if (user.Email.EndsWith("@admin.com"))
                    {
                        Console.WriteLine($"🔄 Auto-creating admin for: {user.Email}");
                        await _adminService.CreateDefaultAdminProfileAsync(user.Email, uid, user.DisplayName);
                        isAdmin = true;
                    }
                }

                if (!isAdmin)
                {
                    return Json(new { success = false, message = "Admin access required" });
                }

                // ✅ SET SESSION VARIABLES
                HttpContext.Session.SetString("IsAdmin", "True");
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.DisplayName ?? user.Email.Split('@')[0]);

                // Create session
                var claims = new[]
                {
            new Claim(ClaimTypes.NameIdentifier, uid),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
            new Claim(ClaimTypes.Role, "Admin")
        };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                    });

                // Update last login
                await _adminService.UpdateLastLoginAsync(user.Email);

                Console.WriteLine("✅ Login successful - session variables set");
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Dashboard") });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login error: {ex.Message}");
                return Json(new { success = false, message = $"Login failed: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear session variables
            HttpContext.Session.Remove("IsAdmin");
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserName");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        //[HttpPost]
        //public async Task<IActionResult> Signup(string email, string password, string fullName)
        //{
        //    try
        //    {
        //        Console.WriteLine("=== SIGNUP PROCESS STARTED ===");

        //        // Create user in Firebase Auth
        //        var user = await _authService.CreateUserAsync(email, password, fullName);
        //        Console.WriteLine($"✅ Firebase user created: {user.Email}, UID: {user.Uid}");

        //        // Create admin record in Firestore
        //        var admin = new Admin
        //        {
        //            UserId = user.Uid,
        //            Email = user.Email,
        //            FirstName = fullName.Split(' ')[0],
        //            LastName = fullName.Split(' ').Length > 1 ? string.Join(" ", fullName.Split(' ').Skip(1)) : "",
        //            FullName = fullName,
        //            IsAdmin = true,
        //            Role = "admin",
        //            CreatedAt =DateTime.UtcNow,
        //            LastLogin = DateTime.UtcNow,
        //            UpdatedAt = DateTime.UtcNow
        //        };

        //        await _adminService.CreateAdminAsync(admin);
        //        Console.WriteLine($"✅ Admin record created in Firestore");

        //        // Log the user in immediately after signup
        //        var claims = new[]
        //        {
        //            new Claim(ClaimTypes.NameIdentifier, user.Uid),
        //            new Claim(ClaimTypes.Email, user.Email),
        //            new Claim(ClaimTypes.Name, fullName),
        //            new Claim(ClaimTypes.Role, "Admin")
        //        };

        //        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        //        var authProperties = new AuthenticationProperties
        //        {
        //            IsPersistent = true,
        //            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
        //        };

        //        await HttpContext.SignInAsync(
        //            CookieAuthenticationDefaults.AuthenticationScheme,
        //            new ClaimsPrincipal(claimsIdentity),
        //            authProperties);

        //        Console.WriteLine($"✅ User logged in automatically after signup");
        //        return RedirectToAction("Index", "Dashboard");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Signup failed: {ex.Message}");
        //        ModelState.AddModelError("", $"Failed to create admin account: {ex.Message}");
        //        return View();
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> Logout()
        //{
        //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //    return RedirectToAction("Login");
        //}
    }
}