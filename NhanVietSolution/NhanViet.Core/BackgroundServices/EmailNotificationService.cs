using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NhanViet.Core.Services;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NhanViet.Core.BackgroundServices
{
    /// <summary>
    /// Background service for processing email notifications
    /// Xử lý email notifications trong background
    /// </summary>
    public class EmailNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly Channel<EmailNotificationItem> _queue;
        private readonly ChannelWriter<EmailNotificationItem> _writer;
        private readonly ChannelReader<EmailNotificationItem> _reader;

        public EmailNotificationService(
            IServiceProvider serviceProvider,
            ILogger<EmailNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Create unbounded channel for email queue
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            };

            _queue = Channel.CreateUnbounded<EmailNotificationItem>(options);
            _writer = _queue.Writer;
            _reader = _queue.Reader;
        }

        /// <summary>
        /// Queue email for sending
        /// </summary>
        public async Task QueueEmailAsync(EmailNotificationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            await _writer.WriteAsync(item);
        }

        /// <summary>
        /// Background processing loop
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Notification Service started");

            await foreach (var item in _reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessEmailNotificationAsync(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email notification for {Recipient}", item.Recipient);
                    
                    // Retry logic
                    if (item.RetryCount < 3)
                    {
                        item.RetryCount++;
                        item.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, item.RetryCount)); // Exponential backoff
                        
                        // Re-queue for retry
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        await _writer.WriteAsync(item, stoppingToken);
                    }
                    else
                    {
                        _logger.LogError("Failed to send email to {Recipient} after {RetryCount} attempts", 
                            item.Recipient, item.RetryCount);
                    }
                }
            }

            _logger.LogInformation("Email Notification Service stopped");
        }

        /// <summary>
        /// Process individual email notification
        /// </summary>
        private async Task ProcessEmailNotificationAsync(EmailNotificationItem item)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            _logger.LogInformation("Processing email notification for {Recipient}", item.Recipient);

            var emailData = new EmailNotificationData
            {
                To = item.Recipient,
                Subject = item.Subject,
                Body = item.Message,
                TemplateId = item.TemplateId,
                Data = item.Data
            };

            var result = await notificationService.SendEmailNotificationAsync(emailData);

            if (result)
            {
                _logger.LogInformation("Email sent successfully to {Recipient}", item.Recipient);
            }
            else
            {
                throw new InvalidOperationException($"Failed to send email to {item.Recipient}");
            }
        }

        public override void Dispose()
        {
            _writer.Complete();
            base.Dispose();
        }
    }

    /// <summary>
    /// Email notification queue item
    /// </summary>
    public class EmailNotificationItem
    {
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string TemplateId { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public int Priority { get; set; } = 1; // 1 = High, 2 = Medium, 3 = Low
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? NextRetryAt { get; set; }
        public int RetryCount { get; set; } = 0;
    }

    /// <summary>
    /// Extension methods for email notification service
    /// </summary>
    public static class EmailNotificationServiceExtensions
    {
        /// <summary>
        /// Queue job application notification
        /// </summary>
        public static async Task QueueJobApplicationNotificationAsync(
            this EmailNotificationService service,
            string jobOrderId,
            string applicantEmail,
            string applicantName)
        {
            var item = new EmailNotificationItem
            {
                Recipient = applicantEmail,
                Subject = "Job Application Confirmation",
                TemplateId = "job-application-confirmation",
                Priority = 1,
                Data = new Dictionary<string, object>
                {
                    ["JobOrderId"] = jobOrderId,
                    ["ApplicantName"] = applicantName,
                    ["ApplicationDate"] = DateTime.UtcNow
                }
            };

            await service.QueueEmailAsync(item);
        }

        /// <summary>
        /// Queue company verification notification
        /// </summary>
        public static async Task QueueCompanyVerificationNotificationAsync(
            this EmailNotificationService service,
            string companyEmail,
            string companyName,
            bool isVerified)
        {
            var item = new EmailNotificationItem
            {
                Recipient = companyEmail,
                Subject = isVerified ? "Company Verification Approved" : "Company Verification Required",
                TemplateId = isVerified ? "company-verification-approved" : "company-verification-required",
                Priority = 2,
                Data = new Dictionary<string, object>
                {
                    ["CompanyName"] = companyName,
                    ["IsVerified"] = isVerified,
                    ["VerificationDate"] = DateTime.UtcNow
                }
            };

            await service.QueueEmailAsync(item);
        }

        /// <summary>
        /// Queue job expiry notification
        /// </summary>
        public static async Task QueueJobExpiryNotificationAsync(
            this EmailNotificationService service,
            string companyEmail,
            string jobTitle,
            DateTime expiryDate)
        {
            var item = new EmailNotificationItem
            {
                Recipient = companyEmail,
                Subject = "Job Posting Expiry Reminder",
                TemplateId = "job-expiry-reminder",
                Priority = 2,
                Data = new Dictionary<string, object>
                {
                    ["JobTitle"] = jobTitle,
                    ["ExpiryDate"] = expiryDate,
                    ["DaysUntilExpiry"] = (expiryDate - DateTime.UtcNow).Days
                }
            };

            await service.QueueEmailAsync(item);
        }
    }
}