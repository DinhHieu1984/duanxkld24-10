using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NhanViet.JobOrders.Services;
using NhanViet.Companies.Services;
using OrchardCore.ContentManagement;
using System.Threading;
using System.Threading.Tasks;

namespace NhanViet.Core.BackgroundServices
{
    /// <summary>
    /// Background service for job-related processing tasks
    /// Xử lý các tác vụ liên quan đến job orders
    /// </summary>
    public class JobProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobProcessingService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromHours(1); // Run every hour

        public JobProcessingService(
            IServiceProvider serviceProvider,
            ILogger<JobProcessingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Background processing loop
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Processing Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessJobOrdersAsync();
                    await ProcessCompanyDataAsync();
                    await CleanupExpiredDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in job processing cycle");
                }

                // Wait for next processing cycle
                await Task.Delay(_processingInterval, stoppingToken);
            }

            _logger.LogInformation("Job Processing Service stopped");
        }

        /// <summary>
        /// Process job orders - check expiry, update status, etc.
        /// </summary>
        private async Task ProcessJobOrdersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var jobOrderService = scope.ServiceProvider.GetRequiredService<IJobOrderService>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailNotificationService>();

            _logger.LogInformation("Processing job orders...");

            try
            {
                // Get job orders expiring soon (within 7 days)
                var expiringJobs = await jobOrderService.GetJobOrdersExpiringAsync(7);
                
                foreach (var job in expiringJobs)
                {
                    // Send expiry notification
                    var jobPart = job.As<NhanViet.JobOrders.Models.JobOrderPart>();
                    if (jobPart != null && !string.IsNullOrEmpty(jobPart.ContactEmail))
                    {
                        await emailService.QueueJobExpiryNotificationAsync(
                            jobPart.ContactEmail,
                            jobPart.JobTitle,
                            jobPart.ExpiryDate);
                    }
                }

                // Update expired job orders
                var expiredJobs = await jobOrderService.GetExpiredJobOrdersAsync();
                foreach (var job in expiredJobs)
                {
                    await jobOrderService.MarkJobOrderAsExpiredAsync(job.ContentItemId);
                }

                _logger.LogInformation("Processed {ExpiringCount} expiring jobs and {ExpiredCount} expired jobs", 
                    expiringJobs.Count(), expiredJobs.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job orders");
            }
        }

        /// <summary>
        /// Process company data - update statistics, verify companies, etc.
        /// </summary>
        private async Task ProcessCompanyDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();

            _logger.LogInformation("Processing company data...");

            try
            {
                // Update company statistics
                var companies = await companyService.GetCompaniesAsync(new CompanyFilterOptions { Take = 1000 });
                
                foreach (var company in companies)
                {
                    // Update company job count, rating, etc.
                    await companyService.UpdateCompanyStatisticsAsync(company.ContentItemId);
                }

                _logger.LogInformation("Processed {CompanyCount} companies", companies.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing company data");
            }
        }

        /// <summary>
        /// Cleanup expired data and temporary files
        /// </summary>
        private async Task CleanupExpiredDataAsync()
        {
            _logger.LogInformation("Cleaning up expired data...");

            try
            {
                // Cleanup old application files (older than 90 days)
                var cutoffDate = DateTime.UtcNow.AddDays(-90);
                
                // This would typically involve:
                // - Removing old resume files
                // - Cleaning up temporary uploads
                // - Archiving old application data
                // - Removing expired sessions

                await Task.Delay(1000); // Placeholder for actual cleanup logic

                _logger.LogInformation("Cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }
    }

    /// <summary>
    /// Extension methods for job processing
    /// </summary>
    public static class JobProcessingExtensions
    {
        /// <summary>
        /// Get job orders expiring within specified days
        /// </summary>
        public static async Task<IEnumerable<OrchardCore.ContentManagement.ContentItem>> GetJobOrdersExpiringAsync(
            this IJobOrderService service, int days)
        {
            var options = new JobOrderFilterOptions
            {
                ExpiringWithinDays = days,
                Take = 1000
            };

            return await service.GetJobOrdersAsync(options);
        }

        /// <summary>
        /// Get expired job orders
        /// </summary>
        public static async Task<IEnumerable<OrchardCore.ContentManagement.ContentItem>> GetExpiredJobOrdersAsync(
            this IJobOrderService service)
        {
            var options = new JobOrderFilterOptions
            {
                IsExpired = true,
                Take = 1000
            };

            return await service.GetJobOrdersAsync(options);
        }

        /// <summary>
        /// Mark job order as expired
        /// </summary>
        public static async Task<bool> MarkJobOrderAsExpiredAsync(
            this IJobOrderService service, string jobOrderId)
        {
            // This would update the job order status to expired
            // Implementation depends on the specific business logic
            return await service.UpdateJobOrderStatusAsync(jobOrderId, "Expired");
        }

        /// <summary>
        /// Update job order status
        /// </summary>
        public static async Task<bool> UpdateJobOrderStatusAsync(
            this IJobOrderService service, string jobOrderId, string status)
        {
            var jobOrder = await service.GetJobOrderAsync(jobOrderId);
            if (jobOrder == null) return false;

            var jobPart = jobOrder.As<NhanViet.JobOrders.Models.JobOrderPart>();
            if (jobPart == null) return false;

            jobPart.IsActive = status == "Active";
            var updatedJob = await service.UpdateJobOrderAsync(jobOrderId, jobPart);
            
            return updatedJob != null;
        }

        /// <summary>
        /// Update company statistics
        /// </summary>
        public static async Task<bool> UpdateCompanyStatisticsAsync(
            this ICompanyService service, string companyId)
        {
            // This would calculate and update company statistics
            // like job count, average rating, etc.
            
            // Placeholder implementation
            await Task.Delay(100);
            return true;
        }
    }
}