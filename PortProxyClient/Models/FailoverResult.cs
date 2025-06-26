using System;
using System.Collections.Generic;

namespace PortProxyClient.Models
{
    public class FailoverResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new List<string>();
        public int RulesChanged { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.Now;
        public string ExecutedBy { get; set; } = Environment.UserName;
        public string Reason { get; set; } = string.Empty;
        public string FromSilo { get; set; } = string.Empty;
        public string ToSilo { get; set; } = string.Empty;
        public List<RuleChange> Changes { get; set; } = new List<RuleChange>();
    }

    public class RuleChange
    {
        public string Action { get; set; } = string.Empty; // "Added", "Removed", "Modified"
        public PortForwardRule? OldRule { get; set; }
        public PortForwardRule? NewRule { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    public class FailoverEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string EventType { get; set; } = string.Empty; // "Failover", "Failback", "HealthCheck"
        public string FromSilo { get; set; } = string.Empty;
        public string ToSilo { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty; // "Manual", "Automatic", "Health"
        public string Reason { get; set; } = string.Empty;
        public string ExecutedBy { get; set; } = Environment.UserName;
        public bool Success { get; set; }
        public string Details { get; set; } = string.Empty;
        public int RulesAffected { get; set; }
        public TimeSpan Duration { get; set; }
    }
}