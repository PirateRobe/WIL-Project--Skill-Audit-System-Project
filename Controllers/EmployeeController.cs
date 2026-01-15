using AspNetCoreGeneratedDocument;
using Google.Apis.Http;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication2.Models;
using WebApplication2.Models.Support_Model;
using WebApplication2.Models.ViewModel;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly FirestoreService _firestoreService;

        public EmployeeController(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }



        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Employee ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                Console.WriteLine($"🔍 Loading details for employee: {id}");

                var employee = await _firestoreService.GetEmployeeWithAllDataAsync(id);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction(nameof(Index));
                }

                // Ensure collections are never null
                employee.Skills ??= new List<Skill>();
                employee.Qualifications ??= new List<Qualification>();
                employee.Trainings ??= new List<Training>();

                // Validate and fix dates if needed
                await ValidateAndFixEmployeeData(employee);

                // Calculate metrics
                employee.CalculateMetrics();

                ViewData["Title"] = $"Employee Details - {employee.FullName}";
                return View(employee);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading employee details: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading employee details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
       
        private async Task ValidateAndFixEmployeeData(Employee employee)
        {
            try
            {
                // Fix hire date if invalid
                if (employee.HireDate == null ||
                    (employee.HireDate is long hireDateLong && hireDateLong == 0) ||
                    (employee.HireDate is long hireDateInt && hireDateInt == 0) ||
                    employee.HireDateDateTime.Year < 2000)
                {
                    employee.SetHireDate(DateTime.UtcNow);
                }

                // Fix created at date if invalid
                if (employee.CreatedAt == null ||
                    (employee.CreatedAt is long createdAtLong && createdAtLong == 0) ||
                    (employee.CreatedAt is long createdAtInt && createdAtInt == 0) ||
                    employee.CreatedAtDateTime.Year < 2000)
                {
                    employee.SetCreatedAt(DateTime.UtcNow);
                }

                // Fix training dates
                if (employee.Trainings != null)
                {
                    foreach (var training in employee.Trainings)
                    {
                        if (training.StartDate == 0 || training.StartDateDateTime().Year < 2000)
                        {
                            training.SetStartDate(DateTime.Now);
                        }
                        if (training.EndDate == 0 || training.EndDateDateTime().Year < 2000)
                        {
                            training.SetEndDate(DateTime.Now.AddMonths(1));
                        }
                        if (training.CreatedAt == 0 || training.CreatedAtDateTime().Year < 2000)
                        {
                            training.SetCreatedAt(DateTime.Now);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error validating employee data: {ex.Message}");
            }
        }
        // In EmployeeController.cs
        //public async Task<IActionResult> Details(string id)
        //{
        //    try
        //    {
        //        Console.WriteLine($"🔍 Loading details for employee: {id}");

        //        if (string.IsNullOrEmpty(id))
        //        {
        //            TempData["ErrorMessage"] = "Employee ID is required";
        //            return RedirectToAction("EmployeeSkills");
        //        }

        //        var employee = await _firestoreService.GetEmployeeWithAllDataAsync(id);
        //        if (employee == null)
        //        {
        //            Console.WriteLine($"❌ Employee {id} not found");
        //            TempData["ErrorMessage"] = $"Employee with ID {id} not found";
        //            return RedirectToAction("EmployeeSkills");
        //        }

        //        Console.WriteLine($"✅ Loaded employee details: {employee.FirstName} {employee.LastName}");
        //        Console.WriteLine($"   - Skills: {employee.Skills?.Count ?? 0}");
        //        Console.WriteLine($"   - Qualifications: {employee.Qualifications?.Count ?? 0}");
        //        Console.WriteLine($"   - Trainings: {employee.Trainings?.Count ?? 0}");

        //        return View(employee);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error loading employee details: {ex.Message}");
        //        TempData["ErrorMessage"] = $"Error loading employee details: {ex.Message}";
        //        return RedirectToAction("EmployeeSkills");
        //    }
        //}
        [HttpGet]
        public async Task<IActionResult> DebugTrainingData(string employeeId, string trainingId)
        {
            if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(trainingId))
            {
                return Content("Please provide employee ID and training ID: /Training/DebugTrainingData?employeeId=ID&trainingId=ID");
            }

            var result = new List<string>();
            try
            {
                result.Add($"🔍 Debugging training: {trainingId} for employee: {employeeId}");
                result.Add($"Timestamp: {DateTime.Now}");

                var viewModel = await _firestoreService.GetTrainingDetailViewModelAsync(employeeId, trainingId);
                if (viewModel != null)
                {
                    result.Add($"\n✅ Training Found:");
                    result.Add($"   Title: {viewModel.Training.Title}");
                    result.Add($"   Provider: {viewModel.Training.Provider}");
                    result.Add($"   Status: {viewModel.Training.Status}");
                    result.Add($"   Employee: {viewModel.Employee.FirstName} {viewModel.Employee.LastName}");
                    result.Add($"   Start Date: {viewModel.Training.StartDateDateTime():MMM dd, yyyy}");
                    result.Add($"   End Date: {viewModel.Training.EndDateDateTime():MMM dd, yyyy}");
                    result.Add($"   Duration: {viewModel.DurationDays} days");
                    result.Add($"   Assigned Reason: {viewModel.Training.AssignedReason ?? "Not specified"}");
                   // result.Add($"   Skill Gap Before: {viewModel.Training.SkillGapBefore}");
                   // result.Add($"   Skill Gap After: {viewModel.Training.SkillGapAfter}");
                   // result.Add($"   Skill Improvement: {viewModel.SkillImprovement}");
                    result.Add($"   Certificate: {viewModel.HasCertificate}");
                    result.Add($"   Certificate URL: {viewModel.Training.CertificateUrl ?? "None"}");
                    result.Add($"   Certificate PDF: {viewModel.Training.CertificatePdfUrl ?? "None"}");
                    result.Add($"   Certificate File: {viewModel.Training.CertificateFileName ?? "None"}");

                    result.Add($"\n📚 Covered Skills ({viewModel.CoveredSkills.Count}):");
                    foreach (var skill in viewModel.CoveredSkills)
                    {
                        result.Add($"   - {skill.Name} (Level: {skill.Level}, Category: {skill.Category})");
                    }

                    result.Add($"\n🛠️ All Employee Skills ({viewModel.EmployeeSkills.Count}):");
                    foreach (var skill in viewModel.EmployeeSkills.Take(10)) // Limit to first 10
                    {
                        result.Add($"   - {skill.Name} (Level: {skill.Level}, Exp: {skill.YearsOfExperience} yrs)");
                    }

                    result.Add($"\n🎓 Related Qualifications ({viewModel.RelatedQualifications.Count}):");
                    foreach (var qual in viewModel.RelatedQualifications)
                    {
                        result.Add($"   - {qual.Degree} from {qual.Institution} ({qual.YearCompleted})");
                    }
                }
                else
                {
                    result.Add($"\n❌ Training not found");
                }
            }
            catch (Exception ex)
            {
                result.Add($"\n❌ Error: {ex.Message}");
                result.Add($"Stack: {ex.StackTrace}");
            }

            return Content(string.Join("\n", result), "text/plain");
        }

        //public async Task<IActionResult> Details(string id)
        //{
        //    try
        //    {
        //        Console.WriteLine($"🔍 Loading details for employee: {id}");

        //        if (string.IsNullOrEmpty(id))
        //        {
        //            ViewBag.Error = "Employee ID is required";
        //            return View("Error");
        //        }

        //        var employee = await _firestoreService.GetEmployeeByIdAsync(id);
        //        if (employee == null)
        //        {
        //            Console.WriteLine($"❌ Employee {id} not found");
        //            ViewBag.Error = $"Employee with ID {id} not found";
        //            return View("Error");
        //        }

        //        // Load all subcollections using the SPECIFIC methods
        //        var skillsTask = _firestoreService.GetEmployeeSkillsAsync(id);
        //        var qualificationsTask = _firestoreService.GetEmployeeQualificationsAsync(id);
        //        var trainingsTask = _firestoreService.GetEmployeeTrainingsAsync(id);

        //        await Task.WhenAll(skillsTask, qualificationsTask, trainingsTask);

        //        ViewBag.Employee = employee;
        //        ViewBag.Skills = skillsTask.Result;
        //        ViewBag.Qualifications = qualificationsTask.Result;
        //        ViewBag.Trainings = trainingsTask.Result;

        //        Console.WriteLine($"✅ Loaded employee details: {employee.FirstName} {employee.LastName}");
        //        Console.WriteLine($"   - Skills: {ViewBag.Skills.Count}");
        //        Console.WriteLine($"   - Qualifications: {ViewBag.Qualifications.Count}");
        //        Console.WriteLine($"   - Trainings: {ViewBag.Trainings.Count}");

        //        return View();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error loading employee details: {ex.Message}");
        //        ViewBag.Error = $"Error loading employee details: {ex.Message}";
        //        return View("Error");
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> DebugSubcollections(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Content("Please provide employee ID: /Employee/DebugSubcollections?id=EMPLOYEE_ID");
            }

            var result = new List<string>();
            try
            {
                result.Add($"🔍 Debugging employee: {id}");
                result.Add($"Timestamp: {DateTime.Now}");

                // Get employee data
                var employee = await _firestoreService.GetEmployeeByIdAsync(id);
                if (employee != null)
                {
                    result.Add($"\n✅ Employee Found:");
                    result.Add($"   Name: {employee.FirstName} {employee.LastName}");
                    result.Add($"   Email: {employee.Email}");
                    result.Add($"   Department: {employee.Department}");
                    result.Add($"   Position: {employee.Position}");
                    result.Add($"   UserId: {employee.UserId}");
                    result.Add($"   EmployeeId: {employee.EmployeeId}");
                    result.Add($"   Hire Date: {employee.HireDateDateTime}");
                    result.Add($"   Created At: {employee.CreatedAtDateTime}");
                    result.Add($"   Profile Image: {employee.ProfileImageUrl ?? "None"}");
                }
                else
                {
                    result.Add($"\n❌ Employee not found");
                }

                // Test each subcollection
                var skills = await _firestoreService.GetEmployeeSkillsAsync(id);
                var qualifications = await _firestoreService.GetEmployeeQualificationsAsync(id);
                var trainings = await _firestoreService.GetEmployeeTrainingsAsync(id);

                result.Add($"\n📊 Subcollection Results:");
                result.Add($"   Skills: {skills.Count} documents");
                result.Add($"   Qualifications: {qualifications.Count} documents");
                result.Add($"   Trainings: {trainings.Count} documents");

                // Show skill details
                result.Add($"\n🛠️ Skills:");
                foreach (var skill in skills)
                {
                    result.Add($"   - {skill.Name} (Level: {skill.Level}, Exp: {skill.YearsOfExperience} yrs, Category: {skill.Category})");
                }

                // Show qualification details
                result.Add($"\n🎓 Qualifications:");
                foreach (var qual in qualifications)
                {
                    result.Add($"   - {qual.Degree} from {qual.Institution} ({qual.YearCompleted}) - Field: {qual.FieldOfStudy}");
                }

                // Show training details
                result.Add($"\n📚 Trainings:");
                foreach (var training in trainings)
                {
                    result.Add($"   - {training.Title} by {training.Provider} (Status: {training.Status})");
                    result.Add($"     Start: {training.StartDate}, End: {training.EndDate}");
                    if (!string.IsNullOrEmpty(training.Description))
                    {
                        result.Add($"     Description: {training.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Add($"\n❌ Error: {ex.Message}");
                result.Add($"Stack: {ex.StackTrace}");
            }

            return Content(string.Join("\n", result), "text/plain");
        }


        // GET: Employee Skills Dashboard
        // GET: Employee Skills Dashboard
        public async Task<IActionResult> EmployeeSkills()
        {
            try
            {
                // Return the EmployeeSkillViewModel instead of List<Employee>
                var viewModel = await _firestoreService.GetEmployeeSkillsViewModel();
                ViewData["Title"] = "Employee Skills Dashboard";
                ViewData["Subtitle"] = "View and manage employee skills and training";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading employee skills: {ex.Message}";
                return View(new EmployeeSkillViewModel()); // Return empty view model
            }
        }

        public async Task<IActionResult> SkillsMatrix()
        {
            try
            {
                var matrix = await _firestoreService.GetSkillsMatrix();
                return View(matrix);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in SkillsMatrix: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading skills matrix.";
                return View(new List<SkillsMatrix>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeSkills(string employeeId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                {
                    return Json(new { success = false, message = "Employee ID is required." });
                }
                var skills = await _firestoreService.GetEmployeeSkillsAsync(employeeId);
                return Json(new { success = true, skills = skills });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetEmployeeSkills: {ex.Message}");
                return Json(new { success = false, message = "Error loading employee skills." });
            }
        }

      

        public async Task<IActionResult> Index()
        {
            try
            {
                var employees = await _firestoreService.GetAllEmployeesAsync();
                ViewData["Title"] = "Employees";
                ViewData["Subtitle"] = "Manage all employees";
                return View(employees);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading employees: {ex.Message}";
                return View(new List<Employee>());
            }
        }
        public async Task<IActionResult> EmployeeProfiles()
        {
            Console.WriteLine("EmployeeProfiles action called");
            try
            {
                var employees = await _firestoreService.GetAllEmployeesWithSkills();
                Console.WriteLine($"Loaded {employees.Count} employees with skills");
                return View(employees);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in EmployeeProfiles: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading employee profiles.";
                return View(new List<Employee>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> ExportEmployeeSkills()
        {
            try
            {
                var model = await _firestoreService.GetEmployeeSkillsViewModel();
                // Implement Excel export logic here
                // Return FileResult with Excel content
                return File(new byte[0], "application/vnd.ms-excel", "employee-skills.xlsx");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex+ "Error exporting employee skills");
                return BadRequest("Error exporting data");
            }
        }
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> DebugEmployeeData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Content("Please provide employee ID: /Employee/DebugEmployeeData?id=EMPLOYEE_ID");
            }

            var result = new List<string>();
            try
            {
                result.Add($"🔍 Debugging employee: {id}");
                result.Add($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                result.Add($"UTC Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

                var employee = await _firestoreService.GetEmployeeWithAllDataAsync(id);
                if (employee != null)
                {
                    result.Add($"\n✅ Employee Found:");
                    result.Add($"   Name: {employee.FullName}");
                    result.Add($"   Email: {employee.Email}");
                    result.Add($"   Department: {employee.Department}");
                    result.Add($"   Position: {employee.Position}");
                    result.Add($"   UserId: {employee.UserId}");
                    result.Add($"   EmployeeId: {employee.EmployeeId}");
                    result.Add($"   Hire Date (ms): {employee.HireDate}");
                    result.Add($"   Hire Date: {employee.HireDateDateTime:yyyy-MM-dd}");
                    result.Add($"   Created At (ms): {employee.CreatedAt}");
                    result.Add($"   Created At: {employee.CreatedAtDateTime:yyyy-MM-dd}");
                    result.Add($"   Profile Image: {employee.ProfileImageUrl ?? "None"}");
                    result.Add($"   Is Active: {employee.IsActive}");

                    result.Add($"\n📊 Calculated Metrics:");
                    result.Add($"   Average Skill Level: {employee.AverageSkillLevel:F1}");
                    result.Add($"   Total Skills Gap: {employee.TotalSkillsGap}");
                    result.Add($"   Critical Gaps: {employee.CriticalGapsCount}");

                    result.Add($"\n🛠️ Skills ({employee.Skills.Count}):");
                    foreach (var skill in employee.Skills)
                    {
                        result.Add($"   - {skill.Name} (Level: {skill.Level}, Exp: {skill.YearsOfExperience} yrs, Category: {skill.Category})");
                    }

                    result.Add($"\n🎓 Qualifications ({employee.Qualifications.Count}):");
                    foreach (var qual in employee.Qualifications)
                    {
                        result.Add($"   - {qual.Degree} from {qual.Institution} ({qual.YearCompleted}) - Field: {qual.FieldOfStudy}");
                    }

                    result.Add($"\n📚 Trainings ({employee.Trainings.Count}):");
                    foreach (var training in employee.Trainings)
                    {
                        result.Add($"   - {training.Title} by {training.Provider} (Status: {training.Status})");
                        result.Add($"     Start: {training.StartDate} -> {training.StartDateDateTime():yyyy-MM-dd}");
                        result.Add($"     End: {training.EndDate} -> {training.EndDateDateTime():yyyy-MM-dd}");
                        result.Add($"     Created: {training.CreatedAt} -> {training.CreatedAtDateTime():yyyy-MM-dd}");
                    }
                }
                else
                {
                    result.Add($"\n❌ Employee not found");
                }
            }
            catch (Exception ex)
            {
                result.Add($"\n❌ Error: {ex.Message}");
                result.Add($"Stack: {ex.StackTrace}");
            }

            return Content(string.Join("\n", result), "text/plain");
        }

        // Your existing methods remain the s

        [HttpGet]
        public async Task<IActionResult> DebugEmployeeFields()
        {
            Console.WriteLine("DebugEmployeeFields action called");
            try
            {
                var debugInfo = await _firestoreService.DebugEmployeeFields();
                return Content(debugInfo, "text/plain");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in DebugEmployeeFields: {ex.Message}");
                return Content($"Error debugging employee fields: {ex.Message}", "text/plain");
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            Console.WriteLine("TestConnection action called");
            try
            {
                var result = await _firestoreService.TestAdminCollectionAsync();
                if (result)
                {
                    return Json(new { success = true, message = "Firestore connection is working correctly!" });
                }
                else
                {
                    return Json(new { success = false, message = "Firestore connection test failed." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in TestConnection: {ex.Message}");
                return Json(new { success = false, message = $"Connection test failed: {ex.Message}" });
            }
        }
        // Primary method for employee details
        //public async Task<IActionResult> Details(string id)
        //{
        //    try
        //    {
        //        Console.WriteLine($"🔍 Loading details for employee: {id}");

        //        if (string.IsNullOrEmpty(id))
        //        {
        //            TempData["ErrorMessage"] = "Employee ID is required";
        //            return RedirectToAction("EmployeeSkills");
        //        }

        //        var employee = await _firestoreService.GetEmployeeWithAllDataAsync(id);
        //        if (employee == null)
        //        {
        //            Console.WriteLine($"❌ Employee {id} not found");
        //            TempData["ErrorMessage"] = $"Employee with ID {id} not found";
        //            return RedirectToAction("EmployeeSkills");
        //        }

        //        Console.WriteLine($"✅ Loaded employee details: {employee.FirstName} {employee.LastName}");
        //        Console.WriteLine($"   - Skills: {employee.Skills.Count}");
        //        Console.WriteLine($"   - Qualifications: {employee.Qualifications.Count}");
        //        Console.WriteLine($"   - Trainings: {employee.Trainings.Count}");

        //        return View(employee);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error loading employee details: {ex.Message}");
        //        TempData["ErrorMessage"] = $"Error loading employee details: {ex.Message}";
        //        return RedirectToAction("EmployeeSkills");
        //    }
        //}
        [HttpPost]
        public async Task<IActionResult> UploadCertificate(string employeeId, string trainingId, IFormFile certificateFile)
        {
            try
            {
                if (certificateFile == null || certificateFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a certificate file to upload.";
                    return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(certificateFile.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Only PDF, JPG, JPEG, and PNG files are allowed.";
                    return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
                }

                // Validate file size (5MB max)
                if (certificateFile.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Certificate file size must be less than 5MB.";
                    return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
                }

                // Upload to Firebase Storage
                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
                var fileName = $"certificate_{trainingId}_{Guid.NewGuid()}{fileExtension}";
                var downloadUrl = await storageService.UploadFileAsync(certificateFile, $"certificates/{employeeId}/{fileName}");

                // Update training record
                var success = await _firestoreService.UpdateTrainingCertificateAsync(
                    employeeId, trainingId, certificateFile.FileName, downloadUrl);

                if (success)
                {
                    TempData["SuccessMessage"] = "Certificate uploaded successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update training record with certificate.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error uploading certificate: {ex.Message}");
                TempData["ErrorMessage"] = $"Error uploading certificate: {ex.Message}";
            }

            return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
        }
        // Error view
        public IActionResult Error()
        {
            return View();
        }
        public async Task<IActionResult> TrainingDetails(string employeeId, string trainingId)
        {
            if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(trainingId))
            {
                TempData["ErrorMessage"] = "Employee ID and Training ID are required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var training = await _firestoreService.GetTrainingAsync(employeeId, trainingId);
                if (training == null)
                {
                    TempData["ErrorMessage"] = "Training not found";
                    return RedirectToAction("Details", new { id = employeeId });
                }

                ViewData["Title"] = $"Training - {training.Title}";
                return View(training);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading training details: {ex.Message}";
                return RedirectToAction("Details", new { id = employeeId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsComplete(string employeeId, string trainingId)
        {
            try
            {
                var success = await _firestoreService.UpdateTrainingProgressAsync(employeeId, trainingId, 100, "Completed");

                if (success)
                {
                    TempData["SuccessMessage"] = "Training marked as completed!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update training status.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
        }
        // Add to EmployeeController.cs
        [HttpGet]
        public async Task<IActionResult> EmployeeDocuments(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Employee ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                Console.WriteLine($"🔍 Loading documents for employee: {id}");

                var employee = await _firestoreService.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction(nameof(Index));
                }

                // Get storage service
                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();

                // Get documents from both Firestore and direct storage
                var firestoreDocumentsTask = _firestoreService.GetEmployeeStorageDocumentsAsync(id);
                var storageFilesTask = storageService.ListEmployeeDocumentsAsync(id);

                await Task.WhenAll(firestoreDocumentsTask, storageFilesTask);

                var viewModel = new EmployeeDocumentsViewModel
                {
                    Employee = employee,
                    FirestoreDocuments = firestoreDocumentsTask.Result,
                    StorageFiles = storageFilesTask.Result,
                   // TotalDocuments = firestoreDocumentsTask.Result.Count + storageFilesTask.Result.Count
                };

                ViewData["Title"] = $"Employee Documents - {employee.FullName}";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading employee documents: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading employee documents: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string storagePath, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(storagePath))
                {
                    TempData["ErrorMessage"] = "Storage path is required";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"📥 Downloading document: {storagePath}");

                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();

                // Check if file exists
                var fileExists = await storageService.FileExistsAsync(storagePath);
                if (!fileExists)
                {
                    TempData["ErrorMessage"] = "File not found in storage";
                    return RedirectToAction(nameof(Index));
                }

                var fileBytes = await storageService.DownloadFileBytesAsync(storagePath);

                // Determine content type
                var contentType = GetContentType(fileName);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading document: {ex.Message}");
                TempData["ErrorMessage"] = $"Error downloading document: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewDocument(string storagePath, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(storagePath))
                {
                    TempData["ErrorMessage"] = "Storage path is required";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"👀 Viewing document: {storagePath}");

                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();

                // Check if file exists
                var fileExists = await storageService.FileExistsAsync(storagePath);
                if (!fileExists)
                {
                    TempData["ErrorMessage"] = "File not found in storage";
                    return RedirectToAction(nameof(Index));
                }

                var downloadUrl = await storageService.GetDownloadUrlAsync(storagePath);

                // For PDFs, we can display in browser
                if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.DocumentUrl = downloadUrl;
                    ViewBag.FileName = fileName;
                    return View("PdfViewer");
                }

                // For images, redirect to download URL
                return Redirect(downloadUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error viewing document: {ex.Message}");
                TempData["ErrorMessage"] = $"Error viewing document: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        // Add to EmployeeController.cs
        [HttpGet]
        public async Task<IActionResult> DownloadQualificationDocument(string employeeId, string qualificationId, string documentType)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(qualificationId))
                {
                    TempData["ErrorMessage"] = "Employee ID and Qualification ID are required";
                    return RedirectToAction(nameof(Details), new { id = employeeId });
                }

                // Get the qualification
                var qualifications = await _firestoreService.GetEmployeeQualificationsAsync(employeeId);
                var qualification = qualifications.FirstOrDefault(q => q.Id == qualificationId);

                if (qualification == null)
                {
                    TempData["ErrorMessage"] = "Qualification not found";
                    return RedirectToAction(nameof(Details), new { id = employeeId });
                }

                string downloadUrl = null;
                string fileName = null;

                if (documentType == "certificate" && !string.IsNullOrEmpty(qualification.CertificatePdfUrl))
                {
                    downloadUrl = qualification.CertificatePdfUrl;
                    fileName = qualification.CertificateFileName ?? $"{qualification.Degree}_certificate.pdf";
                }
                else if (documentType == "transcript" && !string.IsNullOrEmpty(qualification.TranscriptPdfUrl))
                {
                    downloadUrl = qualification.TranscriptPdfUrl;
                    fileName = qualification.TranscriptFileName ?? $"{qualification.Degree}_transcript.pdf";
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    TempData["ErrorMessage"] = "Document not found";
                    return RedirectToAction(nameof(Details), new { id = employeeId });
                }

                // Download the file
                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(downloadUrl);

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading qualification document: {ex.Message}");
                TempData["ErrorMessage"] = $"Error downloading document: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }
        }
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
        // Add to EmployeeController.cs
        [HttpGet]
        public async Task<IActionResult> AddQualification(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                TempData["ErrorMessage"] = "Employee ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction(nameof(Index));
                }

                var qualification = new Qualification { EmployeeId = employeeId };
                ViewBag.Employee = employee;
                return View(qualification);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading employee: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddQualification(Qualification qualification)
        {
            if (!ModelState.IsValid)
            {
                var employee = await _firestoreService.GetEmployeeByIdAsync(qualification.EmployeeId);
                ViewBag.Employee = employee;
                return View(qualification);
            }

            try
            {
                qualification.SetCreatedAt(DateTime.UtcNow);
                var success = await _firestoreService.AddQualificationAsync(qualification);

                if (success)
                {
                    TempData["SuccessMessage"] = "Qualification added successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add qualification.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error adding qualification: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = qualification.EmployeeId });
        }
        [HttpGet]
        public async Task<IActionResult> EditQualification(string employeeId, string qualificationId)
        {
            if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(qualificationId))
            {
                TempData["ErrorMessage"] = "Employee ID and Qualification ID are required";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }

            try
            {
                var qualifications = await _firestoreService.GetEmployeeQualificationsAsync(employeeId);
                var qualification = qualifications.FirstOrDefault(q => q.Id == qualificationId);

                if (qualification == null)
                {
                    TempData["ErrorMessage"] = "Qualification not found";
                    return RedirectToAction(nameof(Details), new { id = employeeId });
                }

                var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
                ViewBag.Employee = employee;
                return View(qualification);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading qualification: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditQualification(Qualification qualification)
        {
            if (!ModelState.IsValid)
            {
                var employee = await _firestoreService.GetEmployeeByIdAsync(qualification.EmployeeId);
                ViewBag.Employee = employee;
                return View(qualification);
            }

            try
            {
                var success = await _firestoreService.UpdateQualificationAsync(qualification);

                if (success)
                {
                    TempData["SuccessMessage"] = "Qualification updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update qualification.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating qualification: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = qualification.EmployeeId });
        }

        [HttpGet]
        public async Task<IActionResult> UploadQualificationDocument(string employeeId, string qualificationId = null)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                TempData["ErrorMessage"] = "Employee ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction(nameof(Index));
                }

                var qualifications = await _firestoreService.GetEmployeeQualificationsAsync(employeeId);
                ViewBag.Employee = employee;
                ViewBag.Qualifications = qualifications;
                ViewBag.SelectedQualificationId = qualificationId;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading qualifications: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadQualificationDocument(string employeeId, string qualificationId,
            IFormFile certificateFile, IFormFile transcriptFile)
        {
            if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(qualificationId))
            {
                TempData["ErrorMessage"] = "Employee ID and Qualification ID are required";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }

            try
            {
                var qualifications = await _firestoreService.GetEmployeeQualificationsAsync(employeeId);
                var qualification = qualifications.FirstOrDefault(q => q.Id == qualificationId);

                if (qualification == null)
                {
                    TempData["ErrorMessage"] = "Qualification not found";
                    return RedirectToAction(nameof(Details), new { id = employeeId });
                }

                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
                var updatedQualification = false;

                // Upload certificate
                if (certificateFile != null && certificateFile.Length > 0)
                {
                    var certificateFileName = $"certificate_{qualificationId}_{Guid.NewGuid()}{Path.GetExtension(certificateFile.FileName)}";
                    var certificateUrl = await storageService.UploadFileAsync(certificateFile, $"qualifications/{employeeId}/{certificateFileName}");

                    qualification.CertificatePdfUrl = certificateUrl;
                    qualification.CertificateFileName = certificateFile.FileName;
                    updatedQualification = true;
                }

                // Upload transcript
                if (transcriptFile != null && transcriptFile.Length > 0)
                {
                    var transcriptFileName = $"transcript_{qualificationId}_{Guid.NewGuid()}{Path.GetExtension(transcriptFile.FileName)}";
                    var transcriptUrl = await storageService.UploadFileAsync(transcriptFile, $"qualifications/{employeeId}/{transcriptFileName}");

                    qualification.TranscriptPdfUrl = transcriptUrl;
                    qualification.TranscriptFileName = transcriptFile.FileName;
                    updatedQualification = true;
                }

                if (updatedQualification)
                {
                    var success = await _firestoreService.UpdateQualificationAsync(qualification);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Qualification documents uploaded successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update qualification with document links.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No files were selected for upload.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading documents: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = employeeId });
        }
        // Add to EmployeeController.cs
        [HttpPost]
        public async Task<IActionResult> UploadTrainingCertificate(string employeeId, string trainingId, IFormFile certificateFile)
        {
            try
            {
                if (certificateFile == null || certificateFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a certificate file to upload.";
                    return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
                }

                // Upload to Firebase Storage
                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
                var downloadUrl = await storageService.UploadTrainingCertificateAsync(certificateFile, employeeId, trainingId);

                // Update training record - FIXED: Use the correct method
                var success = await _firestoreService.UpdateTrainingCertificateAsync(
                    employeeId, trainingId, certificateFile.FileName, downloadUrl);

                if (success)
                {
                    TempData["SuccessMessage"] = "Training certificate uploaded successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update training record with certificate.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error uploading training certificate: {ex.Message}");
                TempData["ErrorMessage"] = $"Error uploading certificate: {ex.Message}";
            }

            return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
        }


        [HttpGet]
        public async Task<IActionResult> DownloadTrainingCertificate(string employeeId, string trainingId, string fileName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(trainingId))
                {
                    TempData["ErrorMessage"] = "Employee ID and Training ID are required";
                    return RedirectToAction(nameof(Index));
                }

                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
                var fileStream = await storageService.DownloadTrainingCertificateAsync(employeeId, trainingId, fileName);

                // Get training details for filename
                var trainingService = HttpContext.RequestServices.GetService<TrainingService>();
                var training = await trainingService.GetTrainingByIdAsync(trainingId);

                var downloadFileName = training?.CertificateFileName ??
                                      $"{training?.Title?.Replace(" ", "_")}_certificate.pdf";

                return File(fileStream, "application/octet-stream", downloadFileName);
            }
            catch (FileNotFoundException ex)
            {
                TempData["ErrorMessage"] = "Certificate not found. Please upload a certificate first.";
                return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading training certificate: {ex.Message}");
                TempData["ErrorMessage"] = $"Error downloading certificate: {ex.Message}";
                return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewTrainingCertificate(string employeeId, string trainingId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(trainingId))
                {
                    TempData["ErrorMessage"] = "Employee ID and Training ID are required";
                    return RedirectToAction(nameof(Index));
                }

                // Get training to check if certificate exists
                var trainingService = HttpContext.RequestServices.GetService<TrainingService>();
                var training = await trainingService.GetTrainingByIdAsync(trainingId);

                if (training == null)
                {
                    TempData["ErrorMessage"] = "Training not found";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrEmpty(training.CertificateUrl) && string.IsNullOrEmpty(training.CertificatePdfUrl))
                {
                    TempData["ErrorMessage"] = "No certificate available for this training";
                    return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
                }

                // If it's a PDF, display in viewer, otherwise redirect to URL
                var certificateUrl = training.CertificatePdfUrl ?? training.CertificateUrl;

                if (certificateUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                    certificateUrl.Contains(".pdf"))
                {
                    ViewBag.DocumentUrl = certificateUrl;
                    ViewBag.FileName = training.CertificateFileName ?? $"{training.Title} Certificate";
                    ViewBag.Training = training;
                    return View("PdfViewer");
                }

                // For images or other files, redirect to the URL
                return Redirect(certificateUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error viewing training certificate: {ex.Message}");
                TempData["ErrorMessage"] = $"Error viewing certificate: {ex.Message}";
                return RedirectToAction("TrainingDetails", new { employeeId, trainingId });
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> TrainingCertificates(string employeeId)
        //{
        //    if (string.IsNullOrEmpty(employeeId))
        //    {
        //        TempData["ErrorMessage"] = "Employee ID is required";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    try
        //    {
        //        var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
        //        if (employee == null)
        //        {
        //            TempData["ErrorMessage"] = "Employee not found";
        //            return RedirectToAction(nameof(Index));
        //        }

        //        var trainingService = HttpContext.RequestServices.GetService<TrainingService>();
        //        var certificates = await trainingService.GetTrainingsWithCertificatesAsync(employeeId);

        //        var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
        //        var storageFiles = await storageService.ListTrainingCertificatesAsync(employeeId);

        //        ViewBag.Employee = employee;
        //        ViewBag.Certificates = certificates;
        //        ViewBag.StorageFiles = storageFiles;

        //        return View();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error loading training certificates: {ex.Message}");
        //        TempData["ErrorMessage"] = $"Error loading certificates: {ex.Message}";
        //        return RedirectToAction("Details", new { id = employeeId });
        //    }
        //}
        [HttpGet]
        public async Task<IActionResult> TrainingCertificates(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                TempData["ErrorMessage"] = "Employee ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction(nameof(Index));
                }

                var trainingService = HttpContext.RequestServices.GetService<TrainingService>();
                var certificates = await trainingService.GetFlutterTrainingsWithCertificatesAsync(employeeId);

                // Ensure we never return null to the view
                certificates ??= new List<Training>();

                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
                var storageFiles = await storageService.ListTrainingCertificatesAsync(employeeId);

                ViewBag.Employee = employee;
                ViewBag.Certificates = certificates;
                ViewBag.StorageFiles = storageFiles;

                return View(certificates);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading training certificates: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading certificates: {ex.Message}";
                return RedirectToAction("Details", new { id = employeeId });
            }
        }
        [HttpGet]
        public async Task<IActionResult> FlutterTrainingDocuments(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Employee ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                Console.WriteLine($"🔍 Loading Flutter training documents for employee: {id}");

                var employee = await _firestoreService.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction(nameof(Index));
                }

                // Get trainings from Flutter subcollection
                var trainingService = HttpContext.RequestServices.GetService<TrainingService>();
                var flutterTrainings = await trainingService.GetFlutterTrainingsWithCertificatesAsync(id);

                // Get storage files
                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
                var storageFiles = await storageService.ListTrainingCertificatesAsync(id);

                var viewModel = new FlutterTrainingDocumentsViewModel
                {
                    Employee = employee,
                    Trainings = flutterTrainings,
                    StorageFiles = storageFiles,
                    TotalCertificates = flutterTrainings.Count,
                    TotalStorageFiles = storageFiles.Count
                };

                ViewData["Title"] = $"Flutter Training Documents - {employee.FullName}";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading Flutter training documents: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading training documents: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncFlutterDocuments(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Employee ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var trainingService = HttpContext.RequestServices.GetService<TrainingService>();
                var success = await trainingService.SyncFlutterTrainingsToMainCollectionAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Flutter documents synchronized successfully with main system!";
                }
                else
                {
                    TempData["WarningMessage"] = "No new Flutter documents found to sync.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error syncing Flutter documents: {ex.Message}");
                TempData["ErrorMessage"] = $"Error syncing documents: {ex.Message}";
            }

            return RedirectToAction(nameof(FlutterTrainingDocuments), new { id = id });
        }
        [HttpGet]
        public async Task<IActionResult> DownloadEmployeeDocument(string employeeId, string documentType, string documentId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                {
                    TempData["ErrorMessage"] = "Employee ID is required";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"📥 Downloading {documentType} document for employee: {employeeId}");

                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
                byte[] fileBytes = null;
                string fileName = null;
                string contentType = "application/octet-stream";

                switch (documentType.ToLower())
                {
                    case "training-certificate":
                        var trainingService = HttpContext.RequestServices.GetService<TrainingService>();
                        var training = await trainingService.GetTrainingByIdAsync(documentId);
                        if (training != null && !string.IsNullOrEmpty(training.CertificatePdfUrl))
                        {
                            fileBytes = await storageService.DownloadFileBytesAsync(ExtractStoragePath(training.CertificatePdfUrl));
                            fileName = training.CertificateFileName ?? $"{training.Title}_Certificate.pdf";
                            contentType = "application/pdf";
                        }
                        break;

                    case "qualification-certificate":
                        var qualifications = await _firestoreService.GetEmployeeQualificationsAsync(employeeId);
                        var qualification = qualifications.FirstOrDefault(q => q.Id == documentId);
                        if (qualification != null && !string.IsNullOrEmpty(qualification.CertificatePdfUrl))
                        {
                            fileBytes = await storageService.DownloadFileBytesAsync(ExtractStoragePath(qualification.CertificatePdfUrl));
                            fileName = qualification.CertificateFileName ?? $"{qualification.Degree}_Certificate.pdf";
                            contentType = "application/pdf";
                        }
                        break;

                    case "transcript":
                        var qualifications2 = await _firestoreService.GetEmployeeQualificationsAsync(employeeId);
                        var qualification2 = qualifications2.FirstOrDefault(q => q.Id == documentId);
                        if (qualification2 != null && !string.IsNullOrEmpty(qualification2.TranscriptPdfUrl))
                        {
                            fileBytes = await storageService.DownloadFileBytesAsync(ExtractStoragePath(qualification2.TranscriptPdfUrl));
                            fileName = qualification2.TranscriptFileName ?? $"{qualification2.Degree}_Transcript.pdf";
                            contentType = "application/pdf";
                        }
                        break;

                    default:
                        TempData["ErrorMessage"] = "Invalid document type";
                        return RedirectToAction(nameof(Details), new { id = employeeId });
                }

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    TempData["ErrorMessage"] = "Document not found or empty";
                    return RedirectToAction(nameof(Details), new { id = employeeId });
                }

                Console.WriteLine($"✅ Downloading: {fileName} ({fileBytes.Length} bytes)");
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error downloading document: {ex.Message}");
                TempData["ErrorMessage"] = $"Error downloading document: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAllCertificates(string employeeId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                {
                    TempData["ErrorMessage"] = "Employee ID is required";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"📦 Creating zip archive for all certificates of employee: {employeeId}");

                var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction(nameof(Index));
                }

                var storageService = HttpContext.RequestServices.GetService<FirebaseStorageService>();
                var trainingService = HttpContext.RequestServices.GetService<TrainingService>();

                using var memoryStream = new MemoryStream();
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    // Add training certificates
                    var trainings = await trainingService.GetFlutterTrainingsWithCertificatesAsync(employeeId);
                    foreach (var training in trainings)
                    {
                        if (!string.IsNullOrEmpty(training.CertificatePdfUrl))
                        {
                            try
                            {
                                var fileBytes = await storageService.DownloadFileBytesAsync(ExtractStoragePath(training.CertificatePdfUrl));
                                var fileName = $"Trainings/{training.Title}_Certificate.pdf";
                                var entry = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.Optimal);
                                using var entryStream = entry.Open();
                                await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Could not add training certificate {training.Title}: {ex.Message}");
                            }
                        }
                    }

                    // Add qualification certificates
                    var qualifications = await _firestoreService.GetEmployeeQualificationsAsync(employeeId);
                    foreach (var qualification in qualifications)
                    {
                        if (!string.IsNullOrEmpty(qualification.CertificatePdfUrl))
                        {
                            try
                            {
                                var fileBytes = await storageService.DownloadFileBytesAsync(ExtractStoragePath(qualification.CertificatePdfUrl));
                                var fileName = $"Qualifications/{qualification.Degree}_Certificate.pdf";
                                var entry = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.Optimal);
                                using var entryStream = entry.Open();
                                await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Could not add qualification certificate {qualification.Degree}: {ex.Message}");
                            }
                        }

                        if (!string.IsNullOrEmpty(qualification.TranscriptPdfUrl))
                        {
                            try
                            {
                                var fileBytes = await storageService.DownloadFileBytesAsync(ExtractStoragePath(qualification.TranscriptPdfUrl));
                                var fileName = $"Qualifications/{qualification.Degree}_Transcript.pdf";
                                var entry = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.Optimal);
                                using var entryStream = entry.Open();
                                await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Could not add transcript {qualification.Degree}: {ex.Message}");
                            }
                        }
                    }
                }

                memoryStream.Position = 0;
                var zipFileName = $"{employee.FirstName}_{employee.LastName}_Certificates_{DateTime.Now:yyyyMMdd}.zip";

                Console.WriteLine($"✅ Created zip archive: {zipFileName}");
                return File(memoryStream.ToArray(), "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating zip archive: {ex.Message}");
                TempData["ErrorMessage"] = $"Error creating download package: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }
        }

        // Helper method to extract storage path from URL
        private string ExtractStoragePath(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return string.Empty;

                if (url.Contains("firebasestorage.googleapis.com"))
                {
                    var uri = new Uri(url);
                    var path = uri.AbsolutePath;
                    if (path.StartsWith("/v0/b/"))
                    {
                        var parts = path.Split('/');
                        if (parts.Length > 5)
                        {
                            var storagePath = string.Join("/", parts.Skip(5));
                            storagePath = Uri.UnescapeDataString(storagePath);
                            return storagePath;
                        }
                    }
                }

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
    }
    
}