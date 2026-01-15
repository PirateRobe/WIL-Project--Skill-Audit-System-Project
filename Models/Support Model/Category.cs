namespace WebApplication2.Models.Support_Model
{
    public class Category
    {
        public string CategoryName { get; set; } = string.Empty;
        public int SkillCount { get; set; }
        public double AverageGap { get; set; }
        public double AverageCurrentLevel { get; set; }
        public double AverageRequiredLevel { get; set; }
        public int SkillsWithGaps { get; set; }
        public int CriticalSkillsCount { get; set; }
    }
}
