using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using System;
using System.Threading.Tasks;

namespace NhanViet.Core.Navigation
{
    /// <summary>
    /// Main Navigation Provider for NhanViet Labor Export Management System
    /// Defines the main menu structure for the frontend
    /// </summary>
    public class MainNavigationProvider : INavigationProvider
    {
        private readonly IStringLocalizer S;

        public MainNavigationProvider(IStringLocalizer<MainNavigationProvider> localizer)
        {
            S = localizer;
        }

        public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!string.Equals(name, "main", StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.CompletedTask;
            }

            builder
                // Home
                .Add(S["Trang chủ"], "1", item => item
                    .Url("~/")
                    .AddClass("nav-home")
                    .Id("nav-home")
                )

                // Jobs Menu
                .Add(S["Việc làm"], "2", jobs => jobs
                    .Url("~/jobs")
                    .AddClass("nav-jobs")
                    .Id("nav-jobs")
                    
                    // Jobs Submenu
                    .Add(S["Tìm việc làm"], "2.1", item => item
                        .Url("~/jobs/search")
                        .AddClass("nav-job-search")
                    )
                    .Add(S["Việc làm nổi bật"], "2.2", item => item
                        .Url("~/jobs/featured")
                        .AddClass("nav-job-featured")
                    )
                    .Add(S["Tuyển gấp"], "2.3", item => item
                        .Url("~/jobs/urgent")
                        .AddClass("nav-job-urgent")
                    )
                    .Add(S["Theo quốc gia"], "2.4", countries => countries
                        .Url("~/jobs/by-country")
                        .AddClass("nav-job-countries")
                        
                        // Country Submenu
                        .Add(S["Nhật Bản"], "2.4.1", item => item
                            .Url("~/jobs/japan")
                            .AddClass("nav-country-japan")
                        )
                        .Add(S["Hàn Quốc"], "2.4.2", item => item
                            .Url("~/jobs/korea")
                            .AddClass("nav-country-korea")
                        )
                        .Add(S["Đức"], "2.4.3", item => item
                            .Url("~/jobs/germany")
                            .AddClass("nav-country-germany")
                        )
                        .Add(S["Úc"], "2.4.4", item => item
                            .Url("~/jobs/australia")
                            .AddClass("nav-country-australia")
                        )
                        .Add(S["Canada"], "2.4.5", item => item
                            .Url("~/jobs/canada")
                            .AddClass("nav-country-canada")
                        )
                    )
                    .Add(S["Theo ngành nghề"], "2.5", industries => industries
                        .Url("~/jobs/by-industry")
                        .AddClass("nav-job-industries")
                        
                        // Industry Submenu
                        .Add(S["Sản xuất"], "2.5.1", item => item
                            .Url("~/jobs/manufacturing")
                            .AddClass("nav-industry-manufacturing")
                        )
                        .Add(S["Xây dựng"], "2.5.2", item => item
                            .Url("~/jobs/construction")
                            .AddClass("nav-industry-construction")
                        )
                        .Add(S["Khách sạn - Nhà hàng"], "2.5.3", item => item
                            .Url("~/jobs/hospitality")
                            .AddClass("nav-industry-hospitality")
                        )
                        .Add(S["Y tế"], "2.5.4", item => item
                            .Url("~/jobs/healthcare")
                            .AddClass("nav-industry-healthcare")
                        )
                        .Add(S["Nông nghiệp"], "2.5.5", item => item
                            .Url("~/jobs/agriculture")
                            .AddClass("nav-industry-agriculture")
                        )
                    )
                )

                // Companies Menu
                .Add(S["Công ty"], "3", companies => companies
                    .Url("~/companies")
                    .AddClass("nav-companies")
                    .Id("nav-companies")
                    
                    .Add(S["Danh sách công ty"], "3.1", item => item
                        .Url("~/companies/list")
                        .AddClass("nav-company-list")
                    )
                    .Add(S["Công ty nổi bật"], "3.2", item => item
                        .Url("~/companies/featured")
                        .AddClass("nav-company-featured")
                    )
                    .Add(S["Đánh giá công ty"], "3.3", item => item
                        .Url("~/companies/reviews")
                        .AddClass("nav-company-reviews")
                    )
                )

