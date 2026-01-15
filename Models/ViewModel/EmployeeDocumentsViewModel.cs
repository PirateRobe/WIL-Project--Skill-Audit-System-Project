// Models/ViewModel/EmployeeDocumentsViewModel.cs
using WebApplication2.Models;

namespace WebApplication2.Models.ViewModel
{
    public class EmployeeDocumentsViewModel
    {

        public Employee Employee { get; set; }
        public List<StorageDocument> FirestoreDocuments { get; set; } = new List<StorageDocument>();
        public List<StorageFileInfo> StorageFiles { get; set; } = new List<StorageFileInfo>();

        // Computed properties (read-only)
        public int TotalDocuments => FirestoreDocuments.Count + StorageFiles.Count;
        public bool HasDocuments => TotalDocuments > 0;
        public int PdfCount => FirestoreDocuments.Count(d => d.IsPdf) + StorageFiles.Count(f => f.Name.EndsWith(".pdf"));
        public int ImageCount => FirestoreDocuments.Count(d => d.IsImage) + StorageFiles.Count(f =>
            f.Name.EndsWith(".jpg") || f.Name.EndsWith(".jpeg") || f.Name.EndsWith(".png"));
    }
}