using WebApplication2.Services;

namespace WebApplication2.Models.Support_Model
{
    public class EmployeeTrainingAnalysis
    {
        public Employee Employee { get; set; }
        public List<Skill> Skills { get; set; } = new List<Skill>();
        public List<TrainingAssignment> Trainings { get; set; } = new List<TrainingAssignment>();
        public List<Skill> SkillGaps { get; set; } = new List<Skill>();
        public bool NeedsTraining { get; set; }
        public int CompletedTrainings { get; set; }
    }
}
