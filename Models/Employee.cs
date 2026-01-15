//using Google.Cloud.Firestore;
//using System.ComponentModel.DataAnnotations;

//namespace WebApplication2.Models
//{
//    [FirestoreData]
//    public class Employee
//    {
//        [FirestoreProperty]
//        public string Id { get; set; }

//        [FirestoreProperty("userId")]
//        public string UserId { get; set; }

//        [FirestoreProperty("firstName")]
//        public string FirstName { get; set; }

//        [FirestoreProperty("lastName")]
//        public string LastName { get; set; }

//        [FirestoreProperty("email")]
//        public string Email { get; set; }

//        [FirestoreProperty("phone")]
//        public string Phone { get; set; }

//        [FirestoreProperty("position")]
//        public string Position { get; set; }

//        [FirestoreProperty("department")]
//        public string Department { get; set; }

//        [FirestoreProperty("profileImageUrl")]
//        public string ProfileImageUrl { get; set; }

//        [FirestoreProperty("employeeId")]
//        public string EmployeeId { get; set; }

//        [FirestoreProperty("isActive")]
//        public bool IsActive { get; set; } = true;

//        // Handle both Timestamp and long milliseconds
//        [FirestoreProperty("hireDate")]
//        public object HireDate { get; set; }

//        [FirestoreProperty("createdAt")]
//        public object CreatedAt { get; set; }

//        // Navigation properties
//        public List<Skill> Skills { get; set; } = new List<Skill>();
//        public List<Qualification> Qualifications { get; set; } = new List<Qualification>();
//        public List<Training> Trainings { get; set; } = new List<Training>();

//        // Computed properties
//        public double AverageSkillLevel { get; set; }
//        public int TotalSkillsGap { get; set; }
//        public int CriticalGapsCount { get; set; }

//        // Helper properties for date handling - SINGLE VERSION (removed duplicates)
//        public DateTime HireDateDateTime
//        {
//            get
//            {
//                try
//                {
//                    if (HireDate is Timestamp timestamp)
//                        return timestamp.ToDateTime();
//                    else if (HireDate is long milliseconds)
//                        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime;
//                    else if (HireDate is int intMilliseconds)
//                        return DateTimeOffset.FromUnixTimeMilliseconds(intMilliseconds).DateTime;
//                    else if (HireDate is DateTime dateTime)
//                        return dateTime;
//                    else if (HireDate is string dateString && DateTime.TryParse(dateString, out var parsedDate))
//                        return parsedDate;
//                    else
//                    {
//                        Console.WriteLine($"⚠️ HireDate is null or invalid, using current date");
//                        return DateTime.Now;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"❌ Error parsing HireDate: {ex.Message}");
//                    return DateTime.Now;
//                }
//            }
//        }

//        public DateTime CreatedAtDateTime
//        {
//            get
//            {
//                try
//                {
//                    if (CreatedAt is Timestamp timestamp)
//                        return timestamp.ToDateTime();
//                    else if (CreatedAt is long milliseconds)
//                        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime;
//                    else if (CreatedAt is int intMilliseconds)
//                        return DateTimeOffset.FromUnixTimeMilliseconds(intMilliseconds).DateTime;
//                    else if (CreatedAt is DateTime dateTime)
//                        return dateTime;
//                    else if (CreatedAt is string dateString && DateTime.TryParse(dateString, out var parsedDate))
//                        return parsedDate;
//                    else
//                    {
//                        Console.WriteLine($"⚠️ CreatedAt is null or invalid, using current date");
//                        return DateTime.Now;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"❌ Error parsing CreatedAt: {ex.Message}");
//                    return DateTime.Now;
//                }
//            }
//        }

//        // Helper methods to set dates properly
//        public void SetHireDate(DateTime date)
//        {
//            HireDate = new DateTimeOffset(date).ToUnixTimeMilliseconds();
//        }

//        public void SetCreatedAt(DateTime date)
//        {
//            CreatedAt = new DateTimeOffset(date).ToUnixTimeMilliseconds();
//        }

//        // Method to calculate metrics
//        public void CalculateMetrics()
//        {
//            try
//            {
//                if (Skills?.Any() == true)
//                {
//                    var validSkills = Skills
//                        .Where(s => s != null && !string.IsNullOrEmpty(s.Level))
//                        .ToList();

//                    if (validSkills.Any())
//                    {
//                        var skillLevels = validSkills.Select(s => ParseSkillLevel(s.Level)).ToList();
//                        AverageSkillLevel = skillLevels.Average();
//                        TotalSkillsGap = skillLevels.Count(level => level < 3);
//                        CriticalGapsCount = skillLevels.Count(level => level < 2);
//                    }
//                    else
//                    {
//                        AverageSkillLevel = 0;
//                        TotalSkillsGap = 0;
//                        CriticalGapsCount = 0;
//                    }
//                }
//                else
//                {
//                    AverageSkillLevel = 0;
//                    TotalSkillsGap = 0;
//                    CriticalGapsCount = 0;
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"❌ Error calculating employee metrics: {ex.Message}");
//                AverageSkillLevel = 0;
//                TotalSkillsGap = 0;
//                CriticalGapsCount = 0;
//            }
//        }

