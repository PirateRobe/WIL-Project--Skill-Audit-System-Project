using Google.Cloud.Firestore;

namespace WebApplication2.Models
{
    [FirestoreData]
    public class Qualification
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty("employeeid")]
        public string EmployeeId { get; set; }

        [FirestoreProperty("institution")]
        public string Institution { get; set; }

        [FirestoreProperty("degree")]
        public string Degree { get; set; }

        [FirestoreProperty("fieldofstudy")]
        public string FieldOfStudy { get; set; }

        [FirestoreProperty("yearcompleted")]
        public int YearCompleted { get; set; }

        [FirestoreProperty("grade")]
        public string Grade { get; set; }

        [FirestoreProperty("createdat")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty]
        public DateTime? ExpiryDate { get; set; }

        // NEW: Certificate and transcript properties to match Flutter app
        [FirestoreProperty("certificatePdfUrl")]
        public string CertificatePdfUrl { get; set; }

        [FirestoreProperty("certificateFileName")]
        public string CertificateFileName { get; set; }

        [FirestoreProperty("transcriptPdfUrl")]
        public string TranscriptPdfUrl { get; set; }

        [FirestoreProperty("transcriptFileName")]
        public string TranscriptFileName { get; set; }

        // Helper properties for easier access
        public bool HasCertificate => !string.IsNullOrEmpty(CertificatePdfUrl);
        public bool HasTranscript => !string.IsNullOrEmpty(TranscriptPdfUrl);
        public bool HasDocuments => HasCertificate || HasTranscript;

        // Navigation property
        public Employee Employee { get; set; }

        // FIXED: Helper method to get created date as DateTime
        public DateTime GetCreatedAt()
        {
            // Check if CreatedAt is default/uninitialized
            if (CreatedAt == default(Timestamp) || CreatedAt.ToDateTime().Year < 2000)
            {
                return DateTime.UtcNow;
            }
            return CreatedAt.ToDateTime();
        }

        // FIXED: Helper method to set dates properly
        public void SetCreatedAt(DateTime date)
        {
            CreatedAt = Timestamp.FromDateTime(date.ToUniversalTime());
        }

        // FIXED: Helper method to check if qualification is expired
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;

        // FIXED: Helper method to get years since completion
        public int YearsSinceCompletion
        {
            get
            {
                var currentYear = DateTime.Now.Year;
                return currentYear - YearCompleted;
            }
        }

        // Display properties
        public string DisplayName => $"{Degree} in {FieldOfStudy}";
        public string DisplayInstitution => Institution;
        public string DisplayYear => YearCompleted.ToString();

        // Document status
        public string DocumentStatus
        {
            get
            {
                if (HasCertificate && HasTranscript) return "Complete";
                if (HasCertificate) return "Certificate Only";
                if (HasTranscript) return "Transcript Only";
                return "No Documents";
            }
        }

        // Validation method
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Degree) &&
                   !string.IsNullOrEmpty(Institution) &&
                   YearCompleted > 1900 &&
                   YearCompleted <= DateTime.Now.Year;
        }

        // NEW: Check if CreatedAt is properly set
        public bool HasValidCreatedAt => CreatedAt != default(Timestamp) && CreatedAt.ToDateTime().Year >= 2000;
    }
}