using NhanViet.JobOrders.Models;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NhanViet.JobOrders.Services
{
    /// <summary>
    /// Service interface for JobOrder business logic
    /// Tuân thủ OrchardCore Service patterns
    /// </summary>
    public interface IJobOrderService
    {
        /// <summary>
        /// Lấy danh sách JobOrders với filtering và pagination
        /// </summary>
        Task<IEnumerable<ContentItem>> GetJobOrdersAsync(JobOrderFilterOptions options = null);

        /// <summary>
        /// Lấy JobOrder theo ID
        /// </summary>
        Task<ContentItem> GetJobOrderAsync(string contentItemId);

        /// <summary>
        /// Tạo JobOrder mới
        /// </summary>
        Task<ContentItem> CreateJobOrderAsync(JobOrderPart jobOrderPart);

        /// <summary>
        /// Cập nhật JobOrder
        /// </summary>
        Task<ContentItem> UpdateJobOrderAsync(string contentItemId, JobOrderPart jobOrderPart);

        /// <summary>
        /// Xóa JobOrder
        /// </summary>
        Task<bool> DeleteJobOrderAsync(string contentItemId);

        /// <summary>
        /// Tìm kiếm JobOrders
        /// </summary>
        Task<IEnumerable<ContentItem>> SearchJobOrdersAsync(string searchTerm, JobOrderFilterOptions options = null);

        /// <summary>
        /// Lấy JobOrders theo quốc gia
        /// </summary>
        Task<IEnumerable<ContentItem>> GetJobOrdersByCountryAsync(string countryId);

        /// <summary>
        /// Lấy JobOrders theo category
        /// </summary>
        Task<IEnumerable<ContentItem>> GetJobOrdersByCategoryAsync(string category);

        /// <summary>
        /// Lấy JobOrders đang active
        /// </summary>
        Task<IEnumerable<ContentItem>> GetActiveJobOrdersAsync();

        /// <summary>
        /// Lấy JobOrders sắp hết hạn
        /// </summary>
        Task<IEnumerable<ContentItem>> GetExpiringJobOrdersAsync(int daysFromNow = 7);

        /// <summary>
        /// Xử lý ứng tuyển JobOrder
        /// </summary>
        Task<bool> ProcessJobApplicationAsync(string jobOrderId, JobApplicationData applicationData);

        /// <summary>
        /// Lấy thống kê JobOrders
        /// </summary>
        Task<JobOrderStatistics> GetJobOrderStatisticsAsync();

        /// <summary>
        /// Publish JobOrder
        /// </summary>
        Task<bool> PublishJobOrderAsync(string contentItemId);

        /// <summary>
        /// Unpublish JobOrder
        /// </summary>
        Task<bool> UnpublishJobOrderAsync(string contentItemId);
    }



    /// <summary>
    /// Data cho job application
    /// </summary>
    public class JobApplicationData
    {
        public string ApplicantName { get; set; }
        public string ApplicantEmail { get; set; }
        public string ApplicantPhone { get; set; }
        public string CoverLetter { get; set; }
        public string ResumeUrl { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Thống kê JobOrders
    /// </summary>
    public class JobOrderStatistics
    {
        public int TotalJobOrders { get; set; }
        public int ActiveJobOrders { get; set; }
        public int ExpiredJobOrders { get; set; }
        public int DraftJobOrders { get; set; }
        public int TotalApplications { get; set; }
        public Dictionary<string, int> JobOrdersByCountry { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> JobOrdersByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal> AverageSalaryByCountry { get; set; } = new Dictionary<string, decimal>();
    }
}