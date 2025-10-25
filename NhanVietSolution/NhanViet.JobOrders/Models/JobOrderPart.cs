using OrchardCore.ContentManagement;

namespace NhanViet.JobOrders.Models;

public class JobOrderPart : ContentPart
{
    public string JobTitle { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string SalaryRange { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty; // Full-time, Part-time, Contract, etc.
    public string ExperienceLevel { get; set; } = string.Empty; // Entry, Mid, Senior
    public DateTime PostedDate { get; set; } = DateTime.Now;
    public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(30);
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int ApplicationCount { get; set; } = 0;
}