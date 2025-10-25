using NhanViet.Companies.Models;
using OrchardCore.ContentManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NhanViet.Companies.Services
{
    /// <summary>
    /// Service interface for Company business logic
    /// </summary>
    public interface ICompanyService
    {
        /// <summary>
        /// Lấy danh sách Companies với filtering và pagination
        /// </summary>
        Task<IEnumerable<ContentItem>> GetCompaniesAsync(CompanyFilterOptions options = null);

        /// <summary>
        /// Lấy Company theo ID
        /// </summary>
        Task<ContentItem> GetCompanyAsync(string contentItemId);

        /// <summary>
        /// Tạo Company mới
        /// </summary>
        Task<ContentItem> CreateCompanyAsync(CompanyPart companyPart);

        /// <summary>
        /// Cập nhật Company
        /// </summary>
        Task<ContentItem> UpdateCompanyAsync(string contentItemId, CompanyPart companyPart);

        /// <summary>
        /// Xóa Company
        /// </summary>
        Task<bool> DeleteCompanyAsync(string contentItemId);

        /// <summary>
        /// Tìm kiếm Companies
        /// </summary>
        Task<IEnumerable<ContentItem>> SearchCompaniesAsync(string searchTerm, CompanyFilterOptions options = null);

        /// <summary>
        /// Lấy Companies theo industry
        /// </summary>
        Task<IEnumerable<ContentItem>> GetCompaniesByIndustryAsync(string industry);

        /// <summary>
        /// Lấy Companies theo size
        /// </summary>
        Task<IEnumerable<ContentItem>> GetCompaniesBySizeAsync(string companySize);

        /// <summary>
        /// Lấy Companies theo location
        /// </summary>
        Task<IEnumerable<ContentItem>> GetCompaniesByLocationAsync(string location);

        /// <summary>
        /// Lấy Featured Companies
        /// </summary>
        Task<IEnumerable<ContentItem>> GetFeaturedCompaniesAsync(int count = 10);

        /// <summary>
        /// Lấy thống kê Companies
        /// </summary>
        Task<CompanyStatistics> GetCompanyStatisticsAsync();

        /// <summary>
        /// Verify Company
        /// </summary>
        Task<bool> VerifyCompanyAsync(string contentItemId);

        /// <summary>
        /// Unverify Company
        /// </summary>
        Task<bool> UnverifyCompanyAsync(string contentItemId);

        /// <summary>
        /// Feature Company
        /// </summary>
        Task<bool> FeatureCompanyAsync(string contentItemId);

        /// <summary>
        /// Unfeature Company
        /// </summary>
        Task<bool> UnfeatureCompanyAsync(string contentItemId);
    }



    /// <summary>
    /// Thống kê Companies
    /// </summary>
    public class CompanyStatistics
    {
        public int TotalCompanies { get; set; }
        public int VerifiedCompanies { get; set; }
        public int FeaturedCompanies { get; set; }
        public int ActiveCompanies { get; set; }
        public Dictionary<string, int> CompaniesByIndustry { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CompaniesBySize { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CompaniesByLocation { get; set; } = new Dictionary<string, int>();
        public int TotalJobPostings { get; set; }
        public double AverageRating { get; set; }
    }
}