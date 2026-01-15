// Models/ViewModel/TrainingDetailViewModel.cs
using System.Collections.Generic;
using WebApplication2.Models.Support_Model;

namespace WebApplication2.Models.ViewModel
{
    public class TrainingDetailViewModel
    {
        public Training Training { get; set; }
        public Employee Employee { get; set; }
        public List<Skill> CoveredSkills { get; set; } = new List<Skill>();
        public List<Skill> EmployeeSkills { get; set; } = new List<Skill>();
  
        public List<TrainingCertificate> Certificates { get; set; } = new List<TrainingCertificate>();
     
        public List<Qualification> RelatedQualifications { get; set; } = new List<Qualification>();
       // public double SkillImprovement => Training.SkillGapBefore - Training.SkillGapAfter;
        public bool HasCertificate => !string.IsNullOrEmpty(Training.CertificateUrl) ||
                                    !string.IsNullOrEmpty(Training.CertificatePdfUrl);

        // Training metrics
        public int DaysUntilStart
        {
            get
            {
                var today = DateTime.Today;
                var startDate = Training.StartDateDateTime();
                return (startDate - today).Days;
            }
        }

        public int DurationDays
        {
            get
            {
                var start = Training.StartDateDateTime();
                var end = Training.EndDateDateTime();
                return (end - start).Days + 1;
            }
        }

        public bool IsOverdue => Training.EndDateDateTime() < DateTime.Today && !Training.IsCompleted();
    }
}