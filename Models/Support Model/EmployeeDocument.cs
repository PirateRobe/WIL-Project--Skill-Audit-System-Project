namespace WebApplication2.Models.Support_Model
{
    public class EmployeeDocument
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string FullPath { get; set; }
        public string DocumentType { get; set; }
        public long Size { get; set; }
        public DateTime Updated { get; set; }
        public string ContentType { get; set; }
    }
}
