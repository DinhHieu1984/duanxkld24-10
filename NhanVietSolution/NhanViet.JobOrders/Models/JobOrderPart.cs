using OrchardCore.ContentManagement;
using System.Text.Json.Serialization;

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
    
    public void Apply(JobOrderPart other)
    {
        if (other == null) return;
        
        if (!string.IsNullOrEmpty(other.JobTitle))
            JobTitle = other.JobTitle;
        if (!string.IsNullOrEmpty(other.JobDescription))
            JobDescription = other.JobDescription;
        if (!string.IsNullOrEmpty(other.Requirements))
            Requirements = other.Requirements;
        if (!string.IsNullOrEmpty(other.Benefits))
            Benefits = other.Benefits;
        if (other.Location != null)
            Location = string.IsNullOrEmpty(other.Location) ? null : other.Location;
        if (!string.IsNullOrEmpty(other.SalaryRange))
            SalaryRange = other.SalaryRange;
        if (!string.IsNullOrEmpty(other.JobType))
            JobType = other.JobType;
        if (!string.IsNullOrEmpty(other.ExperienceLevel))
            ExperienceLevel = other.ExperienceLevel;
        if (!string.IsNullOrEmpty(other.ContactEmail))
            ContactEmail = other.ContactEmail;
        if (!string.IsNullOrEmpty(other.ContactPhone))
            ContactPhone = other.ContactPhone;
        if (!string.IsNullOrEmpty(other.CompanyName))
            CompanyName = other.CompanyName;
        
        // Update dates if they are different from default
        if (other.PostedDate != default(DateTime))
            PostedDate = other.PostedDate;
        if (other.ExpiryDate != default(DateTime))
            ExpiryDate = other.ExpiryDate;
            
        // Update boolean and numeric properties
        IsActive = other.IsActive;
        IsFeatured = other.IsFeatured;
        if (other.ApplicationCount > 0)
            ApplicationCount = other.ApplicationCount;
    }
}