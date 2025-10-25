using OrchardCore.ContentManagement;

namespace NhanViet.Recruitment.Models;

public class RecruitmentPart : ContentPart
{
    public string CandidateName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string ResumeUrl { get; set; } = string.Empty;
    public string CoverLetter { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string Education { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Applied"; // Applied, Reviewing, Interview, Hired, Rejected
    public string Notes { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; } = 0;
    public string PreferredLocation { get; set; } = string.Empty;
    public string ExpectedSalary { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public DateTime AvailableFrom { get; set; } = DateTime.Now;
    public string LinkedInProfile { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;
    public string References { get; set; } = string.Empty;
}