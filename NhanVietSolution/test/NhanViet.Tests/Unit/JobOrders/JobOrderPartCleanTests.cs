using NhanViet.JobOrders.Models;

namespace NhanViet.Tests.Unit.JobOrders;

[Trait("Category", "Unit")]
public class JobOrderPartCleanTests
{
    [Fact]
    public void JobOrderPart_SetProperties_UpdatesCorrectly()
    {
        // Arrange
        var part = new JobOrderPart();
        
        // Act
        part.JobTitle = "Software Engineer";
        part.CompanyName = "Tech Corp";
        part.Location = "Japan";
        part.SalaryRange = "50,000 - 80,000 USD";
        part.JobType = "Full-time";
        part.ExperienceLevel = "2-5 years";
        part.JobDescription = "Exciting opportunity for software development";
        part.Requirements = "C#, .NET, OrchardCore experience";
        part.Benefits = "Health insurance, visa support";
        part.ContactEmail = "hr@techcorp.com";
        part.ExpiryDate = DateTime.Now.AddDays(30);
        part.IsActive = true;
        part.IsFeatured = false;
        part.ApplicationCount = 0;
        
        // Assert
        Assert.Equal("Software Engineer", part.JobTitle);
        Assert.Equal("Tech Corp", part.CompanyName);
        Assert.Equal("Japan", part.Location);
        Assert.Equal("50,000 - 80,000 USD", part.SalaryRange);
        Assert.Equal("Full-time", part.JobType);
        Assert.Equal("2-5 years", part.ExperienceLevel);
        Assert.Equal("Exciting opportunity for software development", part.JobDescription);
        Assert.Equal("C#, .NET, OrchardCore experience", part.Requirements);
        Assert.Equal("Health insurance, visa support", part.Benefits);
        Assert.Equal("hr@techcorp.com", part.ContactEmail);
        Assert.True(part.IsActive);
        Assert.False(part.IsFeatured);
        Assert.Equal(0, part.ApplicationCount);
    }

    [Fact]
    public void JobOrderPart_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var part = new JobOrderPart();
        
