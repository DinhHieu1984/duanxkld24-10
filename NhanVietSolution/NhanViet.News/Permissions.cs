using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NhanViet.News
{
    /// <summary>
    /// Định nghĩa permissions cho module NhanViet.News
    /// Quản lý quyền hạn cho tin tức theo thị trường (Nhật Bản, Đài Loan, Châu Âu)
    /// </summary>
    public class Permissions : IPermissionProvider
    {
        // Quản lý tin tức - Quyền cao nhất (Admin, Editor)
        public static readonly Permission ManageNews = new Permission("ManageNews", "Manage News - Full control over news articles");
        
        // Xem tin tức - Quyền xem cơ bản (All users)
        public static readonly Permission ViewNews = new Permission("ViewNews", "View News - View published news articles");
        
        // Chỉnh sửa tin tức - Quyền chỉnh sửa (Admin, Editor)
        public static readonly Permission EditNews = new Permission("EditNews", "Edit News - Create and modify news articles");
        
        // Xuất bản tin tức - Quyền xuất bản (Admin, Editor)
        public static readonly Permission PublishNews = new Permission("PublishNews", "Publish News - Publish/unpublish news articles");
        
        // Xóa tin tức - Quyền xóa (Admin only)
        public static readonly Permission DeleteNews = new Permission("DeleteNews", "Delete News - Remove news articles");
        
        // Quản lý danh mục tin tức - Quyền quản lý categories (Admin, Editor)
        public static readonly Permission ManageNewsCategories = new Permission("ManageNewsCategories", "Manage News Categories - Manage news categories and markets");
        
        // Xem tin tức draft - Quyền xem bản nháp (Admin, Editor, Author)
        public static readonly Permission ViewDraftNews = new Permission("ViewDraftNews", "View Draft News - View unpublished news articles");
        
        // Xuất tin tức - Quyền xuất dữ liệu (Admin, Editor)
        public static readonly Permission ExportNews = new Permission("ExportNews", "Export News - Export news data and reports");

        /// <summary>
        /// Trả về danh sách tất cả permissions của module
        /// </summary>
        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[]
            {
                ManageNews,
                ViewNews,
                EditNews,
                PublishNews,
                DeleteNews,
                ManageNewsCategories,
                ViewDraftNews,
                ExportNews
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