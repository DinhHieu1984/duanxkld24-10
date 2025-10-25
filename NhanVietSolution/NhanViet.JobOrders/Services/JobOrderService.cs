using NhanViet.JobOrders.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Entities;
using YesSql;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace NhanViet.JobOrders.Services
{
    /// <summary>
    /// JobOrder filter options for service layer
    /// </summary>
    public class JobOrderFilterOptions
    {
        public string? JobType { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? Location { get; set; }
        public string? SalaryRange { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsFeatured { get; set; }
        public string? CompanyName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ExpiringWithinDays { get; set; }
        public bool? IsExpired { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedUtc";
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Implementation của IJobOrderService
    /// </summary>
    public class JobOrderService : IJobOrderService
    {
        private readonly IContentManager _contentManager;
        private readonly ISession _session;
        private readonly ILogger<JobOrderService> _logger;

        public JobOrderService(
            IContentManager contentManager,
            ISession session,
            ILogger<JobOrderService> logger)
        {
            _contentManager = contentManager;
            _session = session;
            _logger = logger;
        }

        public async Task<IEnumerable<ContentItem>> GetJobOrdersAsync(JobOrderFilterOptions options = null)
        {
            try
            {
                options ??= new JobOrderFilterOptions();
                
                var allJobOrders = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "JobOrder" && x.Published).ListAsync();
                
                var filteredJobOrders = allJobOrders.Where(item =>
                {
                    var part = item.As<JobOrderPart>();
                    
                    if (!string.IsNullOrEmpty(options.JobType) && part.JobType != options.JobType)
                        return false;
                        
                    if (!string.IsNullOrEmpty(options.ExperienceLevel) && part.ExperienceLevel != options.ExperienceLevel)
                        return false;
                        
                    if (!string.IsNullOrEmpty(options.Location) && !part.Location.Contains(options.Location))
                        return false;
                        
                    if (options.IsActive.HasValue && part.IsActive != options.IsActive.Value)
                        return false;
                        
                    if (options.IsFeatured.HasValue && part.IsFeatured != options.IsFeatured.Value)
                        return false;
                        
                    if (!string.IsNullOrEmpty(options.CompanyName) && !part.CompanyName.Contains(options.CompanyName))
                        return false;
                    
                    return true;
                });

                return filteredJobOrders.Skip(options.Skip).Take(options.Take);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job orders");
                return new List<ContentItem>();
            }
        }

        public async Task<ContentItem> GetJobOrderAsync(string contentItemId)
        {
            try
            {
                return await _contentManager.GetAsync(contentItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job order {ContentItemId}", contentItemId);
                return null;
            }
        }

        public async Task<ContentItem> CreateJobOrderAsync(JobOrderPart jobOrderPart)
        {
            try
            {
                var contentItem = await _contentManager.NewAsync("JobOrder");
                var part = contentItem.As<JobOrderPart>();
                
                // Map properties correctly - only use available properties
                part.JobTitle = jobOrderPart.JobTitle;
                part.JobDescription = jobOrderPart.JobDescription;
                part.Requirements = jobOrderPart.Requirements;
                part.Benefits = jobOrderPart.Benefits;
                part.Location = jobOrderPart.Location;
                part.SalaryRange = jobOrderPart.SalaryRange;
                part.JobType = jobOrderPart.JobType;
                part.ExperienceLevel = jobOrderPart.ExperienceLevel;
                part.PostedDate = jobOrderPart.PostedDate;
                part.ExpiryDate = jobOrderPart.ExpiryDate;
                part.ContactEmail = jobOrderPart.ContactEmail;
                part.ContactPhone = jobOrderPart.ContactPhone;
                part.CompanyName = jobOrderPart.CompanyName;
                part.IsActive = jobOrderPart.IsActive;
                part.IsFeatured = jobOrderPart.IsFeatured;
                part.ApplicationCount = jobOrderPart.ApplicationCount;

                await _contentManager.CreateAsync(contentItem);
                return contentItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job order");
                return null;
            }
        }

        public async Task<ContentItem> UpdateJobOrderAsync(string contentItemId, JobOrderPart jobOrderPart)
        {
            try
            {
                var contentItem = await _contentManager.GetAsync(contentItemId);
                if (contentItem == null) return null;

                var part = contentItem.As<JobOrderPart>();
                
                // Map properties correctly - only use available properties
                part.JobTitle = jobOrderPart.JobTitle;
                part.JobDescription = jobOrderPart.JobDescription;
                part.Requirements = jobOrderPart.Requirements;
                part.Benefits = jobOrderPart.Benefits;
                part.Location = jobOrderPart.Location;
                part.SalaryRange = jobOrderPart.SalaryRange;
                part.JobType = jobOrderPart.JobType;
                part.ExperienceLevel = jobOrderPart.ExperienceLevel;
                part.PostedDate = jobOrderPart.PostedDate;
                part.ExpiryDate = jobOrderPart.ExpiryDate;
                part.ContactEmail = jobOrderPart.ContactEmail;
                part.ContactPhone = jobOrderPart.ContactPhone;
                part.CompanyName = jobOrderPart.CompanyName;
                part.IsActive = jobOrderPart.IsActive;
                part.IsFeatured = jobOrderPart.IsFeatured;
                part.ApplicationCount = jobOrderPart.ApplicationCount;

                await _contentManager.UpdateAsync(contentItem);
                return contentItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job order {ContentItemId}", contentItemId);
                return null;
            }
        }

        public async Task<bool> DeleteJobOrderAsync(string contentItemId)
        {
            try
            {
                var contentItem = await _contentManager.GetAsync(contentItemId);
                if (contentItem == null) return false;

                await _contentManager.RemoveAsync(contentItem);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job order {ContentItemId}", contentItemId);
                return false;
            }
        }

        public async Task<IEnumerable<ContentItem>> SearchJobOrdersAsync(string searchTerm, JobOrderFilterOptions options = null)
        {
            try
            {
                options ??= new JobOrderFilterOptions();
                
                var allJobOrders = await GetJobOrdersAsync(new JobOrderFilterOptions { Take = 1000 });
                
                var filteredJobOrders = allJobOrders.Where(item =>
                {
                    var part = item.As<JobOrderPart>();
                    return part.JobTitle.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           part.JobDescription.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           part.Requirements.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                });

                return filteredJobOrders.Skip(options.Skip).Take(options.Take);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching job orders");
                return new List<ContentItem>();
            }
        }

        public async Task<IEnumerable<ContentItem>> GetJobOrdersByCountryAsync(string country)
        {
            // Since JobOrderPart doesn't have Country property, we'll search in Location
            var options = new JobOrderFilterOptions { Location = country };
            return await GetJobOrdersAsync(options);
        }

        public async Task<IEnumerable<ContentItem>> GetJobOrdersByCategoryAsync(string category)
        {
            // Since JobOrderPart doesn't have Category property, we'll search in JobType
            var options = new JobOrderFilterOptions { JobType = category };
            return await GetJobOrdersAsync(options);
        }

        public async Task<IEnumerable<ContentItem>> GetActiveJobOrdersAsync()
        {
            var options = new JobOrderFilterOptions { IsActive = true };
            return await GetJobOrdersAsync(options);
        }

        public async Task<IEnumerable<ContentItem>> GetExpiringJobOrdersAsync(int daysFromNow)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(daysFromNow);
                var allJobOrders = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "JobOrder" && x.Published).ListAsync();
                
                var expiringJobOrders = allJobOrders
                    .Where(item => 
                    {
                        var part = item.As<JobOrderPart>();
                        return part.ExpiryDate <= cutoffDate && part.IsActive;
                    })
                    .ToList();

                return expiringJobOrders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring job orders");
                return new List<ContentItem>();
            }
        }

        public async Task<bool> ProcessJobApplicationAsync(string jobOrderId, JobApplicationData applicationData)
        {
            try
            {
                var jobOrder = await GetJobOrderAsync(jobOrderId);
                if (jobOrder == null) return false;

                var part = jobOrder.As<JobOrderPart>();
                part.ApplicationCount++;

                await _contentManager.UpdateAsync(jobOrder);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job application");
                return false;
            }
        }

        public async Task<JobOrderStatistics> GetJobOrderStatisticsAsync()
        {
            try
            {
                var allJobOrders = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "JobOrder" && x.Published).ListAsync();
                
                var stats = new JobOrderStatistics
                {
                    TotalJobOrders = allJobOrders.Count(),
                    ActiveJobOrders = allJobOrders.Count(item => item.As<JobOrderPart>().IsActive),
                    ExpiredJobOrders = allJobOrders.Count(item => item.As<JobOrderPart>().ExpiryDate < DateTime.UtcNow),
                    DraftJobOrders = allJobOrders.Count(item => !item.As<JobOrderPart>().IsActive),
                    TotalApplications = allJobOrders.Sum(item => item.As<JobOrderPart>().ApplicationCount)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job order statistics");
                return new JobOrderStatistics();
            }
        }

        public async Task<bool> PublishJobOrderAsync(string contentItemId)
        {
            try
            {
                var contentItem = await _contentManager.GetAsync(contentItemId);
                if (contentItem == null) return false;

                await _contentManager.PublishAsync(contentItem);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing job order {ContentItemId}", contentItemId);
                return false;
            }
        }

        public async Task<bool> UnpublishJobOrderAsync(string contentItemId)
        {
            try
            {
                var contentItem = await _contentManager.GetAsync(contentItemId);
                if (contentItem == null) return false;

                await _contentManager.UnpublishAsync(contentItem);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpublishing job order {ContentItemId}", contentItemId);
                return false;
            }
        }
    }
}