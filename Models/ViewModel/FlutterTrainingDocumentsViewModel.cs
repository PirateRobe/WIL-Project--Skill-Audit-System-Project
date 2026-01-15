namespace WebApplication2.Models.ViewModel
{
    public class FlutterTrainingDocumentsViewModel
    {
        public Employee Employee { get; set; }
        public List<Training> Trainings { get; set; } = new List<Training>();
        public List<StorageFileInfo> StorageFiles { get; set; } = new List<StorageFileInfo>();
        public int TotalCertificates { get; set; }
        public int TotalStorageFiles { get; set; }
    }
}
