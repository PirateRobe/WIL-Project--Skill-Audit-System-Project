using Google.Cloud.Firestore;

namespace WebApplication2.Models
{
    [FirestoreData]
    public class Skill
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty("employeeid")]
        public string EmployeeId { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("level")]
        public string Level { get; set; }

        [FirestoreProperty("category")]
        public string Category { get; set; }

        [FirestoreProperty("yearsofexperience")]
        public int YearsOfExperience { get; set; }

        [FirestoreProperty("createdat")]
        public Timestamp CreatedAt { get; set; }
        public int EmployeeCount { get; set; }
        public bool IsCritical { get; set; }
        public double Gap { get; set; }
        public int CurrentLevel { get; set; }
        public double RequiredLevel { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string Department { get; set; }
        public int TotalSkills { get; set; }
        public int SkillsWithGaps { get; set; }
        public int CriticalGaps { get; set; }
        public double AverageGap { get; set; }
        
        public bool HasSkill { get; set; }
        public List<String> TopGapSkills { get; set; } = new();
        // Add this method to the Skill class
        public string LevelDescription()
        {
            if (string.IsNullOrEmpty(Level))
                return "Not Specified";

            if (int.TryParse(Level, out int level))
            {
                return level switch
                {
                    1 => "Beginner",
                    2 => "Basic",
                    3 => "Intermediate",
                    4 => "Advanced",
                    5 => "Expert",
                    _ => $"Level {level}"
                };
            }

            return Level;
        }

    }
}