//        private int ParseSkillLevel(string level)
//        {
//            if (string.IsNullOrEmpty(level)) return 0;

//            if (int.TryParse(level, out int result))
//                return result;

//            return level?.ToLower() switch
//            {
//                "beginner" => 1,
//                "basic" => 2,
//                "intermediate" => 3,
//                "advanced" => 4,
//                "expert" => 5,
//                _ => 0
//            };
//        }

//        public string FullName => $"{FirstName} {LastName}";
//    }
//}
using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    [FirestoreData]
    public class Employee
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty("userId")]
        public string UserId { get; set; }

        [FirestoreProperty("firstName")]
        public string FirstName { get; set; }

        [FirestoreProperty("lastName")]
        public string LastName { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("phone")]
        public string Phone { get; set; }

        [FirestoreProperty("position")]
        public string Position { get; set; }

        [FirestoreProperty("department")]
        public string Department { get; set; }

        [FirestoreProperty("profileImageUrl")]
        public string ProfileImageUrl { get; set; }

        [FirestoreProperty("employeeId")]
        public string EmployeeId { get; set; }

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;

        // FIXED: Use long for milliseconds to match Flutter
        [FirestoreProperty("hireDate")]
        public long HireDate { get; set; }

        [FirestoreProperty("createdAt")]
        public long CreatedAt { get; set; }

        // Navigation properties
        public List<Skill> Skills { get; set; } = new List<Skill>();
        public List<Qualification> Qualifications { get; set; } = new List<Qualification>();
        public List<Training> Trainings { get; set; } = new List<Training>();

        // Computed properties
        public double AverageSkillLevel { get; set; }
        public int TotalSkillsGap { get; set; }
        public int CriticalGapsCount { get; set; }

        // FIXED: Simplified date properties
        public DateTime HireDateDateTime
        {
            get
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(HireDate).DateTime;
                }
                catch
                {
                    return DateTime.Now;
                }
            }
        }

        public DateTime CreatedAtDateTime
        {
            get
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt).DateTime;
                }
                catch
                {
                    return DateTime.Now;
                }
            }
        }

        // FIXED: Proper date setting methods
        public void SetHireDate(DateTime date)
        {
            HireDate = new DateTimeOffset(date).ToUnixTimeMilliseconds();
        }

        public void SetCreatedAt(DateTime date)
        {
            CreatedAt = new DateTimeOffset(date).ToUnixTimeMilliseconds();
        }

        // Helper properties
        public string FullName => $"{FirstName} {LastName}";
        public string DisplayDepartment => string.IsNullOrEmpty(Department) ? "Not Assigned" : Department;
        public string DisplayPosition => string.IsNullOrEmpty(Position) ? "Not Assigned" : Position;

        // FIXED: Improved metrics calculation
        public void CalculateMetrics()
        {
            try
            {
                if (Skills?.Any() == true)
                {
                    var validSkills = Skills
                        .Where(s => s != null && !string.IsNullOrEmpty(s.Level))
                        .ToList();

                    if (validSkills.Any())
                    {
                        var skillLevels = validSkills.Select(s => ParseSkillLevel(s.Level)).ToList();
                        AverageSkillLevel = Math.Round(skillLevels.Average(), 1);
                        TotalSkillsGap = skillLevels.Count(level => level < 3);
                        CriticalGapsCount = skillLevels.Count(level => level < 2);
                    }
                    else
                    {
                        AverageSkillLevel = 0;
                        TotalSkillsGap = 0;
                        CriticalGapsCount = 0;
                    }
                }
                else
                {
                    AverageSkillLevel = 0;
                    TotalSkillsGap = 0;
                    CriticalGapsCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calculating employee metrics: {ex.Message}");
                AverageSkillLevel = 0;
                TotalSkillsGap = 0;
                CriticalGapsCount = 0;
            }
        }

        private int ParseSkillLevel(string level)
        {
            if (string.IsNullOrEmpty(level)) return 0;

            if (int.TryParse(level, out int result))
                return Math.Clamp(result, 1, 5);

            return level.ToLower() switch
            {
                "beginner" or "1" => 1,
                "basic" or "2" => 2,
                "intermediate" or "3" => 3,
                "advanced" or "4" => 4,
                "expert" or "5" => 5,
                _ => 0
            };
        }

        // FIXED: Validation method
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(FirstName) &&
                   !string.IsNullOrEmpty(LastName) &&
                   !string.IsNullOrEmpty(Email) &&
                   !string.IsNullOrEmpty(Department);
        }
    }
}