using Google.Cloud.Firestore;
using System.Threading.Tasks;
using WebApplication2.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication2.Services
{
    public class AdminService
    {
        private readonly FirestoreDb _firestore;
        private readonly ILogger<AdminService> _logger;

        public AdminService(FirestoreDb firestoreDb, ILogger<AdminService> logger)
        {
            _firestore = firestoreDb;
            _logger = logger;
        }

        public async Task<bool> IsAdminAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Checking admin status for user: {UserId}", userId);

                var query = _firestore.Collection("admins")
                    .WhereEqualTo("userId", userId)
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    _logger.LogWarning("No admin record found for user: {UserId}", userId);
                    return false;
                }

                var document = snapshot.Documents[0];
                var admin = document.ConvertTo<Admin>();

                bool isAdmin = admin.IsAdmin || (admin.Role?.ToLower() == "admin");
                _logger.LogInformation("Admin check - User: {UserId}, IsAdmin: {IsAdmin}, Role: {Role}",
                    userId, isAdmin, admin.Role);

                return isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsAdminAsync for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsAdminByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Checking admin status for email: {Email}", email);

                var query = _firestore.Collection("admins")
                    .WhereEqualTo("email", email.ToLower().Trim())
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    _logger.LogWarning("No admin record found for email: {Email}", email);
                    return false;
                }

                var document = snapshot.Documents[0];
                var admin = document.ConvertTo<Admin>();

                bool isAdmin = admin.IsAdmin || (admin.Role?.ToLower() == "admin");
                _logger.LogInformation("Admin check - Email: {Email}, IsAdmin: {IsAdmin}, Role: {Role}",
                    email, isAdmin, admin.Role);

                return isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsAdminByEmailAsync for email: {Email}", email);
                return false;
            }
        }

        public async Task<string> GetUserRoleAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting role for user: {UserId}", userId);

                var query = _firestore.Collection("admins")
                    .WhereEqualTo("userId", userId)
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    _logger.LogWarning("No admin record found for user: {UserId}", userId);
                    return "user";
                }

                var document = snapshot.Documents[0];
                var admin = document.ConvertTo<Admin>();

                var role = admin.Role ?? "user";
                _logger.LogInformation("Role found - User: {UserId}, Role: {Role}", userId, role);

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserRoleAsync for user: {UserId}", userId);
                return "user";
            }
        }

        public async Task<string> CheckAdminExists(string userId)
        {
            try
            {
                var result = $"Checking admin for user: {userId}\n";

                // Check by UserId field
                var query = _firestore.Collection("admins")
                    .WhereEqualTo("userId", userId)
                    .Limit(1);
                var snapshot = await query.GetSnapshotAsync();

                result += $"Query by UserId field: {snapshot.Count} documents found\n";

                if (snapshot.Count > 0)
                {
                    var document = snapshot.Documents[0];
                    var admin = document.ConvertTo<Admin>();
                    result += $"Admin details:\n";
                    result += $"  ID: {document.Id}\n";
                    result += $"  UserId: {admin.UserId}\n";
                    result += $"  Email: {admin.Email}\n";
                    result += $"  Role: {admin.Role}\n";
                    result += $"  IsAdmin: {admin.IsAdmin}\n";
                    result += $"  FullName: {admin.FullName}\n";
                }

                // List all admins to see what's there
                var allAdmins = await _firestore.Collection("admins").GetSnapshotAsync();
                result += $"\nTotal admins in collection: {allAdmins.Count}\n";

                foreach (var doc in allAdmins.Documents)
                {
                    var admin = doc.ConvertTo<Admin>();
                    result += $"Admin: {doc.Id} - UserId: {admin.UserId} - Email: {admin.Email} - Role: {admin.Role} - IsAdmin: {admin.IsAdmin}\n";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<Admin> GetAdminByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Getting admin by email: {Email}", email);

                var query = _firestore.Collection("admins")
                    .WhereEqualTo("email", email.ToLower().Trim())
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    _logger.LogWarning("No admin found with email: {Email}", email);
                    return null;
                }

                var document = snapshot.Documents[0];
                var admin = document.ConvertTo<Admin>();
                admin.Id = document.Id;

                _logger.LogInformation("Admin found: {FirstName} {LastName}, Role: {Role}",
                    admin.FirstName, admin.LastName, admin.Role);
                return admin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin by email: {Email}", email);
                return null;
            }
        }

        public async Task<Admin> GetAdminByUserIdAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting admin by user ID: {UserId}", userId);

                var query = _firestore.Collection("admins")
                    .WhereEqualTo("userId", userId)
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    _logger.LogWarning("No admin found with user ID: {UserId}", userId);
                    return null;
                }

                var document = snapshot.Documents[0];
                var admin = document.ConvertTo<Admin>();
                admin.Id = document.Id;

                _logger.LogInformation("Admin found: {Email}, Role: {Role}", admin.Email, admin.Role);
                return admin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin by user ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateAdminAsync(Admin admin)
        {
            try
            {
                _logger.LogInformation("Updating admin: {Email}", admin.Email);

                if (string.IsNullOrEmpty(admin.Id))
                {
                    throw new ArgumentException("Admin ID is required for update");
                }

                var documentRef = _firestore.Collection("admins").Document(admin.Id);

                // Use the helper methods to set timestamps
                admin.SetUpdatedAt(DateTime.UtcNow);

                var updates = new Dictionary<string, object>
                {
                    { "firstName", admin.FirstName ?? string.Empty },
                    { "lastName", admin.LastName ?? string.Empty },
                    { "fullName", admin.GetDisplayName() },
                    { "email", admin.Email.ToLower().Trim() },
                    { "isAdmin", admin.IsAdmin },
                    { "role", admin.Role ?? "admin" },
                    { "updatedAt", admin.UpdatedAt ?? Timestamp.FromDateTime(DateTime.UtcNow) }
                };

                // Only update lastLogin if it's set
                if (admin.LastLogin != null)
                {
                    updates["lastLogin"] = admin.LastLogin;
                }

                await documentRef.UpdateAsync(updates);
                _logger.LogInformation("Admin updated successfully: {Email}", admin.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin: {Email}", admin.Email);
                return false;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(string email)
        {
            try
            {
                var admin = await GetAdminByEmailAsync(email);
                if (admin != null)
                {
                    admin.SetLastLogin(DateTime.UtcNow);
                    return await UpdateAdminAsync(admin);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for: {Email}", email);
                return false;
            }
        }

        public async Task<Admin> CreateDefaultAdminProfileAsync(string email, string userId, string fullName = null)
        {
            try
            {
                _logger.LogInformation("Creating default admin profile for: {Email}", email);

                var names = fullName?.Split(' ') ?? new[] { "Admin", "User" };
                var firstName = names.Length > 0 ? names[0] : "Admin";
                var lastName = names.Length > 1 ? names[1] : "User";

                var admin = new Admin
                {
                    UserId = userId,
                    Email = email.ToLower().Trim(),
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = fullName?.Trim() ?? $"{firstName} {lastName}",
                    IsAdmin = true,
                    Role = "admin"
                };

                // Use helper methods for timestamps
                admin.SetCreatedAt(DateTime.UtcNow);
                admin.SetLastLogin(DateTime.UtcNow);
                admin.SetUpdatedAt(DateTime.UtcNow);

                var collectionRef = _firestore.Collection("admins");
                var result = await collectionRef.AddAsync(admin);
                admin.Id = result.Id;

                _logger.LogInformation("Default admin profile created: {DocumentId}", result.Id);
                return admin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default admin profile for: {Email}", email);
                return null;
            }
        }

        public async Task<List<Admin>> GetAllAdminsAsync()
        {
            try
            {
                var snapshot = await _firestore.Collection("admins").GetSnapshotAsync();
                var admins = new List<Admin>();

                foreach (var document in snapshot.Documents)
                {
                    var admin = document.ConvertTo<Admin>();
                    admin.Id = document.Id;
                    admins.Add(admin);
                }

                return admins;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all admins");
                return new List<Admin>();
            }
        }

        public async Task<bool> DeleteAdminAsync(string adminId)
        {
            try
            {
                await _firestore.Collection("admins").Document(adminId).DeleteAsync();
                _logger.LogInformation("Admin deleted: {AdminId}", adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin: {AdminId}", adminId);
                return false;
            }
        }
    }
}