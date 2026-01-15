using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    [FirestoreData]
    public class ReportRequest
    {
        [FirestoreProperty]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [FirestoreProperty]
        [Required(ErrorMessage = "Report type is required")]
        public string ReportType { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public DateTime? StartDate { get; set; }

        [FirestoreProperty]
        public DateTime? EndDate { get; set; }

        [FirestoreProperty]
        public List<string> Departments { get; set; } = new List<string>();

        [FirestoreProperty]
        public List<string> Statuses { get; set; } = new List<string>();

        [FirestoreProperty]
        [Required(ErrorMessage = "Format is required")]
        public string Format { get; set; } = "PDF";

        [FirestoreProperty]
        public string CreatedBy { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [FirestoreProperty]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        // Remove these since we're not storing files anymore
        // public string FileUrl { get; set; }
        // public string FileName { get; set; }
        // public long FileSize { get; set; }

        [FirestoreProperty]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    // Remove GeneratedReport class since we're not storing reports
    // Keep ReportStatus enum if needed elsewhere
    public enum ReportStatus
    {
        Pending,
        Generating,
        Completed,
        Failed,
        Archived
    }
}