using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.Support_Model
{
    [FirestoreData]
    public class TrainingAssignment
    {
        [FirestoreProperty]
        public string Id { get; set; }=Guid.NewGuid().ToString();
        [FirestoreProperty]
        [Required]
        public string TrainingProgramId { get; set; }

        [FirestoreProperty]
        [Required]
        public string EmployeeId { get; set; }

        [FirestoreProperty]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, InProgress, Completed

        [FirestoreProperty]
        public int Progress { get; set; } = 0;

        [FirestoreProperty]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [FirestoreProperty]
        public DateTime DueDate { get; set; }

        [FirestoreProperty]
        public DateTime? AcceptedDate { get; set; }

        [FirestoreProperty]
        public DateTime? CompletedDate { get; set; }

        [FirestoreProperty]
        public string AssignedReason { get; set; }

        [FirestoreProperty]
        public string CertificateFileName { get; set; }

        [FirestoreProperty]
        public string CertificateUrl { get; set; }

        [FirestoreProperty]
        public double SkillGapBefore { get; set; }

        [FirestoreProperty]
        public double SkillGapAfter { get; set; }

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; }

        // Navigation properties
        public TrainingProgram TrainingProgram { get; set; }
        public Employee Employee { get; set; }
    }
}
