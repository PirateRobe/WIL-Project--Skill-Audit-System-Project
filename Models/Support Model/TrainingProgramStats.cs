namespace WebApplication2.Models.Support_Model
{
    public class TrainingProgramStats
    {
        public string ProgramTitle { get; set; }
        public string Provider { get; set; }
        public int AssignmentCount { get; set; }
        public int CompletedCount { get; set; }
        public double CompletionRate { get; set; }
        public double AverageProgress { get; set; }
        public List<string> CoveredSkills { get; set; } = new List<string>();
    }
}
