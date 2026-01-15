namespace WebApplication2.Models.Support_Model
{
    public class TrainingRecommendation
    {
        public string SkillName { get; set; }
        public int CurrentLevel { get; set; }
        public int RequiredLevel { get; set; }
        public int Gap { get; set; }
        public string RecommendedTraining { get; set; }
        public string TrainingProgramId { get; set; }
        public string Priority { get; set; } // High, Medium, Low
    }
}
