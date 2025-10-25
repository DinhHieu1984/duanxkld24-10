using System.Collections.Generic;
using System.Threading.Tasks;

namespace NhanViet.Core.Services
{
    /// <summary>
    /// Service interface for notification management
    /// Hỗ trợ email, SMS, push notifications
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gửi email notification
        /// </summary>
        Task<bool> SendEmailNotificationAsync(EmailNotificationData emailData);

        /// <summary>
        /// Gửi SMS notification
        /// </summary>
        Task<bool> SendSmsNotificationAsync(SmsNotificationData smsData);

        /// <summary>
        /// Gửi push notification
        /// </summary>
        Task<bool> SendPushNotificationAsync(PushNotificationData pushData);

        /// <summary>
        /// Gửi notification cho job application
        /// </summary>
        Task<bool> SendJobApplicationNotificationAsync(string jobOrderId, string applicantEmail, string applicantName);

        /// <summary>
        /// Gửi notification cho consultation request
        /// </summary>
        Task<bool> SendConsultationRequestNotificationAsync(string consultationId, string clientEmail, string clientName);

        /// <summary>
        /// Gửi notification cho company verification
        /// </summary>
        Task<bool> SendCompanyVerificationNotificationAsync(string companyId, string companyEmail, bool isVerified);

        /// <summary>
        /// Gửi newsletter
        /// </summary>
        Task<bool> SendNewsletterAsync(string subject, string content, List<string> recipients);

        /// <summary>
        /// Gửi bulk notifications
        /// </summary>
        Task<bool> SendBulkNotificationsAsync(List<NotificationData> notifications);

        /// <summary>
        /// Lấy notification templates
        /// </summary>
        Task<NotificationTemplate> GetNotificationTemplateAsync(string templateName);

        /// <summary>
        /// Lưu notification log
        /// </summary>
        Task<bool> LogNotificationAsync(NotificationLog log);

        /// <summary>
        /// Lấy notification history
        /// </summary>
        Task<IEnumerable<NotificationLog>> GetNotificationHistoryAsync(string recipientId, int skip = 0, int take = 20);
    }
}