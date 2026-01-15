// Services/TrainingService.cs
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Models;
using WebApplication2.Models.Support_Model;

namespace WebApplication2.Services
{
    public class TrainingService
    {
        private readonly FirestoreService _firestoreService;
        private readonly FirestoreDb _firestoreDb;

        public TrainingService(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
            _firestoreDb = firestoreService.GetFirestoreDb();
        }
        // Add these methods to your TrainingService.cs
        public async Task<string> CreateFlutterCompatibleTrainingAsync(Training training)
        {
            try
            {
                // Ensure dates are in milliseconds for Flutter
                training.SetCreatedAt(DateTime.UtcNow);
                if (training.AssignedDate == null && training.AssignedBy == "admin")
                {
                    training.SetAssignedDate(DateTime.UtcNow);
                }

                // Add to main trainings collection
                var mainCollection = _firestoreDb.Collection("trainings");
                var document = await mainCollection.AddAsync(training);
                training.Id = document.Id;

                // Also add to employee's trainings subcollection for Flutter app
                if (!string.IsNullOrEmpty(training.EmployeeId))
                {
                    await AddTrainingToEmployeeSubcollection(training);
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

        private async Task AddTrainingToEmployeeSubcollection(Training training)
        {
            try
            {
                var employeeTrainingCollection = _firestoreDb
                    .Collection("employees")
                    .Document(training.EmployeeId)
                    .Collection("trainings");

                // Create a Flutter-compatible training document
                var flutterTraining = new Dictionary<string, object>
                {
                    ["employeeId"] = training.EmployeeId,
                    ["title"] = training.Title,
                    ["provider"] = training.Provider,
                    ["description"] = training.Description ?? "",
                    ["startDate"] = training.StartDate,
                    ["endDate"] = training.EndDate,
                    ["status"] = training.Status,
                    ["certificateUrl"] = training.CertificateUrl ?? "",
                    ["createdAt"] = training.CreatedAt,
                    ["certificatePdfUrl"] = training.CertificatePdfUrl ?? "",
                    ["certificateFileName"] = training.CertificateFileName ?? "",
                    ["progress"] = training.Progress,
                    ["trainingProgramId"] = training.TrainingProgramId ?? "",
                    ["assignedBy"] = training.AssignedBy ?? "admin",
                    ["assignedReason"] = training.AssignedReason ?? "",
                    ["assignedDate"] = training.AssignedDate ?? 0,
                    ["completedDate"] = training.CompletedDate ?? 0
                };

                await employeeTrainingCollection.Document(training.Id).SetAsync(flutterTraining);
                Console.WriteLine($"✅ Training added to employee subcollection: {training.EmployeeId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding training to employee subcollection: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SyncTrainingToEmployeeAsync(string trainingId)
        {
            try
            {
                var training = await GetTrainingByIdAsync(trainingId);
                if (training == null || string.IsNullOrEmpty(training.EmployeeId))
                {
                    Console.WriteLine($"❌ Cannot sync training: training not found or missing employee ID");
                    return false;
                }

                await AddTrainingToEmployeeSubcollection(training);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error syncing training to employee: {ex.Message}");
                return false;
            }
        }

        // Enhanced method to get employee trainings (compatible with Flutter)

        //public async Task<List<Training>> GetEmployeeTrainingsAsync(string employeeId)
        //{
        //    try
        //    {
        //        Console.WriteLine($"🔍 Fetching trainings for employee: {employeeId}");
        //        var trainings = new List<Training>();

        //        // Try both collections for maximum compatibility
        //        var collectionsToCheck = new[] { "trainings", "employees/" + employeeId + "/trainings" };

        //        foreach (var collectionPath in collectionsToCheck)
        //        {
        //            try
        //            {
        //                CollectionReference collection;
        //                if (collectionPath.StartsWith("employees/"))
        //                {
        //                    // Handle subcollection path
        //                    var parts = collectionPath.Split('/');
        //                    collection = _firestoreDb.Collection(parts[0]).Document(parts[1]).Collection(parts[2]);
        //                }
        //                else
        //                {
        //                    collection = _firestoreDb.Collection(collectionPath);
        //                }

        //                var query = collection.WhereEqualTo("EmployeeId", employeeId);
        //                var snapshot = await query.GetSnapshotAsync();

        //                foreach (var document in snapshot.Documents)
        //                {
        //                    try
        //                    {
        //                        var training = await ConvertToTrainingAsync(document);
        //                        if (training != null)
        //                        {
        //                            trainings.Add(training);
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Console.WriteLine($"❌ Error converting training document {document.Id}: {ex.Message}");
        //                    }
        //                }

        //                if (trainings.Any())
        //                {
        //                    Console.WriteLine($"✅ Found {trainings.Count} trainings in {collectionPath}");
        //                    break; // Stop if we found trainings
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"⚠️ Error checking collection {collectionPath}: {ex.Message}");
        //            }
        //        }

        //        Console.WriteLine($"✅ Retrieved {trainings.Count} trainings for employee {employeeId}");
        //        return trainings;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error in GetEmployeeTrainingsAsync: {ex.Message}");
        //        return new List<Training>();
        //    }
        //}
        public async Task<List<Training>> GetEmployeeTrainingsAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Fetching trainings for employee: {employeeId}");
                var trainings = new List<Training>();

                // Query main trainings collection where EmployeeId matches
                var collection = _firestoreDb.Collection("trainings");
                var query = collection.WhereEqualTo("EmployeeId", employeeId);
                var snapshot = await query.GetSnapshotAsync();

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

                        // Load training program data if exists
                        if (!string.IsNullOrEmpty(training.TrainingProgramId))
                        {
                            training.TrainingProgram = await GetTrainingProgramByIdAsync(training.TrainingProgramId);
                        }

                        trainings.Add(training);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error converting training document {document.Id}: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ Retrieved {trainings.Count} trainings for employee {employeeId}");
                return trainings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting employee trainings: {ex.Message}");
                return new List<Training>();
            }
        }
        private async Task<Training> ConvertToTrainingAsync(DocumentSnapshot document)
        {
            try
            {
                var data = document.ToDictionary();
                var training = new Training
                {
                    Id = document.Id,
                    EmployeeId = data.ContainsKey("employeeId") ? data["employeeId"]?.ToString() : "",
                    Title = data.ContainsKey("title") ? data["title"]?.ToString() : "Unknown Title",
                    Provider = data.ContainsKey("provider") ? data["provider"]?.ToString() : "Unknown Provider",
                    Description = data.ContainsKey("description") ? data["description"]?.ToString() : "",
                    Status = data.ContainsKey("status") ? data["status"]?.ToString() : "Pending",
                    CertificateUrl = data.ContainsKey("certificateUrl") ? data["certificateUrl"]?.ToString() : "",
                    CertificatePdfUrl = data.ContainsKey("certificatePdfUrl") ? data["certificatePdfUrl"]?.ToString() : "",
                    CertificateFileName = data.ContainsKey("certificateFileName") ? data["certificateFileName"]?.ToString() : "",
                    TrainingProgramId = data.ContainsKey("trainingProgramId") ? data["trainingProgramId"]?.ToString() : "",
                    AssignedBy = data.ContainsKey("assignedBy") ? data["assignedBy"]?.ToString() : "admin",
                    AssignedReason = data.ContainsKey("assignedReason") ? data["assignedReason"]?.ToString() : "",
                    Progress = data.ContainsKey("progress") ? Convert.ToDouble(data["progress"]) : 0.0
                };

                // Handle date fields with better error handling
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

                return training;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error converting document {document.Id} to Training: {ex.Message}");
                return null;
            }
        }
        #region Training Program CRUD Operations

        //public async Task<string> CreateTrainingProgramAsync(TrainingProgram program)
        //{
        //    try
        //    {
        //        // Set creation timestamp
        //        program.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        //        // Ensure required properties have values
        //        if (string.IsNullOrEmpty(program.DifficultyLevel))
        //            program.DifficultyLevel = "Intermediate";

        //        if (string.IsNullOrEmpty(program.Format))
        //            program.Format = "Online";

        //        if (program.Duration <= 0)
        //            program.Duration = 40;

        //        // Ensure CoveredSkills is never null
        //        program.CoveredSkills = program.CoveredSkills ?? new List<string>();

        //        var collection = _firestoreDb.Collection("training_programs");
        //        var document = await collection.AddAsync(program);

        //        Console.WriteLine($"✅ Training program created with ID: {document.Id}");
        //        return document.Id;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error creating training program: {ex.Message}");
        //        throw;
        //    }
        //}
        public async Task<string> CreateTrainingProgramAsync(TrainingProgram program)
        {
            try
            {
                Console.WriteLine($"🏗️ Starting to create training program: {program.Title}");

                // Set creation timestamp
                program.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

                // Ensure required properties have values
                if (string.IsNullOrEmpty(program.DifficultyLevel))
                    program.DifficultyLevel = "Intermediate";

                if (string.IsNullOrEmpty(program.Format))
                    program.Format = "Online";

                if (program.Duration <= 0)
                    program.Duration = 40;

                // Ensure CoveredSkills is never null
                program.CoveredSkills = program.CoveredSkills ?? new List<string>();

                Console.WriteLine($"📝 Program data prepared:");
                Console.WriteLine($"   Title: {program.Title}");
                Console.WriteLine($"   Description: {program.Description}");
                Console.WriteLine($"   Provider: {program.Provider}");
                Console.WriteLine($"   Category: {program.Category}");
                Console.WriteLine($"   Duration: {program.Duration}");
                Console.WriteLine($"   CoveredSkills: {program.CoveredSkills?.Count ?? 0} skills");
                Console.WriteLine($"   DifficultyLevel: {program.DifficultyLevel}");
                Console.WriteLine($"   Format: {program.Format}");
                Console.WriteLine($"   IsActive: {program.IsActive}");

                var collection = _firestoreDb.Collection("training_programs");
                Console.WriteLine($"🔥 Adding to Firestore collection: training_programs");

                var document = await collection.AddAsync(program);
                var documentId = document.Id;

                Console.WriteLine($"✅ Training program created successfully with ID: {documentId}");
                return documentId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating training program: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to create training program: {ex.Message}", ex);
            }
        }
        // Update training in both collections
        //public async Task<bool> UpdateFlutterCompatibleTrainingAsync(string trainingId, Training training)
        //{
        //    try
        //    {
        //        // Update in main collection
        //        var mainDocument = _firestoreDb.Collection("trainings").Document(trainingId);
        //        await mainDocument.SetAsync(training, SetOptions.MergeAll);

        //        // Also update in employee's subcollection if employeeId exists
        //        if (!string.IsNullOrEmpty(training.EmployeeId))
        //        {
        //            var employeeTrainingDoc = _firestoreDb
        //                .Collection("employees")
        //                .Document(training.EmployeeId)
        //                .Collection("trainings")
        //                .Document(trainingId);

        //            await employeeTrainingDoc.SetAsync(training, SetOptions.MergeAll);
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error updating flutter-compatible training: {ex.Message}");
        //        return false;
        //    }
        //}
        public async Task<bool> UpdateFlutterCompatibleTrainingAsync(string trainingId, Training training)
        {
            try
            {
                // Update in main collection
                var mainDocument = _firestoreDb.Collection("trainings").Document(trainingId);
                await mainDocument.SetAsync(training, SetOptions.MergeAll);

                // Also update in employee's subcollection if employeeId exists
                if (!string.IsNullOrEmpty(training.EmployeeId))
                {
                    var employeeTrainingDoc = _firestoreDb
                        .Collection("employees")
                        .Document(training.EmployeeId)
                        .Collection("trainings")
                        .Document(trainingId);

                    await employeeTrainingDoc.SetAsync(training, SetOptions.MergeAll);
                }

                Console.WriteLine($"✅ Training updated successfully: {trainingId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating training: {ex.Message}");
                return false;
            }
        }
        // Add these methods to your TrainingService class

        //public async Task<List<Training>> GetAllTrainingsAsync()
        //{
        //    try
        //    {
        //        var trainings = new List<Training>();
        //        var collection = _firestoreDb.Collection("trainings");
        //        var snapshot = await collection.GetSnapshotAsync();

        //        foreach (var document in snapshot.Documents)
        //        {
        //            try
        //            {
        //                var training = document.ConvertTo<Training>();
        //                training.Id = document.Id;

        //                // Load employee data
        //                if (!string.IsNullOrEmpty(training.EmployeeId))
        //                {
        //                    training.Employee = await _firestoreService.GetEmployeeByIdAsync(training.EmployeeId);
        //                }

        //                trainings.Add(training);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Error converting training document: {ex.Message}");
        //            }
        //        }

        //        return trainings;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error getting trainings: {ex.Message}");
        //        return new List<Training>();
        //    }
        //}
        //public async Task<List<Training>> GetAllTrainingsAsync()
        //{
        //    try
        //    {
        //        var trainings = new List<Training>();
        //        var collection = _firestoreDb.Collection("trainings");
        //        var snapshot = await collection.GetSnapshotAsync();

        //        foreach (var document in snapshot.Documents)
        //        {
        //            try
        //            {
        //                var training = await ConvertToTrainingAsync(document);
        //                if (training != null)
        //                {
        //                    // Load employee data
        //                    if (!string.IsNullOrEmpty(training.EmployeeId))
        //                    {
        //                        training.Employee = await _firestoreService.GetEmployeeByIdAsync(training.EmployeeId);
        //                    }

        //                    // Load program data
        //                    if (!string.IsNullOrEmpty(training.TrainingProgramId))
        //                    {
        //                        training.TrainingProgram = await GetTrainingProgramByIdAsync(training.TrainingProgramId);
        //                    }

        //                    trainings.Add(training);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Error processing training document {document.Id}: {ex.Message}");
        //            }
        //        }

        //        Console.WriteLine($"✅ Retrieved {trainings.Count} trainings from main collection");
        //        return trainings;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error getting all trainings: {ex.Message}");
        //        return new List<Training>();
        //    }
        //}
        public async Task<List<Training>> GetAllTrainingsAsync()
        {
            try
            {
                var trainings = new List<Training>();
                var collection = _firestoreDb.Collection("trainings");
                var snapshot = await collection.GetSnapshotAsync();

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

                        // Load program data if exists
                        if (!string.IsNullOrEmpty(training.TrainingProgramId))
                        {
                            training.TrainingProgram = await GetTrainingProgramByIdAsync(training.TrainingProgramId);
                        }

                        trainings.Add(training);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing training document {document.Id}: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ Retrieved {trainings.Count} trainings from main collection");
                return trainings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting all trainings: {ex.Message}");
                return new List<Training>();
            }
        }
        //public async Task<Training> GetTrainingByIdAsync(string trainingId)
        //{
        //    try
        //    {
        //        var document = _firestoreDb.Collection("trainings").Document(trainingId);
        //        var snapshot = await document.GetSnapshotAsync();

        //        if (snapshot.Exists)
        //        {
        //            var training = snapshot.ConvertTo<Training>();
        //            training.Id = snapshot.Id;

        //            // Load employee data
        //            if (!string.IsNullOrEmpty(training.EmployeeId))
        //            {
        //                training.Employee = await _firestoreService.GetEmployeeByIdAsync(training.EmployeeId);
        //            }

        //            return training;
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error getting training: {ex.Message}");
        //        return null;
        //    }
        //}
        // Fix the GetTrainingByIdAsync method
        public async Task<Training> GetTrainingByIdAsync(string id)
        {
            try
            {
                var document = _firestoreDb.Collection("trainings").Document(id);
                var snapshot = await document.GetSnapshotAsync();

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
                Console.WriteLine($"❌ Error getting training by ID: {ex.Message}");
                return null;
            }
        }
        //public async Task<List<TrainingProgram>> GetAllTrainingProgramsAsync()
        //{
        //    try
        //    {
        //        var programs = new List<TrainingProgram>();
        //        var collection = _firestoreDb.Collection("training_programs");
        //        var snapshot = await collection.GetSnapshotAsync();

        //        Console.WriteLine($"🔍 Firestore returned {snapshot.Documents.Count} documents");

        //        foreach (var document in snapshot.Documents)
        //        {
        //            try
        //            {
        //                Console.WriteLine($"📖 Processing document: {document.Id}");

        //                // Convert document to dictionary to inspect fields
        //                var data = document.ToDictionary();
        //                Console.WriteLine($"   Fields: {string.Join(", ", data.Keys)}");

        //                // Try to convert to TrainingProgram
        //                var program = document.ConvertTo<TrainingProgram>();
        //                program.Id = document.Id;

        //                // Handle potential null values
        //                program.CoveredSkills = program.CoveredSkills ?? new List<string>();
        //                program.Duration = program.Duration > 0 ? program.Duration : 40;
        //                program.DifficultyLevel = program.DifficultyLevel ?? "Intermediate";
        //                program.Format = program.Format ?? "Online";
        //                program.IsActive = program.IsActive; // Keep existing value

        //                // Get assignment counts for this program
        //                var assignments = await GetAssignmentsForProgramAsync(program.Id);
        //                program.AssignmentCount = assignments.Count;
        //                program.CompletedCount = assignments.Count(a => a.Status == "Completed");

        //                programs.Add(program);

        //                Console.WriteLine($"✅ Successfully loaded: {program.Title}");
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"❌ Error converting document {document.Id}: {ex.Message}");
        //                // Continue with next document
        //            }
        //        }

        //        Console.WriteLine($"✅ Retrieved {programs.Count} training programs");
        //        return programs;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error getting training programs: {ex.Message}");
        //        return new List<TrainingProgram>();
        //    }
        //}
        public async Task<List<TrainingProgram>> GetAllTrainingProgramsAsync()
        {
            try
            {
                var programs = new List<TrainingProgram>();
                var collection = _firestoreDb.Collection("training_programs");
                var snapshot = await collection.GetSnapshotAsync();

                Console.WriteLine($"🔍 Firestore returned {snapshot.Documents.Count} training program documents");

                foreach (var document in snapshot.Documents)
                {
                    try
                    {
                        Console.WriteLine($"📖 Processing training program document: {document.Id}");

                        var program = document.ConvertTo<TrainingProgram>();
                        program.Id = document.Id; // Set the ID from Firestore document

                        // Handle potential null values
                        program.CoveredSkills = program.CoveredSkills ?? new List<string>();
                        program.Duration = program.Duration > 0 ? program.Duration : 40;
                        program.DifficultyLevel = program.DifficultyLevel ?? "Intermediate";
                        program.Format = program.Format ?? "Online";

                        // Get assignment counts for this program
                        var assignments = await GetAssignmentsForProgramAsync(program.Id);
                        program.AssignmentCount = assignments.Count;
                        program.CompletedCount = assignments.Count(a => a.Status == "Completed");

                        programs.Add(program);

                        Console.WriteLine($"✅ Successfully loaded: {program.Title} (ID: {program.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error converting document {document.Id}: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ Retrieved {programs.Count} training programs");
                return programs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting training programs: {ex.Message}");
                return new List<TrainingProgram>();
            }
        }
        //public async Task<TrainingProgram> GetTrainingProgramByIdAsync(string programId)
        //{
        //    try
        //    {
        //        var document = _firestoreDb.Collection("training_programs").Document(programId);
        //        var snapshot = await document.GetSnapshotAsync();

        //        if (snapshot.Exists)
        //        {
        //            var program = snapshot.ConvertTo<TrainingProgram>();
        //            program.Id = snapshot.Id;

        //            // Get assignments for this program
        //            program.Assignments = await GetAssignmentsForProgramAsync(programId);
        //            program.AssignmentCount = program.Assignments.Count;
        //            program.CompletedCount = program.Assignments.Count(a => a.Status == "Completed");

        //            Console.WriteLine($"✅ Retrieved training program: {program.Title}");
        //            return program;
        //        }

        //        Console.WriteLine($"❌ Training program {programId} not found");
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error getting training program: {ex.Message}");
        //        return null;
        //    }
        //}
        //public async Task<TrainingProgram> GetTrainingProgramByIdAsync(string programId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(programId))
        //        {
        //            Console.WriteLine("❌ Program ID is null or empty");
        //            return null;
        //        }

        //        Console.WriteLine($"🔍 Looking for training program with ID: {programId}");

        //        var document = _firestoreDb.Collection("training_programs").Document(programId);
        //        var snapshot = await document.GetSnapshotAsync();

        //        if (snapshot.Exists)
        //        {
        //            var program = snapshot.ConvertTo<TrainingProgram>();
        //            program.Id = snapshot.Id; // Set the ID from the document

        //            Console.WriteLine($"✅ Found training program: {program.Title}");

        //            // Get assignments for this program
        //            try
        //            {
        //                program.Assignments = await GetAssignmentsForProgramAsync(programId);
        //                program.AssignmentCount = program.Assignments.Count;
        //                program.CompletedCount = program.Assignments.Count(a => a.Status == "Completed");
        //                Console.WriteLine($"📊 Program has {program.AssignmentCount} assignments, {program.CompletedCount} completed");
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"⚠️ Error loading assignments: {ex.Message}");
        //                program.Assignments = new List<TrainingAssignment>();
        //                program.AssignmentCount = 0;
        //                program.CompletedCount = 0;
        //            }

        //            return program;
        //        }

        //        Console.WriteLine($"❌ Training program {programId} not found in Firestore");
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error getting training program: {ex.Message}");
        //        return null;
        //    }
        //}
        public async Task<TrainingProgram> GetTrainingProgramByIdAsync(string id)
        {
            try
            {
                var document = _firestoreDb.Collection("training_programs").Document(id);
                var snapshot = await document.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    var program = snapshot.ConvertTo<TrainingProgram>();
                    program.Id = snapshot.Id;
                    return program;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting training program by ID: {ex.Message}");
                return null;
            }
        }
        public async Task<bool> UpdateTrainingProgramAsync(string programId, TrainingProgram program)
        {
            try
            {
                var document = _firestoreDb.Collection("training_programs").Document(programId);
                await document.SetAsync(program, SetOptions.MergeAll);

                Console.WriteLine($"✅ Training program updated: {program.Title}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating training program: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteTrainingProgramAsync(string programId)
        {
            try
            {
                // First check if there are any assignments for this program
                var assignments = await GetAssignmentsForProgramAsync(programId);
                if (assignments.Any())
                {
                    Console.WriteLine($"❌ Cannot delete program with {assignments.Count} active assignments");
                    return false;
                }

                var document = _firestoreDb.Collection("training_programs").Document(programId);
                await document.DeleteAsync();

                Console.WriteLine($"✅ Training program deleted: {programId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting training program: {ex.Message}");
                return false;
            }
        }
        // Get trainings for employee (compatible with Flutter)
        //public async Task<List<Training>> GetEmployeeTrainingsAsync(string employeeId)
        //{
        //    try
        //    {
        //        var trainings = new List<Training>();
        //        var collection = _firestoreDb.Collection("employees")
        //            .Document(employeeId)
        //            .Collection("trainings");

        //        var snapshot = await collection.GetSnapshotAsync();

        //        foreach (var document in snapshot.Documents)
        //        {
        //            try
        //            {
        //                var training = document.ConvertTo<Training>();
        //                training.Id = document.Id;

        //                // Load related data if needed
        //                if (!string.IsNullOrEmpty(training.TrainingProgramId))
        //                {
        //                    training.TrainingProgram = await GetTrainingProgramByIdAsync(training.TrainingProgramId);
        //                }

        //                trainings.Add(training);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"❌ Error converting training document: {ex.Message}");
        //            }
        //        }

        //        return trainings;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error getting employee trainings: {ex.Message}");
        //        return new List<Training>();
        //    }
        //}

        #endregion

        #region Training Assignment CRUD Operations

        // In TrainingService.cs - Update CreateTrainingAssignmentAsync to ensure data compatibility
        public async Task<string> CreateTrainingAssignmentAsync(TrainingAssignment assignment)
        {
            try
            {
                assignment.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

                // Add to main trainings collection
                var mainCollection = _firestoreDb.Collection("trainings");
                var document = await mainCollection.AddAsync(assignment);
                assignment.Id = document.Id;

                // Also add to employee's trainings subcollection with Flutter-compatible structure
                if (!string.IsNullOrEmpty(assignment.EmployeeId))
                {
                    var employeeTrainingCollection = _firestoreDb
                        .Collection("employees")
                        .Document(assignment.EmployeeId)
                        .Collection("trainings");

                    // Convert to Flutter-compatible Training structure
                    var flutterTraining = new
                    {
                        employeeId = assignment.EmployeeId,
                        title = assignment.TrainingProgram?.Title ?? "Unknown Training",
                        provider = assignment.TrainingProgram?.Provider ?? "Unknown Provider",
                        description = assignment.TrainingProgram?.Description ?? "",
                        startDate = assignment.AssignedDate,
                        endDate = assignment.DueDate,
                        status = assignment.Status,
                        certificateUrl = assignment.CertificateUrl ?? "",
                        createdAt = assignment.CreatedAt,
                        certificatePdfUrl = assignment.CertificateUrl,
                        certificateFileName = assignment.CertificateFileName,
                        trainingProgramId = assignment.TrainingProgramId, // For reference
                        assignedReason = assignment.AssignedReason
                    };

                    await employeeTrainingCollection.Document(document.Id).SetAsync(flutterTraining);
                }

                return document.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateTrainingAssignmentAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<TrainingAssignment>> GetAllTrainingAssignmentsAsync()
        {
            try
            {
                var assignments = new List<TrainingAssignment>();
                var collection = _firestoreDb.Collection("trainings");
                var snapshot = await collection.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    var assignment = await ConvertToTrainingAssignmentAsync(document);
                    if (assignment != null)
                    {
                        assignments.Add(assignment);
                    }
                }

                Console.WriteLine($"✅ Retrieved {assignments.Count} training assignments");
                return assignments;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting training assignments: {ex.Message}");
                return new List<TrainingAssignment>();
            }
        }

        public async Task<TrainingAssignment> GetTrainingAssignmentByIdAsync(string assignmentId)
        {
            try
            {
                var document = _firestoreDb.Collection("trainings").Document(assignmentId);
                var snapshot = await document.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    var assignment = await ConvertToTrainingAssignmentAsync(snapshot);
                    Console.WriteLine($"✅ Retrieved training assignment: {assignmentId}");
                    return assignment;
                }

                Console.WriteLine($"❌ Training assignment {assignmentId} not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting training assignment: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateTrainingAssignmentAsync(string assignmentId, TrainingAssignment assignment)
        {
            try
            {
                var document = _firestoreDb.Collection("trainings").Document(assignmentId);
                await document.SetAsync(assignment, SetOptions.MergeAll);

                // Also update in employee's subcollection
                if (!string.IsNullOrEmpty(assignment.EmployeeId))
                {
                    var employeeTrainingDoc = _firestoreDb
                        .Collection("employees")
                        .Document(assignment.EmployeeId)
                        .Collection("trainings")
                        .Document(assignmentId);

                    await employeeTrainingDoc.SetAsync(assignment, SetOptions.MergeAll);
                }

                Console.WriteLine($"✅ Training assignment updated: {assignmentId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating training assignment: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateTrainingProgressAsync(string assignmentId, int progress, string status = null)
        {
            try
            {
                var assignment = await GetTrainingAssignmentByIdAsync(assignmentId);
                if (assignment == null) return false;

                assignment.Progress = progress;
                if (!string.IsNullOrEmpty(status))
                {
                    assignment.Status = status;
                }

                // Auto-complete if progress is 100%
                if (progress == 100 && assignment.Status != "Completed")
                {
                    assignment.Status = "Completed";
                    assignment.CompletedDate = DateTime.Now;
                }

                var result = await UpdateTrainingAssignmentAsync(assignmentId, assignment);
                Console.WriteLine($"✅ Training progress updated to {progress}% for assignment {assignmentId}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating training progress: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteTrainingAssignmentAsync(string assignmentId)
        {
            try
            {
                // First get the assignment to find employeeId
                var assignment = await GetTrainingAssignmentByIdAsync(assignmentId);

                // Delete from main collection
                var document = _firestoreDb.Collection("trainings").Document(assignmentId);
                await document.DeleteAsync();

                // Also delete from employee's subcollection if exists
                if (assignment != null && !string.IsNullOrEmpty(assignment.EmployeeId))
                {
                    var employeeTrainingDoc = _firestoreDb
                        .Collection("employees")
                        .Document(assignment.EmployeeId)
                        .Collection("trainings")
                        .Document(assignmentId);

                    await employeeTrainingDoc.DeleteAsync();
                }

                Console.WriteLine($"✅ Training assignment deleted: {assignmentId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting training assignment: {ex.Message}");
                return false;
            }
        }

        public async Task<List<TrainingAssignment>> GetEmployeeTrainingAssignmentsAsync(string employeeId)
        {
            try
            {
                var assignments = new List<TrainingAssignment>();
                var collection = _firestoreDb.Collection("employees")
                    .Document(employeeId)
                    .Collection("trainings");

                var snapshot = await collection.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    var assignment = await ConvertToTrainingAssignmentAsync(document);
                    if (assignment != null)
                    {
                        assignments.Add(assignment);
                    }
                }

                Console.WriteLine($"✅ Retrieved {assignments.Count} training assignments for employee {employeeId}");
                return assignments;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting employee training assignments: {ex.Message}");
                return new List<TrainingAssignment>();
            }
        }

        private async Task<List<TrainingAssignment>> GetAssignmentsForProgramAsync(string programId)
        {

            try
            {
                var assignments = new List<TrainingAssignment>();
                var collection = _firestoreDb.Collection("trainings");

                // Use WhereEqualTo to filter by TrainingProgramId
                var query = collection.WhereEqualTo("TrainingProgramId", programId);
                var snapshot = await query.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    try
                    {
                        var assignment = document.ConvertTo<TrainingAssignment>();
                        assignment.Id = document.Id;
                        assignments.Add(assignment);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error converting assignment document: {ex.Message}");
                    }
                }

                Console.WriteLine($"📊 Found {assignments.Count} assignments for program {programId}");
                return assignments;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting assignments for program: {ex.Message}");
                return new List<TrainingAssignment>();
            }
        }

        private async Task<TrainingAssignment> ConvertToTrainingAssignmentAsync(DocumentSnapshot document)
        {
            try
            {
                var assignment = document.ConvertTo<TrainingAssignment>();
                assignment.Id = document.Id;

                // Load training program details
                if (!string.IsNullOrEmpty(assignment.TrainingProgramId))
                {
                    assignment.TrainingProgram = await GetTrainingProgramByIdAsync(assignment.TrainingProgramId);
                }

                // Load employee details
                if (!string.IsNullOrEmpty(assignment.EmployeeId))
                {
                    assignment.Employee = await _firestoreService.GetEmployeeByIdAsync(assignment.EmployeeId);
                }

                return assignment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error converting document to training assignment: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Training Assignment Methods

        public async Task<bool> AssignTrainingToEmployeeAsync(string trainingProgramId, string employeeId, string reason = null, DateTime? dueDate = null)
        {
            try
            {
                // Create assignment directly without complex validation
                var assignment = new TrainingAssignment
                {
                    TrainingProgramId = trainingProgramId,
                    EmployeeId = employeeId,
                    Status = "Pending",
                    Progress = 0,
                    AssignedDate = DateTime.UtcNow, // Use UTC
                    DueDate = dueDate?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(30), // Convert to UTC
                    AssignedReason = reason ?? "Training assignment",
                    SkillGapBefore = 0
                };


                var assignmentId = await CreateTrainingAssignmentAsync(assignment);
                return !string.IsNullOrEmpty(assignmentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error assigning training: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AssignTrainingToMultipleEmployeesAsync(string trainingProgramId, List<string> employeeIds, string reason = null)
        {
            try
            {
                var tasks = employeeIds.Select(employeeId =>
                    AssignTrainingToEmployeeAsync(trainingProgramId, employeeId, reason));

                await Task.WhenAll(tasks);
                Console.WriteLine($"✅ Training assigned to {employeeIds.Count} employees");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error assigning training to multiple employees: {ex.Message}");
                return false;
            }
        }

        private async Task<double> CalculateSkillGapForAssignmentAsync(string employeeId, string trainingProgramId)
        {
            try
            {
                var program = await GetTrainingProgramByIdAsync(trainingProgramId);
                var employeeSkills = await _firestoreService.GetEmployeeSkillsAsync(employeeId);
                var requiredSkills = GetRequiredSkills();

                if (program?.CoveredSkills == null || !program.CoveredSkills.Any())
                    return 0;

                double totalGap = 0;
                int skillCount = 0;

                foreach (var coveredSkill in program.CoveredSkills)
                {
                    var requiredSkill = requiredSkills.FirstOrDefault(rs =>
                        rs.Name.Equals(coveredSkill, StringComparison.OrdinalIgnoreCase));

                    if (requiredSkill != null)
                    {
                        var employeeSkill = employeeSkills.FirstOrDefault(es =>
                            es.Name.Equals(coveredSkill, StringComparison.OrdinalIgnoreCase));

                        var currentLevel = employeeSkill != null ? ParseSkillLevel(employeeSkill.Level) : 0;
                        var gap = Math.Max(0, requiredSkill.RequiredLevel - currentLevel);
                        totalGap += gap;
                        skillCount++;
                    }
                }

                return skillCount > 0 ? totalGap / skillCount : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calculating skill gap: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Training Analysis Methods

        public async Task<List<EmployeeTrainingAnalysis>> GetTrainingAnalysisAsync()
        {
            var analysis = new List<EmployeeTrainingAnalysis>();
            var employees = await _firestoreService.GetAllEmployeesAsync();

            foreach (var employee in employees)
            {
                var employeeAssignments = await GetEmployeeTrainingAssignmentsAsync(employee.Id);

                var employeeAnalysis = new EmployeeTrainingAnalysis
                {
                    Employee = employee,
                    Skills = await _firestoreService.GetEmployeeSkillsAsync(employee.Id),
                    Trainings = employeeAssignments
                };

                // Calculate skill gaps for required skills
                employeeAnalysis.SkillGaps = await CalculateSkillGapsAsync(employee.Id);
                employeeAnalysis.NeedsTraining = employeeAnalysis.SkillGaps.Any(gap => gap.Gap > 0);
                employeeAnalysis.CompletedTrainings = employeeAssignments
                    .Count(t => t.Status == "Completed");

                analysis.Add(employeeAnalysis);
            }

            Console.WriteLine($"✅ Generated training analysis for {analysis.Count} employees");
            return analysis;
        }

        public async Task<List<Skill>> CalculateSkillGapsAsync(string employeeId)
        {
            var gaps = new List<Skill>();
            var requiredSkills = GetRequiredSkills();
            var employeeSkills = await _firestoreService.GetEmployeeSkillsAsync(employeeId);

            foreach (var requiredSkill in requiredSkills)
            {
                var employeeSkill = employeeSkills.FirstOrDefault(s =>
                    s.Name.Equals(requiredSkill.Name, StringComparison.OrdinalIgnoreCase));

                var currentLevel = employeeSkill != null ? ParseSkillLevel(employeeSkill.Level) : 0;
                var gap = Math.Max(0, requiredSkill.RequiredLevel - currentLevel);

                gaps.Add(new Skill
                {
                    Name = requiredSkill.Name,
                    CurrentLevel = currentLevel,
                    RequiredLevel = requiredSkill.RequiredLevel,
                    Gap = gap,
                    HasSkill = employeeSkill != null
                });
            }

            return gaps;
        }

        public async Task<List<Employee>> GetEmployeesNeedingTrainingAsync()
        {
            var analysis = await GetTrainingAnalysisAsync();
            return analysis
                .Where(a => a.NeedsTraining)
                .Select(a => a.Employee)
                .ToList();
        }

        public async Task<List<TrainingRecommendation>> GetTrainingRecommendationsAsync(string employeeId)
        {
            var recommendations = new List<TrainingRecommendation>();
            var skillGaps = await CalculateSkillGapsAsync(employeeId);
            var availablePrograms = await GetAllTrainingProgramsAsync();
            var significantGaps = skillGaps.Where(gap => gap.Gap >= 2).ToList();

            foreach (var gap in significantGaps)
            {
                // Find programs that cover this skill
                var matchingPrograms = availablePrograms
                    .Where(p => p.CoveredSkills?.Any(s =>
                        s.Equals(gap.Name, StringComparison.OrdinalIgnoreCase)) == true)
                    .ToList();

                foreach (var program in matchingPrograms.Take(2)) // Limit to 2 recommendations per skill
                {
                    recommendations.Add(new TrainingRecommendation
                    {
                        SkillName = gap.Name,
                        CurrentLevel = gap.CurrentLevel,
                        RequiredLevel = (int)gap.RequiredLevel,
                        Gap = (int)gap.Gap,
                        RecommendedTraining = program.Title,
                        TrainingProgramId = program.Id,
                        Priority = gap.Gap >= 3 ? "High" : gap.Gap >= 2 ? "Medium" : "Low"
                    });
                }
            }

            Console.WriteLine($"✅ Generated {recommendations.Count} training recommendations for employee {employeeId}");
            return recommendations;
        }

        public async Task<bool> AssignTrainingBasedOnSkillGapsAsync(string employeeId)
        {
            try
            {
                var recommendations = await GetTrainingRecommendationsAsync(employeeId);
                var highPriorityRecommendations = recommendations.Where(r => r.Priority == "High").ToList();

                foreach (var recommendation in highPriorityRecommendations.Take(2)) // Assign max 2 high priority trainings
                {
                    var assignment = new TrainingAssignment
                    {
                        TrainingProgramId = recommendation.TrainingProgramId,
                        EmployeeId = employeeId,
                        Status = "Assigned",
                        Progress = 0,
                        AssignedDate = DateTime.Now,
                        DueDate = DateTime.Now.AddDays(30),
                        AssignedReason = $"Auto-assigned to address skill gap in {recommendation.SkillName} (Current: {recommendation.CurrentLevel}, Required: {recommendation.RequiredLevel})",
                        SkillGapBefore = recommendation.Gap
                    };

                    await CreateTrainingAssignmentAsync(assignment);
                }

                Console.WriteLine($"✅ Auto-assigned {highPriorityRecommendations.Count} trainings to employee {employeeId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error assigning training based on skill gaps: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private List<RequiredSkill> GetRequiredSkills()
        {
            return new List<RequiredSkill>
            {
                new RequiredSkill { Name = "Financial Modeling", RequiredLevel = 3 },
                new RequiredSkill { Name = "Budgeting & Forecasting", RequiredLevel = 3 },
                new RequiredSkill { Name = "Data Analysis", RequiredLevel = 3 },
                new RequiredSkill { Name = "Analytics & Insights", RequiredLevel = 3 },
                new RequiredSkill { Name = "Risk Assessment", RequiredLevel = 3 },
                new RequiredSkill { Name = "Risk Analysis & Mitigation", RequiredLevel = 3 },
                new RequiredSkill { Name = "Project Management", RequiredLevel = 3 },
                new RequiredSkill { Name = "Planning & Execution", RequiredLevel = 3 },
                new RequiredSkill { Name = "Report Writing", RequiredLevel = 3 },
                new RequiredSkill { Name = "Documentation & Reporting", RequiredLevel = 3 }
            };
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

        #endregion
        #region Training Assignment Workflow Methods

        public async Task<bool> AcceptTrainingAsync(string assignmentId)
        {
            try
            {
                var assignment = await GetTrainingAssignmentByIdAsync(assignmentId);
                if (assignment == null) return false;

                assignment.Status = "Accepted";
                assignment.AcceptedDate = DateTime.Now;

                return await UpdateTrainingAssignmentAsync(assignmentId, assignment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error accepting training: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StartTrainingAsync(string assignmentId)
        {
            try
            {
                var assignment = await GetTrainingAssignmentByIdAsync(assignmentId);
                if (assignment == null) return false;

                assignment.Status = "InProgress";
                assignment.Progress = 10; // Initial progress when started

                return await UpdateTrainingAssignmentAsync(assignmentId, assignment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error starting training: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CompleteTrainingAsync(string assignmentId, string certificateFileName = null, string certificateUrl = null)
        {
            try
            {
                var assignment = await GetTrainingAssignmentByIdAsync(assignmentId);
                if (assignment == null) return false;

                // Update assignment status
                assignment.Status = "Completed";
                assignment.Progress = 100;
                assignment.CompletedDate = DateTime.Now;
                assignment.CertificateFileName = certificateFileName;
                assignment.CertificateUrl = certificateUrl;

                // Calculate skill gap after training
                assignment.SkillGapAfter = await CalculateSkillGapForAssignmentAsync(assignment.EmployeeId, assignment.TrainingProgramId);

                // Add skills to employee
                await AddTrainingSkillsToEmployeeAsync(assignment.EmployeeId, assignment.TrainingProgramId);

                return await UpdateTrainingAssignmentAsync(assignmentId, assignment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error completing training: {ex.Message}");
                return false;
            }
        }

        private async Task AddTrainingSkillsToEmployeeAsync(string employeeId, string trainingProgramId)
        {
            try
            {
                var program = await GetTrainingProgramByIdAsync(trainingProgramId);
                if (program?.CoveredSkills == null || !program.CoveredSkills.Any())
                    return;

                var employeeSkillsCollection = _firestoreDb
                    .Collection("employees")
                    .Document(employeeId)
                    .Collection("skills");

                foreach (var skillName in program.CoveredSkills)
                {
                    // Check if employee already has this skill
                    var existingSkillQuery = employeeSkillsCollection.WhereEqualTo("Name", skillName);
                    var existingSkillSnapshot = await existingSkillQuery.GetSnapshotAsync();

                    if (existingSkillSnapshot.Documents.Any())
                    {
                        // Update existing skill
                        var existingDoc = existingSkillSnapshot.Documents.First();
                        var existingSkill = existingDoc.ConvertTo<Skill>();

                        // Increase skill level or add experience
                        existingSkill.Level = IncreaseSkillLevel(existingSkill.Level);
                        existingSkill.YearsOfExperience += (int)0.5; // Add 6 months experience

                        await employeeSkillsCollection.Document(existingDoc.Id).SetAsync(existingSkill, SetOptions.MergeAll);
                        Console.WriteLine($"✅ Updated skill {skillName} for employee {employeeId}");
                    }
                    else
                    {
                        // Add new skill
                        var newSkill = new Skill
                        {
                            Name = skillName,
                            Level = "Intermediate", // Default level for completed training
                            Category = GetSkillCategory(skillName),
                            YearsOfExperience = (int)0.5, // 6 months experience
                            EmployeeId = employeeId,
                            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
                        };

                        await employeeSkillsCollection.AddAsync(newSkill);
                        Console.WriteLine($"✅ Added new skill {skillName} to employee {employeeId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding training skills to employee: {ex.Message}");
            }
        }

        private string IncreaseSkillLevel(string currentLevel)
        {
            return currentLevel?.ToLower() switch
            {
                "beginner" or "1" => "Intermediate",
                "intermediate" or "2" or "3" => "Advanced",
                "advanced" or "4" => "Expert",
                "expert" or "5" => "Expert", // Already at max
                _ => "Intermediate"
            };
        }

        private string GetSkillCategory(string skillName)
        {
            if (skillName.Contains("Financial") || skillName.Contains("Budget") || skillName.Contains("Analysis"))
                return "Financial & Budgeting";
            if (skillName.Contains("Data") || skillName.Contains("Analytics") || skillName.Contains("Statistical"))
                return "Data & Analytics";
            if (skillName.Contains("Risk") || skillName.Contains("Compliance") || skillName.Contains("Audit"))
                return "Risk Management";
            if (skillName.Contains("Project") || skillName.Contains("Planning") || skillName.Contains("Management"))
                return "Project Management";
            if (skillName.Contains("Report") || skillName.Contains("Documentation") || skillName.Contains("Writing"))
                return "Reporting & Documentation";

            return "General";
        }

        #endregion
        // Add these methods to your TrainingService
        public async Task<bool> UpdateTrainingStatusAsync(string trainingId, string status, double progress, string employeeId = null)
        {
            try
            {
                var training = await GetTrainingByIdAsync(trainingId);
                if (training == null) return false;

                training.Status = status;
                training.Progress = progress;

                // Set completion date if completed
                if (status == "Completed")
                {
                    training.SetCompletedDate(DateTime.UtcNow);
                }

                // Update in both collections
                var result = await UpdateFlutterCompatibleTrainingAsync(trainingId, training);

                if (result)
                {
                    Console.WriteLine($"✅ Training {trainingId} status updated to {status} with progress {progress}");

                    // If this is a completion, update employee skills
                    if (status == "Completed" && !string.IsNullOrEmpty(training.TrainingProgramId))
                    {
                        await UpdateEmployeeSkillsFromTrainingAsync(training.EmployeeId, training.TrainingProgramId);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating training status: {ex.Message}");
                return false;
            }
        }
        private async Task UpdateEmployeeSkillsFromTrainingAsync(string employeeId, string trainingProgramId)
        {
            try
            {
                var program = await GetTrainingProgramByIdAsync(trainingProgramId);
                if (program?.CoveredSkills == null || !program.CoveredSkills.Any())
                    return;

                var employeeSkills = await _firestoreService.GetEmployeeSkillsAsync(employeeId);

                foreach (var skillName in program.CoveredSkills)
                {
                    var existingSkill = employeeSkills.FirstOrDefault(s =>
                        s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

                    if (existingSkill != null)
                    {
                        // Update existing skill level
                        var newLevel = IncreaseSkillLevel(existingSkill.Level);
                        await UpdateEmployeeSkillLevelAsync(employeeId, existingSkill.Id, newLevel);
                    }
                    else
                    {
                        // Add new skill
                        await AddNewSkillToEmployeeAsync(employeeId, skillName, program.Category);
                    }
                }

                Console.WriteLine($"✅ Updated skills for employee {employeeId} from training completion");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating employee skills from training: {ex.Message}");
            }
        }
        private async Task UpdateEmployeeSkillLevelAsync(string employeeId, string skillId, string newLevel)
        {
            try
            {
                var skillDoc = _firestoreDb.Collection("employees")
                    .Document(employeeId)
                    .Collection("skills")
                    .Document(skillId);

                await skillDoc.UpdateAsync(new Dictionary<string, object>
        {
            { "level", newLevel },
            { "lastUpdated", FieldValue.ServerTimestamp }
        });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating skill level: {ex.Message}");
                throw;
            }
        }

        private async Task AddNewSkillToEmployeeAsync(string employeeId, string skillName, string category)
        {
            try
            {
                var skillsCollection = _firestoreDb.Collection("employees")
                    .Document(employeeId)
                    .Collection("skills");

                var newSkill = new Skill
                {
                    Name = skillName,
                    Level = "Intermediate", // Default level for completed training
                    Category = category ?? "General",
                    YearsOfExperience = 1, // Default 1 year experience for completed training
                    EmployeeId = employeeId,
                    CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow)
                };

                await skillsCollection.AddAsync(newSkill);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding new skill: {ex.Message}");
                throw;
            }
        }

        // Add to TrainingService.cs
        public async Task<bool> UpdateTrainingCertificateAsync(string trainingId, string certificateFileName, string certificateUrl, string certificatePdfUrl = null)
        {
            try
            {
                var training = await GetTrainingByIdAsync(trainingId);
                if (training == null) return false;

                training.CertificateFileName = certificateFileName;
                training.CertificateUrl = certificateUrl;
                training.CertificatePdfUrl = certificatePdfUrl ?? certificateUrl;

                // Update in main collection
                var mainDocument = _firestoreDb.Collection("trainings").Document(trainingId);
                await mainDocument.SetAsync(training, SetOptions.MergeAll);

                // Also update in employee's subcollection if employeeId exists
                if (!string.IsNullOrEmpty(training.EmployeeId))
                {
                    var employeeTrainingDoc = _firestoreDb
                        .Collection("employees")
                        .Document(training.EmployeeId)
                        .Collection("trainings")
                        .Document(trainingId);

                    await employeeTrainingDoc.SetAsync(training, SetOptions.MergeAll);
                }

                Console.WriteLine($"✅ Training certificate updated: {trainingId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating training certificate: {ex.Message}");
                return false;
            }
        }

        //public async Task<List<Training>> GetTrainingsWithCertificatesAsync(string employeeId = null)
        //{
        //    try
        //    {
        //        var trainings = new List<Training>();

        //        if (!string.IsNullOrEmpty(employeeId))
        //        {
        //            // Get trainings for specific employee
        //            var employeeTrainings = await GetEmployeeTrainingsAsync(employeeId);
        //            return employeeTrainings
        //                .Where(t => !string.IsNullOrEmpty(t.CertificateUrl) || !string.IsNullOrEmpty(t.CertificatePdfUrl))
        //                .ToList();
        //        }
        //        else
        //        {
        //            // Get all trainings with certificates across all employees
        //            var collection = _firestoreDb.Collection("trainings");
        //            var snapshot = await collection.GetSnapshotAsync();

        //            foreach (var document in snapshot.Documents)
        //            {
        //                try
        //                {
        //                    var training = document.ConvertTo<Training>();
        //                    training.Id = document.Id;

        //                    if (!string.IsNullOrEmpty(training.CertificateUrl) || !string.IsNullOrEmpty(training.CertificatePdfUrl))
        //                    {
        //                        // Load employee data
        //                        if (!string.IsNullOrEmpty(training.EmployeeId))
        //                        {
        //                            training.Employee = await _firestoreService.GetEmployeeByIdAsync(training.EmployeeId);
        //                        }
        //                        trainings.Add(training);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($"Error processing training document {document.Id}: {ex.Message}");
        //                }
        //            }
        //        }

        //        return trainings;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error getting trainings with certificates: {ex.Message}");
        //        return new List<Training>();
        //    }
        //}
        public async Task<List<Training>> GetFlutterTrainingsWithCertificatesAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔍 Getting Flutter trainings with certificates for employee: {employeeId}");

                var trainings = new List<Training>();

                // Check employee's trainings subcollection (Flutter structure)
                var employeeTrainingsCollection = _firestoreDb
                    .Collection("employees")
                    .Document(employeeId)
                    .Collection("trainings");

                var snapshot = await employeeTrainingsCollection.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    try
                    {
                        var training = await ConvertToTrainingAsync(document);
                        if (training != null && (!string.IsNullOrEmpty(training.CertificateUrl) || !string.IsNullOrEmpty(training.CertificatePdfUrl)))
                        {
                            trainings.Add(training);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error converting Flutter training document {document.Id}: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ Found {trainings.Count} Flutter trainings with certificates for employee {employeeId}");
                return trainings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting Flutter trainings with certificates: {ex.Message}");
                return new List<Training>();
            }
        }

        public async Task<bool> SyncFlutterTrainingsToMainCollectionAsync(string employeeId)
        {
            try
            {
                Console.WriteLine($"🔄 Syncing Flutter trainings to main collection for employee: {employeeId}");

                var flutterTrainings = await GetFlutterTrainingsWithCertificatesAsync(employeeId);
                int syncedCount = 0;

                // Define mainCollection at the method level to fix the scope issue
                var mainCollection = _firestoreDb.Collection("trainings");

                foreach (var training in flutterTrainings)
                {
                    try
                    {
                        // Check if training already exists in main collection
                        var existingTraining = await GetTrainingByIdAsync(training.Id);
                        if (existingTraining == null)
                        {
                            // Add to main collection
                            await mainCollection.Document(training.Id).SetAsync(training);
                            syncedCount++;
                            Console.WriteLine($"✅ Synced training: {training.Title}");
                        }
                        else
                        {
                            // Update existing training with Flutter data
                            await mainCollection.Document(training.Id).SetAsync(training, SetOptions.MergeAll);
                            syncedCount++;
                            Console.WriteLine($"✅ Updated training: {training.Title}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error syncing training {training.Id}: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ Successfully synced {syncedCount} trainings from Flutter to main collection");
                return syncedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error syncing Flutter trainings: {ex.Message}");
                return false;
            }
        }


    }
}