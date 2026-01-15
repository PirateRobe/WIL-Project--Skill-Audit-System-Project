using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication2.Interface;
using WebApplication2.Models;
using WebApplication2.Models.Support_Model;

namespace WebApplication2.Services
{
    public class ReportService : IReportService
    {
        private readonly TrainingService _trainingService;
        private readonly FirestoreService _firestoreService;

        public ReportService(
            TrainingService trainingService,
            FirestoreService firestoreService)
        {
            _trainingService = trainingService;
            _firestoreService = firestoreService;
        }

        public async Task<ReportData> GenerateReportDataAsync(ReportRequest request)
        {
            var reportData = new ReportData();

            try
            {
                Console.WriteLine("📊 Starting report data generation...");

                // Get all employees
                var employees = await _firestoreService.GetAllEmployeesAsync();
                reportData.TotalEmployees = employees?.Count ?? 0;
                Console.WriteLine($"✅ Found {reportData.TotalEmployees} employees");

                // Get all trainings
                var trainings = await _trainingService.GetAllTrainingsAsync();
                reportData.TotalTrainingPrograms = trainings?.Count ?? 0;
                Console.WriteLine($"✅ Found {reportData.TotalTrainingPrograms} trainings");

                // Calculate assignments statistics
                reportData.TotalAssignments = trainings?.Count ?? 0;
                reportData.CompletedAssignments = trainings?.Count(t => t.Status == "Completed") ?? 0;
                reportData.InProgressAssignments = trainings?.Count(t => t.Status == "In Progress") ?? 0;
                reportData.PendingAssignments = trainings?.Count(t => t.Status == "Pending" || t.Status == "Assigned") ?? 0;

                reportData.CompletionRate = reportData.TotalAssignments > 0
                    ? (double)reportData.CompletedAssignments / reportData.TotalAssignments * 100
                    : 0;

                Console.WriteLine($"✅ Processed {reportData.TotalAssignments} assignments");

                // Generate department statistics
                reportData.DepartmentStatistics = await GenerateDepartmentStatsAsync(employees ?? new List<Employee>(), trainings ?? new List<Training>());
                Console.WriteLine($"✅ Generated department stats for {reportData.DepartmentStatistics.Count} departments");

                // Generate program statistics
                reportData.ProgramStatistics = GenerateProgramStats(trainings ?? new List<Training>());
                Console.WriteLine($"✅ Generated program stats for {reportData.ProgramStatistics.Count} programs");

                // Generate skill gap analysis
                reportData.SkillGapAnalysis = await GenerateSkillGapAnalysisAsync(employees ?? new List<Employee>());
                Console.WriteLine($"✅ Generated skill gap analysis for {reportData.SkillGapAnalysis.Count} skills");

                Console.WriteLine($"🎉 Report data generation completed successfully!");
                return reportData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating report data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<byte[]> GeneratePdfReportAsync(ReportData data, ReportRequest request)
        {
            try
            {
                Console.WriteLine("📄 Starting PDF report generation...");

                // Validate data
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data), "Report data cannot be null");
                }

                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Report request cannot be null");
                }

                // Generate PDF
                var pdfBytes = GeneratePdfDocument(data, request);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    throw new InvalidOperationException("PDF generation returned empty bytes");
                }

