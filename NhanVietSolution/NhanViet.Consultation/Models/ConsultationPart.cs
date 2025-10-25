using OrchardCore.ContentManagement;

namespace NhanViet.Consultation.Models;

public class ConsultationPart : ContentPart
{
    public string ClientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty; // Labor Supply, Recruitment, Training, etc.
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "New"; // New, InProgress, Completed, Cancelled
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent
    public string AssignedTo { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? FollowUpDate { get; set; }
    public string PreferredContactMethod { get; set; } = "Email"; // Email, Phone, Meeting
    public string Country { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public int EstimatedBudget { get; set; } = 0;
    public string Requirements { get; set; } = string.Empty;
    public DateTime? CompletedDate { get; set; }
    public int Rating { get; set; } = 0; // 1-5 stars
    public string Feedback { get; set; } = string.Empty;
}