using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NhanViet.JobOrders
{
    /// <summary>
    /// Định nghĩa permissions cho module NhanViet.JobOrders
    /// Quản lý quyền hạn cho đơn hàng xuất khẩu lao động
    /// </summary>
    public class Permissions : IPermissionProvider
    {
        // Quản lý đơn hàng - Quyền cao nhất (Admin, HR Manager)
        public static readonly Permission ManageJobOrders = new Permission("ManageJobOrders", "Manage Job Orders - Full control over job orders");
        
        // Xem đơn hàng - Quyền xem cơ bản (Editor, Consultant, Authenticated)
        public static readonly Permission ViewJobOrders = new Permission("ViewJobOrders", "View Job Orders - View job order listings");
        
        // Ứng tuyển đơn hàng - Quyền ứng tuyển (User, Authenticated)
        public static readonly Permission ApplyJobOrders = new Permission("ApplyJobOrders", "Apply Job Orders - Submit job applications");
        
        // Chỉnh sửa đơn hàng - Quyền chỉnh sửa (Admin, HR Manager)
        public static readonly Permission EditJobOrders = new Permission("EditJobOrders", "Edit Job Orders - Modify job order details");
        
        // Xuất bản đơn hàng - Quyền xuất bản (Admin, Editor)
        public static readonly Permission PublishJobOrders = new Permission("PublishJobOrders", "Publish Job Orders - Publish/unpublish job orders");
        
        // Xóa đơn hàng - Quyền xóa (Admin only)
        public static readonly Permission DeleteJobOrders = new Permission("DeleteJobOrders", "Delete Job Orders - Remove job orders");
        
        // Xem báo cáo đơn hàng - Quyền xem báo cáo (Admin, HR Manager)
        public static readonly Permission ViewJobOrderReports = new Permission("ViewJobOrderReports", "View Job Order Reports - Access job order analytics");
        
        // Xuất báo cáo đơn hàng - Quyền xuất báo cáo (Admin, HR Manager)
        public static readonly Permission ExportJobOrderReports = new Permission("ExportJobOrderReports", "Export Job Order Reports - Export job order data");

        /// <summary>
        /// Trả về danh sách tất cả permissions của module
        /// </summary>
        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[]
            {
                ManageJobOrders,
                ViewJobOrders,
                ApplyJobOrders,
                EditJobOrders,
                PublishJobOrders,
                DeleteJobOrders,
                ViewJobOrderReports,
                ExportJobOrderReports
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