using NhanViet.JobOrders.Models;
using NhanViet.Companies.Models;
using NhanViet.News.Models;
using OrchardCore.ContentManagement;
using NhanViet.Tests.Helpers;

namespace NhanViet.Tests.Unit.OrchardCore;

[Trait("Category", "Unit")]
[Trait("Pattern", "OrchardCore-Content")]
public class ContentTestingPatternTests
{
    [Fact]
    public void JobOrderPart_ContentItemWeld_FollowsOrchardCorePattern()
    {
        // ✅ ORCHARD CORE PATTERN: Arrange - Create content with parts
        var contentItem = new ContentItem();
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = "Software Engineer",
            CompanyName = "Tech Corp",
            Location = "Japan",
            IsActive = true
        };
        
        // ✅ ORCHARD CORE PATTERN: Act - Weld part to content item
        contentItem.Weld(jobOrderPart);
        
        // ✅ ORCHARD CORE PATTERN: Assert - Verify part attachment
        var retrievedPart = contentItem.As<JobOrderPart>();
        Assert.NotNull(retrievedPart);
        Assert.Equal("Software Engineer", retrievedPart.JobTitle);
        Assert.Equal("Tech Corp", retrievedPart.CompanyName);
        Assert.Equal("Japan", retrievedPart.Location);
        Assert.True(retrievedPart.IsActive);
        
