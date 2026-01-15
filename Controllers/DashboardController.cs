using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Interface;
using WebApplication2.Models;
using WebApplication2.Models.Support_Model;
using WebApplication2.Models.ViewModel;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class DashboardController : Controller
    {
        private readonly FirestoreService _firestoreService;
        private readonly TrainingService _trainingService;
        private readonly IReportService _reportService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            FirestoreService firestoreService,
            TrainingService trainingService,
            IReportService reportService,
            ILogger<DashboardController> logger)
        {
            _firestoreService = firestoreService;
            _trainingService = trainingService;
            _reportService = reportService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardData = await GetDashboardDataAsync();
                ViewData["Title"] = "Dashboard Overview";
                ViewData["Subtitle"] = "Comprehensive view of your organization's performance";
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard data. Please try again.";
                return View(new DashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var dashboardData = await GetDashboardDataAsync();
                return Ok(new
                {
                    success = true,
                    data = dashboardData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard stats");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error loading dashboard statistics"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportDashboard()
        {
            try
            {
                var dashboardData = await GetDashboardDataAsync();
                // Implement export logic here
                return Json(new { success = true, message = "Export feature coming soon!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dashboard");
                return Json(new { success = false, message = "Error exporting dashboard data" });
            }
        }

        private async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var dashboard = new DashboardViewModel();

            try
            {
                _logger.LogInformation("Loading dashboard data...");

                // Load all data in parallel for better performance
                var employeesTask = _firestoreService.GetAllEmployeesAsync();
                var trainingsTask = _trainingService.GetAllTrainingsAsync();
                var programsTask = _trainingService.GetAllTrainingProgramsAsync();
                var skillsDataTask = _firestoreService.GetEmployeeSkillsViewModel();

                await Task.WhenAll(employeesTask, trainingsTask, programsTask, skillsDataTask);

                var employees = await employeesTask ?? new List<Employee>();
                var trainings = await trainingsTask ?? new List<Training>();
                var programs = await programsTask ?? new List<TrainingProgram>();
                var skillsData = await skillsDataTask ?? new EmployeeSkillViewModel();

                // Basic Statistics
                dashboard.TotalEmployees = employees.Count;
                dashboard.TotalTrainings = trainings.Count;
                dashboard.TotalTrainingPrograms = programs.Count;
                dashboard.TotalSkillsTracked = skillsData.TotalSkillsTracked;

                // Training Statistics - Simplified without date checks
                dashboard.CompletedTrainings = trainings.Count(t =>
                    t.Status?.Equals("Completed", StringComparison.OrdinalIgnoreCase) == true);
                dashboard.InProgressTrainings = trainings.Count(t =>
                    t.Status?.Equals("In Progress", StringComparison.OrdinalIgnoreCase) == true);
                dashboard.PendingTrainings = trainings.Count(t =>
                    t.Status?.Equals("Pending", StringComparison.OrdinalIgnoreCase) == true ||
                    t.Status?.Equals("Assigned", StringComparison.OrdinalIgnoreCase) == true);

                // Simplified overdue calculation - remove date dependency
                dashboard.OverdueTrainings = trainings.Count(t =>
                    !IsTrainingCompleted(t) &&
                    (t.Status?.Equals("In Progress", StringComparison.OrdinalIgnoreCase) == true ||
                     t.Status?.Equals("Assigned", StringComparison.OrdinalIgnoreCase) == true));

                dashboard.ActiveTrainings = dashboard.InProgressTrainings + dashboard.PendingTrainings;

                // Calculate completion rate safely
                dashboard.TrainingCompletionRate = dashboard.TotalTrainings > 0 ?
                    Math.Round((double)dashboard.CompletedTrainings / dashboard.TotalTrainings * 100, 1) : 0;

                // Set overall status
                dashboard.OverallStatus = dashboard.TrainingCompletionRate >= 80 ? "Excellent" :
                                         dashboard.TrainingCompletionRate >= 60 ? "Good" :
                                         dashboard.TrainingCompletionRate >= 40 ? "Fair" : "Needs Improvement";

                // Skills Statistics
                dashboard.AverageSkillLevel = Math.Round(skillsData.AverageSkillLevel, 1);
                dashboard.CriticalSkillsGap = skillsData.CriticalSkillsGap;
                dashboard.SkillsRequiringAttention = skillsData.SkillsGapAnalysis?
                    .Count(s => s.IsCritical) ?? 0;

                // Department Overview
                dashboard.DepartmentStats = skillsData.DepartmentStats ?? new List<Department>();
                dashboard.TopDepartments = dashboard.DepartmentStats
                    .OrderByDescending(d => d.AverageSkillLevel)
                    .Take(5)
                    .ToList();

                // Recent Activities - Simplified without date sorting
                dashboard.RecentTrainings = trainings
                    .Take(10)
                    .ToList();

                // Upcoming Deadlines - Simplified to just pending/in progress trainings
                dashboard.UpcomingDeadlines = trainings
                    .Where(t => !IsTrainingCompleted(t))
                    .Take(10)
                    .ToList();

                // Performance Metrics
                var employeesNeedingTraining = await _trainingService.GetEmployeesNeedingTrainingAsync();
                dashboard.EmployeesNeedingTraining = employeesNeedingTraining?.ToList() ?? new List<Employee>();
                dashboard.TotalEmployeesNeedingTraining = dashboard.EmployeesNeedingTraining.Count;

                // Training Program Performance
                dashboard.TopPerformingPrograms = programs
                    .Where(p => p.AssignmentCount > 0)
                    .Select(p => new
                    {
                        Program = p,
                        CompletionRate = (double)p.CompletedCount / p.AssignmentCount * 100
                    })
                    .OrderByDescending(x => x.CompletionRate)
                    .Select(x => x.Program)
                    .Take(5)
                    .ToList();

                // Skills Analysis
                dashboard.TopSkillGaps = skillsData.SkillsGapAnalysis?
                    .Where(s => s.Gap > 0)
                    .OrderByDescending(s => s.Gap)
                    .Take(10)
                    .ToList() ?? new List<Skill>();

                dashboard.SkillsByCategory = skillsData.CategoryStats ?? new List<Category>();

                // Quick Actions Data
                dashboard.PendingApprovals = trainings.Count(t =>
                    t.Status?.Equals("Pending", StringComparison.OrdinalIgnoreCase) == true);
                dashboard.CertificatesPending = trainings.Count(t =>
                    t.Status?.Equals("Completed", StringComparison.OrdinalIgnoreCase) == true &&
                    string.IsNullOrEmpty(t.CertificateUrl));

                // Load employee data for recent activities
                await LoadEmployeeDataForTrainings(dashboard.RecentTrainings);
                await LoadEmployeeDataForTrainings(dashboard.UpcomingDeadlines);

                _logger.LogInformation("Dashboard data loaded successfully: {EmployeeCount} employees, {TrainingCount} trainings, {CompletionRate}% completion",
                    dashboard.TotalEmployees, dashboard.TotalTrainings, dashboard.TrainingCompletionRate);

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardDataAsync");
                return dashboard; // Return empty dashboard instead of throwing
            }
        }

        private async Task LoadEmployeeDataForTrainings(List<Training> trainings)
        {
            foreach (var training in trainings)
            {
                try
                {
                    if (!string.IsNullOrEmpty(training.EmployeeId) && training.Employee == null)
                    {
                        training.Employee = await _firestoreService.GetEmployeeByIdAsync(training.EmployeeId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load employee data for training {TrainingId}", training.Id);
                    // Continue with other trainings even if one fails
                }
            }
        }

        private bool IsTrainingCompleted(Training training)
        {
            return training.Status?.Equals("Completed", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}