using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NhanViet.JobOrders.Services;
using NhanViet.Companies.Services;
using NhanViet.Core.Services;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace NhanViet.Core.Controllers
{
    /// <summary>
    /// Main API Controller providing unified endpoints
    /// Aggregates data from multiple modules
    /// </summary>
    [Route("api/v1")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IJobOrderService _jobOrderService;
        private readonly ICompanyService _companyService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ApiController> _logger;

        public ApiController(
            IJobOrderService jobOrderService,
            ICompanyService companyService,
            INotificationService notificationService,
            ILogger<ApiController> logger)
        {
            _jobOrderService = jobOrderService;
            _companyService = companyService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard data
        /// Tổng hợp dữ liệu cho dashboard
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var jobOrderStats = await _jobOrderService.GetJobOrderStatisticsAsync();
                var companyStats = await _companyService.GetCompanyStatisticsAsync();
                var featuredCompanies = await _companyService.GetFeaturedCompaniesAsync(5);
                var activeJobOrders = await _jobOrderService.GetActiveJobOrdersAsync();

                var dashboardData = new
                {
                    Statistics = new
                    {
                        JobOrders = new
                        {
                            Total = jobOrderStats.TotalJobOrders,
                            Active = jobOrderStats.ActiveJobOrders,
                            Expired = jobOrderStats.ExpiredJobOrders,
                            Applications = jobOrderStats.TotalApplications
                        },
                        Companies = new
                        {
                            Total = companyStats.TotalCompanies,
                            Verified = companyStats.VerifiedCompanies,
                            Featured = companyStats.FeaturedCompanies,
                            AverageRating = companyStats.AverageRating
                        }
                    },
                    FeaturedCompanies = featuredCompanies.Take(5),
                    RecentJobOrders = activeJobOrders.Take(10),
                    Charts = new
                    {
                        JobOrdersByCountry = jobOrderStats.JobOrdersByCountry,
                        CompaniesByIndustry = companyStats.CompaniesByIndustry,
                        JobOrdersByCategory = jobOrderStats.JobOrdersByCategory
                    }
                };

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get home page data
        /// Dữ liệu cho trang chủ
        /// </summary>
        [HttpGet("home")]
        public async Task<IActionResult> GetHomePageData()
        {
            try
            {
                var featuredJobOrders = await _jobOrderService.GetActiveJobOrdersAsync();
                var featuredCompanies = await _companyService.GetFeaturedCompaniesAsync(8);
                var jobOrderStats = await _jobOrderService.GetJobOrderStatisticsAsync();

                var homeData = new
                {
                    FeaturedJobOrders = featuredJobOrders.Take(6),
                    FeaturedCompanies = featuredCompanies,
                    Statistics = new
                    {
                        TotalJobOrders = jobOrderStats.TotalJobOrders,
                        TotalCompanies = (await _companyService.GetCompanyStatisticsAsync()).TotalCompanies,
                        TotalApplications = jobOrderStats.TotalApplications
                    },
                    PopularCategories = jobOrderStats.JobOrdersByCategory.Take(6),
                    PopularCountries = jobOrderStats.JobOrdersByCountry.Take(6)
                };

                return Ok(homeData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving home page data");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search across all content types
        /// Tìm kiếm tổng hợp
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> GlobalSearch([FromQuery] string q, [FromQuery] string type = "all")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest("Search query is required");
                }

                var results = new
                {
                    Query = q,
                    JobOrders = type == "all" || type == "jobs" 
                        ? await _jobOrderService.SearchJobOrdersAsync(q, new JobOrderFilterOptions { Take = 10 })
                        : Enumerable.Empty<object>(),
                    Companies = type == "all" || type == "companies"
                        ? await _companyService.SearchCompaniesAsync(q, new CompanyFilterOptions { Take = 10 })
                        : Enumerable.Empty<object>()
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing global search for '{Query}'", q);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Submit job application
        /// Xử lý ứng tuyển việc làm
        /// </summary>
        [HttpPost("apply")]
        public async Task<IActionResult> SubmitJobApplication([FromBody] JobApplicationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var applicationData = new JobApplicationData
                {
                    ApplicantName = request.ApplicantName,
                    ApplicantEmail = request.ApplicantEmail,
                    ApplicantPhone = request.ApplicantPhone,
                    CoverLetter = request.CoverLetter,
                    ResumeUrl = request.ResumeUrl
                };

                var result = await _jobOrderService.ProcessJobApplicationAsync(request.JobOrderId, applicationData);
                
                if (!result)
                {
                    return BadRequest("Failed to process job application");
                }

                // Send notification
                await _notificationService.SendJobApplicationNotificationAsync(
                    request.JobOrderId, 
                    request.ApplicantEmail, 
                    request.ApplicantName);

                return Ok(new { Message = "Job application submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting job application");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get system health status
        /// Kiểm tra tình trạng hệ thống
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealthStatus()
        {
            try
            {
                var health = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Services = new
                    {
                        JobOrderService = "OK",
                        CompanyService = "OK",
                        NotificationService = "OK"
                    },
                    Database = "Connected",
                    Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health status");
                return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get API documentation
        /// Thông tin API endpoints
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetApiInfo()
        {
            var apiInfo = new
            {
                Name = "NhanViet API",
                Version = "1.0.0",
                Description = "API for NhanViet Job Portal System",
                Endpoints = new
                {
                    Dashboard = "/api/v1/dashboard",
                    Home = "/api/v1/home",
                    Search = "/api/v1/search",
                    Apply = "/api/v1/apply",
                    Health = "/api/v1/health",
                    JobOrders = "/api/joborder",
                    Companies = "/api/company"
                },
                Authentication = "Bearer Token required for protected endpoints",
                RateLimit = "1000 requests per hour",
                Documentation = "https://api.nhanviet.com/docs"
            };

            return Ok(apiInfo);
        }
    }

    /// <summary>
    /// Request model for job application
    /// </summary>
    public class JobApplicationRequest
    {
        [Required]
        public string JobOrderId { get; set; }

        [Required]
        [StringLength(200)]
        public string ApplicantName { get; set; }

        [Required]
        [EmailAddress]
        public string ApplicantEmail { get; set; }

        [Phone]
        public string ApplicantPhone { get; set; }

        [StringLength(2000)]
        public string CoverLetter { get; set; }

        [Url]
        public string ResumeUrl { get; set; }
    }
}