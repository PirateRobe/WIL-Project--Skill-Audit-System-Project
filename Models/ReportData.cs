using WebApplication2.Models.Support_Model;

namespace WebApplication2.Models
{
    public class ReportData
    {
        public int TotalEmployees { get; set; }
        public int TotalTrainingPrograms { get; set; }
        public int TotalAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int InProgressAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public double CompletionRate { get; set; }
        public List<DepartmentStats> DepartmentStatistics { get; set; } = new List<DepartmentStats>();
        public List<TrainingProgramStats> ProgramStatistics { get; set; } = new List<TrainingProgramStats>();
        public List<EmployeeTrainingStats> EmployeeStatistics { get; set; } = new List<EmployeeTrainingStats>();
        public List<SkillGapAnalysis> SkillGapAnalysis { get; set; } = new List<SkillGapAnalysis>();
        public List<Employee> EmployeeSummaries { get; set; } = new List<Employee>();
    }
}
