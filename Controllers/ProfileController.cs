using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebApplication2.Models;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AdminService _adminService;
        private readonly AuthService _authService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(AdminService adminService, AuthService authService, ILogger<ProfileController> logger)
        {
            _adminService = adminService;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("User not authenticated - redirecting to login");
                    TempData["ErrorMessage"] = "Please login to access your profile.";
                    return RedirectToAction("Login", "Account");
                }

                // Get or create admin profile
                var profile = await _adminService.GetAdminByEmailAsync(userEmail);
                if (profile == null)
                {
                    _logger.LogInformation("Creating default admin profile for: {Email}", userEmail);
                    var fullName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin User";
                    profile = await _adminService.CreateDefaultAdminProfileAsync(userEmail, userId, fullName);

                    if (profile == null)
                    {
                        _logger.LogError("Failed to create default admin profile for: {Email}", userEmail);
                        TempData["ErrorMessage"] = "Failed to create your profile. Please contact support.";
                        return RedirectToAction("Index", "Dashboard");
                    }

                    TempData["SuccessMessage"] = "Profile created successfully!";
                }
                else
                {
                    // Update last login
                    await _adminService.UpdateLastLoginAsync(userEmail);
                }

                // Set session variables
                HttpContext.Session.SetString("IsAdmin", "True");
                HttpContext.Session.SetString("UserEmail", userEmail);

                ViewData["Title"] = "My Profile";
                ViewData["Subtitle"] = "Manage your account information and preferences";

                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile page");
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    TempData["ErrorMessage"] = "Please login to edit your profile.";
                    return RedirectToAction("Login", "Account");
                }

                var profile = await _adminService.GetAdminByEmailAsync(userEmail);

                if (profile == null)
                {
                    TempData["ErrorMessage"] = "Profile not found.";
                    return RedirectToAction("Index");
                }

                ViewData["Title"] = "Edit Profile";
                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile edit page");
                TempData["ErrorMessage"] = "An error occurred while loading the edit page.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Admin model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Profile";
                return View(model);
            }

            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    TempData["ErrorMessage"] = "Authentication required.";
                    return RedirectToAction("Login", "Account");
                }

                var existingProfile = await _adminService.GetAdminByEmailAsync(userEmail);

                if (existingProfile == null)
                {
                    TempData["ErrorMessage"] = "Profile not found.";
                    return RedirectToAction("Index");
                }

                // Update profile information
                existingProfile.FirstName = model.FirstName?.Trim();
                existingProfile.LastName = model.LastName?.Trim();
                existingProfile.FullName = $"{model.FirstName} {model.LastName}".Trim();

                var success = await _adminService.UpdateAdminAsync(existingProfile);

                if (success)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                ModelState.AddModelError("", "An error occurred while updating your profile.");
                ViewData["Title"] = "Edit Profile";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewData["Title"] = "Change Password";
            return View(new Admin());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(Admin model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Change Password";
                return View(model);
            }

            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    TempData["ErrorMessage"] = "Authentication required.";
                    return RedirectToAction("Login", "Account");
                }

                // Verify current password
                var isCurrentPasswordValid = await _authService.VerifyCurrentPasswordAsync(userEmail, model.CurrentPassword);
                if (!isCurrentPasswordValid)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View(model);
                }

                // Update password
                var passwordUpdated = await _authService.UpdatePasswordAsync(userEmail, model.CurrentPassword, model.NewPassword);

                if (passwordUpdated)
                {
                    // Update profile timestamp
                    var admin = await _adminService.GetAdminByEmailAsync(userEmail);
                    if (admin != null)
                    {
                        await _adminService.UpdateAdminAsync(admin);
                    }

                    TempData["SuccessMessage"] = "Password updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update password. Please try again.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                ModelState.AddModelError("", "An error occurred while changing your password.");
                ViewData["Title"] = "Change Password";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLastLogin()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var success = await _adminService.UpdateLastLoginAsync(userEmail);
                    return Json(new { success = success, message = success ? "Last login updated successfully" : "Failed to update last login" });
                }
                return Json(new { success = false, message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Debug()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Content("No user email found in claims");
            }

            try
            {
                var profile = await _adminService.GetAdminByEmailAsync(userEmail);

                if (profile == null)
                {
                    return Content($"No profile found for: {userEmail}\nUser ID: {userId}");
                }

                return Content($"Profile found!\n\n" +
                              $"ID: {profile.Id}\n" +
                              $"Email: {profile.Email}\n" +
                              $"Name: {profile.FirstName} {profile.LastName}\n" +
                              $"User ID: {profile.UserId}\n" +
                              $"Role: {profile.Role}\n" +
                              $"IsAdmin: {profile.IsAdmin}\n" +
                              $"Last Login: {profile.GetLastLogin()}\n" +
                              $"Created: {profile.GetCreatedAt()}\n" +
                              $"Updated: {profile.GetUpdatedAt()}");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }
    }
}