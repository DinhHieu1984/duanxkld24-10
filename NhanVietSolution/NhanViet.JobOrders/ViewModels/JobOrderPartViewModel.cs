using NhanViet.JobOrders.Models;
using OrchardCore.ContentManagement;

namespace NhanViet.JobOrders.ViewModels;

public class JobOrderPartViewModel
{
    public string JobTitle { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string SalaryRange { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public DateTime PostedDate { get; set; } = DateTime.Now;
    public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(30);
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int ApplicationCount { get; set; } = 0;
    
    public JobOrderPart JobOrderPart { get; set; } = new();
    public ContentItem ContentItem { get; set; } = new();
}