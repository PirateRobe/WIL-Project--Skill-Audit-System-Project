using WebApplication2.Models;

namespace WebApplication2.Interface
{
    public interface IReportService
    {

        Task<ReportData> GenerateReportDataAsync(ReportRequest request);
        Task<byte[]> GeneratePdfReportAsync(ReportData data, ReportRequest request);
        byte[] GeneratePdfFromHtml(string htmlContent);
    }
}