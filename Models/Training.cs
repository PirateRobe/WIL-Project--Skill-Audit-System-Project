// In your Training.cs model, ensure it matches Flutter structure
using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    [FirestoreData]
    public class Training
    {
        [FirestoreProperty]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [FirestoreProperty]
        [Required(ErrorMessage = "Employee ID is required")]
        public string EmployeeId { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "Provider is required")]
        public string Provider { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public long StartDate { get; set; } // Change to long for milliseconds

        [FirestoreProperty]
        public long EndDate { get; set; } // Change to long for milliseconds

        [FirestoreProperty]
        public string Status { get; set; } = "Pending";

        [FirestoreProperty]
        public string CertificateUrl { get; set; } = "";

        [FirestoreProperty]
        public long CreatedAt { get; set; } // Change to long for milliseconds

        [FirestoreProperty]
        public string CertificatePdfUrl { get; set; }

        [FirestoreProperty]
        public string CertificateFileName { get; set; }

        [FirestoreProperty]
        public double Progress { get; set; } = 0.0;

        [FirestoreProperty]
        public string TrainingProgramId { get; set; }

        [FirestoreProperty]
        public string AssignedBy { get; set; } = "admin"; // 'admin' or 'employee'

        [FirestoreProperty]
        public string AssignedReason { get; set; }

        [FirestoreProperty]
        public long? AssignedDate { get; set; } // Change to long for milliseconds

        [FirestoreProperty]
        public long? CompletedDate { get; set; } // Change to long for milliseconds
     
           

            // Navigation properties
            public Employee Employee { get; set; }
            public TrainingProgram TrainingProgram { get; set; }

            public void SetCreatedAt(DateTime dateTime)
            {
                CreatedAt = ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
            }

            public void SetAssignedDate(DateTime dateTime)
            {
                AssignedDate = ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
            }
        

        // Helper methods to convert between DateTime and milliseconds
        public DateTime GetStartDate() => DateTimeOffset.FromUnixTimeMilliseconds(StartDate).DateTime;
        public void SetStartDate(DateTime date) => StartDate = new DateTimeOffset(date).ToUnixTimeMilliseconds();

        public DateTime GetEndDate() => DateTimeOffset.FromUnixTimeMilliseconds(EndDate).DateTime;
        public void SetEndDate(DateTime date) => EndDate = new DateTimeOffset(date).ToUnixTimeMilliseconds();

        public DateTime GetCreatedAt() => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt).DateTime;
     

        public DateTime? GetAssignedDate() => AssignedDate.HasValue ?
            DateTimeOffset.FromUnixTimeMilliseconds(AssignedDate.Value).DateTime : null;
        public void SetAssignedDate(DateTime? date) => AssignedDate = date.HasValue ?
            new DateTimeOffset(date.Value).ToUnixTimeMilliseconds() : null;

        public DateTime? GetCompletedDate() => CompletedDate.HasValue ?
            DateTimeOffset.FromUnixTimeMilliseconds(CompletedDate.Value).DateTime : null;
        public void SetCompletedDate(DateTime? date) => CompletedDate = date.HasValue ?
            new DateTimeOffset(date.Value).ToUnixTimeMilliseconds() : null;
        // Add to Training.cs model
        public bool IsCompleted()
        {
            return Status?.ToLower() == "completed";
        }

        // Add these extension methods for date handling
        public DateTime StartDateDateTime()
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(StartDate).DateTime;
        }

        public DateTime EndDateDateTime()
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(EndDate).DateTime;
        }

        public DateTime CreatedAtDateTime()
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt).DateTime;
        }
        // Add these methods to the Training class
        public bool IsInProgress()
        {
            return Status?.ToLower() == "in progress";
        }

        public bool IsAssigned()
        {
            return Status?.ToLower() == "assigned";
        }

        public bool IsPending()
        {
            return Status?.ToLower() == "pending";
        }
        // In Models/Training.cs
      
            public DateTime? CertificateIssueDate { get; set; }

        // Date properties


        // Progress tracking
        public string CancellationReason { get; set; }
        public string CancelledBy { get; set; }
        public long? CancelledDate { get; set; }

        // Helper methods for dates
        public void SetCancelledDate(DateTime date)
        {
            CancelledDate = new DateTimeOffset(date).ToUnixTimeMilliseconds();
        }

        public DateTime? GetCancelledDate()
        {
            return CancelledDate.HasValue && CancelledDate.Value > 0 ?
                DateTimeOffset.FromUnixTimeMilliseconds(CancelledDate.Value).DateTime :
                null;
        }

      
        public bool IsCancelled() => Status == "Cancelled";
        public bool IsActive() => Status == "In Progress" || Status == "Pending" || Status == "Assigned";
        public bool IsOverdue() => !IsCompleted() && !IsCancelled() && GetEndDate() < DateTime.UtcNow;

    }
}