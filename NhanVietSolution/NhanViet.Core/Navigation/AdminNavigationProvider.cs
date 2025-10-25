using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using System;
using System.Threading.Tasks;

namespace NhanViet.Core.Navigation
{
    /// <summary>
    /// Admin Navigation Provider for NhanViet Labor Export Management System
    /// Defines the admin menu structure for the backend management
    /// </summary>
    public class AdminNavigationProvider : INavigationProvider
    {
        private readonly IStringLocalizer S;

        public AdminNavigationProvider(IStringLocalizer<AdminNavigationProvider> localizer)
        {
            S = localizer;
        }

        public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.CompletedTask;
            }

            builder
                // Dashboard
                .Add(S["Dashboard"], "1", dashboard => dashboard
                    .Url("~/Admin")
                    .AddClass("nav-dashboard")
                    .Id("nav-dashboard")
                    .Permission(Permissions.AccessAdminPanel)
                )

                // Content Management
                .Add(S["Quản lý nội dung"], "2", content => content
                    .Url("~/Admin/Contents")
                    .AddClass("nav-content")
                    .Id("nav-content")
                    .Permission(Permissions.ManageContent)
                    
                    .Add(S["Việc làm"], "2.1", jobs => jobs
                        .Url("~/Admin/Contents/ContentTypes/JobOrder")
                        .AddClass("nav-admin-jobs")
                        .Permission(Permissions.ManageJobOrders)
                        
                        .Add(S["Danh sách việc làm"], "2.1.1", item => item
                            .Url("~/Admin/Contents/ContentItems/JobOrder")
                            .AddClass("nav-admin-job-list")
                        )
                        .Add(S["Thêm việc làm"], "2.1.2", item => item
                            .Url("~/Admin/Contents/ContentItems/JobOrder/Create")
                            .AddClass("nav-admin-job-create")
                        )
                        .Add(S["Việc làm nổi bật"], "2.1.3", item => item
                            .Url("~/Admin/Contents/ContentItems/JobOrder?featured=true")
                            .AddClass("nav-admin-job-featured")
                        )
                        .Add(S["Việc làm hết hạn"], "2.1.4", item => item
                            .Url("~/Admin/Contents/ContentItems/JobOrder?expired=true")
                            .AddClass("nav-admin-job-expired")
                        )
                    )
                    
                    .Add(S["Công ty"], "2.2", companies => companies
                        .Url("~/Admin/Contents/ContentTypes/Company")
                        .AddClass("nav-admin-companies")
                        .Permission(Permissions.ManageCompanies)
                        
                        .Add(S["Danh sách công ty"], "2.2.1", item => item
                            .Url("~/Admin/Contents/ContentItems/Company")
                            .AddClass("nav-admin-company-list")
                        )
                        .Add(S["Thêm công ty"], "2.2.2", item => item
                            .Url("~/Admin/Contents/ContentItems/Company/Create")
                            .AddClass("nav-admin-company-create")
                        )
                        .Add(S["Xác minh công ty"], "2.2.3", item => item
                            .Url("~/Admin/Contents/ContentItems/Company?verification=pending")
                            .AddClass("nav-admin-company-verification")
                        )
                        .Add(S["Công ty nổi bật"], "2.2.4", item => item
                            .Url("~/Admin/Contents/ContentItems/Company?featured=true")
                            .AddClass("nav-admin-company-featured")
                        )
                    )
                    
                    .Add(S["Tin tức"], "2.3", news => news
                        .Url("~/Admin/Contents/ContentTypes/News")
                        .AddClass("nav-admin-news")
                        .Permission(Permissions.ManageNews)
                        
                        .Add(S["Danh sách tin tức"], "2.3.1", item => item
                            .Url("~/Admin/Contents/ContentItems/News")
                            .AddClass("nav-admin-news-list")
                        )
                        .Add(S["Thêm tin tức"], "2.3.2", item => item
                            .Url("~/Admin/Contents/ContentItems/News/Create")
                            .AddClass("nav-admin-news-create")
                        )
                        .Add(S["Tin nổi bật"], "2.3.3", item => item
                            .Url("~/Admin/Contents/ContentItems/News?featured=true")
                            .AddClass("nav-admin-news-featured")
                        )
                        .Add(S["Bản th草"], "2.3.4", item => item
                            .Url("~/Admin/Contents/ContentItems/News?published=false")
                            .AddClass("nav-admin-news-draft")
                        )
                    )
                    
                    .Add(S["Quốc gia"], "2.4", countries => countries
                        .Url("~/Admin/Contents/ContentTypes/Country")
                        .AddClass("nav-admin-countries")
                        .Permission(Permissions.ManageCountries)
                        
                        .Add(S["Danh sách quốc gia"], "2.4.1", item => item
                            .Url("~/Admin/Contents/ContentItems/Country")
                            .AddClass("nav-admin-country-list")
                        )
                        .Add(S["Thêm quốc gia"], "2.4.2", item => item
                            .Url("~/Admin/Contents/ContentItems/Country/Create")
                            .AddClass("nav-admin-country-create")
                        )
                        .Add(S["Thông tin visa"], "2.4.3", item => item
                            .Url("~/Admin/Countries/VisaInfo")
                            .AddClass("nav-admin-country-visa")
                        )
                    )
                    
                    .Add(S["Tư vấn"], "2.5", consultations => consultations
                        .Url("~/Admin/Contents/ContentTypes/Consultation")
                        .AddClass("nav-admin-consultations")
                        .Permission(Permissions.ManageConsultations)
                        
                        .Add(S["Danh sách tư vấn"], "2.5.1", item => item
                            .Url("~/Admin/Contents/ContentItems/Consultation")
                            .AddClass("nav-admin-consultation-list")
                        )
                        .Add(S["Lịch hẹn mới"], "2.5.2", item => item
                            .Url("~/Admin/Contents/ContentItems/Consultation?status=pending")
                            .AddClass("nav-admin-consultation-pending")
                        )
                        .Add(S["Đang tư vấn"], "2.5.3", item => item
                            .Url("~/Admin/Contents/ContentItems/Consultation?status=in-progress")
                            .AddClass("nav-admin-consultation-progress")
                        )
                        .Add(S["Hoàn thành"], "2.5.4", item => item
                            .Url("~/Admin/Contents/ContentItems/Consultation?status=completed")
                            .AddClass("nav-admin-consultation-completed")
                        )
                    )
                )

