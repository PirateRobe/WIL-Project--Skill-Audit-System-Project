namespace WebApplication2.Models.Support_Model
{
    public class DepartmentStats
    {
        public string DepartmentName { get; set; }
        public int EmployeeCount { get; set; }
        public int AssignmentCount { get; set; }
        public int CompletedCount { get; set; }
        public double CompletionRate { get; set; }
        public double AverageSkillLevel { get; set; }
    }
}
