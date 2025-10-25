using NhanViet.News.Models;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NhanViet.News.Services
{
    /// <summary>
    /// Service interface for News business logic
    /// </summary>
    public interface INewsService
    {
        /// <summary>
        /// Lấy danh sách News với filtering và pagination
        /// </summary>
        Task<IEnumerable<ContentItem>> GetNewsAsync(NewsFilterOptions options = null);

        /// <summary>
        /// Lấy News theo ID
        /// </summary>
        Task<ContentItem> GetNewsAsync(string contentItemId);

        /// <summary>
        /// Tạo News mới
        /// </summary>
        Task<ContentItem> CreateNewsAsync(NewsPart newsPart);

        /// <summary>
        /// Cập nhật News
        /// </summary>
        Task<ContentItem> UpdateNewsAsync(string contentItemId, NewsPart newsPart);

        /// <summary>
        /// Xóa News
        /// </summary>
        Task<bool> DeleteNewsAsync(string contentItemId);

        /// <summary>
        /// Tìm kiếm News
        /// </summary>
        Task<IEnumerable<ContentItem>> SearchNewsAsync(string searchTerm, NewsFilterOptions options = null);

        /// <summary>
        /// Lấy News theo category
        /// </summary>
        Task<IEnumerable<ContentItem>> GetNewsByCategoryAsync(string category);

        /// <summary>
        /// Lấy Featured News
        /// </summary>
        Task<IEnumerable<ContentItem>> GetFeaturedNewsAsync(int count = 10);

        /// <summary>
        /// Lấy Latest News
        /// </summary>
        Task<IEnumerable<ContentItem>> GetLatestNewsAsync(int count = 10);

        /// <summary>
        /// Lấy Popular News
        /// </summary>
        Task<IEnumerable<ContentItem>> GetPopularNewsAsync(int count = 10);

        /// <summary>
        /// Lấy Related News
        /// </summary>
        Task<IEnumerable<ContentItem>> GetRelatedNewsAsync(string newsId, int count = 5);

        /// <summary>
        /// Increment view count
        /// </summary>
        Task<bool> IncrementViewCountAsync(string contentItemId);

        /// <summary>
        /// Lấy thống kê News
        /// </summary>
        Task<NewsStatistics> GetNewsStatisticsAsync();

        /// <summary>
        /// Publish News
        /// </summary>
        Task<bool> PublishNewsAsync(string contentItemId);

        /// <summary>
        /// Unpublish News
        /// </summary>
        Task<bool> UnpublishNewsAsync(string contentItemId);

        /// <summary>
        /// Feature News
        /// </summary>
        Task<bool> FeatureNewsAsync(string contentItemId);

        /// <summary>
        /// Unfeature News
        /// </summary>
        Task<bool> UnfeatureNewsAsync(string contentItemId);
    }

    /// <summary>
    /// Filter options cho News queries
    /// </summary>
    public class NewsFilterOptions
    {
        public string Category { get; set; }
        public string Author { get; set; }
        public bool? IsFeatured { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedUtc";
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Thống kê News
    /// </summary>
    public class NewsStatistics
    {
        public int TotalNews { get; set; }
        public int PublishedNews { get; set; }
        public int DraftNews { get; set; }
        public int FeaturedNews { get; set; }
        public int TotalViews { get; set; }
        public Dictionary<string, int> NewsByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> NewsByAuthor { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ViewsByMonth { get; set; } = new Dictionary<string, int>();
        public IEnumerable<ContentItem> MostViewedNews { get; set; } = new List<ContentItem>();
        public IEnumerable<ContentItem> RecentNews { get; set; } = new List<ContentItem>();
    }
}