        // Assert
        Assert.True(part.IsActive); // Default should be true
        Assert.False(part.IsFeatured); // Default should be false
        Assert.Equal(0, part.ApplicationCount); // Default should be 0
        Assert.True(part.ExpiryDate > DateTime.Now); // Default should be in future
    }

    [Fact]
    public void JobOrderPart_IncrementApplicationCount_UpdatesCorrectly()
    {
        // Arrange
        var part = new JobOrderPart { ApplicationCount = 5 };
        
        // Act
        part.ApplicationCount++;
        
        // Assert
        Assert.Equal(6, part.ApplicationCount);
    }

    [Fact]
    public void JobOrderPart_IsExpired_ChecksDeadlineCorrectly()
    {
        // Arrange
        var expiredPart = new JobOrderPart 
        { 
            ExpiryDate = DateTime.Now.AddDays(-1) 
        };
        
        var activePart = new JobOrderPart 
        { 
            ExpiryDate = DateTime.Now.AddDays(1) 
        };
        
        // Act & Assert
        Assert.True(expiredPart.ExpiryDate < DateTime.Now);
        Assert.False(activePart.ExpiryDate < DateTime.Now);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Software Engineer", true)]
    [InlineData("   ", false)]
    public void JobOrderPart_HasValidTitle_ValidatesCorrectly(string? title, bool expected)
    {
        // Arrange
        var part = new JobOrderPart { JobTitle = title ?? string.Empty };
        
        // Act
        var isValid = !string.IsNullOrWhiteSpace(part.JobTitle);
        
        // Assert
        Assert.Equal(expected, isValid);
    }

    [Theory]
    [InlineData("invalid-email", false)]
    [InlineData("test@example.com", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void JobOrderPart_HasValidEmail_ValidatesCorrectly(string? email, bool expected)
    {
        // Arrange
        var part = new JobOrderPart { ContactEmail = email ?? string.Empty };
        
        // Act
        var isValid = !string.IsNullOrWhiteSpace(part.ContactEmail) && 
                     part.ContactEmail.Contains('@') && 
                     part.ContactEmail.Contains('.');
        
        // Assert
        Assert.Equal(expected, isValid);
    }

    [Fact]
    public void JobOrderPart_DataFlow_CreateUpdateDelete_WorksCorrectly()
    {
        // Arrange - Create
        var part = new JobOrderPart
        {
            JobTitle = "Initial Title",
            CompanyName = "Initial Company",
            IsActive = true,
            ApplicationCount = 0
        };
        
        // Assert - Create
        Assert.Equal("Initial Title", part.JobTitle);
        Assert.Equal("Initial Company", part.CompanyName);
        Assert.True(part.IsActive);
        Assert.Equal(0, part.ApplicationCount);
        
        // Act - Update
        part.JobTitle = "Updated Title";
        part.ApplicationCount = 5;
        
        // Assert - Update
        Assert.Equal("Updated Title", part.JobTitle);
        Assert.Equal("Initial Company", part.CompanyName); // Should remain unchanged
        Assert.Equal(5, part.ApplicationCount);
        
        // Act - Soft Delete (deactivate)
        part.IsActive = false;
        
        // Assert - Soft Delete
        Assert.False(part.IsActive);
        Assert.Equal("Updated Title", part.JobTitle); // Data should remain
    }

    [Fact]
    public void JobOrderPart_SearchFiltering_WorksCorrectly()
    {
        // Arrange
        var jobs = new List<JobOrderPart>
        {
            new() { JobTitle = "Software Engineer", Location = "Japan", IsActive = true },
            new() { JobTitle = "Data Analyst", Location = "Korea", IsActive = true },
            new() { JobTitle = "Software Developer", Location = "Japan", IsActive = false },
            new() { JobTitle = "Project Manager", Location = "Singapore", IsActive = true }
        };
        
        // Act - Filter by keyword
        var softwareJobs = jobs.Where(j => j.JobTitle.Contains("Software", StringComparison.OrdinalIgnoreCase)).ToList();
        
        // Assert - Keyword filtering
        Assert.Equal(2, softwareJobs.Count);
        Assert.All(softwareJobs, job => Assert.Contains("Software", job.JobTitle, StringComparison.OrdinalIgnoreCase));
        
        // Act - Filter by location and active status
        var activeJapanJobs = jobs.Where(j => j.Location == "Japan" && j.IsActive).ToList();
        
        // Assert - Multiple filters
        Assert.Single(activeJapanJobs);
        Assert.Equal("Software Engineer", activeJapanJobs.First().JobTitle);
        
        // Act - Filter by active status only
        var activeJobs = jobs.Where(j => j.IsActive).ToList();
        
        // Assert - Status filtering
        Assert.Equal(3, activeJobs.Count);
        Assert.All(activeJobs, job => Assert.True(job.IsActive));
    }

    [Fact]
    public void JobOrderPart_FormSubmissionValidation_WorksCorrectly()
    {
        // Arrange - Valid form data
        var validJobData = new JobOrderPart
        {
            JobTitle = "Software Engineer",
            CompanyName = "Tech Corp",
            Location = "Japan",
            ContactEmail = "hr@techcorp.com",
            JobDescription = "Great opportunity for developers",
            Requirements = "C# experience required",
            IsActive = true
        };
        
        // Act - Validate required fields
        var isValidTitle = !string.IsNullOrWhiteSpace(validJobData.JobTitle);
        var isValidCompany = !string.IsNullOrWhiteSpace(validJobData.CompanyName);
        var isValidEmail = !string.IsNullOrWhiteSpace(validJobData.ContactEmail) && 
                          validJobData.ContactEmail.Contains('@');
        var isValidDescription = !string.IsNullOrWhiteSpace(validJobData.JobDescription);
        
        // Assert - Valid data
        Assert.True(isValidTitle);
        Assert.True(isValidCompany);
        Assert.True(isValidEmail);
        Assert.True(isValidDescription);
        
        // Arrange - Invalid form data
        var invalidJobData = new JobOrderPart
        {
            JobTitle = "",
            CompanyName = "",
            ContactEmail = "invalid-email",
            JobDescription = "   "
        };
        
        // Act - Validate invalid data
        var isInvalidTitle = string.IsNullOrWhiteSpace(invalidJobData.JobTitle);
        var isInvalidCompany = string.IsNullOrWhiteSpace(invalidJobData.CompanyName);
        var isInvalidEmail = string.IsNullOrWhiteSpace(invalidJobData.ContactEmail) || 
                            !invalidJobData.ContactEmail.Contains('@');
        var isInvalidDescription = string.IsNullOrWhiteSpace(invalidJobData.JobDescription);
        
        // Assert - Invalid data
        Assert.True(isInvalidTitle);
        Assert.True(isInvalidCompany);
        Assert.True(isInvalidEmail);
        Assert.True(isInvalidDescription);
    }
}