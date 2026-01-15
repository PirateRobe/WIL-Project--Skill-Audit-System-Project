namespace WebApplication2.Models.Support_Model
{
    public class SkillGapAnalysis
    {
        public string SkillName { get; set; }
        public string Category { get; set; }
        public int EmployeesWithSkill { get; set; }
        public int EmployeesNeedingTraining { get; set; }
        public double AverageGap { get; set; }
        public double Criticality { get; set; }
    }
}
