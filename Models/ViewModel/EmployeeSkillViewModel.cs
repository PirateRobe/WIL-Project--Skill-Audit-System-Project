using WebApplication2.Models.Support_Model;

namespace WebApplication2.Models.ViewModel
{
    public class EmployeeSkillViewModel
    {
        public int TotalSkillsTracked {  get; set; }
        public int CriticalSkillsGap { get; set;}
        public double AverageSkillLevel {  get; set;}
        public List<Skill> SkillsGapAnalysis {  get; set;}
        public List<Department> DepartmentSkills { get; set;}
        public List<SkillsMatrix> SkillsMatrix {  get; set;}
        public List<Department> DepartmentStats {  get; set;}
        public List<Category> CategoryStats { get; set;}
        public int TotalEmployees {  get; set;}
        public List<Employee> Employees { get; set;}
        public Employee EmployeeDetails { get; set;}
        public Employee Employee { get; set; }
        public List<Skill> Skills { get; set; }
        public List<Qualification> Qualifications { get; set; }
        public List<Training> Trainings { get; set; }
    }
}