                // Countries Menu
                .Add(S["Quốc gia"], "4", countries => countries
                    .Url("~/countries")
                    .AddClass("nav-countries")
                    .Id("nav-countries")
                    
                    .Add(S["Thông tin quốc gia"], "4.1", item => item
                        .Url("~/countries/info")
                        .AddClass("nav-country-info")
                    )
                    .Add(S["Thủ tục visa"], "4.2", item => item
                        .Url("~/countries/visa")
                        .AddClass("nav-country-visa")
                    )
                    .Add(S["Chi phí sinh hoạt"], "4.3", item => item
                        .Url("~/countries/cost-of-living")
                        .AddClass("nav-country-cost")
                    )
                    .Add(S["Văn hóa & Phong tục"], "4.4", item => item
                        .Url("~/countries/culture")
                        .AddClass("nav-country-culture")
                    )
                )

                // News Menu
                .Add(S["Tin tức"], "5", news => news
                    .Url("~/news")
                    .AddClass("nav-news")
                    .Id("nav-news")
                    
                    .Add(S["Tin mới nhất"], "5.1", item => item
                        .Url("~/news/latest")
                        .AddClass("nav-news-latest")
                    )
                    .Add(S["Tin nổi bật"], "5.2", item => item
                        .Url("~/news/featured")
                        .AddClass("nav-news-featured")
                    )
                    .Add(S["Chính sách"], "5.3", item => item
                        .Url("~/news/policy")
                        .AddClass("nav-news-policy")
                    )
                    .Add(S["Kinh nghiệm"], "5.4", item => item
                        .Url("~/news/experience")
                        .AddClass("nav-news-experience")
                    )
                    .Add(S["Thành công"], "5.5", item => item
                        .Url("~/news/success-stories")
                        .AddClass("nav-news-success")
                    )
                )

                // Consultation Menu
                .Add(S["Tư vấn"], "6", consultation => consultation
                    .Url("~/consultations")
                    .AddClass("nav-consultations")
                    .Id("nav-consultations")
                    
                    .Add(S["Đặt lịch tư vấn"], "6.1", item => item
                        .Url("~/consultations/book")
                        .AddClass("nav-consultation-book")
                    )
                    .Add(S["Tư vấn trực tuyến"], "6.2", item => item
                        .Url("~/consultations/online")
                        .AddClass("nav-consultation-online")
                    )
                    .Add(S["Câu hỏi thường gặp"], "6.3", item => item
                        .Url("~/consultations/faq")
                        .AddClass("nav-consultation-faq")
                    )
                    .Add(S["Liên hệ chuyên gia"], "6.4", item => item
                        .Url("~/consultations/experts")
                        .AddClass("nav-consultation-experts")
                    )
                )

                // Services Menu
                .Add(S["Dịch vụ"], "7", services => services
                    .Url("~/services")
                    .AddClass("nav-services")
                    .Id("nav-services")
                    
                    .Add(S["Hỗ trợ hồ sơ"], "7.1", item => item
                        .Url("~/services/document-support")
                        .AddClass("nav-service-documents")
                    )
                    .Add(S["Đào tạo kỹ năng"], "7.2", item => item
                        .Url("~/services/training")
                        .AddClass("nav-service-training")
                    )
                    .Add(S["Học ngoại ngữ"], "7.3", item => item
                        .Url("~/services/language")
                        .AddClass("nav-service-language")
                    )
                    .Add(S["Hỗ trợ định cư"], "7.4", item => item
                        .Url("~/services/settlement")
                        .AddClass("nav-service-settlement")
                    )
                )

                // About Menu
                .Add(S["Giới thiệu"], "8", about => about
                    .Url("~/about")
                    .AddClass("nav-about")
                    .Id("nav-about")
                    
                    .Add(S["Về chúng tôi"], "8.1", item => item
                        .Url("~/about/company")
                        .AddClass("nav-about-company")
                    )
                    .Add(S["Đội ngũ"], "8.2", item => item
                        .Url("~/about/team")
                        .AddClass("nav-about-team")
                    )
                    .Add(S["Thành tích"], "8.3", item => item
                        .Url("~/about/achievements")
                        .AddClass("nav-about-achievements")
                    )
                    .Add(S["Đối tác"], "8.4", item => item
                        .Url("~/about/partners")
                        .AddClass("nav-about-partners")
                    )
                )

                // Contact
                .Add(S["Liên hệ"], "9", item => item
                    .Url("~/contact")
                    .AddClass("nav-contact")
                    .Id("nav-contact")
                );

            return ValueTask.CompletedTask;
        }
    }
}