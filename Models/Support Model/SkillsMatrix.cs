using Google.Cloud.Firestore;

namespace WebApplication2.Models.Support_Model
{
    public class SkillsMatrix
    {
        public string Id { get; set; } = string.Empty;
       public string UserId { get; set; } = string.Empty;
        
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public Employee Employee { get; set; }
        public string Email { get; set; }

        // Core skills from your matrix
        public string FinancialModeling { get; set; }
        public string DataAnalysis { get; set; }
        public string RiskAssessment { get; set; }
        public string RegulatoryCompliance { get; set; }
        public string ProjectManagement { get; set; }
        public string StatisticalAnalysis { get; set; }
        public string ReportWriting { get; set; }

        public DateTime LastUpdated { get; set; }

        // Helper methods to get numeric values
        public int FinancialModelingLevel => GetSkillLevel(FinancialModeling);
        public int DataAnalysisLevel => GetSkillLevel(DataAnalysis);
        public int RiskAssessmentLevel => GetSkillLevel(RiskAssessment);
        public int RegulatoryComplianceLevel => GetSkillLevel(RegulatoryCompliance);
        public int ProjectManagementLevel => GetSkillLevel(ProjectManagement);
        public int StatisticalAnalysisLevel => GetSkillLevel(StatisticalAnalysis);
        public int ReportWritingLevel => GetSkillLevel(ReportWriting);

        private int GetSkillLevel(string skillLevel)
        {
            return skillLevel?.ToLower() switch
            {
                "beginner" or "1" => 1,
                "basic" or "2" => 2,
                "intermediate" or "3" => 3,
                "advanced" or "4" => 4,
                "expert" or "master" or "5" => 5,
                _ => 0
            };
        }
    }
}
