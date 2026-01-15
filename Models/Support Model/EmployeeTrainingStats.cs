namespace WebApplication2.Models.Support_Model
{
    public class EmployeeTrainingStats
    {
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public int TotalAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int InProgressAssignments { get; set; }
        public double CompletionRate { get; set; }
        public double AverageProgress { get; set; }
        public int SkillCount { get; set; }
        public int SkillGaps { get; set; }
    }
}
