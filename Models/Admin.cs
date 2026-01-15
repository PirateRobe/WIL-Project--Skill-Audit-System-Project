using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    [FirestoreData]
    public class Admin
    {
        [FirestoreProperty("userid")]
        public string UserId { get; set; }

        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("firstname")]
        public string FirstName { get; set; }

        [FirestoreProperty("lastname")]
        public string LastName { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("fullname")]
        public string FullName { get; set; }

        [FirestoreProperty("role")]
        public string Role { get; set; } = "admin";

        [FirestoreProperty("isAdmin")]
        public bool IsAdmin { get; set; } = true;

        [FirestoreProperty("createdat")]
        public Timestamp? CreatedAt { get; set; }

        [FirestoreProperty("lastlogin")]
        public Timestamp? LastLogin { get; set; }

        [FirestoreProperty("updatedat")]
        public Timestamp? UpdatedAt { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Helper methods to convert Firestore Timestamp to DateTime
        public DateTime? GetCreatedAt()
        {
            return CreatedAt?.ToDateTime();
        }

        public DateTime? GetLastLogin()
        {
            return LastLogin?.ToDateTime();
        }

        public DateTime? GetUpdatedAt()
        {
            return UpdatedAt?.ToDateTime();
        }

        public void SetCreatedAt(DateTime dateTime)
        {
            CreatedAt = Timestamp.FromDateTime(dateTime.ToUniversalTime());
        }

        public void SetLastLogin(DateTime dateTime)
        {
            LastLogin = Timestamp.FromDateTime(dateTime.ToUniversalTime());
        }

        public void SetUpdatedAt(DateTime dateTime)
        {
            UpdatedAt = Timestamp.FromDateTime(dateTime.ToUniversalTime());
        }

        // Helper to get display name
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(FullName))
                return FullName;

            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                return $"{FirstName} {LastName}";

            if (!string.IsNullOrEmpty(FirstName))
                return FirstName;

            if (!string.IsNullOrEmpty(Email))
                return Email.Split('@')[0];

            return "Admin User";
        }

        // Helper to get initials for avatar
        public string GetInitials()
        {
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                return $"{FirstName[0]}{LastName[0]}".ToUpper();

            if (!string.IsNullOrEmpty(FirstName))
                return FirstName[0].ToString().ToUpper();

            if (!string.IsNullOrEmpty(LastName))
                return LastName[0].ToString().ToUpper();

            if (!string.IsNullOrEmpty(Email))
                return Email[0].ToString().ToUpper();

            return "AU";
        }
    }
}