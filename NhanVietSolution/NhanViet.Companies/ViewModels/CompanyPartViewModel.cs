using NhanViet.Companies.Models;
using OrchardCore.ContentManagement;

namespace NhanViet.Companies.ViewModels;

public class CompanyPartViewModel
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
    
    public CompanyPart CompanyPart { get; set; } = new();
    public ContentItem ContentItem { get; set; } = new();
}