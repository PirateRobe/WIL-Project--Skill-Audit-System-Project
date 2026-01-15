using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebApplication2.Models.Support_Model;

namespace WebApplication2.Models
{
    [FirestoreData]
    public class TrainingProgram
    {
        [FirestoreDocumentId]
        public string Id { get; set; }= Guid.NewGuid().ToString();

        [FirestoreProperty]
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "Provider is required")]
        [StringLength(100, ErrorMessage = "Provider cannot exceed 100 characters")]
        public string Provider { get; set; }

        [FirestoreProperty]
        public string Category { get; set; }

        [FirestoreProperty]
        [Range(1, 500, ErrorMessage = "Duration must be between 1 and 500 hours")]
        public int Duration { get; set; } = 40;

        [FirestoreProperty]
        public string DifficultyLevel { get; set; } = "Intermediate";

        [FirestoreProperty]
        public string Format { get; set; } = "Online";

        [FirestoreProperty]
        public string Prerequisites { get; set; }

        [FirestoreProperty]
        public List<string> CoveredSkills { get; set; } = new List<string>();

        [FirestoreProperty]
        public bool IsActive { get; set; } = true;

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; }

        // Navigation properties (not stored in Firestore)
        public List<TrainingAssignment> Assignments { get; set; } = new List<TrainingAssignment>();
        public int AssignmentCount { get; set; }
        public int CompletedCount { get; set; }

        // Constructor to ensure list is initialized
        public TrainingProgram()
        {
            CoveredSkills = new List<string>();
        }
    }
}