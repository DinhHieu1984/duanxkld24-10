using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NhanViet.Analytics
{
    /// <summary>
    /// Định nghĩa permissions cho module NhanViet.Analytics
    /// Quản lý quyền hạn cho thống kê và phân tích dữ liệu
    /// </summary>
    public class Permissions : IPermissionProvider
    {
        // Quản lý analytics - Quyền cao nhất (Admin)
        public static readonly Permission ManageAnalytics = new Permission("ManageAnalytics", "Manage Analytics - Full control over analytics and reporting");
        
        // Xem dashboard analytics - Quyền xem dashboard (Admin, HR Manager)
        public static readonly Permission ViewAnalyticsDashboard = new Permission("ViewAnalyticsDashboard", "View Analytics Dashboard - View main analytics dashboard");
        
        // Xem báo cáo tuyển dụng - Quyền xem recruitment reports (Admin, HR Manager, Consultant)
        public static readonly Permission ViewRecruitmentReports = new Permission("ViewRecruitmentReports", "View Recruitment Reports - View recruitment statistics and reports");
        
        // Xem báo cáo đơn hàng - Quyền xem job order reports (Admin, HR Manager)
        public static readonly Permission ViewJobOrderReports = new Permission("ViewJobOrderReports", "View Job Order Reports - View job order statistics and performance");
        
        // Xem báo cáo khách hàng - Quyền xem customer reports (Admin, HR Manager, Consultant)
        public static readonly Permission ViewCustomerReports = new Permission("ViewCustomerReports", "View Customer Reports - View customer analytics and insights");
        
        // Xem báo cáo tài chính - Quyền xem financial reports (Admin only)
        public static readonly Permission ViewFinancialReports = new Permission("ViewFinancialReports", "View Financial Reports - View financial analytics and revenue reports");
        
        // Xuất báo cáo - Quyền xuất reports (Admin, HR Manager)
        public static readonly Permission ExportReports = new Permission("ExportReports", "Export Reports - Export analytics data and reports");
        
        // Cấu hình analytics - Quyền cấu hình (Admin only)
        public static readonly Permission ConfigureAnalytics = new Permission("ConfigureAnalytics", "Configure Analytics - Configure analytics settings and parameters");
        
        // Xem real-time data - Quyền xem dữ liệu real-time (Admin, HR Manager)
        public static readonly Permission ViewRealTimeData = new Permission("ViewRealTimeData", "View Real-time Data - View live analytics and real-time statistics");

        /// <summary>
        /// Trả về danh sách tất cả permissions của module
        /// </summary>
        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[]
            {
                ManageAnalytics,
                ViewAnalyticsDashboard,
                ViewRecruitmentReports,
                ViewJobOrderReports,
                ViewCustomerReports,
                ViewFinancialReports,
                ExportReports,
                ConfigureAnalytics,
                ViewRealTimeData
            }.AsEnumerable());
        }

        /// <summary>
        /// DEPRECATED: GetDefaultStereotypes() không được sử dụng trong OrchardCore hiện tại
        /// Permissions sẽ được assign thông qua Admin UI hoặc Recipes
        /// Reference: https://github.com/OrchardCMS/OrchardCore/issues/4037
        /// </summary>
        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            // Method này không được OrchardCore sử dụng nữa
            // Permissions phải được assign manually qua Admin UI
            return Enumerable.Empty<PermissionStereotype>();
        }
    }
}