using FirebaseAdmin.Auth;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebApplication2.Services
{
    public class AuthService
    {
        private readonly FirebaseAuth _firebaseAuth;
        private readonly string _firebaseApiKey = "AIzaSyBNvrr3B87ZYIS0ALCP6OO9guAHfwN1Um4";

        public AuthService()
        {
            try
            {
                _firebaseAuth = FirebaseAuth.DefaultInstance;
                if (_firebaseAuth == null)
                {
                    throw new InvalidOperationException("FirebaseAuth is null. Check Firebase initialization in Program.cs");
                }
                Console.WriteLine("✅ AuthService initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AuthService initialization failed: {ex.Message}");
                throw;
            }
        }

        public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
        {
            try
            {
                if (string.IsNullOrEmpty(idToken))
                {
                    throw new ArgumentException("ID token cannot be null or empty");
                }

                Console.WriteLine("🔐 Verifying ID token...");
                var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(idToken);
                Console.WriteLine($"✅ ID token verified for user: {decodedToken.Uid}");
                return decodedToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ID token verification failed: {ex.Message}");
                throw;
            }
        }

        public async Task<UserRecord> GetUserAsync(string uid)
        {
            try
            {
                Console.WriteLine($"🔍 Getting user record for: {uid}");
                var user = await _firebaseAuth.GetUserAsync(uid);
                Console.WriteLine($"✅ User record retrieved: {user.Email}");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to get user record: {ex.Message}");
                throw;
            }
        }

        public async Task<UserRecord> CreateUserAsync(string email, string password, string displayName)
        {
            try
            {
                Console.WriteLine($"📝 Creating user: {email}");
                var args = new UserRecordArgs
                {
                    Email = email,
                    Password = password,
                    DisplayName = displayName
                };

                var user = await _firebaseAuth.CreateUserAsync(args);
                Console.WriteLine($"✅ User created successfully: {user.Email}, UID: {user.Uid}");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create user: {ex.Message}");
                throw;
            }
        }
        // NEW PASSWORD MANAGEMENT METHODS
        public async Task<bool> UpdatePasswordAsync(string email, string currentPassword, string newPassword)
        {
            try
            {
                Console.WriteLine($"🔐 Updating password for: {email}");

                if (string.IsNullOrEmpty(email) || newPassword.Length < 6)
                {
                    return false;
                }

                using var client = new HttpClient();

                // Verify current password by signing in
                var verifyPayload = new
                {
                    email = email,
                    password = currentPassword,
                    returnSecureToken = true
                };

                var verifyResponse = await client.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_firebaseApiKey}",
                    new StringContent(JsonSerializer.Serialize(verifyPayload), Encoding.UTF8, "application/json"));

                if (!verifyResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("❌ Current password verification failed");
                    return false;
                }

                // Get the ID token from the response
                var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
                var verifyResult = JsonSerializer.Deserialize<JsonElement>(verifyContent);
                var idToken = verifyResult.GetProperty("idToken").GetString();

                // Update password using the ID token
                var updatePayload = new
                {
                    idToken = idToken,
                    password = newPassword,
                    returnSecureToken = false
                };

                var updateResponse = await client.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_firebaseApiKey}",
                    new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json"));

                if (updateResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Password updated successfully for: {email}");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Password update failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating password: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> VerifyCurrentPasswordAsync(string email, string currentPassword)
        {
            try
            {
                using var client = new HttpClient();

                var verifyPayload = new
                {
                    email = email,
                    password = currentPassword,
                    returnSecureToken = true
                };

                var verifyResponse = await client.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_firebaseApiKey}",
                    new StringContent(JsonSerializer.Serialize(verifyPayload), Encoding.UTF8, "application/json"));

                return verifyResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error verifying current password: {ex.Message}");
                return false;
            }
        }
    }
}