                // User Management
                .Add(S["Quản lý người dùng"], "3", users => users
                    .Url("~/Admin/Users")
                    .AddClass("nav-users")
                    .Id("nav-users")
                    .Permission(Permissions.ManageUsers)
                    
                    .Add(S["Danh sách người dùng"], "3.1", item => item
                        .Url("~/Admin/Users")
                        .AddClass("nav-user-list")
                    )
                    .Add(S["Thêm người dùng"], "3.2", item => item
                        .Url("~/Admin/Users/Create")
                        .AddClass("nav-user-create")
                    )
                    .Add(S["Vai trò"], "3.3", item => item
                        .Url("~/Admin/Roles")
                        .AddClass("nav-user-roles")
                    )
                    .Add(S["Quyền hạn"], "3.4", item => item
                        .Url("~/Admin/Permissions")
                        .AddClass("nav-user-permissions")
                    )
                )

                // Applications Management
                .Add(S["Quản lý đơn ứng tuyển"], "4", applications => applications
                    .Url("~/Admin/Applications")
                    .AddClass("nav-applications")
                    .Id("nav-applications")
                    .Permission(Permissions.ManageApplications)
                    
                    .Add(S["Đơn mới"], "4.1", item => item
                        .Url("~/Admin/Applications?status=new")
                        .AddClass("nav-application-new")
                    )
                    .Add(S["Đang xử lý"], "4.2", item => item
                        .Url("~/Admin/Applications?status=processing")
                        .AddClass("nav-application-processing")
                    )
                    .Add(S["Đã duyệt"], "4.3", item => item
                        .Url("~/Admin/Applications?status=approved")
                        .AddClass("nav-application-approved")
                    )
                    .Add(S["Từ chối"], "4.4", item => item
                        .Url("~/Admin/Applications?status=rejected")
                        .AddClass("nav-application-rejected")
                    )
                    .Add(S["Báo cáo"], "4.5", item => item
                        .Url("~/Admin/Applications/Reports")
                        .AddClass("nav-application-reports")
                    )
                )

