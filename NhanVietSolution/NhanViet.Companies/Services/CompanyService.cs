using NhanViet.Companies.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Entities;
using YesSql;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace NhanViet.Companies.Services
{
    /// <summary>
    /// Company filter options for service layer
    /// </summary>
    public class CompanyFilterOptions
    {
        public string? Industry { get; set; }
        public string? Location { get; set; }
        public string? CompanySize { get; set; }
        public bool? IsVerified { get; set; }
        public bool? IsFeatured { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public decimal? MinRating { get; set; }
        public decimal? MaxRating { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedUtc";
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Implementation của ICompanyService
    /// </summary>
    public class CompanyService : ICompanyService
    {
        private readonly IContentManager _contentManager;
        private readonly ISession _session;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(
            IContentManager contentManager,
            ISession session,
            ILogger<CompanyService> logger)
        {
            _contentManager = contentManager;
            _session = session;
            _logger = logger;
        }

        public async Task<IEnumerable<ContentItem>> GetCompaniesAsync(CompanyFilterOptions options = null)
        {
            try
            {
                options ??= new CompanyFilterOptions();
                
                var allCompanies = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "Company" && x.Published).ListAsync();
                
                var filteredCompanies = allCompanies.Where(item =>
                {
                    var part = item.As<CompanyPart>();
                    
                    if (!string.IsNullOrEmpty(options.Industry) && part.Industry != options.Industry)
                        return false;
                        
                    if (!string.IsNullOrEmpty(options.Location) && !part.Location.Contains(options.Location))
                        return false;
                        
                    if (options.IsVerified.HasValue && part.IsVerified != options.IsVerified.Value)
                        return false;
                    
                    return true;
                });

                return filteredCompanies.Skip(options.Skip).Take(options.Take);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies");
                return new List<ContentItem>();
            }
        }

        public async Task<ContentItem> GetCompanyAsync(string contentItemId)
        {
            try
            {
                return await _contentManager.GetAsync(contentItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company {ContentItemId}", contentItemId);
                return null;
            }
        }

        public async Task<ContentItem> CreateCompanyAsync(CompanyPart companyPart)
        {
            try
            {
                var contentItem = await _contentManager.NewAsync("Company");
                
                contentItem.Alter<CompanyPart>(part =>
                {
                    part.CompanyName = companyPart.CompanyName;
                    part.Description = companyPart.Description;
                    part.Industry = companyPart.Industry;
                    part.EmployeeCount = companyPart.EmployeeCount;
                    part.Website = companyPart.Website;
                    part.ContactEmail = companyPart.ContactEmail;
                    part.ContactPhone = companyPart.ContactPhone;
                    part.Location = companyPart.Location;
                    part.EstablishedDate = companyPart.EstablishedDate;
                    part.LogoUrl = companyPart.LogoUrl;
                    part.IsVerified = companyPart.IsVerified;
                });

                await _contentManager.CreateAsync(contentItem);
                return contentItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return null;
            }
        }

        public async Task<ContentItem> UpdateCompanyAsync(string contentItemId, CompanyPart companyPart)
        {
            try
            {
                var contentItem = await _contentManager.GetAsync(contentItemId);
                if (contentItem == null) return null;

                contentItem.Alter<CompanyPart>(part =>
                {
                    part.CompanyName = companyPart.CompanyName;
                    part.Description = companyPart.Description;
                    part.Industry = companyPart.Industry;
                    part.EmployeeCount = companyPart.EmployeeCount;
                    part.Website = companyPart.Website;
                    part.ContactEmail = companyPart.ContactEmail;
                    part.ContactPhone = companyPart.ContactPhone;
                    part.Location = companyPart.Location;
                    part.EstablishedDate = companyPart.EstablishedDate;
                    part.LogoUrl = companyPart.LogoUrl;
                    part.IsVerified = companyPart.IsVerified;
                });

                await _contentManager.UpdateAsync(contentItem);
                return contentItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {ContentItemId}", contentItemId);
                return null;
            }
        }

        public async Task<bool> DeleteCompanyAsync(string contentItemId)
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
                _logger.LogError(ex, "Error deleting company {ContentItemId}", contentItemId);
                return false;
            }
        }

        public async Task<IEnumerable<ContentItem>> SearchCompaniesAsync(string searchTerm, CompanyFilterOptions options = null)
        {
            try
            {
                options ??= new CompanyFilterOptions();
                
                var allCompanies = await GetCompaniesAsync(new CompanyFilterOptions { Take = 1000 });
                
                var filteredCompanies = allCompanies.Where(item =>
                {
                    var part = item.As<CompanyPart>();
                    return part.CompanyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           part.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           part.Industry.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                });

                return filteredCompanies.Skip(options.Skip).Take(options.Take);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching companies");
                return new List<ContentItem>();
            }
        }

        public async Task<IEnumerable<ContentItem>> GetCompaniesByIndustryAsync(string industry)
        {
            var options = new CompanyFilterOptions { Industry = industry };
            return await GetCompaniesAsync(options);
        }

        public async Task<IEnumerable<ContentItem>> GetCompaniesByLocationAsync(string location)
        {
            var options = new CompanyFilterOptions { Location = location };
            return await GetCompaniesAsync(options);
        }

        public async Task<IEnumerable<ContentItem>> GetVerifiedCompaniesAsync()
        {
            var options = new CompanyFilterOptions { IsVerified = true };
            return await GetCompaniesAsync(options);
        }

        public async Task<IEnumerable<ContentItem>> GetFeaturedCompaniesAsync()
        {
            // Since CompanyPart doesn't have IsFeatured, return verified companies
            var options = new CompanyFilterOptions { IsVerified = true };
            return await GetCompaniesAsync(options);
        }

        public async Task<CompanyStatistics> GetCompanyStatisticsAsync()
        {
            try
            {
                var allCompanies = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "Company" && x.Published).ListAsync();
                
                var stats = new CompanyStatistics
                {
                    TotalCompanies = allCompanies.Count(),
                    VerifiedCompanies = allCompanies.Count(item => item.As<CompanyPart>().IsVerified),
                    CompaniesByIndustry = allCompanies
                        .GroupBy(item => item.As<CompanyPart>().Industry)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    CompaniesByLocation = allCompanies
                        .GroupBy(item => item.As<CompanyPart>().Location)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company statistics");
                return new CompanyStatistics();
            }
        }

        public async Task<bool> VerifyCompanyAsync(string contentItemId)
        {
            try
            {
                var contentItem = await _contentManager.GetAsync(contentItemId);
                if (contentItem == null) return false;

                contentItem.Alter<CompanyPart>(part =>
                {
                    part.IsVerified = true;
                });

                await _contentManager.UpdateAsync(contentItem);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying company {ContentItemId}", contentItemId);
                return false;
            }
        }

        public async Task<bool> UnverifyCompanyAsync(string contentItemId)
        {
            try
            {
                var contentItem = await _contentManager.GetAsync(contentItemId);
                if (contentItem == null) return false;

                contentItem.Alter<CompanyPart>(part =>
                {
                    part.IsVerified = false;
                });

                await _contentManager.UpdateAsync(contentItem);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unverifying company {ContentItemId}", contentItemId);
                return false;
            }
        }

        public async Task<IEnumerable<ContentItem>> GetCompaniesBySizeAsync(string size)
        {
            try
            {
                var allCompanies = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "Company" && x.Published).ListAsync();
                
                return allCompanies.Where(item =>
                {
                    var part = item.As<CompanyPart>();
                    return part.EmployeeCount.ToString() == size;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies by size");
                return new List<ContentItem>();
            }
        }

        public async Task<IEnumerable<ContentItem>> GetFeaturedCompaniesAsync(int count)
        {
            // Since CompanyPart doesn't have IsFeatured, return verified companies
            var options = new CompanyFilterOptions { IsVerified = true, Take = count };
            return await GetCompaniesAsync(options);
        }

        public async Task<bool> FeatureCompanyAsync(string contentItemId)
        {
            // Since CompanyPart doesn't have IsFeatured, we'll verify the company instead
            return await VerifyCompanyAsync(contentItemId);
        }

        public async Task<bool> UnfeatureCompanyAsync(string contentItemId)
        {
            // Since CompanyPart doesn't have IsFeatured, we'll unverify the company instead
            return await UnverifyCompanyAsync(contentItemId);
        }
    }
}