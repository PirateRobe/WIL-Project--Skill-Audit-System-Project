namespace WebApplication2.Models.Support_Model
{
    public class TrainingStatusSummary
    {
        public int TotalTrainings { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Pending { get; set; }
        public int Assigned { get; set; }
        public int Cancelled { get; set; }
        public int Overdue { get; set; }
        public double CompletionRate { get; set; }
        public double AverageProgress { get; set; }

        // Calculated properties
        public int ActiveTrainings => InProgress + Pending + Assigned;
        public int NeedAttention => Overdue + (Pending > 0 ? Pending : 0);
    }
}
