using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NhanViet.Companies.Models;
using NhanViet.Companies.Services;
using NhanViet.Companies.ViewModels;
using OrchardCore.ContentManagement;
using OrchardCore.Entities;
using System.Threading.Tasks;

namespace NhanViet.Companies.Controllers
{
    /// <summary>
    /// Controller for Company management
    /// Provides CRUD operations and business logic for companies
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(
            ICompanyService companyService,
            ILogger<CompanyController> logger)
        {
            _companyService = companyService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách companies với filtering và pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCompanies([FromQuery] ViewModels.CompanyFilterOptions options)
        {
            try
            {
                var serviceOptions = new Services.CompanyFilterOptions
                {
                    Industry = options.Industry,
                    CompanySize = options.CompanySize,
                    Location = options.Location,
                    IsVerified = options.IsVerified,
                    IsFeatured = options.IsFeatured,
                    Country = options.Country,
                    City = options.City,
                    MinRating = options.MinRating,
                    MaxRating = options.MaxRating
                };
                var companies = await _companyService.GetCompaniesAsync(serviceOptions);
                var viewModels = companies.Select(MapToViewModel);
                
                return Ok(new
                {
                    Data = viewModels,
                    Total = viewModels.Count(),
                    Skip = options != null ? options.Skip : 0,
                    Take = options != null ? options.Take : 20
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Lấy company theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompany(string id)
        {
            try
            {
                var company = await _companyService.GetCompanyAsync(id);
                if (company == null)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                var viewModel = MapToViewModel(company);
                return Ok(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company {CompanyId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Tạo company mới
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ManageCompanies")]
        public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var companyPart = new CompanyPart
                {
                    CompanyName = model.CompanyName,
                    Description = model.Description,
                    Industry = model.Industry,
                    Website = model.Website,
                    ContactEmail = model.Email,
                    ContactPhone = model.Phone,
                    Location = model.Address + ", " + model.City + ", " + model.Country,
                    EmployeeCount = int.TryParse(model.CompanySize, out var empCount) ? empCount : 0,
                    EstablishedDate = model.FoundedYear.HasValue ? new DateTime(model.FoundedYear.Value, 1, 1) : DateTime.Now
                };

                var company = await _companyService.CreateCompanyAsync(companyPart);
                if (company == null)
                {
                    return StatusCode(500, "Failed to create company");
                }

                var viewModel = MapToViewModel(company);
                return CreatedAtAction(nameof(GetCompany), new { id = company.ContentItemId }, viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Cập nhật company
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "ManageCompanies")]
        public async Task<IActionResult> UpdateCompany(string id, [FromBody] UpdateCompanyViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var companyPart = new CompanyPart
                {
                    CompanyName = model.CompanyName,
                    Description = model.Description,
                    Industry = model.Industry,
                    Website = model.Website,
                    ContactEmail = model.Email,
                    ContactPhone = model.Phone,
                    Location = model.Address + ", " + model.City + ", " + model.Country,
                    EmployeeCount = int.TryParse(model.CompanySize, out var empCount) ? empCount : 0,
                    EstablishedDate = model.FoundedYear.HasValue ? new DateTime(model.FoundedYear.Value, 1, 1) : DateTime.Now
                };

                var company = await _companyService.UpdateCompanyAsync(id, companyPart);
                if (company == null)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                var viewModel = MapToViewModel(company);
                return Ok(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Xóa company
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "ManageCompanies")]
        public async Task<IActionResult> DeleteCompany(string id)
        {
            try
            {
                var result = await _companyService.DeleteCompanyAsync(id);
                if (!result)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {CompanyId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Tìm kiếm companies
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchCompanies([FromQuery] string q, [FromQuery] ViewModels.CompanyFilterOptions options)
        {
            try
            {
                var serviceOptions = new Services.CompanyFilterOptions
                {
                    Industry = options.Industry,
                    CompanySize = options.CompanySize,
                    Location = options.Location,
                    IsVerified = options.IsVerified,
                    IsFeatured = options.IsFeatured,
                    Country = options.Country,
                    City = options.City,
                    MinRating = options.MinRating,
                    MaxRating = options.MaxRating
                };
                var companies = await _companyService.SearchCompaniesAsync(q, serviceOptions);
                var viewModels = companies.Select(MapToViewModel);
                
                return Ok(new
                {
                    Query = q,
                    Data = viewModels,
                    Total = viewModels.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching companies with query '{Query}'", q);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Lấy companies theo industry
        /// </summary>
        [HttpGet("by-industry/{industry}")]
        public async Task<IActionResult> GetCompaniesByIndustry(string industry)
        {
            try
            {
                var companies = await _companyService.GetCompaniesByIndustryAsync(industry);
                var viewModels = companies.Select(MapToViewModel);
                
                return Ok(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies by industry '{Industry}'", industry);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Lấy featured companies
        /// </summary>
        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedCompanies([FromQuery] int count = 10)
        {
            try
            {
                var companies = await _companyService.GetFeaturedCompaniesAsync(count);
                var viewModels = companies.Select(MapToViewModel);
                
                return Ok(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured companies");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Lấy thống kê companies
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Policy = "ViewCompanyStatistics")]
        public async Task<IActionResult> GetCompanyStatistics()
        {
            try
            {
                var statistics = await _companyService.GetCompanyStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Verify company
        /// </summary>
        [HttpPost("{id}/verify")]
        [Authorize(Policy = "ManageCompanies")]
        public async Task<IActionResult> VerifyCompany(string id)
        {
            try
            {
                var result = await _companyService.VerifyCompanyAsync(id);
                if (!result)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                return Ok(new { Message = "Company verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying company {CompanyId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Feature company
        /// </summary>
        [HttpPost("{id}/feature")]
        [Authorize(Policy = "ManageCompanies")]
        public async Task<IActionResult> FeatureCompany(string id)
        {
            try
            {
                var result = await _companyService.FeatureCompanyAsync(id);
                if (!result)
                {
                    return NotFound($"Company with ID {id} not found");
                }

                return Ok(new { Message = "Company featured successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error featuring company {CompanyId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Map ContentItem to ViewModel
        /// </summary>
        private CompanyViewModel MapToViewModel(ContentItem contentItem)
        {
            var companyPart = contentItem.As<CompanyPart>();
            
            // Parse location to extract address, city, country
            var locationParts = companyPart?.Location?.Split(", ") ?? new string[0];
            
            return new CompanyViewModel
            {
                ContentItemId = contentItem.ContentItemId,
                CompanyName = companyPart?.CompanyName,
                Description = companyPart?.Description,
                Industry = companyPart?.Industry,
                CompanySize = companyPart?.EmployeeCount.ToString(),
                Website = companyPart?.Website,
                Email = companyPart?.ContactEmail,
                Phone = companyPart?.ContactPhone,
                Address = locationParts.Length > 0 ? locationParts[0] : "",
                City = locationParts.Length > 1 ? locationParts[1] : "",
                Country = locationParts.Length > 2 ? locationParts[2] : "",
                PostalCode = "", // Not available in CompanyPart
                FoundedYear = companyPart?.EstablishedDate.Year,
                IsVerified = companyPart?.IsVerified ?? false,
                IsFeatured = false, // Not available in CompanyPart
                Rating = 0, // Not available in CompanyPart
                ReviewCount = 0, // Not available in CompanyPart
                CreatedUtc = contentItem.CreatedUtc,
                ModifiedUtc = contentItem.ModifiedUtc,
                Published = contentItem.Published
            };
        }
    }
}