                // System Management
                .Add(S["Quản lý hệ thống"], "5", system => system
                    .Url("~/Admin/Settings")
                    .AddClass("nav-system")
                    .Id("nav-system")
                    .Permission(Permissions.ManageSettings)
                    
                    .Add(S["Cài đặt chung"], "5.1", item => item
                        .Url("~/Admin/Settings")
                        .AddClass("nav-system-settings")
                    )
                    .Add(S["Menu"], "5.2", item => item
                        .Url("~/Admin/Menu")
                        .AddClass("nav-system-menu")
                    )
                    .Add(S["Giao diện"], "5.3", item => item
                        .Url("~/Admin/Themes")
                        .AddClass("nav-system-themes")
                    )
                    .Add(S["Modules"], "5.4", item => item
                        .Url("~/Admin/Features")
                        .AddClass("nav-system-modules")
                    )
                    .Add(S["Backup"], "5.5", item => item
                        .Url("~/Admin/Deployment")
                        .AddClass("nav-system-backup")
                    )
                )

                // Reports & Analytics
                .Add(S["Báo cáo & Thống kê"], "6", reports => reports
                    .Url("~/Admin/Reports")
                    .AddClass("nav-reports")
                    .Id("nav-reports")
                    .Permission(Permissions.ViewReports)
                    
                    .Add(S["Tổng quan"], "6.1", item => item
                        .Url("~/Admin/Reports/Overview")
                        .AddClass("nav-report-overview")
                    )
                    .Add(S["Việc làm"], "6.2", item => item
                        .Url("~/Admin/Reports/Jobs")
                        .AddClass("nav-report-jobs")
                    )
                    .Add(S["Ứng viên"], "6.3", item => item
                        .Url("~/Admin/Reports/Candidates")
                        .AddClass("nav-report-candidates")
                    )
                    .Add(S["Công ty"], "6.4", item => item
                        .Url("~/Admin/Reports/Companies")
                        .AddClass("nav-report-companies")
                    )
                    .Add(S["Tài chính"], "6.5", item => item
                        .Url("~/Admin/Reports/Financial")
                        .AddClass("nav-report-financial")
                    )
                )

                // Tools & Utilities
                .Add(S["Công cụ"], "7", tools => tools
                    .Url("~/Admin/Tools")
                    .AddClass("nav-tools")
                    .Id("nav-tools")
                    .Permission(Permissions.AccessTools)
                    
                    .Add(S["Import/Export"], "7.1", item => item
                        .Url("~/Admin/Tools/ImportExport")
                        .AddClass("nav-tool-import-export")
                    )
                    .Add(S["Email Templates"], "7.2", item => item
                        .Url("~/Admin/Tools/EmailTemplates")
                        .AddClass("nav-tool-email-templates")
                    )
                    .Add(S["Logs"], "7.3", item => item
                        .Url("~/Admin/Tools/Logs")
                        .AddClass("nav-tool-logs")
                    )
                    .Add(S["Cache"], "7.4", item => item
                        .Url("~/Admin/Tools/Cache")
                        .AddClass("nav-tool-cache")
                    )
                    .Add(S["Database"], "7.5", item => item
                        .Url("~/Admin/Tools/Database")
                        .AddClass("nav-tool-database")
                    )
                );

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Permissions for NhanViet Admin Navigation
    /// </summary>
    public static class Permissions
    {
        public static readonly Permission AccessAdminPanel = new Permission("AccessAdminPanel", "Access Admin Panel");
        public static readonly Permission ManageContent = new Permission("ManageContent", "Manage Content");
        public static readonly Permission ManageJobOrders = new Permission("ManageJobOrders", "Manage Job Orders");
        public static readonly Permission ManageCompanies = new Permission("ManageCompanies", "Manage Companies");
        public static readonly Permission ManageNews = new Permission("ManageNews", "Manage News");
        public static readonly Permission ManageCountries = new Permission("ManageCountries", "Manage Countries");
        public static readonly Permission ManageConsultations = new Permission("ManageConsultations", "Manage Consultations");
        public static readonly Permission ManageUsers = new Permission("ManageUsers", "Manage Users");
        public static readonly Permission ManageApplications = new Permission("ManageApplications", "Manage Applications");
        public static readonly Permission ManageSettings = new Permission("ManageSettings", "Manage Settings");
        public static readonly Permission ViewReports = new Permission("ViewReports", "View Reports");
        public static readonly Permission AccessTools = new Permission("AccessTools", "Access Tools");
    }
}