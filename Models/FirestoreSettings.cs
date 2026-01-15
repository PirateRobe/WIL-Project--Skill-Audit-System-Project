namespace WebApplication2.Models
{
    public class FirestoreSettings
    {
        public string ProjectId { get; set; }
        public string CredentialsPath { get; set; }
        public string StorageBucket { get; set; } // Add this property
    }
}