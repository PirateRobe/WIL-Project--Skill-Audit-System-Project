using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.ViewModel
{
    // Create this new class in your Models folder
    public class TrainingViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        [Display(Name = "Employee")]
        public string? EmployeeId { get; set; }

        [Required(ErrorMessage = "Training title is required")]
        [Display(Name = "Training Title")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Provider is required")]
        public string? Provider { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; } = "Pending";

        [Display(Name = "Certificate URL")]
        public string? CertificateUrl { get; set; }

        [Display(Name = "Certificate PDF URL")]
        public string? CertificatePdfUrl { get; set; }

        [Display(Name = "Certificate File Name")]
        public string? CertificateFileName { get; set; }

        [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100")]
        public double Progress { get; set; }

        [Display(Name = "Training Program")]
        public string? TrainingProgramId { get; set; }

        [Display(Name = "Assigned By")]
        public string? AssignedBy { get; set; }

        [Display(Name = "Assigned Reason")]
        public string? AssignedReason { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

        [Display(Name = "Assigned Date")]
        [DataType(DataType.Date)]
        public DateTime? AssignedDate { get; set; }

        [Display(Name = "Completed Date")]
        [DataType(DataType.Date)]
        public DateTime? CompletedDate { get; set; }
       
          

            // Dropdown lists
            public List<Employee> Employees { get; set; } = new List<Employee>();
            public List<TrainingProgram> TrainingPrograms { get; set; } = new List<TrainingProgram>();
      

        // Navigation properties for dropdowns
      
      

    }
}
