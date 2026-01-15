using WebApplication2.Models.Support_Model;

namespace WebApplication2.Models.ViewModel
{
    public class DashboardViewModel
    {

        // Basic Statistics
        public int TotalEmployees { get; set; }
        public int TotalTrainings { get; set; }
        public int TotalTrainingPrograms { get; set; }
        public int TotalSkillsTracked { get; set; }

        // Training Statistics
        public int CompletedTrainings { get; set; }
        public int InProgressTrainings { get; set; }
        public int PendingTrainings { get; set; }
        public int OverdueTrainings { get; set; }
        public int ActiveTrainings { get; set; } // Add this
        public double TrainingCompletionRate { get; set; }
        public string OverallStatus { get; set; } // Add this

        // Skills Statistics
        public double AverageSkillLevel { get; set; }
        public int CriticalSkillsGap { get; set; }
        public int SkillsRequiringAttention { get; set; }

        // Collections
        public List<Department> DepartmentStats { get; set; } = new List<Department>();
        public List<Department> TopDepartments { get; set; } = new List<Department>();
        public List<Training> RecentTrainings { get; set; } = new List<Training>();
        public List<Training> UpcomingDeadlines { get; set; } = new List<Training>();
        public List<Employee> EmployeesNeedingTraining { get; set; } = new List<Employee>();
        public int TotalEmployeesNeedingTraining { get; set; }
        public List<TrainingProgram> TopPerformingPrograms { get; set; } = new List<TrainingProgram>();
        public List<Skill> TopSkillGaps { get; set; } = new List<Skill>();
        public List<Category> SkillsByCategory { get; set; } = new List<Category>();

        // Quick Actions
        public int PendingApprovals { get; set; }
        public int CertificatesPending { get; set; }
    }
}
