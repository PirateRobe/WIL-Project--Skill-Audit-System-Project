namespace WebApplication2.Models.Support_Model
{
    public class TrainingProgressHistory
    {
        public string Id { get; set; }
        public string TrainingId { get; set; }
        public double Progress { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string LoggedBy { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

