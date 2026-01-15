using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Models;
using WebApplication2.Models.Support_Model;
using WebApplication2.Models.ViewModel;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class TrainingController : Controller
    {
        private readonly TrainingService _trainingService;
        private readonly FirestoreService _firestoreService;
        private readonly FirebaseStorageService _firebaseStorageService;
        private readonly FirestoreDb _firestoreDb;

        public TrainingController(
         TrainingService trainingService,
         FirestoreService firestoreService,
         FirebaseStorageService firebaseStorageService) // Add this parameter
        {
            _trainingService = trainingService;
            _firestoreService = firestoreService;
            _firebaseStorageService = firebaseStorageService; // Assign it
            _firestoreDb = firestoreService.GetFirestoreDb();
        }

        #region Training Programs

        // GET: Training Programs Overview
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all trainings with employee data
                var trainings = await _trainingService.GetAllTrainingsAsync();

                // Load employee data for each training
                foreach (var training in trainings)
                {
                    if (!string.IsNullOrEmpty(training.EmployeeId) && training.Employee == null)
                    {
                        training.Employee = await _firestoreService.GetEmployeeByIdAsync(training.EmployeeId);
                    }
                }

                ViewData["Title"] = "Training Management";
                ViewData["Subtitle"] = "Manage and track all training activities";
                return View(trainings);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading trainings: {ex.Message}";
                return View(new List<Training>());
            }
        }

        // GET: Create Training Program
        public IActionResult CreateProgram()
        {
            ViewData["Title"] = "Create Training Program";
            ViewData["Subtitle"] = "Add new training program to library";
            return View(new TrainingProgram());
        }

        // POST: Create Training Program
        // POST: Create Training Program
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProgram(TrainingProgram program)
        {
            Console.WriteLine($"📥 Received TrainingProgram data:");
            Console.WriteLine($"   Title: {program.Title}");
            Console.WriteLine($"   Description: {program.Description}");
            Console.WriteLine($"   Provider: {program.Provider}");
            Console.WriteLine($"   Category: {program.Category}");
            Console.WriteLine($"   Duration: {program.Duration}");
            Console.WriteLine($"   CoveredSkills Count: {program.CoveredSkills?.Count ?? 0}");

            // Debug ModelState
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"❌ ModelState is invalid:");
                foreach (var state in ModelState)
                {
                    Console.WriteLine($"   Field: {state.Key}");
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"     - {error.ErrorMessage}");
                    }
                }
            }

            // Manually validate required fields to provide better error messages
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(program.Title))
                validationErrors.Add("Title is required");

            if (string.IsNullOrWhiteSpace(program.Description))
                validationErrors.Add("Description is required");

            if (string.IsNullOrWhiteSpace(program.Provider))
                validationErrors.Add("Provider is required");

            if (program.Duration <= 0)
                validationErrors.Add("Duration must be at least 1 hour");

            if (validationErrors.Any())
            {
                foreach (var error in validationErrors)
                {
                    ModelState.AddModelError("", error);
                }
                TempData["ErrorMessage"] = "Please fix the validation errors.";

                ViewData["Title"] = "Create Training Program";
                ViewData["Subtitle"] = "Add new training program to library";
                return View(program);
            }

            // If we get here, basic validation passed
            try
            {
                // Ensure CoveredSkills is not null
                program.CoveredSkills ??= new List<string>();

                var programId = await _trainingService.CreateTrainingProgramAsync(program);
                Console.WriteLine($"✅ Training program created successfully with ID: {programId}");

                TempData["SuccessMessage"] = "Training program created successfully!";
                return RedirectToAction(nameof(ProgramsLibrary));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating training program: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"Error creating training program: {ex.Message}";

                ViewData["Title"] = "Create Training Program";
                ViewData["Subtitle"] = "Add new training program to library";
                return View(program);
            }
        }

        // GET: Create Training (Flutter compatible)
        public async Task<IActionResult> CreateTraining()
        {
            try
            {
                var employees = await _firestoreService.GetAllEmployeesAsync();
                var programs = await _trainingService.GetAllTrainingProgramsAsync();

                var viewModel = new TrainingViewModel
                {
                    Employees = employees ?? new List<Employee>(),
                    TrainingPrograms = programs ?? new List<TrainingProgram>()
                };

                ViewData["Title"] = "Create Training";
                ViewData["Subtitle"] = "Add new training record";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading training form: {ex.Message}";
                return View(new TrainingViewModel());
            }
        }

        // POST: Create Training (Flutter compatible)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTraining(TrainingViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Convert ViewModel to Training model with proper date handling
                    var training = new Training
                    {
                        EmployeeId = viewModel.EmployeeId,
                        Title = viewModel.Title,
                        Provider = viewModel.Provider,
                        Description = viewModel.Description,
                        Status = viewModel.Status ?? "Pending",
                        CertificateUrl = viewModel.CertificateUrl ?? "",
                        CertificatePdfUrl = viewModel.CertificatePdfUrl,
                        CertificateFileName = viewModel.CertificateFileName,
                        Progress = viewModel.Progress,
                        TrainingProgramId = viewModel.TrainingProgramId,
                        AssignedBy = viewModel.AssignedBy ?? "admin",
                        AssignedReason = viewModel.AssignedReason
                    };

                    // Set dates using the new methods that convert to milliseconds
                    training.SetStartDate(viewModel.StartDate);
                    training.SetEndDate(viewModel.EndDate);
                    training.SetCreatedAt(DateTime.UtcNow);

                    if (viewModel.AssignedDate.HasValue)
                        training.SetAssignedDate(viewModel.AssignedDate.Value);

                    if (viewModel.CompletedDate.HasValue)
                        training.SetCompletedDate(viewModel.CompletedDate.Value);

                    // Use the FirestoreDb directly to create training
                    await CreateFlutterCompatibleTrainingAsync(training);

                    TempData["SuccessMessage"] = "Training created successfully!";
                    return RedirectToAction(nameof(TrainingsList));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating training: {ex.Message}";
                }
            }

            // Reload dropdown data if validation fails
            var employees = await _firestoreService.GetAllEmployeesAsync();
            var programs = await _trainingService.GetAllTrainingProgramsAsync();

            viewModel.Employees = employees ?? new List<Employee>();
            viewModel.TrainingPrograms = programs ?? new List<TrainingProgram>();

            ViewData["Title"] = "Create Training";
            ViewData["Subtitle"] = "Add new training record";
            return View(viewModel);
        }

        // GET: Trainings List (Flutter compatible view)
        public async Task<IActionResult> TrainingsList()
        {
            try
            {
                // Get all trainings from main collection
                var trainingsCollection = _firestoreDb.Collection("trainings");
                var snapshot = await trainingsCollection.GetSnapshotAsync();

                var trainings = new List<Training>();
                foreach (var document in snapshot.Documents)
                {
                    try
                    {
                        var training = document.ConvertTo<Training>();
                        training.Id = document.Id;

                        // Load employee data
                        if (!string.IsNullOrEmpty(training.EmployeeId))
                        {
                            training.Employee = await _firestoreService.GetEmployeeByIdAsync(training.EmployeeId);
                        }

                        // Load program data
                        if (!string.IsNullOrEmpty(training.TrainingProgramId))
                        {
                            training.TrainingProgram = await _trainingService.GetTrainingProgramByIdAsync(training.TrainingProgramId);
                        }

                        trainings.Add(training);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing training document: {ex.Message}");
                    }
                }

                ViewData["Title"] = "All Trainings";
                ViewData["Subtitle"] = "View all training records";
                return View(trainings);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading trainings: {ex.Message}";
                return View(new List<Training>());
            }
        }

        // GET: Training Programs Library
        public async Task<IActionResult> ProgramsLibrary()
        {
            try
            {
                var programs = await _trainingService.GetAllTrainingProgramsAsync();
                ViewData["Title"] = "Training Programs Library";
                ViewData["Subtitle"] = "Browse available training programs";
                return View(programs);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading training programs: {ex.Message}";
                return View(new List<TrainingProgram>());
            }
        }

        // GET: Training Program Details
        public async Task<IActionResult> ProgramDetails(string id)
        {
            Console.WriteLine($"🔍 ProgramDetails called with ID: {id}");

            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine("❌ Program ID is null or empty");
                TempData["ErrorMessage"] = "Program ID is required";
                return RedirectToAction(nameof(ProgramsLibrary));
            }

            try
            {
                var program = await _trainingService.GetTrainingProgramByIdAsync(id);
                if (program == null)
                {
                    Console.WriteLine($"❌ Program not found for ID: {id}");
                    TempData["ErrorMessage"] = "Training program not found";
                    return RedirectToAction(nameof(ProgramsLibrary));
                }

                Console.WriteLine($"✅ Loaded program: {program.Title}");

                ViewData["Title"] = program.Title;
                ViewData["Subtitle"] = "Training Program Details";
                return View(program);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ProgramDetails: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading program details: {ex.Message}";
                return RedirectToAction(nameof(ProgramsLibrary));
            }
        }

        // GET: Edit Training Program
        public async Task<IActionResult> EditProgram(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Program ID is required";
                return RedirectToAction(nameof(ProgramsLibrary));
            }

            try
            {
                var program = await _trainingService.GetTrainingProgramByIdAsync(id);
                if (program == null)
                {
                    TempData["ErrorMessage"] = "Training program not found";
                    return RedirectToAction(nameof(ProgramsLibrary));
                }

                ViewData["Title"] = "Edit Training Program";
                ViewData["Subtitle"] = program.Title;
                return View(program);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading program for editing: {ex.Message}";
                return RedirectToAction(nameof(ProgramsLibrary));
            }
        }

        // POST: Edit Training Program
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProgram(string id, TrainingProgram program)
        {
            if (id != program.Id)
            {
                TempData["ErrorMessage"] = "Program ID mismatch";
                return RedirectToAction(nameof(ProgramsLibrary));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _trainingService.UpdateTrainingProgramAsync(id, program);
                    TempData["SuccessMessage"] = "Training program updated successfully!";
                    return RedirectToAction(nameof(ProgramsLibrary));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating training program: {ex.Message}";
                }
            }

            ViewData["Title"] = "Edit Training Program";
            ViewData["Subtitle"] = program.Title;
            return View(program);
        }

        // POST: Delete Training Program
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgram(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Program ID is required";
                return RedirectToAction(nameof(ProgramsLibrary));
            }

            try
            {
                var result = await _trainingService.DeleteTrainingProgramAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Training program deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Cannot delete program with active assignments";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting training program: {ex.Message}";
            }

            return RedirectToAction(nameof(ProgramsLibrary));
        }

        #endregion

        #region Employee Trainings

        // GET: Employee Trainings
        public async Task<IActionResult> EmployeeTrainings(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                TempData["ErrorMessage"] = "Employee ID is required";
                return RedirectToAction("Index", "Employee");
            }

            try
            {
                // Get employee details
                var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction("Index", "Employee");
                }

                // Get employee trainings
                var trainings = await _trainingService.GetEmployeeTrainingsAsync(employeeId);

                ViewBag.Employee = employee;
                ViewData["Title"] = $"{employee.FirstName} {employee.LastName}'s Trainings";
                ViewData["Subtitle"] = "View and manage employee training records";

                return View(trainings);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading employee trainings: {ex.Message}";
                return RedirectToAction("Details", "Employee", new { id = employeeId });
            }
        }

        // GET: Training Details
        public async Task<IActionResult> TrainingDetails(string id, string employeeId)
        {
            try
            {
                var training = await _trainingService.GetTrainingByIdAsync(id);
                if (training == null)
                {
                    TempData["ErrorMessage"] = "Training record not found.";
                    return RedirectToAction("EmployeeTrainings", new { employeeId });
                }

                // Ensure related data is loaded
                if (!string.IsNullOrEmpty(training.EmployeeId) && training.Employee == null)
                {
                    training.Employee = await _firestoreService.GetEmployeeByIdAsync(training.EmployeeId);
                }

                if (!string.IsNullOrEmpty(training.TrainingProgramId) && training.TrainingProgram == null)
                {
                    training.TrainingProgram = await _trainingService.GetTrainingProgramByIdAsync(training.TrainingProgramId);
                }

                return View(training);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading training details: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading training details.";
                return RedirectToAction("EmployeeTrainings", new { employeeId });
            }
        }

        // GET: Edit Training
        public async Task<IActionResult> EditTraining(string id, string employeeId)
        {
            Console.WriteLine($"🔍 EditTraining called with ID: {id}, EmployeeId: {employeeId}");

            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Training ID is required";
                return RedirectToAction(nameof(EmployeeTrainings), new { employeeId = employeeId });
            }

            try
            {
                var training = await _trainingService.GetTrainingByIdAsync(id);
                if (training == null)
                {
                    TempData["ErrorMessage"] = "Training record not found";
                    return RedirectToAction(nameof(EmployeeTrainings), new { employeeId = employeeId });
                }

                ViewData["Title"] = "Edit Training";
                ViewData["Subtitle"] = training.Title;
                ViewBag.EmployeeId = employeeId;
                return View(training);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditTraining: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading training for editing: {ex.Message}";
                return RedirectToAction(nameof(EmployeeTrainings), new { employeeId = employeeId });
            }
        }

        // POST: Edit Training
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTraining(string id, Training training, string employeeId)
        {
            Console.WriteLine($"🔍 EditTraining POST called with ID: {id}, EmployeeId: {employeeId}");

            if (id != training.Id)
            {
                TempData["ErrorMessage"] = "Training ID mismatch";
                return RedirectToAction(nameof(EmployeeTrainings), new { employeeId = employeeId });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the training using the service
                    var result = await _trainingService.UpdateFlutterCompatibleTrainingAsync(id, training);

                    if (result)
                    {
                        TempData["SuccessMessage"] = "Training updated successfully!";
                        return RedirectToAction(nameof(EmployeeTrainings), new { employeeId = employeeId });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update training";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error updating training: {ex.Message}");
                    TempData["ErrorMessage"] = $"Error updating training: {ex.Message}";
                }
            }
            else
            {
                Console.WriteLine($"❌ ModelState is invalid:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"   - {error.ErrorMessage}");
                }
                TempData["ErrorMessage"] = "Please fix the validation errors.";
            }

            ViewData["Title"] = "Edit Training";
            ViewData["Subtitle"] = training.Title;
            ViewBag.EmployeeId = employeeId;
            return View(training);
        }

        // POST: Delete Training
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTraining(string id, string employeeId)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Training ID is required";
                return RedirectToAction(nameof(EmployeeTrainings), new { employeeId = employeeId });
            }

            try
            {
                // First get the training to check if it exists and get employeeId
                var training = await _trainingService.GetTrainingByIdAsync(id);
                if (training == null)
                {
                    TempData["ErrorMessage"] = "Training record not found";
                    return RedirectToAction(nameof(EmployeeTrainings), new { employeeId = employeeId });
                }

                // Delete from main collection
                var document = _firestoreDb.Collection("trainings").Document(id);
                await document.DeleteAsync();

                // Also delete from employee's subcollection if exists
                if (!string.IsNullOrEmpty(training.EmployeeId))
                {
                    var employeeTrainingDoc = _firestoreDb
                        .Collection("employees")
                        .Document(training.EmployeeId)
                        .Collection("trainings")
                        .Document(id);

                    await employeeTrainingDoc.DeleteAsync();
                }

                TempData["SuccessMessage"] = "Training deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting training: {ex.Message}";
            }

            return RedirectToAction(nameof(EmployeeTrainings), new { employeeId = employeeId });
        }

        #endregion

        #region Training Assignments

        // GET: Assign Training
        public async Task<IActionResult> AssignTraining()
        {
            try
            {
                var programs = await _trainingService.GetAllTrainingProgramsAsync();
                var employees = await _firestoreService.GetAllEmployeesAsync();

                // Convert to lists to avoid dynamic type issues
                ViewBag.TrainingPrograms = programs ?? new List<TrainingProgram>();
                ViewBag.Employees = employees ?? new List<Employee>();

                ViewData["Title"] = "Assign Training";
                ViewData["Subtitle"] = "Assign training programs to employees";

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading assignment form: {ex.Message}";
                return View();
            }
        }

        // POST: Assign Training
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTraining(string trainingProgramId, string employeeId, string reason, DateTime dueDate)
        {
            if (string.IsNullOrEmpty(trainingProgramId) || string.IsNullOrEmpty(employeeId))
            {
                TempData["ErrorMessage"] = "Please select both a training program and an employee.";
                return RedirectToAction(nameof(AssignTraining));
            }

            try
            {
                // Get training program details
                var program = await _trainingService.GetTrainingProgramByIdAsync(trainingProgramId);
                if (program == null)
                {
                    TempData["ErrorMessage"] = "Training program not found.";
                    return RedirectToAction(nameof(AssignTraining));
                }

                // Create Flutter-compatible training with proper date handling
                var training = new Training
                {
                    EmployeeId = employeeId,
                    Title = program.Title,
                    Provider = program.Provider,
                    Description = program.Description,
                    Status = "Assigned", // Use "Assigned" status for admin-assigned trainings
                    CertificateUrl = "",
                    TrainingProgramId = trainingProgramId,
                    AssignedBy = "admin",
                    AssignedReason = reason,
                    Progress = 0.0
                };

                // Set dates as milliseconds since epoch
                training.SetStartDate(DateTime.UtcNow);
                training.SetEndDate(dueDate.ToUniversalTime());
                training.SetCreatedAt(DateTime.UtcNow);
                training.SetAssignedDate(DateTime.UtcNow);

                // Create the training record
                var trainingId = await _trainingService.CreateFlutterCompatibleTrainingAsync(training);

                TempData["SuccessMessage"] = $"Training assigned successfully! Employee can now see this in their Flutter app. (ID: {trainingId})";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning training: {ex.Message}";
            }

            return RedirectToAction(nameof(TrainingsList));
        }

        // New method to sync existing trainings
        public async Task<IActionResult> SyncEmployeeTrainings(string employeeId)
        {
            try
            {
                // Get all trainings for this employee from main collection
                var trainings = await _trainingService.GetAllTrainingsAsync();
                var employeeTrainings = trainings.Where(t => t.EmployeeId == employeeId).ToList();

                foreach (var training in employeeTrainings)
                {
                    await _trainingService.SyncTrainingToEmployeeAsync(training.Id);
                }

                TempData["SuccessMessage"] = $"Synced {employeeTrainings.Count} trainings to employee subcollection";
                return RedirectToAction(nameof(TrainingsList));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error syncing trainings: {ex.Message}";
                return RedirectToAction(nameof(TrainingsList));
            }
        }

        #endregion

        #region Helper Methods

        //private async Task<string> CreateFlutterCompatibleTrainingAsync(Training training)
        //{
        //    try
        //    {
        //        // Add to main trainings collection
        //        var mainCollection = _firestoreDb.Collection("trainings");
        //        var document = await mainCollection.AddAsync(training);
        //        training.Id = document.Id;

        //        // Also add to employee's trainings subcollection for Flutter compatibility
        //        if (!string.IsNullOrEmpty(training.EmployeeId))
        //        {
        //            var employeeTrainingCollection = _firestoreDb
        //                .Collection("employees")
        //                .Document(training.EmployeeId)
        //                .Collection("trainings");

        //            await employeeTrainingCollection.Document(document.Id).SetAsync(training);
        //        }

        //        Console.WriteLine($"✅ Flutter-compatible training created with ID: {document.Id}");
        //        return document.Id;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error creating flutter-compatible training: {ex.Message}");
        //        throw;
        //    }
        //}

        #endregion
        #region Certificate Download Methods

        // Download single certificate
        //public async Task<IActionResult> DownloadEmployeeDocument(string employeeId, string documentType, string documentId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(documentId))
        //        {
        //            TempData["ErrorMessage"] = "Employee ID and Document ID are required";
        //            return RedirectToAction(nameof(EmployeeTrainings), new { employeeId });
        //        }

        //        // Get the training to find the certificate file name
        //        var training = await _trainingService.GetTrainingByIdAsync(documentId);
        //        if (training == null)
        //        {
        //            TempData["ErrorMessage"] = "Training record not found";
        //            return RedirectToAction(nameof(EmployeeTrainings), new { employeeId });
        //        }

        //        // Check if certificate exists
        //        if (string.IsNullOrEmpty(training.CertificatePdfUrl) && string.IsNullOrEmpty(training.CertificateUrl))
        //        {
        //            TempData["ErrorMessage"] = "No certificate file found for this training";
        //            return RedirectToAction(nameof(EmployeeTrainings), new { employeeId });
        //        }

        //        // Use the injected FirebaseStorageService
        //        string fileUrl = !string.IsNullOrEmpty(training.CertificatePdfUrl)
        //            ? training.CertificatePdfUrl
        //            : training.CertificateUrl;

        //        // Extract file name from URL or use training title
        //        string fileName = !string.IsNullOrEmpty(training.CertificateFileName)
        //            ? training.CertificateFileName
        //            : $"{training.Title}_certificate.pdf";

        //        try
        //        {
        //            // Download file from Firebase Storage using the injected service
        //            var fileStream = await _firebaseStorageService.DownloadFileAsync(GetStoragePathFromUrl(fileUrl));

        //            // Determine content type based on file extension
        //            string contentType = GetContentType(fileName);

        //            return File(fileStream, contentType, fileName);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"❌ Error downloading file: {ex.Message}");
        //            TempData["ErrorMessage"] = "Error downloading certificate file";
        //            return RedirectToAction(nameof(EmployeeTrainings), new { employeeId });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error in DownloadEmployeeDocument: {ex.Message}");
        //        TempData["ErrorMessage"] = $"Error downloading document: {ex.Message}";
        //        return RedirectToAction(nameof(EmployeeTrainings), new { employeeId });
        //    }
        //}

        // Download all certificates as ZIP
        //public async Task<IActionResult> DownloadAllCertificates(string employeeId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(employeeId))
        //        {
        //            TempData["ErrorMessage"] = "Employee ID is required";
        //            return RedirectToAction(nameof(Index));
        //        }

        //        // Get all trainings for employee
        //        var trainings = await _trainingService.GetEmployeeTrainingsAsync(employeeId);
        //        var certificates = trainings.Where(t =>
        //            !string.IsNullOrEmpty(t.CertificatePdfUrl) || !string.IsNullOrEmpty(t.CertificateUrl)).ToList();

        //        if (!certificates.Any())
        //        {
        //            TempData["ErrorMessage"] = "No certificates found for this employee";
        //            return RedirectToAction(nameof(EmployeeTrainings), new { employeeId });
        //        }

        //        // Get employee info
        //        var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
        //        string employeeName = employee != null ? $"{employee.FirstName}_{employee.LastName}" : employeeId;

        //        // Create ZIP file in memory
        //        using var memoryStream = new MemoryStream();
        //        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
        //        {
        //            foreach (var training in certificates)
        //            {
        //                try
        //                {
        //                    string fileUrl = !string.IsNullOrEmpty(training.CertificatePdfUrl)
        //                        ? training.CertificatePdfUrl
        //                        : training.CertificateUrl;

        //                    if (!string.IsNullOrEmpty(fileUrl))
        //                    {
        //                        string fileName = !string.IsNullOrEmpty(training.CertificateFileName)
        //                            ? training.CertificateFileName
        //                            : $"{SanitizeFileName(training.Title)}_certificate.pdf";

        //                        // Download file using the injected service
        //                        var fileStream = await _firebaseStorageService.DownloadFileAsync(GetStoragePathFromUrl(fileUrl));

        //                        // Add to ZIP
        //                        var zipEntry = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.Optimal);
        //                        using var entryStream = zipEntry.Open();
        //                        await fileStream.CopyToAsync(entryStream);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($"⚠️ Error adding {training.Title} to ZIP: {ex.Message}");
        //                    // Continue with next file
        //                }
        //            }
        //        }

        //        memoryStream.Position = 0;
        //        string zipFileName = $"{employeeName}_Certificates_{DateTime.Now:yyyyMMddHHmmss}.zip";

        //        TempData["SuccessMessage"] = $"Downloaded {certificates.Count} certificates successfully";
        //        return File(memoryStream, "application/zip", zipFileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error in DownloadAllCertificates: {ex.Message}");
        //        TempData["ErrorMessage"] = $"Error downloading certificates: {ex.Message}";
        //        return RedirectToAction(nameof(EmployeeTrainings), new { employeeId });
        //    }
        //}

        // View certificate in browser
        public async Task<IActionResult> ViewTrainingCertificate(string employeeId, string trainingId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(trainingId))
                {
                    return BadRequest("Employee ID and Training ID are required");
                }

                var training = await _trainingService.GetTrainingByIdAsync(trainingId);
                if (training == null)
                {
                    return NotFound("Training record not found");
                }

                // Check if certificate exists
                if (string.IsNullOrEmpty(training.CertificatePdfUrl) && string.IsNullOrEmpty(training.CertificateUrl))
                {
                    return NotFound("No certificate file found for this training");
                }

                // Prefer PDF URL for viewing, fall back to image URL
                string fileUrl = !string.IsNullOrEmpty(training.CertificatePdfUrl)
                    ? training.CertificatePdfUrl
                    : training.CertificateUrl;

                if (string.IsNullOrEmpty(fileUrl))
                {
                    return NotFound("Certificate URL is empty");
                }

                // For PDFs, we can use the Google Storage viewer or download
                if (fileUrl.Contains(".pdf") || !string.IsNullOrEmpty(training.CertificatePdfUrl))
                {
                    // Use Google's PDF viewer
                    string googleViewerUrl = $"https://docs.google.com/gview?embedded=true&url={Uri.EscapeDataString(fileUrl)}";
                    ViewBag.PdfViewerUrl = googleViewerUrl;
                    return View("PdfViewer");
                }
                else
                {
                    // For images, redirect directly to the image
                    return Redirect(fileUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ViewTrainingCertificate: {ex.Message}");
                TempData["ErrorMessage"] = $"Error viewing certificate: {ex.Message}";
                return RedirectToAction(nameof(EmployeeTrainings), new { employeeId });
            }
        }

        #endregion
        #region Helper Methods

        private async Task<string> CreateFlutterCompatibleTrainingAsync(Training training)
        {
            try
            {
                // Add to main trainings collection
                var mainCollection = _firestoreDb.Collection("trainings");
                var document = await mainCollection.AddAsync(training);
                training.Id = document.Id;

                // Also add to employee's trainings subcollection for Flutter compatibility
                if (!string.IsNullOrEmpty(training.EmployeeId))
                {
                    var employeeTrainingCollection = _firestoreDb
                        .Collection("employees")
                        .Document(training.EmployeeId)
                        .Collection("trainings");

                    await employeeTrainingCollection.Document(document.Id).SetAsync(training);
                }

                Console.WriteLine($"✅ Flutter-compatible training created with ID: {document.Id}");
                return document.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating flutter-compatible training: {ex.Message}");
                throw;
            }
        }

        private string GetStoragePathFromUrl(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return null;

            try
            {
                var uri = new Uri(fileUrl);
                // Extract path from Firebase Storage URL
                // URL format: https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{path}?alt=media
                if (uri.Host == "firebasestorage.googleapis.com")
                {
                    var pathSegments = uri.AbsolutePath.Split('/');
                    if (pathSegments.Length >= 5)
                    {
                        // The path is URL encoded, so we need to decode it
                        string encodedPath = pathSegments[4];
                        return Uri.UnescapeDataString(encodedPath);
                    }
                }

                // For direct storage URLs: https://storage.googleapis.com/{bucket}/{path}
                if (uri.Host == "storage.googleapis.com")
                {
                    var path = uri.AbsolutePath.Substring(1); // Remove leading slash
                    var bucketIndex = path.IndexOf('/');
                    if (bucketIndex > 0)
                    {
                        return path.Substring(bucketIndex + 1);
                    }
                }

                // If it's a direct path, return as is
                return fileUrl;
            }
            catch
            {
                // If URL parsing fails, try to extract the path manually
                if (fileUrl.Contains("/o/"))
                {
                    var parts = fileUrl.Split(new[] { "/o/" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        var pathPart = parts[1].Split('?')[0];
                        return Uri.UnescapeDataString(pathPart);
                    }
                }
                return fileUrl;
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLower();
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

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "certificate";

            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
                .Replace(" ", "_")
                .Trim();
        }

        #endregion
        #region Additional Actions for the View

        // GET: Training Certificates - This is the action your view expects
        public async Task<IActionResult> TrainingCertificates(string employeeId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                {
                    TempData["ErrorMessage"] = "Employee ID is required";
                    return RedirectToAction("Index", "Employee");
                }

                // Get employee details
                var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction("Index", "Employee");
                }

                // Get employee trainings with certificates
                var trainings = await _trainingService.GetEmployeeTrainingsAsync(employeeId);
                var certificates = trainings.Where(t =>
                    !string.IsNullOrEmpty(t.CertificateUrl) || !string.IsNullOrEmpty(t.CertificatePdfUrl)).ToList();

                ViewBag.Employee = employee;
                ViewData["Title"] = "Training Certificates";
                return View(certificates);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading training certificates: {ex.Message}";
                return RedirectToAction("Index", "Employee");
            }
        }

        // GET: Employee Details (if needed for the Back to Employee link)
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Employee ID is required";
                    return RedirectToAction("Index");
                }

                var employee = await _firestoreService.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToAction("Index");
                }

                // Load employee trainings
                var trainings = await _trainingService.GetEmployeeTrainingsAsync(id);
                ViewBag.Trainings = trainings;

                ViewData["Title"] = $"{employee.FirstName} {employee.LastName} - Details";
                return View(employee);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading employee details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        #endregion
        #region Certificate Download Methods

        // Download single certificate
        public async Task<IActionResult> DownloadEmployeeDocument(string employeeId, string documentType, string documentId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(documentId))
                {
                    TempData["ErrorMessage"] = "Employee ID and Document ID are required";
                    return RedirectToAction(nameof(TrainingCertificates), new { employeeId });
                }

                var training = await _trainingService.GetTrainingByIdAsync(documentId);
                if (training == null)
                {
                    TempData["ErrorMessage"] = "Training record not found";
                    return RedirectToAction(nameof(TrainingCertificates), new { employeeId });
                }

                // Check if certificate exists
                if (string.IsNullOrEmpty(training.CertificatePdfUrl) && string.IsNullOrEmpty(training.CertificateUrl))
                {
                    TempData["ErrorMessage"] = "No certificate file found for this training";
                    return RedirectToAction(nameof(TrainingCertificates), new { employeeId });
                }

                string fileUrl = !string.IsNullOrEmpty(training.CertificatePdfUrl)
                    ? training.CertificatePdfUrl
                    : training.CertificateUrl;

                string fileName = !string.IsNullOrEmpty(training.CertificateFileName)
                    ? training.CertificateFileName
                    : $"{SanitizeFileName(training.Title)}_certificate.pdf";

                try
                {
                    // Use the updated FirebaseStorageService to download the certificate
                    var fileStream = await _firebaseStorageService.DownloadCertificateAsync(fileUrl);
                    string contentType = GetContentType(fileName);

                    Console.WriteLine($"✅ Successfully downloaded certificate: {fileName}");
                    return File(fileStream, contentType, fileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error downloading certificate: {ex.Message}");

                    // Fallback: redirect to direct URL
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        Console.WriteLine($"🔄 Falling back to direct URL: {fileUrl}");
                        return Redirect(fileUrl);
                    }

                    TempData["ErrorMessage"] = "Error downloading certificate file";
                    return RedirectToAction(nameof(TrainingCertificates), new { employeeId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in DownloadEmployeeDocument: {ex.Message}");
                TempData["ErrorMessage"] = $"Error downloading document: {ex.Message}";
                return RedirectToAction(nameof(TrainingCertificates), new { employeeId });
            }
        }

        // Download all certificates as ZIP
        public async Task<IActionResult> DownloadAllCertificates(string employeeId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                {
                    TempData["ErrorMessage"] = "Employee ID is required";
                    return RedirectToAction(nameof(Index));
                }

                // Get employee info
                var employee = await _firestoreService.GetEmployeeByIdAsync(employeeId);
                string employeeName = employee != null ? $"{employee.FirstName}_{employee.LastName}" : employeeId;

                try
                {
                    // Use the new method to download all certificates as ZIP
                    var zipBytes = await _firebaseStorageService.DownloadAllCertificatesAsZipAsync(employeeId);

                    string zipFileName = $"{employeeName}_Certificates_{DateTime.Now:yyyyMMddHHmmss}.zip";

                    TempData["SuccessMessage"] = "All certificates downloaded successfully as ZIP file";
                    return File(zipBytes, "application/zip", zipFileName);
                }
                catch (FileNotFoundException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction(nameof(TrainingCertificates), new { employeeId });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error creating ZIP: {ex.Message}");
                    TempData["ErrorMessage"] = "Error creating ZIP file with certificates";
                    return RedirectToAction(nameof(TrainingCertificates), new { employeeId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in DownloadAllCertificates: {ex.Message}");
                TempData["ErrorMessage"] = $"Error downloading certificates: {ex.Message}";
                return RedirectToAction(nameof(TrainingCertificates), new { employeeId });
            }
        }

        #endregion
        #region Debug Methods

        // Debug method to check certificate data
        public async Task<IActionResult> DebugCertificates(string employeeId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                {
                    return Content("❌ Employee ID is required");
                }

                var trainings = await _trainingService.GetEmployeeTrainingsAsync(employeeId);
                var debugInfo = new System.Text.StringBuilder();

                debugInfo.AppendLine($"🔍 Debugging certificates for employee: {employeeId}");
                debugInfo.AppendLine($"📊 Total trainings: {trainings.Count}");
                debugInfo.AppendLine("---");

                foreach (var training in trainings)
                {
                    debugInfo.AppendLine($"Training ID: {training.Id}");
                    debugInfo.AppendLine($"Title: {training.Title}");
                    debugInfo.AppendLine($"EmployeeId: {training.EmployeeId}");
                    debugInfo.AppendLine($"CertificatePdfUrl: {training.CertificatePdfUrl ?? "NULL"}");
                    debugInfo.AppendLine($"CertificateUrl: {training.CertificateUrl ?? "NULL"}");
                    debugInfo.AppendLine($"CertificateFileName: {training.CertificateFileName ?? "NULL"}");
                    debugInfo.AppendLine("---");
                }

                return Content(debugInfo.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                return Content($"❌ Error: {ex.Message}\n{ex.StackTrace}", "text/plain");
            }
        }

        #endregion

    }
}