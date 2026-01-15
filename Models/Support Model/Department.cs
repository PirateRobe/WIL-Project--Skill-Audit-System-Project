namespace WebApplication2.Models.Support_Model
{
    public class Department
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TotalSkills {  get; set; }
        public int SkillsWithGaps {  get; set; }
        public int EmployeeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public double AverageSkillLevel { get; set; }
        public int TotalSkillsGap { get; set; }
        public int CriticalGapsCount { get; set; }
        public int CriticalGaps { get; set; }
        public int RecommendedTrainings { get; set; }
        public double AverageSkillGap { get; set; }
        public string TopSkillNeed { get; set; } = string.Empty;
        public Dictionary<string, int> MostNeededSkills { get; set; } = new();
        // Navigation properties
        public virtual ICollection<Employee> Employees { get; set; }
      public double AverageGap {  get; set; }
      
        public int AssignmentCount { get; set; }
        public int CompletedCount { get; set; }
        public double CompletionRate { get; set; }
    }
}
