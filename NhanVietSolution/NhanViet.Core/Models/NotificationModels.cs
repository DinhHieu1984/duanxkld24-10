using System.Collections.Generic;

namespace NhanViet.Core.Services
{
    /// <summary>
    /// Notification data model
    /// </summary>
    public class NotificationData
    {
        public NotificationType Type { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Notification types
    /// </summary>
    public enum NotificationType
    {
        Email,
        SMS,
        Push
    }

    /// <summary>
    /// Bulk notification result
    /// </summary>
    public class BulkNotificationResult
    {
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Notification template
    /// </summary>
    public class NotificationTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>
    /// Notification log
    /// </summary>
    public class NotificationLog
    {
        public string Id { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Email notification data
    /// </summary>
    public class EmailNotificationData
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? From { get; set; }
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
        public Dictionary<string, object> TemplateData { get; set; } = new();
        public string? TemplateId { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    /// <summary>
    /// SMS notification data
    /// </summary>
    public class SmsNotificationData
    {
        public string To { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? From { get; set; }
    }

    /// <summary>
    /// Push notification data
    /// </summary>
    public class PushNotificationData
    {
        public string To { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }
}