                Console.WriteLine($"✅ PDF report generated successfully: {pdfBytes.Length} bytes");
                return pdfBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating PDF report: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public byte[] GeneratePdfFromHtml(string htmlContent)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .AlignCenter()
                            .Text("Sample Report")
                            .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(15);
                                column.Item().Text("This is a sample report generated from HTML content.");
                                column.Item().Text("Generated on: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"));
                            });
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating PDF from HTML: {ex.Message}");
                return System.Text.Encoding.UTF8.GetBytes("Error generating PDF");
            }
        }

        // Keep all your existing helper methods (GeneratePdfDocument, AddExecutiveSummary, etc.)
        // ... all the existing PDF generation methods remain the same

        private byte[] GeneratePdfDocument(ReportData data, ReportRequest request)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .AlignCenter()
                            .Text($"Training and Skills Report: {request.Title}")
                            .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(15);

                                // Executive Summary
                                AddExecutiveSummary(column, data);

                                // Department Performance
                                AddDepartmentPerformance(column, data);

                                // Training Statistics
                                AddTrainingStatistics(column, data);

                                // Skill Gap Analysis
                                AddSkillGapAnalysis(column, data);

                                // Report Metadata
                                AddReportMetadata(column, request);
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                                x.Span($" | Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                            });
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating PDF document: {ex.Message}");
                throw;
            }
        }

        private void AddExecutiveSummary(ColumnDescriptor column, ReportData data)
        {
            column.Item().Text("Executive Summary").Bold().FontSize(14);
            column.Item().Grid(grid =>
            {
                grid.Columns(4);
                grid.Spacing(10);

                grid.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                {
                    col.Item().AlignCenter().Text(data.TotalEmployees.ToString()).Bold().FontSize(16);
                    col.Item().AlignCenter().Text("Total Employees").FontSize(10);
                });

                grid.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                {
                    col.Item().AlignCenter().Text(data.TotalTrainingPrograms.ToString()).Bold().FontSize(16);
                    col.Item().AlignCenter().Text("Training Programs").FontSize(10);
                });

                grid.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                {
                    col.Item().AlignCenter().Text(data.TotalAssignments.ToString()).Bold().FontSize(16);
                    col.Item().AlignCenter().Text("Assignments").FontSize(10);
                });

                grid.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                {
                    col.Item().AlignCenter().Text($"{data.CompletionRate:F1}%").Bold().FontSize(16);
                    col.Item().AlignCenter().Text("Completion Rate").FontSize(10);
                });
            });
        }

        private void AddDepartmentPerformance(ColumnDescriptor column, ReportData data)
        {
            if (data.DepartmentStatistics == null || !data.DepartmentStatistics.Any())
            {
                column.Item().Text("No department data available").Italic();
                return;
            }

            column.Item().Text("Department Performance").Bold().FontSize(14);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Department
                    columns.RelativeColumn();  // Employees
                    columns.RelativeColumn();  // Assignments
                    columns.RelativeColumn();  // Completed
                    columns.RelativeColumn();  // Completion Rate
                });

                table.Header(header =>
                {
                    header.Cell().Text("Department").Bold();
                    header.Cell().Text("Employees").Bold();
                    header.Cell().Text("Assignments").Bold();
                    header.Cell().Text("Completed").Bold();
                    header.Cell().Text("Completion Rate").Bold();
                });

                foreach (var dept in data.DepartmentStatistics)
                {
                    table.Cell().Text(dept.DepartmentName);
                    table.Cell().Text(dept.EmployeeCount.ToString());
                    table.Cell().Text(dept.AssignmentCount.ToString());
                    table.Cell().Text(dept.CompletedCount.ToString());
                    table.Cell().Text($"{dept.CompletionRate:F1}%");
                }
            });
        }

        private void AddTrainingStatistics(ColumnDescriptor column, ReportData data)
        {
            if (data.ProgramStatistics == null || !data.ProgramStatistics.Any())
            {
                column.Item().Text("No training program data available").Italic();
                return;
            }

            column.Item().Text("Training Program Statistics").Bold().FontSize(14);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Program
                    columns.RelativeColumn(2); // Provider
                    columns.RelativeColumn();  // Assignments
                    columns.RelativeColumn();  // Completed
                    columns.RelativeColumn();  // Completion Rate
                });

                table.Header(header =>
                {
                    header.Cell().Text("Program").Bold();
                    header.Cell().Text("Provider").Bold();
                    header.Cell().Text("Assignments").Bold();
                    header.Cell().Text("Completed").Bold();
                    header.Cell().Text("Completion Rate").Bold();
                });

                foreach (var program in data.ProgramStatistics.Take(10))
                {
                    table.Cell().Text(program.ProgramTitle);
                    table.Cell().Text(program.Provider);
                    table.Cell().Text(program.AssignmentCount.ToString());
                    table.Cell().Text(program.CompletedCount.ToString());
                    table.Cell().Text($"{program.CompletionRate:F1}%");
                }
            });
        }

        private void AddSkillGapAnalysis(ColumnDescriptor column, ReportData data)
        {
            if (data.SkillGapAnalysis == null || !data.SkillGapAnalysis.Any())
            {
                column.Item().Text("No skill gap analysis data available").Italic();
                return;
            }

            column.Item().Text("Skill Gap Analysis (Top 10)").Bold().FontSize(14);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Skill
                    columns.RelativeColumn(2); // Category
                    columns.RelativeColumn();  // Employees with Skill
                    columns.RelativeColumn();  // Needing Training
                    columns.RelativeColumn();  // Average Gap
                });

                table.Header(header =>
                {
                    header.Cell().Text("Skill").Bold();
                    header.Cell().Text("Category").Bold();
                    header.Cell().Text("Employees with Skill").Bold();
                    header.Cell().Text("Needing Training").Bold();
                    header.Cell().Text("Average Gap").Bold();
                });

                foreach (var skill in data.SkillGapAnalysis.Take(10))
                {
                    table.Cell().Text(skill.SkillName);
                    table.Cell().Text(skill.Category);
                    table.Cell().Text(skill.EmployeesWithSkill.ToString());
                    table.Cell().Text(skill.EmployeesNeedingTraining.ToString());
                    table.Cell().Text($"{skill.AverageGap:F1}");
                }
            });
        }

        private void AddReportMetadata(ColumnDescriptor column, ReportRequest request)
        {
            column.Item().Text("Report Information").Bold().FontSize(14);
            column.Item().Grid(grid =>
            {
                grid.Columns(2);
                grid.Spacing(5);

                grid.Item().Text("Report Type:");
                grid.Item().Text(request.ReportType);

                grid.Item().Text("Generated By:");
                grid.Item().Text(request.CreatedBy);

                grid.Item().Text("Generated On:");
                grid.Item().Text(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"));

                grid.Item().Text("Description:");
                grid.Item().Text(string.IsNullOrEmpty(request.Description) ? "No description provided" : request.Description);
            });
        }

        // Keep all your existing helper methods (GenerateDepartmentStatsAsync, GenerateProgramStats, etc.)
        // ... all existing helper methods remain the same

        private async Task<List<DepartmentStats>> GenerateDepartmentStatsAsync(List<Employee> employees, List<Training> trainings)
        {
            var stats = new List<DepartmentStats>();

            var departments = employees
                .Where(e => !string.IsNullOrEmpty(e.Department))
                .Select(e => e.Department)
                .Distinct()
                .ToList();

            foreach (var dept in departments)
            {
                var deptEmployees = employees.Where(e => e.Department == dept).ToList();
                var deptEmployeeIds = deptEmployees.Select(e => e.Id).ToList();
                var deptTrainings = trainings.Where(t => deptEmployeeIds.Contains(t.EmployeeId)).ToList();

                var deptStats = new DepartmentStats
                {
                    DepartmentName = dept,
                    EmployeeCount = deptEmployees.Count,
                    AssignmentCount = deptTrainings.Count,
                    CompletedCount = deptTrainings.Count(t => t.Status == "Completed"),
                    AverageSkillLevel = await CalculateAverageSkillLevelAsync(deptEmployees)
                };

                deptStats.CompletionRate = deptStats.AssignmentCount > 0
                    ? (double)deptStats.CompletedCount / deptStats.AssignmentCount * 100
                    : 0;

                stats.Add(deptStats);
            }

            return stats;
        }

        private List<TrainingProgramStats> GenerateProgramStats(List<Training> trainings)
        {
            var stats = new List<TrainingProgramStats>();

            var programGroups = trainings
                .Where(t => !string.IsNullOrEmpty(t.Title))
                .GroupBy(t => t.Title)
                .ToList();

            foreach (var group in programGroups)
            {
                var programTrainings = group.ToList();
                var programStats = new TrainingProgramStats
                {
                    ProgramTitle = group.Key,
                    Provider = programTrainings.FirstOrDefault()?.Provider ?? "Unknown Provider",
                    AssignmentCount = programTrainings.Count,
                    CompletedCount = programTrainings.Count(t => t.Status == "Completed")
                };

                programStats.CompletionRate = programStats.AssignmentCount > 0
                    ? (double)programStats.CompletedCount / programStats.AssignmentCount * 100
                    : 0;

                stats.Add(programStats);
            }

            return stats;
        }

        private async Task<List<SkillGapAnalysis>> GenerateSkillGapAnalysisAsync(List<Employee> employees)
        {
            var analysis = new List<SkillGapAnalysis>();
            var allSkills = new Dictionary<string, SkillGapAnalysis>();

            foreach (var employee in employees.Take(100))
            {
                var skills = await _firestoreService.GetEmployeeSkillsAsync(employee.Id);

                foreach (var skill in skills)
                {
                    if (!allSkills.ContainsKey(skill.Name))
                    {
                        allSkills[skill.Name] = new SkillGapAnalysis
                        {
                            SkillName = skill.Name,
                            Category = skill.Category ?? "General",
                            EmployeesWithSkill = 0,
                            EmployeesNeedingTraining = 0,
                            AverageGap = 0,
                            Criticality = 0
                        };
                    }

                    var skillAnalysis = allSkills[skill.Name];
                    skillAnalysis.EmployeesWithSkill++;

                    if (skill.Gap > 0)
                    {
                        skillAnalysis.EmployeesNeedingTraining++;
                        skillAnalysis.AverageGap += skill.Gap;
                    }
                }
            }

            foreach (var skillAnalysis in allSkills.Values)
            {
                if (skillAnalysis.EmployeesNeedingTraining > 0)
                {
                    skillAnalysis.AverageGap /= skillAnalysis.EmployeesNeedingTraining;
                }

                skillAnalysis.Criticality = employees.Count > 0
                    ? (double)skillAnalysis.EmployeesNeedingTraining / employees.Count * 100
                    : 0;

                analysis.Add(skillAnalysis);
            }

            return analysis.OrderByDescending(a => a.Criticality).ToList();
        }

        private async Task<double> CalculateAverageSkillLevelAsync(List<Employee> employees)
        {
            double totalSkillLevel = 0;
            int employeeCount = 0;

            foreach (var employee in employees.Take(50))
            {
                var skills = await _firestoreService.GetEmployeeSkillsAsync(employee.Id);
                if (skills.Any())
                {
                    totalSkillLevel += skills.Average(s => ParseSkillLevel(s.Level));
                    employeeCount++;
                }
            }

            return employeeCount > 0 ? totalSkillLevel / employeeCount : 0;
        }

        private int ParseSkillLevel(string level)
        {
            if (string.IsNullOrEmpty(level)) return 0;

            return level.ToLower() switch
            {
                "beginner" => 1,
                "basic" => 2,
                "intermediate" => 3,
                "advanced" => 4,
                "expert" => 5,
                _ => int.TryParse(level, out int result) ? result : 0
            };
        }

        // Keep TestDataRetrieval method
        public async Task<string> TestDataRetrieval()
        {
            // ... your existing TestDataRetrieval implementation
            var result = new StringBuilder();
            try
            {
                result.AppendLine("🧪 STARTING DATA RETRIEVAL TEST");
                // ... rest of your TestDataRetrieval implementation
                return result.ToString();
            }
            catch (Exception ex)
            {
                result.AppendLine("💥 CRITICAL ERROR IN DATA RETRIEVAL TEST");
                result.AppendLine($"Error: {ex.Message}");
                return result.ToString();
            }
        }
    }
}