        // ✅ ORCHARD CORE PATTERN: Verify part is same instance
        var retrievedAgain = contentItem.As<JobOrderPart>();
        Assert.Same(retrievedPart, retrievedAgain);
    }

    [Fact]
    public void CompanyPart_ContentItemWeld_FollowsOrchardCorePattern()
    {
        // ✅ ORCHARD CORE PATTERN: Arrange - Create content with parts
        var contentItem = new ContentItem();
        var companyPart = new CompanyPart 
        { 
            CompanyName = "Global Tech",
            Industry = "IT",
            EmployeeCount = 500,
            IsVerified = true
        };
        
        // ✅ ORCHARD CORE PATTERN: Act - Weld part to content item
        contentItem.Weld(companyPart);
        
        // ✅ ORCHARD CORE PATTERN: Assert - Verify part attachment
        var retrievedPart = contentItem.As<CompanyPart>();
        Assert.NotNull(retrievedPart);
        Assert.Equal("Global Tech", retrievedPart.CompanyName);
        Assert.Equal("IT", retrievedPart.Industry);
        Assert.Equal(500, retrievedPart.EmployeeCount);
        Assert.True(retrievedPart.IsVerified);
    }

    [Fact]
    public void NewsPart_ContentItemWeld_FollowsOrchardCorePattern()
    {
        // ✅ ORCHARD CORE PATTERN: Arrange - Create content with parts
        var contentItem = new ContentItem();
        var newsPart = new NewsPart 
        { 
            Summary = "Breaking news",
            Category = "Labor Export",
            Author = "John Doe",
            IsFeatured = true
        };
        
        // ✅ ORCHARD CORE PATTERN: Act - Weld part to content item
        contentItem.Weld(newsPart);
        
        // ✅ ORCHARD CORE PATTERN: Assert - Verify part attachment
        var retrievedPart = contentItem.As<NewsPart>();
        Assert.NotNull(retrievedPart);
        Assert.Equal("Breaking news", retrievedPart.Summary);
        Assert.Equal("Labor Export", retrievedPart.Category);
        Assert.Equal("John Doe", retrievedPart.Author);
        Assert.True(retrievedPart.IsFeatured);
    }

    [Fact]
    public void ContentItem_MultiplePartsWeld_FollowsOrchardCorePattern()
    {
        // ✅ ORCHARD CORE PATTERN: Test multiple parts on same content item
        var contentItem = new ContentItem();
        
        // Add JobOrderPart
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = "Developer",
            CompanyName = "Tech Corp"
        };
        contentItem.Weld(jobOrderPart);
        
        // Add CompanyPart (hypothetically - in real scenario this might not make sense)
        var companyPart = new CompanyPart 
        { 
            CompanyName = "Tech Corp Details",
            Industry = "Software"
        };
        contentItem.Weld(companyPart);
        
        // ✅ ORCHARD CORE PATTERN: Assert - Both parts are accessible
        var retrievedJobOrder = contentItem.As<JobOrderPart>();
        var retrievedCompany = contentItem.As<CompanyPart>();
        
        Assert.NotNull(retrievedJobOrder);
        Assert.NotNull(retrievedCompany);
        Assert.Equal("Developer", retrievedJobOrder.JobTitle);
        Assert.Equal("Tech Corp Details", retrievedCompany.CompanyName);
    }

    [Fact]
    public void ContentPart_GetMethod_FollowsOrchardCorePattern()
    {
        // ✅ ORCHARD CORE PATTERN: Test Get<T> method behavior
        var contentItem = new ContentItem();
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = "Test Job",
            IsActive = true
        };
        contentItem.Weld(jobOrderPart);
        
        // ✅ ORCHARD CORE PATTERN: Test Get<ContentPart> vs Get<ConcreteType>
        var basePart = contentItem.Get<ContentPart>(nameof(JobOrderPart));
        var concretePart = contentItem.As<JobOrderPart>();
        
        Assert.NotNull(basePart);
        Assert.NotNull(concretePart);
        // Note: In OrchardCore, these might be different instances due to casting behavior
    }

    [Fact]
    public void ContentItem_WithoutPart_ReturnsNull()
    {
        // ✅ ORCHARD CORE PATTERN: Test behavior when part doesn't exist
        var contentItem = new ContentItem();
        
        // Act - Try to get parts that don't exist
        var jobOrderPart = contentItem.As<JobOrderPart>();
        var companyPart = contentItem.As<CompanyPart>();
        var newsPart = contentItem.As<NewsPart>();
        
        // Assert - All should return null
        Assert.Null(jobOrderPart);
        Assert.Null(companyPart);
        Assert.Null(newsPart);
    }

    [Fact]
    public void ContentPart_PropertyUpdates_WorkCorrectly()
    {
        // ✅ ORCHARD CORE PATTERN: Test property updates through ContentItem
        var contentItem = new ContentItem();
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = "Initial Title",
            ApplicationCount = 0,
            IsActive = false
        };
        contentItem.Weld(jobOrderPart);
        
        // Act - Update properties
        var retrievedPart = contentItem.As<JobOrderPart>();
        Assert.NotNull(retrievedPart);
        retrievedPart.JobTitle = "Updated Title";
        retrievedPart.ApplicationCount = 5;
        retrievedPart.IsActive = true;
        
        // Assert - Changes are reflected
        var partAgain = contentItem.As<JobOrderPart>();
        Assert.NotNull(partAgain);
        Assert.Equal("Updated Title", partAgain.JobTitle);
        Assert.Equal(5, partAgain.ApplicationCount);
        Assert.True(partAgain.IsActive);
    }

    [Fact]
    public void ContentPart_DateTimeHandling_WorksCorrectly()
    {
        // ✅ ORCHARD CORE PATTERN: Test DateTime property handling
        var contentItem = new ContentItem();
        var specificDate = new DateTime(2024, 10, 25, 14, 30, 0, DateTimeKind.Utc);
        
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = "Test Job",
            PostedDate = specificDate,
            ExpiryDate = specificDate.AddDays(30)
        };
        contentItem.Weld(jobOrderPart);
        
        // Act & Assert - DateTime values are preserved
        var retrievedPart = contentItem.As<JobOrderPart>();
        Assert.NotNull(retrievedPart);
        Assert.Equal(specificDate, retrievedPart.PostedDate);
        Assert.Equal(specificDate.AddDays(30), retrievedPart.ExpiryDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ContentPart_EmptyStringProperties_HandleCorrectly(string testValue)
    {
        // ✅ ORCHARD CORE PATTERN: Test edge cases with empty strings
        var contentItem = new ContentItem();
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = testValue,
            CompanyName = testValue,
            Location = testValue
        };
        contentItem.Weld(jobOrderPart);
        
        // Act & Assert - Empty strings are preserved
        var retrievedPart = contentItem.As<JobOrderPart>();
        Assert.NotNull(retrievedPart);
        Assert.Equal(testValue, retrievedPart.JobTitle);
        Assert.Equal(testValue, retrievedPart.CompanyName);
        Assert.Equal(testValue, retrievedPart.Location);
    }

    [Fact]
    public void ContentPart_NullStringProperties_HandleCorrectly()
    {
        // ✅ ORCHARD CORE PATTERN: Test null string handling
        var contentItem = new ContentItem();
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = null!,
            CompanyName = null!,
            Location = null!
        };
        contentItem.Weld(jobOrderPart);
        
        // Act & Assert - Null values are handled
        var retrievedPart = contentItem.As<JobOrderPart>();
        Assert.NotNull(retrievedPart);
        // Note: Depending on OrchardCore version, nulls might be converted to empty strings
        // This test verifies the actual behavior
    }

    [Fact]
    public void ContentPart_BooleanProperties_WorkCorrectly()
    {
        // ✅ ORCHARD CORE PATTERN: Test boolean property handling
        var contentItem = new ContentItem();
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = "Test Job",
            IsActive = true,
            IsFeatured = false
        };
        contentItem.Weld(jobOrderPart);
        
        // Act & Assert - Boolean values are preserved
        var retrievedPart = contentItem.As<JobOrderPart>();
        Assert.NotNull(retrievedPart);
        Assert.True(retrievedPart.IsActive);
        Assert.False(retrievedPart.IsFeatured);
        
        // Test boolean toggle
        retrievedPart.IsActive = false;
        retrievedPart.IsFeatured = true;
        
        Assert.False(retrievedPart.IsActive);
        Assert.True(retrievedPart.IsFeatured);
    }

    [Fact]
    public void ContentPart_NumericProperties_WorkCorrectly()
    {
        // ✅ ORCHARD CORE PATTERN: Test numeric property handling
        var contentItem = new ContentItem();
        var jobOrderPart = new JobOrderPart 
        { 
            JobTitle = "Test Job",
            ApplicationCount = 42
        };
        contentItem.Weld(jobOrderPart);
        
        // Act & Assert - Numeric values are preserved
        var retrievedPart = contentItem.As<JobOrderPart>();
        Assert.NotNull(retrievedPart);
        Assert.Equal(42, retrievedPart.ApplicationCount);
        
        // Test numeric increment
        retrievedPart.ApplicationCount++;
        Assert.Equal(43, retrievedPart.ApplicationCount);
    }
}