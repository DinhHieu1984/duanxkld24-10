using OrchardCore.ContentManagement;
using System.Text.Json.Serialization;

namespace NhanViet.Companies.Models;

public class CompanyPart : ContentPart
{
    
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EmployeeCount { get; set; } = 0;
    public DateTime EstablishedDate { get; set; } = DateTime.Now;
    public string LogoUrl { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    
    public void Apply(CompanyPart other)
    {
        if (other == null) return;
        
        if (!string.IsNullOrEmpty(other.CompanyName))
            CompanyName = other.CompanyName;
        if (!string.IsNullOrEmpty(other.Industry))
            Industry = other.Industry;
        if (!string.IsNullOrEmpty(other.Location))
            Location = other.Location;
        if (!string.IsNullOrEmpty(other.Website))
            Website = other.Website;
        if (!string.IsNullOrEmpty(other.ContactEmail))
            ContactEmail = other.ContactEmail;
        if (!string.IsNullOrEmpty(other.ContactPhone))
            ContactPhone = other.ContactPhone;
        if (!string.IsNullOrEmpty(other.Description))
            Description = other.Description;
        if (!string.IsNullOrEmpty(other.LogoUrl))
            LogoUrl = other.LogoUrl;
            
        // Update numeric and date properties
        if (other.EmployeeCount > 0)
            EmployeeCount = other.EmployeeCount;
        if (other.EstablishedDate != default(DateTime))
            EstablishedDate = other.EstablishedDate;
            
        // Update boolean properties
        IsVerified = other.IsVerified;
    }
}