using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Models;
using WebApplication2.Models.Support_Model;
using WebApplication2.Models.ViewModel;

namespace WebApplication2.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly Models.FirestoreSettings _settings;

        public FirestoreService(IOptions<Models.FirestoreSettings> firestoreSettings)
        {
            _settings = firestoreSettings.Value;
            _firestoreDb = InitializeFirestore();
        }

        private FirestoreDb InitializeFirestore()
        {
            try
            {
                Console.WriteLine($"🔥 Initializing Firestore for project: {_settings.ProjectId}");
                Console.WriteLine($"📁 Using credentials from: {_settings.CredentialsPath}");

                if (!File.Exists(_settings.CredentialsPath))
                {
                    throw new FileNotFoundException($"Firebase credentials file not found at: {_settings.CredentialsPath}");
                }

                var credential = GoogleCredential.FromFile(_settings.CredentialsPath);

                var firestoreDb = new FirestoreDbBuilder
                {
                    ProjectId = _settings.ProjectId,
                    Credential = credential
                }.Build();

                Console.WriteLine("✅ Firestore service initialized successfully!");
                return firestoreDb;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Firestore service initialization failed: {ex.Message}");
                throw new InvalidOperationException($"Failed to initialize Firestore service: {ex.Message}", ex);
            }
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                var employees = new List<Employee>();
                var collection = _firestoreDb.Collection("employees");
                var snapshot = await collection.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    try
                    {
                        var employee = document.ConvertTo<Employee>();
                        employee.Id = document.Id;
                        employees.Add(employee);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting employee document {document.Id}: {ex.Message}");
                    }
                }

                return employees;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting employees: {ex.Message}");
                return new List<Employee>();
            }
        }
        //public async Task<Employee> GetEmployeeWithAllDataAsync(string employeeId)
        //{
        //    try
        //    {
        //        Console.WriteLine($"🔍 Fetching complete employee data for: {employeeId}");

        //        var employee = await GetEmployeeByIdAsync(employeeId);
        //        if (employee == null) return null;

        //        // Load all related data in parallel
        //        var skillsTask = GetEmployeeSkillsAsync(employeeId);
        //        var qualificationsTask = GetEmployeeQualificationsAsync(employeeId);
        //        var trainingsTask = GetEmployeeTrainingsAsync(employeeId);

        //        await Task.WhenAll(skillsTask, qualificationsTask, trainingsTask);

        //        // Assign the data
        //        employee.Skills = skillsTask.Result;
        //        employee.Qualifications = qualificationsTask.Result;
        //        employee.Trainings = trainingsTask.Result;

        //        // Calculate metrics
        //        CalculateEmployeeMetrics(employee);

        //        Console.WriteLine($"✅ Loaded complete data for {employee.FirstName} {employee.LastName}");
        //        return employee;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error in GetEmployeeWithAllDataAsync: {ex.Message}");
        //        return null;
        //    }
        //}
        public async Task<Employee> GetEmployeeWithAllDataAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching complete employee data for: {employeeId}");

                var employee = await GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    Console.WriteLine($"❌ Employee {employeeId} not found");
                    return null;
                }

                // Load all related data in parallel with better error handling
                var skillsTask = GetEmployeeSkillsAsync(employeeId);
                var qualificationsTask = GetEmployeeQualificationsAsync(employeeId);
                var trainingsTask = GetEmployeeTrainingsAsync(employeeId);

                await Task.WhenAll(skillsTask, qualificationsTask, trainingsTask);

                // Assign the data with null checks
                employee.Skills = skillsTask.Result ?? new List<Skill>();
                employee.Qualifications = qualificationsTask.Result ?? new List<Qualification>();
                employee.Trainings = trainingsTask.Result ?? new List<Training>();

                // Calculate metrics
                CalculateEmployeeMetrics(employee);

                Console.WriteLine($"✅ Loaded complete data for {employee.FullName}");
                Console.WriteLine($"   - Skills: {employee.Skills.Count}");
                Console.WriteLine($"   - Qualifications: {employee.Qualifications.Count}");
                Console.WriteLine($"   - Trainings: {employee.Trainings.Count}");
                Console.WriteLine($"   - Avg Skill Level: {employee.AverageSkillLevel:F1}");
                Console.WriteLine($"   - Skills Gap: {employee.TotalSkillsGap}");

                return employee;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetEmployeeWithAllDataAsync: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return null;
            }
        }

        // Add to FirestoreService class
        private Employee ConvertDocumentToEmployee(DocumentSnapshot document)
        {
            try
            {
                var employee = document.ConvertTo<Employee>();
                employee.Id = document.Id;

                // Ensure UserId is set
                if (string.IsNullOrEmpty(employee.UserId))
                {
                    employee.UserId = document.Id;
                }

                return employee;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error converting document {document.Id} to Employee: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Employee>> GetAllEmployeesWithDataAsync()
        {
            try
            {
                var employees = await GetAllEmployeesAsync();
                var employeesWithData = new List<Employee>();

                foreach (var employee in employees)
                {
                    var fullEmployee = await GetEmployeeWithAllDataAsync(employee.Id);
                    if (fullEmployee != null)
                    {
                        employeesWithData.Add(fullEmployee);
                    }
                }

                return employeesWithData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetAllEmployeesWithDataAsync: {ex.Message}");
                return new List<Employee>();
            }
        }
        //private void CalculateEmployeeMetrics(Employee employee)
        //{
        //    if (employee.Skills?.Any() == true)
        //    {
        //        employee.AverageSkillLevel = employee.Skills.Average(s => ParseSkillLevel(s.Level));
        //        employee.TotalSkillsGap = employee.Skills.Count(s => ParseSkillLevel(s.Level) < 3);
        //        employee.CriticalGapsCount = employee.Skills.Count(s => ParseSkillLevel(s.Level) < 2);
        //    }
        //    else
        //    {
        //        employee.AverageSkillLevel = 0;
        //        employee.TotalSkillsGap = 0;
        //        employee.CriticalGapsCount = 0;
        //    }
        //}
        //private void CalculateEmployeeMetrics(Employee employee)
        //{
        //    try
        //    {
        //        if (employee.Skills?.Any() == true)
        //        {
        //            var skillLevels = employee.Skills
        //                .Where(s => s != null && !string.IsNullOrEmpty(s.Level))
        //                .Select(s => ParseSkillLevel(s.Level))
        //                .Where(level => level > 0)
        //                .ToList();

        //            if (skillLevels.Any())
        //            {
        //                employee.AverageSkillLevel = skillLevels.Average();
        //                employee.TotalSkillsGap = skillLevels.Count(level => level < 3);
        //                employee.CriticalGapsCount = skillLevels.Count(level => level < 2);
        //            }
        //            else
        //            {
        //                employee.AverageSkillLevel = 0;
        //                employee.TotalSkillsGap = 0;
        //                employee.CriticalGapsCount = 0;
        //            }
        //        }
        //        else
        //        {
        //            employee.AverageSkillLevel = 0;
        //            employee.TotalSkillsGap = 0;
        //            employee.CriticalGapsCount = 0;
        //        }

        //        Console.WriteLine($"📊 Calculated metrics - Avg: {employee.AverageSkillLevel}, Gap: {employee.TotalSkillsGap}, Critical: {employee.CriticalGapsCount}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error calculating metrics: {ex.Message}");
        //        employee.AverageSkillLevel = 0;
        //        employee.TotalSkillsGap = 0;
        //        employee.CriticalGapsCount = 0;
        //    }
        //}
        private void CalculateEmployeeMetrics(Employee employee)
        {
            try
            {
                if (employee.Skills?.Any() == true)
                {
                    var validSkills = employee.Skills
                        .Where(s => s != null && !string.IsNullOrEmpty(s.Level))
                        .ToList();

                    if (validSkills.Any())
                    {
                        var skillLevels = validSkills.Select(s => ParseSkillLevel(s.Level)).ToList();
                        employee.AverageSkillLevel = skillLevels.Average();
                        employee.TotalSkillsGap = skillLevels.Count(level => level < 3);
                        employee.CriticalGapsCount = skillLevels.Count(level => level < 2);
                    }
                    else
                    {
                        employee.AverageSkillLevel = 0;
                        employee.TotalSkillsGap = 0;
                        employee.CriticalGapsCount = 0;
                    }
                }
                else
                {
                    employee.AverageSkillLevel = 0;
                    employee.TotalSkillsGap = 0;
                    employee.CriticalGapsCount = 0;
                }

                Console.WriteLine($"📊 Calculated metrics - Avg: {employee.AverageSkillLevel}, Gap: {employee.TotalSkillsGap}, Critical: {employee.CriticalGapsCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calculating employee metrics: {ex.Message}");
                employee.AverageSkillLevel = 0;
                employee.TotalSkillsGap = 0;
                employee.CriticalGapsCount = 0;
            }
        }
        // New method to get trainings from Flutter subcollection
        public async Task<List<Training>> GetEmployeeTrainingsFromFlutterSubcollectionAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching trainings from Flutter subcollection for employee: {employeeId}");
                var trainings = new List<Training>();

                // Query the employee's trainings subcollection (where Flutter stores data)
                var employeeTrainingCollection = _firestoreDb
                    .Collection("employees")
                    .Document(employeeId)
                    .Collection("trainings");

                var snapshot = await employeeTrainingCollection.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    try
                    {
                        var training = await ConvertFlutterDocumentToTrainingAsync(document);
                        if (training != null)
                        {
                            trainings.Add(training);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error converting Flutter training document {document.Id}: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ Retrieved {trainings.Count} trainings from Flutter subcollection for employee {employeeId}");
                return trainings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting Flutter trainings: {ex.Message}");
                return new List<Training>();
            }
        }

        // Convert Flutter document format to Training model
        private async Task<Training> ConvertFlutterDocumentToTrainingAsync(DocumentSnapshot document)
        {
            try
            {
                var data = document.ToDictionary();

                var training = new Training
                {
                    Id = document.Id,
                    EmployeeId = data.ContainsKey("employeeId") ? data["employeeId"]?.ToString() : "",
                    Title = data.ContainsKey("title") ? data["title"]?.ToString() : "Unknown Training",
                    Provider = data.ContainsKey("provider") ? data["provider"]?.ToString() : "Unknown Provider",
                    Description = data.ContainsKey("description") ? data["description"]?.ToString() : "",
                    Status = data.ContainsKey("status") ? data["status"]?.ToString() : "Pending",
                    CertificateUrl = data.ContainsKey("certificateUrl") ? data["certificateUrl"]?.ToString() : "",
                    CertificatePdfUrl = data.ContainsKey("certificatePdfUrl") ? data["certificatePdfUrl"]?.ToString() : "",
                    CertificateFileName = data.ContainsKey("certificateFileName") ? data["certificateFileName"]?.ToString() : "",
                    TrainingProgramId = data.ContainsKey("trainingProgramId") ? data["trainingProgramId"]?.ToString() : "",
                    AssignedBy = data.ContainsKey("assignedBy") ? data["assignedBy"]?.ToString() : "employee",
                    AssignedReason = data.ContainsKey("assignedReason") ? data["assignedReason"]?.ToString() : "",
                    Progress = data.ContainsKey("progress") ? Convert.ToDouble(data["progress"]) : 0.0
                };

                // Handle date fields - Flutter uses milliseconds since epoch
                if (data.ContainsKey("startDate") && data["startDate"] != null)
                {
                    if (long.TryParse(data["startDate"].ToString(), out long startDate))
                        training.StartDate = startDate;
                }

                if (data.ContainsKey("endDate") && data["endDate"] != null)
                {
                    if (long.TryParse(data["endDate"].ToString(), out long endDate))
                        training.EndDate = endDate;
                }

                if (data.ContainsKey("createdAt") && data["createdAt"] != null)
                {
                    if (long.TryParse(data["createdAt"].ToString(), out long createdAt))
                        training.CreatedAt = createdAt;
                }

                if (data.ContainsKey("assignedDate") && data["assignedDate"] != null)
                {
                    if (long.TryParse(data["assignedDate"].ToString(), out long assignedDate) && assignedDate > 0)
                        training.AssignedDate = assignedDate;
                }

                if (data.ContainsKey("completedDate") && data["completedDate"] != null)
                {
                    if (long.TryParse(data["completedDate"].ToString(), out long completedDate) && completedDate > 0)
                        training.CompletedDate = completedDate;
                }

                Console.WriteLine($"✅ Converted Flutter training: {training.Title} (ID: {training.Id})");
                return training;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error converting Flutter document {document.Id}: {ex.Message}");
                return null;
            }
        }

        // Get all trainings with certificates from Flutter subcollection
        public async Task<List<Training>> GetFlutterTrainingsWithCertificatesAsync(string employeeId)
        {
            try
            {
                var trainings = await GetEmployeeTrainingsFromFlutterSubcollectionAsync(employeeId);

                // Filter only trainings with certificates
                var certifiedTrainings = trainings
                    .Where(t => !string.IsNullOrEmpty(t.CertificateUrl) || !string.IsNullOrEmpty(t.CertificatePdfUrl))
                    .ToList();

                Console.WriteLine($"✅ Found {certifiedTrainings.Count} Flutter trainings with certificates for employee {employeeId}");
                return certifiedTrainings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting Flutter trainings with certificates: {ex.Message}");
                return new List<Training>();
            }
        }
        //private Employee ConvertToEmployeeWithFallback(DocumentSnapshot document)
        //{
        //    try
        //    {
        //        var data = document.ToDictionary();
        //        var employee = new Employee
        //        {
        //            Id = document.Id,
        //            UserId = GetFieldValue(data, "userId", "UserId"),
        //            FirstName = GetFieldValue(data, "firstName", "FirstName", "first_name", "displayName"),
        //            LastName = GetFieldValue(data, "lastName", "LastName", "last_name"),
        //            Email = GetFieldValue(data, "email", "Email"),
        //            Phone = GetFieldValue(data, "phone", "Phone", "phoneNumber"),
        //            Position = GetFieldValue(data, "position", "Position", "jobTitle"),
        //            Department = GetFieldValue(data, "department", "Department", "dept"),
        //            ProfileImageUrl = GetFieldValue(data, "profileImageUrl", "ProfileImageUrl"),
        //            EmployeeId = GetFieldValue(data, "employeeId", "EmployeeId")
        //        };

        //        // Handle dates properly
        //        employee.HireDate = GetDateField(data, "hireDate", "HireDate", "hire_date");
        //        employee.CreatedAt = GetDateField(data, "createdAt", "CreatedAt", "created_date", "timestamp");

        //        // Ensure UserId is set
        //        if (string.IsNullOrEmpty(employee.UserId))
        //        {
        //            employee.UserId = document.Id;
        //        }

        //        return employee;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Fallback conversion failed for {document.Id}: {ex.Message}");
        //        return null;
        //    }
        //}
        private object GetDateField(Dictionary<string, object> data, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (data.ContainsKey(fieldName) && data[fieldName] != null)
                {
                    try
                    {
                        if (data[fieldName] is Google.Cloud.Firestore.Timestamp timestamp)
                            return timestamp;

                        if (data[fieldName] is long milliseconds)
                            return milliseconds;

                        if (data[fieldName] is int intMilliseconds)
                            return intMilliseconds;

                        if (data[fieldName] is DateTime dateTime)
                            return dateTime;

                        if (DateTime.TryParse(data[fieldName]?.ToString(), out var parsedDate))
                            return parsedDate;

                        // Try to parse as long (milliseconds from Flutter)
                        if (long.TryParse(data[fieldName]?.ToString(), out var longValue))
                            return longValue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error parsing date field {fieldName}: {ex.Message}");
                    }
                }
            }
            return DateTime.Now;
        }
        //public async Task<Employee> GetEmployeeByIdAsync(string employeeId)
        //{
        //    try
        //    {
        //        Console.WriteLine($"🔍 Fetching employee: {employeeId}");
        //        var document = _firestoreDb.Collection("employees").Document(employeeId);
        //        var snapshot = await document.GetSnapshotAsync();

        //        if (snapshot.Exists)
        //        {
        //            try
        //            {
        //                var employee = snapshot.ConvertTo<Employee>();
        //                employee.Id = snapshot.Id;

        //                if (string.IsNullOrEmpty(employee.UserId))
        //                {
        //                    employee.UserId = snapshot.Id;
        //                }

        //                Console.WriteLine($"✅ Found employee: {employee.FirstName} {employee.LastName}");
        //                return employee;
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"❌ Conversion error, using fallback: {ex.Message}");
        //                return ConvertToEmployeeWithFallback(snapshot);
        //            }
        //        }

        //        Console.WriteLine($"❌ Employee document {employeeId} does not exist");
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error in GetEmployeeByIdAsync: {ex.Message}");
        //        return null;
        //    }
        //}
        public async Task<Employee> GetEmployeeByIdAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching employee: {employeeId}");

                if (string.IsNullOrEmpty(employeeId))
                {
                    Console.WriteLine("❌ Employee ID is null or empty");
                    return null;
                }

                var document = _firestoreDb.Collection("employees").Document(employeeId);
                var snapshot = await document.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    try
                    {
                        var employee = snapshot.ConvertTo<Employee>();
                        employee.Id = snapshot.Id;

                        // Ensure UserId is set
                        if (string.IsNullOrEmpty(employee.UserId))
                        {
                            employee.UserId = snapshot.Id;
                        }

                        Console.WriteLine($"✅ Found employee: {employee.FullName}");
                        return employee;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error converting employee document: {ex.Message}");
                        return null;
                    }
                }

                Console.WriteLine($"❌ Employee document {employeeId} does not exist");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetEmployeeByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Skill>> GetEmployeeSkillsAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching skills for employee: {employeeId}");
                var skills = new List<Skill>();
                var collection = _firestoreDb.Collection("employees").Document(employeeId).Collection("skills");
                var snapshot = await collection.GetSnapshotAsync();

                Console.WriteLine($"📊 Found {snapshot.Count} skills for employee {employeeId}");

                foreach (var document in snapshot.Documents)
                {
                    try
                    {
                        var skill = document.ConvertTo<Skill>();
                        skill.Id = document.Id;
                        skill.EmployeeId = employeeId;

                        // Calculate additional properties for the skill
                        skill.CurrentLevel = ParseSkillLevel(skill.Level);
                        skill.RequiredLevel = 3; // Default required level
                        skill.Gap = Math.Max(0, skill.RequiredLevel - skill.CurrentLevel);
                        skill.IsCritical = skill.Gap > 0;

                        skills.Add(skill);
                        Console.WriteLine($"✅ Skill: {skill.Name} (Level: {skill.Level}, Current: {skill.CurrentLevel}, Gap: {skill.Gap})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error converting skill {document.Id}: {ex.Message}");
                    }
                }

                return skills;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetEmployeeSkillsAsync: {ex.Message}");
                return new List<Skill>();
            }
        }
        // Add to FirestoreService.cs
        public async Task<bool> AddQualificationAsync(Qualification qualification)
        {
            try
            {
                var collection = _firestoreDb.Collection("employees")
                                   .Document(qualification.EmployeeId)
                                   .Collection("qualifications");

                var document = await collection.AddAsync(qualification);
                qualification.Id = document.Id;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding qualification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateQualificationAsync(Qualification qualification)
        {
            try
            {
                var document = _firestoreDb.Collection("employees")
                                 .Document(qualification.EmployeeId)
                                 .Collection("qualifications")
                                 .Document(qualification.Id);

                await document.SetAsync(qualification, SetOptions.MergeAll);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating qualification: {ex.Message}");
                return false;
            }
        }
        public async Task<List<Qualification>> GetEmployeeQualificationsAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching qualifications for employee: {employeeId}");
                var qualifications = new List<Qualification>();
                var collection = _firestoreDb.Collection("employees").Document(employeeId).Collection("qualifications");
                var snapshot = await collection.GetSnapshotAsync();

                Console.WriteLine($"📊 Found {snapshot.Count} qualifications for employee {employeeId}");

                foreach (var document in snapshot.Documents)
                {
                    try
                    {
                        var qualification = document.ConvertTo<Qualification>();
                        qualification.Id = document.Id;
                        qualification.EmployeeId = employeeId;

                        // Ensure CreatedAt is valid
                        if (!qualification.HasValidCreatedAt)
                        {
                            Console.WriteLine($"⚠️ Qualification {qualification.Id} has invalid CreatedAt, setting to current time");
                            qualification.SetCreatedAt(DateTime.UtcNow);
                        }

                        qualifications.Add(qualification);
                        Console.WriteLine($"✅ Qualification: {qualification.Degree} - {qualification.Institution}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error converting qualification {document.Id}: {ex.Message}");
                    }
                }

                return qualifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetEmployeeQualificationsAsync: {ex.Message}");
                return new List<Qualification>();
            }
        }

        public async Task<List<Training>> GetEmployeeTrainingsAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching trainings for employee: {employeeId}");
                var trainings = new List<Training>();

                // Try multiple collection strategies
                var collectionStrategies = new[]
                {
            () => _firestoreDb.Collection("trainings").WhereEqualTo("EmployeeId", employeeId),
            () => _firestoreDb.Collection("employees").Document(employeeId).Collection("trainings")
        };

                foreach (var strategy in collectionStrategies)
                {
                    try
                    {
                        var query = strategy();
                        var snapshot = await query.GetSnapshotAsync();

                        Console.WriteLine($"📊 Found {snapshot.Count} trainings using current strategy");

                        foreach (var document in snapshot.Documents)
                        {
                            try
                            {
                                var training = document.ConvertTo<Training>();
                                training.Id = document.Id;
                                training.EmployeeId = employeeId;

                                // Ensure dates are valid
                                if (training.StartDate == 0)
                                    training.SetStartDate(DateTime.Now);

                                if (training.EndDate == 0)
                                    training.SetEndDate(DateTime.Now.AddMonths(1));

                                if (training.CreatedAt == 0)
                                    training.SetCreatedAt(DateTime.Now);

                                trainings.Add(training);
                                Console.WriteLine($"✅ Training: {training.Title} (Status: {training.Status})");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Error converting training document {document.Id}: {ex.Message}");
                            }
                        }

                        // If we found trainings, break out of the loop
                        if (trainings.Any())
                            break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Strategy failed: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ Retrieved {trainings.Count} trainings for employee {employeeId}");
                return trainings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetEmployeeTrainingsAsync: {ex.Message}");
                return new List<Training>();
            }
        }

        // Helper methods for flexible field mapping
        private string GetFieldValue(Dictionary<string, object> data, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (data.ContainsKey(fieldName) && data[fieldName] != null)
                {
                    return data[fieldName]?.ToString() ?? "";
                }
            }
            return "";
        }

        private Google.Cloud.Firestore.Timestamp GetTimestampField(Dictionary<string, object> data, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (data.ContainsKey(fieldName) && data[fieldName] != null)
                {
                    try
                    {
                        if (data[fieldName] is Google.Cloud.Firestore.Timestamp timestamp)
                            return timestamp;

                        if (data[fieldName] is DateTime dateTime)
                            return Google.Cloud.Firestore.Timestamp.FromDateTime(dateTime.ToUniversalTime());

                        if (DateTime.TryParse(data[fieldName]?.ToString(), out var parsedDate))
                            return Google.Cloud.Firestore.Timestamp.FromDateTime(parsedDate.ToUniversalTime());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error parsing timestamp field {fieldName}: {ex.Message}");
                    }
                }
            }
            return Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);
        }

        public async Task<bool> TestAdminCollectionAsync()
        {
            try
            {
                Console.WriteLine("🧪 Testing admin collection access...");

                // Test if we can read from admin collection
                var adminCollection = _firestoreDb.Collection("admins");
                var snapshot = await adminCollection.Limit(1).GetSnapshotAsync();

                Console.WriteLine($"✅ Admin collection accessible. Contains {snapshot.Count} documents");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Admin collection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Dictionary<string, object>>> GetEmployeeSubCollectionAsync(string employeeId, string subcollectionName)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching {subcollectionName} for employee: {employeeId}");
                var subData = new List<Dictionary<string, object>>();

                var collectionRef = _firestoreDb.Collection("employees").Document(employeeId).Collection(subcollectionName);
                var snapshot = await collectionRef.GetSnapshotAsync();

                Console.WriteLine($"📊 Found {snapshot.Count} {subcollectionName} for employee {employeeId}");

                foreach (var document in snapshot.Documents)
                {
                    var data = document.ToDictionary();
                    data["id"] = document.Id;
                    subData.Add(data);
                    Console.WriteLine($"✅ {subcollectionName} document: {document.Id}");
                }

                return subData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetEmployeeSubCollectionAsync: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        // Debugging methods
        public async Task<string> DebugEmployeeFields()
        {
            var result = new List<string>();

            try
            {
                result.Add("=== FIRESTORE FIELD DEBUG ===");
                var employeesCollection = _firestoreDb.Collection("employees");
                var snapshot = await employeesCollection.Limit(5).GetSnapshotAsync();

                result.Add($"Found {snapshot.Count} employee documents");

                foreach (var doc in snapshot.Documents)
                {
                    result.Add($"\n📄 Document ID: {doc.Id}");
                    var data = doc.ToDictionary();

                    result.Add("Available fields:");
                    foreach (var field in data)
                    {
                        result.Add($"  '{field.Key}': '{field.Value}'");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Add($"❌ Error: {ex.Message}");
            }

            return string.Join("\n", result);
        }

        public async Task<string> DebugEmployeeData(string employeeId = null)
        {
            var result = new List<string>();

            try
            {
                result.Add("=== FIRESTORE DATA DEBUG ===");
                result.Add($"Project: {_settings.ProjectId}");
                result.Add($"Time: {DateTime.Now}");

                // Check employees collection
                var employeesCollection = _firestoreDb.Collection("employees");
                var employeesSnapshot = await employeesCollection.Limit(10).GetSnapshotAsync();
                result.Add($"\n📁 Employees Collection: {employeesSnapshot.Count} documents found");

                foreach (var doc in employeesSnapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    result.Add($"\n👤 Employee ID: {doc.Id}");
                    result.Add($"   Name: {data.GetValueOrDefault("FirstName", "N/A")} {data.GetValueOrDefault("LastName", "N/A")}");
                    result.Add($"   Email: {data.GetValueOrDefault("Email", "N/A")}");
                    result.Add($"   Available Fields: {string.Join(", ", data.Keys)}");
                }

                // Check specific employee's subcollections
                if (!string.IsNullOrEmpty(employeeId))
                {
                    result.Add($"\n🔍 Detailed check for employee: {employeeId}");

                    var employeeDoc = _firestoreDb.Collection("employees").Document(employeeId);
                    var employeeSnapshot = await employeeDoc.GetSnapshotAsync();
                    result.Add($"   Employee exists: {employeeSnapshot.Exists}");

                    if (employeeSnapshot.Exists)
                    {
                        string[] subcollections = { "skills", "qualifications", "trainings" };

                        foreach (var subcollection in subcollections)
                        {
                            try
                            {
                                var subCollectionRef = employeeDoc.Collection(subcollection);
                                var subSnapshot = await subCollectionRef.Limit(5).GetSnapshotAsync();
                                result.Add($"\n   📂 {subcollection}: {subSnapshot.Count} documents");

                                foreach (var subDoc in subSnapshot.Documents)
                                {
                                    var subData = subDoc.ToDictionary();
                                    result.Add($"      📄 {subDoc.Id}: {System.Text.Json.JsonSerializer.Serialize(subData)}");
                                }
                            }
                            catch (Exception ex)
                            {
                                result.Add($"   ❌ {subcollection} error: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Add($"\n❌ Debug error: {ex.Message}");
                result.Add($"Stack: {ex.StackTrace}");
            }

            return string.Join("\n", result);
        }

        public FirestoreDb GetFirestoreDb() => _firestoreDb;

        // NEW METHODS ADDED:

        public async Task<List<Employee>> GetAllEmployeesWithSkills()
        {
            try
            {
                Console.WriteLine("GetAllEmployeesWithSkills: Starting to load employees with skills...");
                var employees = await GetAllEmployeesAsync();
                var employeesWithSkills = new List<Employee>();

                foreach (var employee in employees)
                {
                    var skills = await GetEmployeeSkillsAsync(employee.Id);
                    // Add skills to employee (you may need to add a Skills property to Employee model)
                    employeesWithSkills.Add(employee);
                }

                Console.WriteLine($"GetAllEmployeesWithSkills: Loaded {employeesWithSkills.Count} employees with skills");
                return employeesWithSkills;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetAllEmployeesWithSkills: {ex.Message}");
                return new List<Employee>();
            }
        }

        public async Task<EmployeeSkillViewModel> GetEmployeeSkillsViewModel()
        {
            try
            {
                Console.WriteLine("GetEmployeeSkillsViewModel: Starting to load data...");

                // Get all employees
                var employees = await GetAllEmployeesAsync();
                Console.WriteLine($"GetEmployeeSkillsViewModel: Found {employees.Count} employees");

                // Get all skills across all employees
                var allSkills = new List<Skill>();
                foreach (var employee in employees)
                {
                    var skills = await GetEmployeeSkillsAsync(employee.Id);
                    allSkills.AddRange(skills);
                }

                Console.WriteLine($"GetEmployeeSkillsViewModel: Found {allSkills.Count} total skills");

                // Calculate metrics
                var totalSkillsTracked = allSkills.Count;
                var totalEmployees = employees.Count;

                // Calculate critical skills gap (skills with level < 3)
                var criticalSkillsGap = allSkills.Count(s => ParseSkillLevel(s.Level) < 3);

                // Calculate average skill level
                var averageSkillLevel = allSkills.Any()
                    ? allSkills.Average(s => ParseSkillLevel(s.Level))
                    : 0.0;

                // Calculate department stats
                var departmentStats = CalculateDepartmentStats(employees, allSkills);

                // Calculate category stats
                var categoryStats = CalculateCategoryStats(allSkills);

                // Get skills matrix
                var skillsMatrix = await GetSkillsMatrix();

                // Create skills gap analysis
                var skillsGapAnalysis = allSkills
                    .GroupBy(s => s.Name)
                    .Select(g => new Skill
                    {
                        Name = g.Key,
                        Gap = g.Average(s => ParseSkillLevel(s.Level) < 3 ? 1 : 0),
                        EmployeeCount = g.Count(),
                        IsCritical = g.Average(s => ParseSkillLevel(s.Level)) < 3
                    })
                    .ToList();

                var viewModel = new EmployeeSkillViewModel
                {
                    TotalSkillsTracked = totalSkillsTracked,
                    CriticalSkillsGap = criticalSkillsGap,
                    AverageSkillLevel = Math.Round(averageSkillLevel, 1),
                    SkillsGapAnalysis = skillsGapAnalysis,
                    DepartmentSkills = new List<Department>(), // You'll need to implement this
                    SkillsMatrix = skillsMatrix,
                    DepartmentStats = departmentStats,
                    CategoryStats = categoryStats,
                    TotalEmployees = totalEmployees,
                    Employees = employees, // This assumes your Employee model matches what the view expects
                    EmployeeDetails = employees.FirstOrDefault() ?? new Employee()
                };

                Console.WriteLine("GetEmployeeSkillsViewModel: ViewModel created successfully");
                return viewModel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetEmployeeSkillsViewModel: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return new EmployeeSkillViewModel
                {
                    TotalSkillsTracked = 0,
                    CriticalSkillsGap = 0,
                    AverageSkillLevel = 0,
                    SkillsGapAnalysis = new List<Skill>(),
                    DepartmentSkills = new List<Department>(),
                    Employees = new List<Employee>(),
                    SkillsMatrix = new List<SkillsMatrix>(),
                    DepartmentStats = new List<Department>(),
                    CategoryStats = new List<Category>(),
                    TotalEmployees = 0,
                    EmployeeDetails = new Employee()
                };
            }
        }

        public async Task<List<SkillsMatrix>> GetSkillsMatrix()
        {
            try
            {
                Console.WriteLine("GetSkillsMatrix: Fetching skills matrix data...");
                var skillsMatrix = new List<SkillsMatrix>();

                // Get all employees
                var employees = await GetAllEmployeesAsync();
                Console.WriteLine($"GetSkillsMatrix: Processing {employees.Count} employees");

                foreach (var employee in employees)
                {
                    var skills = await GetEmployeeSkillsAsync(employee.Id);

                    var matrixEntry = new SkillsMatrix
                    {
                        EmployeeName = $"{employee.FirstName} {employee.LastName}",
                        Department = employee.Department,
                        Email = employee.Email,
                        FinancialModeling = GetSkillLevelForMatrix(skills, "Financial Modeling"),
                        DataAnalysis = GetSkillLevelForMatrix(skills, "Data Analysis"),
                        RiskAssessment = GetSkillLevelForMatrix(skills, "Risk Assessment"),
                        ProjectManagement = GetSkillLevelForMatrix(skills, "Project Management"),
                        ReportWriting = GetSkillLevelForMatrix(skills, "Report Writing"),
                        LastUpdated = DateTime.UtcNow
                    };

                    skillsMatrix.Add(matrixEntry);
                }

                Console.WriteLine($"GetSkillsMatrix: Created matrix with {skillsMatrix.Count} entries");
                return skillsMatrix;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetSkillsMatrix: {ex.Message}");
                return new List<SkillsMatrix>();
            }
        }

        // HELPER METHODS:

        private List<Department> CalculateDepartmentStats(List<Employee> employees, List<Skill> allSkills)
        {
            if (employees == null || !employees.Any())
                return new List<Department>();

            var departmentStats = employees
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Department) ? "Unknown" : e.Department.Trim())
                .Select(g =>
                {
                    var deptName = g.Key;
                    var deptEmployees = g.ToList();
                    var deptEmployeeIds = new HashSet<string>(deptEmployees.Select(e => e.Id));

                    var skillsForDept = allSkills
                        .Where(s => s != null && !string.IsNullOrEmpty(s.EmployeeId) && deptEmployeeIds.Contains(s.EmployeeId))
                        .ToList();

                    var totalSkills = skillsForDept.Count;
                    var averageSkillLevel = skillsForDept.Any()
                        ? skillsForDept.Average(s => ParseSkillLevel(s.Level))
                        : 0.0;

                    return new Department
                    {
                        Name = deptName,
                        EmployeeCount = deptEmployees.Count,
                        AverageSkillLevel = Math.Round(averageSkillLevel, 1),
                        TotalSkills = totalSkills
                    };
                })
                .ToList();

            return departmentStats;
        }

        private List<Category> CalculateCategoryStats(List<Skill> allSkills)
        {
            if (allSkills == null || !allSkills.Any())
                return new List<Category>();

            var categoryStats = allSkills
                .Where(s => s != null && !string.IsNullOrEmpty(s.Category))
                .GroupBy(s => s.Category)
                .Select(g =>
                {
                    var skillsInCategory = g.ToList();
                    return new Category
                    {
                        CategoryName = g.Key,
                        SkillCount = skillsInCategory.Count,
                        AverageGap = skillsInCategory.Count(s => ParseSkillLevel(s.Level) < 3),
                        SkillsWithGaps = skillsInCategory.Count(s => ParseSkillLevel(s.Level) < 3)
                    };
                })
                .ToList();

            return categoryStats;
        }

        private int ParseSkillLevel(string level)
        {
            if (int.TryParse(level, out int result))
                return result;

            return level?.ToLower() switch
            {
                "beginner" => 1,
                "basic" => 2,
                "intermediate" => 3,
                "advanced" => 4,
                "expert" => 5,
                _ => 0
            };
        }

        private string GetSkillLevelForMatrix(List<Skill> skills, string skillName)
        {
            var skill = skills.FirstOrDefault(s =>
                s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

            if (skill == null) return "Not Rated";

            var level = ParseSkillLevel(skill.Level);
            return level switch
            {
                1 => "Beginner",
                2 => "Basic",
                3 => "Intermediate",
                4 => "Advanced",
                5 => "Expert",
                _ => "Not Rated"
            };
        }
        private DateTime ConvertFirestoreDate(object firestoreDate)
        {
            try
            {
                if (firestoreDate is Google.Cloud.Firestore.Timestamp timestamp)
                {
                    return timestamp.ToDateTime();
                }
                else if (firestoreDate is long milliseconds)
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime;
                }
                else if (firestoreDate is int intMilliseconds)
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(intMilliseconds).DateTime;
                }
                else if (firestoreDate is string dateString)
                {
                    return DateTime.Parse(dateString);
                }
                else if (firestoreDate is DateTime dateTime)
                {
                    return dateTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting date: {ex.Message}");
            }

            return DateTime.Now;
        }

        // Add to FirestoreService.cs
        public async Task<TrainingDetailViewModel> GetTrainingDetailViewModelAsync(string employeeId, string trainingId)
        {
            try
            {
                Console.WriteLine($"🔍 Loading training details for employee: {employeeId}, training: {trainingId}");

                // Get the specific training
                var trainingCollection = _firestoreDb.Collection("employees").Document(employeeId).Collection("trainings");
                var trainingDoc = await trainingCollection.Document(trainingId).GetSnapshotAsync();

                if (!trainingDoc.Exists)
                {
                    Console.WriteLine($"❌ Training {trainingId} not found for employee {employeeId}");
                    return null;
                }

                var training = trainingDoc.ConvertTo<Training>();
                training.Id = trainingDoc.Id;
                training.EmployeeId = employeeId;

                // Get employee details
                var employee = await GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    Console.WriteLine($"❌ Employee {employeeId} not found");
                    return null;
                }

                // Get all employee skills to match with covered skills
                var employeeSkills = await GetEmployeeSkillsAsync(employeeId);

                // Get covered skills details
                var coveredSkills = new List<Skill>();
                //if (training.CoveredSkills != null && training.CoveredSkills.Any())
                //{
                //    foreach (var skillName in training.CoveredSkills)
                //    {
                //        var skill = employeeSkills.FirstOrDefault(s =>
                //            s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));
                //        if (skill != null)
                //        {
                //            coveredSkills.Add(skill);
                //        }
                //        else
                //        {
                //            // Create a placeholder skill if not found in employee's skills
                //            coveredSkills.Add(new Skill
                //            {
                //                Name = skillName,
                //                Level = "Not Rated",
                //                Category = "Training Coverage"
                //            });
                //        }
                //    }
                //}

                // Get related qualifications (qualifications that might be relevant to this training)
                var qualifications = await GetEmployeeQualificationsAsync(employeeId);
                var relatedQualifications = qualifications
                    .Where(q => training.Title.ToLower().Contains(q.FieldOfStudy?.ToLower() ?? "") ||
                               training.Description.ToLower().Contains(q.FieldOfStudy?.ToLower() ?? "") ||
                               training.Provider.ToLower().Contains(q.Institution?.ToLower() ?? ""))
                    .ToList();

                var viewModel = new TrainingDetailViewModel
                {
                    Training = training,
                    Employee = employee,
                    CoveredSkills = coveredSkills,
                    EmployeeSkills = employeeSkills,
                    RelatedQualifications = relatedQualifications
                };

                Console.WriteLine($"✅ Training details loaded: {training.Title}");
                Console.WriteLine($"   - Covered Skills: {coveredSkills.Count}");
                Console.WriteLine($"   - Employee Skills: {employeeSkills.Count}");
                Console.WriteLine($"   - Related Qualifications: {relatedQualifications.Count}");

                return viewModel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetTrainingDetailViewModelAsync: {ex.Message}");
                return null;
            }
        }
        // Add these methods to your FirestoreService class
        public async Task<Employee> GetEmployeeWithDetailsAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Loading detailed employee data for: {employeeId}");

                var employee = await GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    Console.WriteLine($"❌ Employee {employeeId} not found");
                    return null;
                }

                // Load related data in parallel
                var skillsTask = GetEmployeeSkillsAsync(employeeId);
                var qualificationsTask = GetEmployeeQualificationsAsync(employeeId);
                var trainingsTask = GetEmployeeTrainingsAsync(employeeId);

                await Task.WhenAll(skillsTask, qualificationsTask, trainingsTask);

                employee.Skills = skillsTask.Result;
                employee.Qualifications = qualificationsTask.Result;
                employee.Trainings = trainingsTask.Result;

                // Calculate metrics
                CalculateEmployeeMetrics(employee);

                Console.WriteLine($"✅ Loaded employee: {employee.FullName}");
                Console.WriteLine($"   - Skills: {employee.Skills.Count}");
                Console.WriteLine($"   - Qualifications: {employee.Qualifications.Count}");
                Console.WriteLine($"   - Trainings: {employee.Trainings.Count}");

                return employee;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading employee details: {ex.Message}");
                return null;
            }
        }
        // Add to FirestoreService.cs
        public async Task<bool> UpdateTrainingCertificateAsync(string employeeId, string trainingId, string fileName, string downloadUrl)
        {
            try
            {
                var trainingDoc = _firestoreDb.Collection("trainings").Document(trainingId);
                var updateData = new Dictionary<string, object>
        {
            { "CertificateFileName", fileName },
            { "CertificateUrl", downloadUrl },
            { "CertificatePdfUrl", downloadUrl },
            { "UpdatedAt", FieldValue.ServerTimestamp }
        };

                await trainingDoc.UpdateAsync(updateData);
                Console.WriteLine($"✅ Training certificate updated: {trainingId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating training certificate: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateTrainingProgressAsync(string employeeId, string trainingId, int progress, string status)
        {
            try
            {
                var trainingDoc = _firestoreDb.Collection("trainings").Document(trainingId);
                var updateData = new Dictionary<string, object>
        {
            { "Progress", progress },
            { "Status", status },
            { "UpdatedAt", FieldValue.ServerTimestamp }
        };

                if (status == "Completed")
                {
                    updateData["CompletedDate"] = DateTime.UtcNow.ToTimestamp();
                }

                await trainingDoc.UpdateAsync(updateData);
                Console.WriteLine($"✅ Training progress updated: {trainingId} -> {progress}% ({status})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating training progress: {ex.Message}");
                return false;
            }
        }
        public async Task<Training> GetTrainingAsync(string employeeId, string trainingId)
        {
            try
            {
                var trainingDoc = _firestoreDb.Collection("trainings").Document(trainingId);
                var snapshot = await trainingDoc.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    var training = snapshot.ConvertTo<Training>();
                    training.Id = snapshot.Id;
                    return training;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting training: {ex.Message}");
                return null;
            }
        }
        // Add to FirestoreService.cs
        public async Task<List<StorageDocument>> GetEmployeeStorageDocumentsAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching storage documents for employee: {employeeId}");
                var documents = new List<StorageDocument>();

                // Get all trainings with certificates
                var trainings = await GetEmployeeTrainingsAsync(employeeId);
                foreach (var training in trainings)
                {
                    if (!string.IsNullOrEmpty(training.CertificatePdfUrl))
                    {
                        documents.Add(new StorageDocument
                        {
                            Id = training.Id,
                            Type = "Training Certificate",
                            FileName = training.CertificateFileName ?? "certificate.pdf",
                            DownloadUrl = training.CertificatePdfUrl,
                            StoragePath = ExtractStoragePath(training.CertificatePdfUrl),
                            UploadDate = training.CompletedDate.HasValue ?
                                DateTimeOffset.FromUnixTimeMilliseconds(training.CompletedDate.Value).DateTime :
                                DateTime.Now,
                            Title = training.Title,
                            Description = $"Training: {training.Title} - {training.Provider}"
                        });
                    }

                    if (!string.IsNullOrEmpty(training.CertificateUrl))
                    {
                        documents.Add(new StorageDocument
                        {
                            Id = training.Id + "_image",
                            Type = "Training Certificate Image",
                            FileName = "certificate.jpg",
                            DownloadUrl = training.CertificateUrl,
                            StoragePath = ExtractStoragePath(training.CertificateUrl),
                            UploadDate = training.CompletedDate.HasValue ?
                                DateTimeOffset.FromUnixTimeMilliseconds(training.CompletedDate.Value).DateTime :
                                DateTime.Now,
                            Title = training.Title,
                            Description = $"Training Certificate Image: {training.Title}"
                        });
                    }
                }

                // Get all qualifications with documents
                var qualifications = await GetEmployeeQualificationsAsync(employeeId);
                foreach (var qualification in qualifications)
                {
                    if (!string.IsNullOrEmpty(qualification.CertificatePdfUrl))
                    {
                        documents.Add(new StorageDocument
                        {
                            Id = qualification.Id + "_cert",
                            Type = "Qualification Certificate",
                            FileName = qualification.CertificateFileName ?? $"{qualification.Degree}_certificate.pdf",
                            DownloadUrl = qualification.CertificatePdfUrl,
                            StoragePath = ExtractStoragePath(qualification.CertificatePdfUrl),
                            UploadDate = DateTime.Now, // You might want to store upload date in qualifications
                            Title = $"{qualification.Degree} Certificate",
                            Description = $"{qualification.Degree} from {qualification.Institution}"
                        });
                    }

                    if (!string.IsNullOrEmpty(qualification.TranscriptPdfUrl))
                    {
                        documents.Add(new StorageDocument
                        {
                            Id = qualification.Id + "_transcript",
                            Type = "Transcript",
                            FileName = qualification.TranscriptFileName ?? $"{qualification.Degree}_transcript.pdf",
                            DownloadUrl = qualification.TranscriptPdfUrl,
                            StoragePath = ExtractStoragePath(qualification.TranscriptPdfUrl),
                            UploadDate = DateTime.Now,
                            Title = $"{qualification.Degree} Transcript",
                            Description = $"Transcript for {qualification.Degree} from {qualification.Institution}"
                        });
                    }
                }

                Console.WriteLine($"✅ Found {documents.Count} storage documents for employee {employeeId}");
                return documents;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting employee storage documents: {ex.Message}");
                return new List<StorageDocument>();
            }
        }
        // Add to FirestoreService.cs
        public async Task<bool> UpdateQualificationDocumentsAsync(string employeeId, string qualificationId,
            string certificatePdfUrl = null, string certificateFileName = null,
            string transcriptPdfUrl = null, string transcriptFileName = null)
        {
            try
            {
                Console.WriteLine($"📄 Updating documents for qualification: {qualificationId}");

                var qualificationRef = _firestoreDb.Collection("employees")
                    .Document(employeeId)
                    .Collection("qualifications")
                    .Document(qualificationId);

                var updateData = new Dictionary<string, object>();

                if (certificatePdfUrl != null)
                    updateData["certificatePdfUrl"] = certificatePdfUrl;

                if (certificateFileName != null)
                    updateData["certificateFileName"] = certificateFileName;

                if (transcriptPdfUrl != null)
                    updateData["transcriptPdfUrl"] = transcriptPdfUrl;

                if (transcriptFileName != null)
                    updateData["transcriptFileName"] = transcriptFileName;

                await qualificationRef.UpdateAsync(updateData);
                Console.WriteLine($"✅ Qualification documents updated successfully: {qualificationId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating qualification documents: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Qualification>> GetQualificationsWithDocumentsAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching qualifications with documents for employee: {employeeId}");

                var qualifications = await GetEmployeeQualificationsAsync(employeeId);
                var qualificationsWithDocs = qualifications
                    .Where(q => !string.IsNullOrEmpty(q.CertificatePdfUrl) ||
                               !string.IsNullOrEmpty(q.TranscriptPdfUrl))
                    .ToList();

                Console.WriteLine($"✅ Found {qualificationsWithDocs.Count} qualifications with documents");
                return qualificationsWithDocs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting qualifications with documents: {ex.Message}");
                return new List<Qualification>();
            }
        }
        private string ExtractStoragePath(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return string.Empty;

                // Handle Firebase Storage URLs
                if (url.Contains("firebasestorage.googleapis.com"))
                {
                    var uri = new Uri(url);
                    // Extract path from URL: https://firebasestorage.googleapis.com/v0/b/codets-6e4e2.appspot.com/o/documents%2F...
                    var path = uri.AbsolutePath;
                    if (path.StartsWith("/v0/b/"))
                    {
                        // Remove the /v0/b/bucket-name/o/ part
                        var parts = path.Split('/');
                        if (parts.Length > 5)
                        {
                            // Reconstruct the storage path
                            var storagePath = string.Join("/", parts.Skip(5));
                            // URL decode the path
                            storagePath = Uri.UnescapeDataString(storagePath);
                            return storagePath;
                        }
                    }
                }

                // Handle direct storage paths
                if (url.StartsWith("gs://"))
                {
                    return url.Replace("gs://codets-6e4e2.firebasestorage.app/", "");
                }

                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error extracting storage path: {ex.Message}");
                return url;
            }
        }
        public async Task<List<Training>> GetTrainingsWithCertificatesAsync(string employeeId = null)
        {
            try
            {
                var trainings = new List<Training>();

                if (!string.IsNullOrEmpty(employeeId))
                {
                    // Get trainings for specific employee
                    var employeeTrainings = await GetEmployeeTrainingsAsync(employeeId);
                    return employeeTrainings.Where(t => !string.IsNullOrEmpty(t.CertificateUrl)).ToList();
                }
                else
                {
                    // Get all trainings with certificates across all employees
                    var employees = await GetAllEmployeesAsync();

                    foreach (var employee in employees)
                    {
                        var employeeTrainings = await GetEmployeeTrainingsAsync(employee.Id);
                        var certifiedTrainings = employeeTrainings.Where(t => !string.IsNullOrEmpty(t.CertificateUrl));
                        trainings.AddRange(certifiedTrainings);
                    }
                }

                return trainings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting trainings with certificates: {ex.Message}");
                return new List<Training>();
            }
        }
        // Add to FirestoreService.cs
        public async Task<List<Employee>> GetAllEmployeesDetailedAsync()
        {
            try
            {
                Console.WriteLine("🔍 Fetching all employees with detailed data...");

                var employees = await GetAllEmployeesAsync();
                var detailedEmployees = new List<Employee>();

                foreach (var employee in employees)
                {
                    try
                    {
                        var detailedEmployee = await GetEmployeeWithAllDataAsync(employee.Id);
                        if (detailedEmployee != null)
                        {
                            detailedEmployees.Add(detailedEmployee);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error loading details for employee {employee.Id}: {ex.Message}");
                        // Add basic employee data even if details fail
                        employee.CalculateMetrics();
                        detailedEmployees.Add(employee);
                    }
                }

                Console.WriteLine($"✅ Loaded {detailedEmployees.Count} detailed employees");
                return detailedEmployees;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetAllEmployeesDetailedAsync: {ex.Message}");
                return new List<Employee>();
            }
        }

        public async Task<Employee> GetEmployeeByEmployeeIdAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching employee by EmployeeId: {employeeId}");

                var query = _firestoreDb.Collection("employees")
                    .WhereEqualTo("employeeId", employeeId)
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count > 0)
                {
                    var document = snapshot.Documents[0];
                    var employee = document.ConvertTo<Employee>();
                    employee.Id = document.Id;

                    Console.WriteLine($"✅ Found employee: {employee.FullName}");
                    return employee;
                }

                Console.WriteLine($"❌ No employee found with EmployeeId: {employeeId}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetEmployeeByEmployeeIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<Employee> GetEmployeeByUserIdAsync(string userId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching employee by UserId: {userId}");

                var query = _firestoreDb.Collection("employees")
                    .WhereEqualTo("userId", userId)
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count > 0)
                {
                    var document = snapshot.Documents[0];
                    var employee = document.ConvertTo<Employee>();
                    employee.Id = document.Id;

                    Console.WriteLine($"✅ Found employee: {employee.FullName}");
                    return employee;
                }

                Console.WriteLine($"❌ No employee found with UserId: {userId}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetEmployeeByUserIdAsync: {ex.Message}");
                return null;
            }
        }

    }
}