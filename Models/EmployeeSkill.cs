using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class EmployeeSkill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int SkillId { get; set; }
        public string SkillName {  get; set; }
        public int CurrentLevel {  get; set; }
        public int RequiredLevel { get; set; }
        public bool IsCritical { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required]
        [Range(1, 5)]
        public int ProficiencyLevel { get; set; } // 1-5 scale

        [Range(0, 5)]
        public double Gap { get; set; } // Difference between expected and actual

        public DateTime LastAssessed { get; set; } = DateTime.Now;
        public string AssessedBy { get; set; }
        public string Notes { get; set; }

        public bool IsVerified { get; set; }
        public DateTime? VerificationDate { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [ForeignKey("SkillId")]
        public virtual Skill Skill { get; set; }

        // Computed property for skill level text
        public string ProficiencyLevelText => ProficiencyLevel switch
        {
            1 => "Beginner",
            2 => "Basic",
            3 => "Intermediate",
            4 => "Advanced",
            5 => "Expert",
            _ => "Not Assessed"
        };
    }
}
