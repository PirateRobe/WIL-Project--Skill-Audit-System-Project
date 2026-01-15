using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Interface;
using WebApplication2.Models;
using WebApplication2.Models.Support_Model;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly FirestoreService _firestoreService;
        private readonly TrainingService _trainingService;

        public ReportsController(
            IReportService reportService,
            FirestoreService firestoreService,
            TrainingService trainingService)
        {
            _reportService = reportService;
            _firestoreService = firestoreService;
            _trainingService = trainingService;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Reports";
            ViewData["Subtitle"] = "Generate and download training reports";
            return View();
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                var employees = await _firestoreService.GetAllEmployeesAsync();
                var departments = employees
                    .Where(e => !string.IsNullOrEmpty(e.Department))
                    .Select(e => e.Department)
                    .Distinct()
                    .ToList();

                ViewBag.Departments = departments;
                ViewData["Title"] = "Generate Report";
                ViewData["Subtitle"] = "Create a new training report";

                return View(new ReportRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    Format = "PDF",
                    ReportType = "SkillsAndTraining"
                });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading report form: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportRequest request)
        {
            // Remove hidden fields from model state since we're not using them anymore
            ModelState.Remove("FileUrl");
            ModelState.Remove("FileName");
            ModelState.Remove("FileSize");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"❌ Model state invalid: {string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");

                // Repopulate departments for the view
                var employees = await _firestoreService.GetAllEmployeesAsync();
                var departments = employees?
                    .Where(e => !string.IsNullOrEmpty(e.Department))
                    .Select(e => e.Department)
                    .Distinct()
                    .ToList() ?? new List<string>();

                ViewBag.Departments = departments;
                return View(request);
            }

            try
            {
                // Initialize report
                request.CreatedBy = User.Identity?.Name ?? "System";
                request.CreatedAt = DateTime.UtcNow;

                Console.WriteLine($"🚀 Starting report generation for: {request.Title}");
                Console.WriteLine($"Report Type: {request.ReportType}, Format: {request.Format}");

                // Generate report data
                var reportData = await _reportService.GenerateReportDataAsync(request);
                Console.WriteLine($"✅ Report data generated successfully");

                if (reportData == null)
                {
                    throw new InvalidOperationException("Report data generation returned null");
                }

                // Generate PDF bytes
                var pdfBytes = await _reportService.GeneratePdfReportAsync(reportData, request);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    throw new InvalidOperationException("PDF generation returned empty bytes");
                }

                Console.WriteLine($"✅ PDF generated successfully: {pdfBytes.Length} bytes");

                // Return PDF as file download
                var fileName = $"{SanitizeFileName(request.Title)}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in report generation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";

                // Repopulate departments for the view
                var employees = await _firestoreService.GetAllEmployeesAsync();
                var departments = employees?
                    .Where(e => !string.IsNullOrEmpty(e.Department))
                    .Select(e => e.Department)
                    .Distinct()
                    .ToList() ?? new List<string>();
                ViewBag.Departments = departments;

                return View(request);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateQuickReport(string reportType)
        {
            try
            {
                var request = new ReportRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    ReportType = reportType,
                    Title = $"Quick {reportType} Report - {DateTime.UtcNow:yyyy-MM-dd}",
                    Description = $"Automatically generated {reportType.ToLower()} report",
                    Format = "PDF",
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedAt = DateTime.UtcNow
                };

                var reportData = await _reportService.GenerateReportDataAsync(request);
                var pdfBytes = await _reportService.GeneratePdfReportAsync(reportData, request);

                var fileName = $"{SanitizeFileName(request.Title)}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Return JSON error for AJAX calls
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }

                // For non-AJAX requests, show error message
                TempData["ErrorMessage"] = $"Error generating quick report: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var employees = await _firestoreService.GetAllEmployeesAsync();
                var trainings = await _trainingService.GetAllTrainingsAsync();

                ViewBag.TotalEmployees = employees?.Count ?? 0;
                ViewBag.TotalAssignments = trainings?.Count ?? 0;

                // Create simple stats for dashboard - since we're not storing reports anymore
                ViewBag.ReportStats = new
                {
                    Total = 0,
                    ThisMonth = 0,
                    LastMonth = 0
                };

                // Empty recent reports since we're not storing
                ViewBag.RecentReports = new List<object>();

                ViewData["Title"] = "Reports Dashboard";
                ViewData["Subtitle"] = "Generate and download training reports";
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> TestData()
        {
            try
            {
                // Cast to ReportService to access the test method
                if (_reportService is ReportService reportService)
                {
                    var testResult = await reportService.TestDataRetrieval();
                    return Content(testResult, "text/plain");
                }
                else
                {
                    return Content("❌ Cannot access ReportService implementation", "text/plain");
                }
            }
            catch (Exception ex)
            {
                return Content($"❌ Test failed: {ex.Message}\n\nStack trace: {ex.StackTrace}", "text/plain");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Debug()
        {
            try
            {
                // Test with minimal data
                var testData = new ReportData
                {
                    TotalEmployees = 150,
                    TotalTrainingPrograms = 25,
                    TotalAssignments = 300,
                    CompletedAssignments = 180,
                    InProgressAssignments = 75,
                    PendingAssignments = 45,
                    CompletionRate = 60.0
                };

                // Add sample department stats
                testData.DepartmentStatistics = new List<DepartmentStats>
                {
                    new DepartmentStats { DepartmentName = "IT", EmployeeCount = 45, AssignmentCount = 90, CompletedCount = 60, CompletionRate = 66.7, AverageSkillLevel = 3.8 },
                    new DepartmentStats { DepartmentName = "HR", EmployeeCount = 25, AssignmentCount = 50, CompletedCount = 35, CompletionRate = 70.0, AverageSkillLevel = 3.2 },
                    new DepartmentStats { DepartmentName = "Finance", EmployeeCount = 30, AssignmentCount = 60, CompletedCount = 40, CompletionRate = 66.7, AverageSkillLevel = 4.1 }
                };

                // Add sample program stats
                testData.ProgramStatistics = new List<TrainingProgramStats>
                {
                    new TrainingProgramStats { ProgramTitle = "Leadership Skills", Provider = "Internal", AssignmentCount = 45, CompletedCount = 30, CompletionRate = 66.7 },
                    new TrainingProgramStats { ProgramTitle = "Project Management", Provider = "External", AssignmentCount = 35, CompletedCount = 25, CompletionRate = 71.4 },
                    new TrainingProgramStats { ProgramTitle = "Technical Training", Provider = "Internal", AssignmentCount = 60, CompletedCount = 40, CompletionRate = 66.7 }
                };

                // Add sample skill gap analysis
                testData.SkillGapAnalysis = new List<SkillGapAnalysis>
                {
                    new SkillGapAnalysis { SkillName = "Cloud Computing", Category = "Technical", EmployeesWithSkill = 45, EmployeesNeedingTraining = 25, AverageGap = 2.1, Criticality = 55.6 },
                    new SkillGapAnalysis { SkillName = "Data Analysis", Category = "Analytical", EmployeesWithSkill = 60, EmployeesNeedingTraining = 35, AverageGap = 1.8, Criticality = 58.3 },
                    new SkillGapAnalysis { SkillName = "Team Leadership", Category = "Soft Skills", EmployeesWithSkill = 55, EmployeesNeedingTraining = 30, AverageGap = 1.5, Criticality = 54.5 }
                };

                var testRequest = new ReportRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Debug Test Report",
                    ReportType = "SkillsAndTraining",
                    Format = "PDF",
                    CreatedBy = "Debug User",
                    CreatedAt = DateTime.UtcNow
                };

                var pdfBytes = await _reportService.GeneratePdfReportAsync(testData, testRequest);

                if (pdfBytes != null && pdfBytes.Length > 0)
                {
                    var fileName = $"Debug_Test_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    return Content("Debug test failed: PDF generation returned empty bytes");
                }
            }
            catch (Exception ex)
            {
                return Content($"Debug test failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateStandardReports()
        {
            try
            {
                var reportTypes = new[] { "SkillsAndTraining", "DepartmentPerformance", "SkillGapAnalysis", "TrainingCompletion" };
                var generatedReports = new List<string>();

                foreach (var reportType in reportTypes)
                {
                    try
                    {
                        var request = new ReportRequest
                        {
                            Id = Guid.NewGuid().ToString(),
                            ReportType = reportType,
                            Title = $"Standard {reportType} Report - {DateTime.UtcNow:yyyy-MM-dd}",
                            Description = $"Standard {reportType.ToLower()} report generated automatically",
                            Format = "PDF",
                            CreatedBy = User.Identity?.Name ?? "System",
                            CreatedAt = DateTime.UtcNow
                        };

                        var reportData = await _reportService.GenerateReportDataAsync(request);
                        var pdfBytes = await _reportService.GeneratePdfReportAsync(reportData, request);

                        var fileName = $"{SanitizeFileName(request.Title)}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                        generatedReports.Add(fileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error generating {reportType} report: {ex.Message}");
                        // Continue with other reports even if one fails
                    }
                }

                if (generatedReports.Any())
                {
                    TempData["SuccessMessage"] = $"Successfully generated {generatedReports.Count} standard reports!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to generate any standard reports.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating standard reports: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "Report";

            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}