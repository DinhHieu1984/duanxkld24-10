using NhanViet.Consultation.Models;
using OrchardCore.ContentManagement;

namespace NhanViet.Consultation.ViewModels;

public class ConsultationPartViewModel
{
    public string ClientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";
    public string AssignedTo { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? FollowUpDate { get; set; }
    public string PreferredContactMethod { get; set; } = "Email";
    public string Country { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public int EstimatedBudget { get; set; } = 0;
    public string Requirements { get; set; } = string.Empty;
    public DateTime? CompletedDate { get; set; }
    public int Rating { get; set; } = 0;
    public string Feedback { get; set; } = string.Empty;
    
    public ConsultationPart ConsultationPart { get; set; } = new();
    public ContentItem ContentItem { get; set; } = new();
}