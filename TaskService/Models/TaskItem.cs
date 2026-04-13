using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TaskService.Models
{
    public class TaskItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // Low, Medium, High
        public string Status { get; set; } = "Open";     // Open, In Progress, Blocked, Completed
        public string? AssigneeId { get; set; }

        public string? AssigneeName { get; set; }

        public string? CreatedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        //public List<ActivityLog> StatusChangeLogs { get; set; } = new();

        public List<ActivityLog> ActivityLogs { get; set; } = new();

        // Helper for SLA
        [BsonIgnore]
        public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow && Status != "Completed";
    }

    public class ActivityLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ChangedBy { get; set; } = string.Empty;
        public string ChangeDescription { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string FromStatus { get; set; } = null!;
        public string ToStatus { get; set; } = null!;
    }
}