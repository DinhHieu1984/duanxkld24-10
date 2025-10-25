using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using System;

namespace NhanViet.Core.Routing
{
    /// <summary>
    /// Route Configuration for NhanViet Labor Export Management System
    /// Defines SEO-friendly URLs and routing patterns
    /// </summary>
    public class RouteConfiguration : IStartup
    {
        public int Order => 1000;

        public void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            // Job Routes
            routes.MapAreaControllerRoute(
                name: "Jobs_Index",
                areaName: "NhanViet.Core",
                pattern: "viec-lam",
                defaults: new { controller = "Job", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "Jobs_Search",
                areaName: "NhanViet.Core",
                pattern: "viec-lam/tim-kiem",
                defaults: new { controller = "Job", action = "Search" }
            );

            routes.MapAreaControllerRoute(
                name: "Jobs_Featured",
                areaName: "NhanViet.Core",
                pattern: "viec-lam/noi-bat",
                defaults: new { controller = "Job", action = "Featured" }
            );

            routes.MapAreaControllerRoute(
                name: "Jobs_Urgent",
                areaName: "NhanViet.Core",
                pattern: "viec-lam/tuyen-gap",
                defaults: new { controller = "Job", action = "Urgent" }
            );

            routes.MapAreaControllerRoute(
                name: "Jobs_ByCountry",
                areaName: "NhanViet.Core",
                pattern: "viec-lam/theo-quoc-gia/{country?}",
                defaults: new { controller = "Job", action = "ByCountry" }
            );

            routes.MapAreaControllerRoute(
                name: "Jobs_ByIndustry",
                areaName: "NhanViet.Core",
                pattern: "viec-lam/theo-nganh-nghe/{industry?}",
                defaults: new { controller = "Job", action = "ByIndustry" }
            );

            routes.MapAreaControllerRoute(
                name: "Jobs_Detail",
                areaName: "NhanViet.Core",
                pattern: "viec-lam/{slug}",
                defaults: new { controller = "Job", action = "Detail" }
            );

            routes.MapAreaControllerRoute(
                name: "Jobs_Apply",
                areaName: "NhanViet.Core",
                pattern: "viec-lam/{slug}/ung-tuyen",
                defaults: new { controller = "Job", action = "Apply" }
            );

            // Company Routes
            routes.MapAreaControllerRoute(
                name: "Companies_Index",
                areaName: "NhanViet.Core",
                pattern: "cong-ty",
                defaults: new { controller = "Company", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "Companies_List",
                areaName: "NhanViet.Core",
                pattern: "cong-ty/danh-sach",
                defaults: new { controller = "Company", action = "List" }
            );

            routes.MapAreaControllerRoute(
                name: "Companies_Featured",
                areaName: "NhanViet.Core",
                pattern: "cong-ty/noi-bat",
                defaults: new { controller = "Company", action = "Featured" }
            );

            routes.MapAreaControllerRoute(
                name: "Companies_Reviews",
                areaName: "NhanViet.Core",
                pattern: "cong-ty/danh-gia",
                defaults: new { controller = "Company", action = "Reviews" }
            );

            routes.MapAreaControllerRoute(
                name: "Companies_Detail",
                areaName: "NhanViet.Core",
                pattern: "cong-ty/{slug}",
                defaults: new { controller = "Company", action = "Detail" }
            );

            routes.MapAreaControllerRoute(
                name: "Companies_Jobs",
                areaName: "NhanViet.Core",
                pattern: "cong-ty/{slug}/viec-lam",
                defaults: new { controller = "Company", action = "Jobs" }
            );

            // News Routes
            routes.MapAreaControllerRoute(
                name: "News_Index",
                areaName: "NhanViet.Core",
                pattern: "tin-tuc",
                defaults: new { controller = "News", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "News_Latest",
                areaName: "NhanViet.Core",
                pattern: "tin-tuc/moi-nhat",
                defaults: new { controller = "News", action = "Latest" }
            );

            routes.MapAreaControllerRoute(
                name: "News_Featured",
                areaName: "NhanViet.Core",
                pattern: "tin-tuc/noi-bat",
                defaults: new { controller = "News", action = "Featured" }
            );

            routes.MapAreaControllerRoute(
                name: "News_Category",
                areaName: "NhanViet.Core",
                pattern: "tin-tuc/{category}",
                defaults: new { controller = "News", action = "Category" }
            );

            routes.MapAreaControllerRoute(
                name: "News_Detail",
                areaName: "NhanViet.Core",
                pattern: "tin-tuc/{category}/{slug}",
                defaults: new { controller = "News", action = "Detail" }
            );

            // Country Routes
            routes.MapAreaControllerRoute(
                name: "Countries_Index",
                areaName: "NhanViet.Core",
                pattern: "quoc-gia",
                defaults: new { controller = "Country", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "Countries_Info",
                areaName: "NhanViet.Core",
                pattern: "quoc-gia/thong-tin",
                defaults: new { controller = "Country", action = "Info" }
            );

            routes.MapAreaControllerRoute(
                name: "Countries_Visa",
                areaName: "NhanViet.Core",
                pattern: "quoc-gia/visa",
                defaults: new { controller = "Country", action = "Visa" }
            );

            routes.MapAreaControllerRoute(
                name: "Countries_CostOfLiving",
                areaName: "NhanViet.Core",
                pattern: "quoc-gia/chi-phi-sinh-hoat",
                defaults: new { controller = "Country", action = "CostOfLiving" }
            );

            routes.MapAreaControllerRoute(
                name: "Countries_Culture",
                areaName: "NhanViet.Core",
                pattern: "quoc-gia/van-hoa",
                defaults: new { controller = "Country", action = "Culture" }
            );

            routes.MapAreaControllerRoute(
                name: "Countries_Detail",
                areaName: "NhanViet.Core",
                pattern: "quoc-gia/{slug}",
                defaults: new { controller = "Country", action = "Detail" }
            );

            // Consultation Routes
            routes.MapAreaControllerRoute(
                name: "Consultations_Index",
                areaName: "NhanViet.Core",
                pattern: "tu-van",
                defaults: new { controller = "Consultation", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "Consultations_Book",
                areaName: "NhanViet.Core",
                pattern: "tu-van/dat-lich",
                defaults: new { controller = "Consultation", action = "Book" }
            );

            routes.MapAreaControllerRoute(
                name: "Consultations_Online",
                areaName: "NhanViet.Core",
                pattern: "tu-van/truc-tuyen",
                defaults: new { controller = "Consultation", action = "Online" }
            );

            routes.MapAreaControllerRoute(
                name: "Consultations_FAQ",
                areaName: "NhanViet.Core",
                pattern: "tu-van/cau-hoi-thuong-gap",
                defaults: new { controller = "Consultation", action = "FAQ" }
            );

            routes.MapAreaControllerRoute(
                name: "Consultations_Experts",
                areaName: "NhanViet.Core",
                pattern: "tu-van/chuyen-gia",
                defaults: new { controller = "Consultation", action = "Experts" }
            );

            routes.MapAreaControllerRoute(
                name: "Consultations_Detail",
                areaName: "NhanViet.Core",
                pattern: "tu-van/{slug}",
                defaults: new { controller = "Consultation", action = "Detail" }
            );

            // Service Routes
            routes.MapAreaControllerRoute(
                name: "Services_Index",
                areaName: "NhanViet.Core",
                pattern: "dich-vu",
                defaults: new { controller = "Service", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "Services_DocumentSupport",
                areaName: "NhanViet.Core",
                pattern: "dich-vu/ho-tro-ho-so",
                defaults: new { controller = "Service", action = "DocumentSupport" }
            );

            routes.MapAreaControllerRoute(
                name: "Services_Training",
                areaName: "NhanViet.Core",
                pattern: "dich-vu/dao-tao",
                defaults: new { controller = "Service", action = "Training" }
            );

            routes.MapAreaControllerRoute(
                name: "Services_Language",
                areaName: "NhanViet.Core",
                pattern: "dich-vu/ngoai-ngu",
                defaults: new { controller = "Service", action = "Language" }
            );

            routes.MapAreaControllerRoute(
                name: "Services_Settlement",
                areaName: "NhanViet.Core",
                pattern: "dich-vu/dinh-cu",
                defaults: new { controller = "Service", action = "Settlement" }
            );

            // User Routes
            routes.MapAreaControllerRoute(
                name: "User_Profile",
                areaName: "NhanViet.Core",
                pattern: "ho-so",
                defaults: new { controller = "User", action = "Profile" }
            );

            routes.MapAreaControllerRoute(
                name: "User_Applications",
                areaName: "NhanViet.Core",
                pattern: "don-ung-tuyen",
                defaults: new { controller = "User", action = "Applications" }
            );

            routes.MapAreaControllerRoute(
                name: "User_Consultations",
                areaName: "NhanViet.Core",
                pattern: "tu-van-cua-toi",
                defaults: new { controller = "User", action = "Consultations" }
            );

            // Authentication Routes
            routes.MapAreaControllerRoute(
                name: "Auth_Login",
                areaName: "NhanViet.Core",
                pattern: "dang-nhap",
                defaults: new { controller = "Auth", action = "Login" }
            );

            routes.MapAreaControllerRoute(
                name: "Auth_Register",
                areaName: "NhanViet.Core",
                pattern: "dang-ky",
                defaults: new { controller = "Auth", action = "Register" }
            );

            routes.MapAreaControllerRoute(
                name: "Auth_Logout",
                areaName: "NhanViet.Core",
                pattern: "dang-xuat",
                defaults: new { controller = "Auth", action = "Logout" }
            );

            routes.MapAreaControllerRoute(
                name: "Auth_ForgotPassword",
                areaName: "NhanViet.Core",
                pattern: "quen-mat-khau",
                defaults: new { controller = "Auth", action = "ForgotPassword" }
            );

            // Static Pages Routes
            routes.MapAreaControllerRoute(
                name: "About_Index",
                areaName: "NhanViet.Core",
                pattern: "gioi-thieu",
                defaults: new { controller = "About", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "About_Company",
                areaName: "NhanViet.Core",
                pattern: "gioi-thieu/cong-ty",
                defaults: new { controller = "About", action = "Company" }
            );

            routes.MapAreaControllerRoute(
                name: "About_Team",
                areaName: "NhanViet.Core",
                pattern: "gioi-thieu/doi-ngu",
                defaults: new { controller = "About", action = "Team" }
            );

            routes.MapAreaControllerRoute(
                name: "About_Achievements",
                areaName: "NhanViet.Core",
                pattern: "gioi-thieu/thanh-tich",
                defaults: new { controller = "About", action = "Achievements" }
            );

            routes.MapAreaControllerRoute(
                name: "About_Partners",
                areaName: "NhanViet.Core",
                pattern: "gioi-thieu/doi-tac",
                defaults: new { controller = "About", action = "Partners" }
            );

            routes.MapAreaControllerRoute(
                name: "Contact_Index",
                areaName: "NhanViet.Core",
                pattern: "lien-he",
                defaults: new { controller = "Contact", action = "Index" }
            );

            // Search Routes
            routes.MapAreaControllerRoute(
                name: "Search_Index",
                areaName: "NhanViet.Core",
                pattern: "tim-kiem",
                defaults: new { controller = "Search", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "Search_Advanced",
                areaName: "NhanViet.Core",
                pattern: "tim-kiem/nang-cao",
                defaults: new { controller = "Search", action = "Advanced" }
            );

            // API Routes
            routes.MapAreaControllerRoute(
                name: "Api_Jobs",
                areaName: "NhanViet.Core",
                pattern: "api/jobs/{action=Index}",
                defaults: new { controller = "JobApi" }
            );

            routes.MapAreaControllerRoute(
                name: "Api_Companies",
                areaName: "NhanViet.Core",
                pattern: "api/companies/{action=Index}",
                defaults: new { controller = "CompanyApi" }
            );

            routes.MapAreaControllerRoute(
                name: "Api_News",
                areaName: "NhanViet.Core",
                pattern: "api/news/{action=Index}",
                defaults: new { controller = "NewsApi" }
            );

            routes.MapAreaControllerRoute(
                name: "Api_Countries",
                areaName: "NhanViet.Core",
                pattern: "api/countries/{action=Index}",
                defaults: new { controller = "CountryApi" }
            );

            // Sitemap Routes
            routes.MapAreaControllerRoute(
                name: "Sitemap_Index",
                areaName: "NhanViet.Core",
                pattern: "sitemap.xml",
                defaults: new { controller = "Sitemap", action = "Index" }
            );

            routes.MapAreaControllerRoute(
                name: "Sitemap_Jobs",
                areaName: "NhanViet.Core",
                pattern: "sitemap-jobs.xml",
                defaults: new { controller = "Sitemap", action = "Jobs" }
            );

            routes.MapAreaControllerRoute(
                name: "Sitemap_News",
                areaName: "NhanViet.Core",
                pattern: "sitemap-news.xml",
                defaults: new { controller = "Sitemap", action = "News" }
            );

            // RSS Feeds
            routes.MapAreaControllerRoute(
                name: "Feed_Jobs",
                areaName: "NhanViet.Core",
                pattern: "feed/jobs.xml",
                defaults: new { controller = "Feed", action = "Jobs" }
            );

            routes.MapAreaControllerRoute(
                name: "Feed_News",
                areaName: "NhanViet.Core",
                pattern: "feed/news.xml",
                defaults: new { controller = "Feed", action = "News" }
            );
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure routing options
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
                options.AppendTrailingSlash = false;
            });
        }
    }
}