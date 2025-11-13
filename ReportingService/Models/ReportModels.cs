namespace ReportingService.Models
{
    public class TasksByUserReport
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Blocked { get; set; }
        public int Completed { get; set; }
    }

    public class TasksByStatusReport
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class SLAReportItem
    {
        public string TaskId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AssigneeId { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
    }
}