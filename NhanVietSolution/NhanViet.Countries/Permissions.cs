using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NhanViet.Countries
{
    /// <summary>
    /// Định nghĩa permissions cho module NhanViet.Countries
    /// Quản lý quyền hạn cho thông tin quốc gia (Nhật Bản, Đài Loan, Châu Âu)
    /// </summary>
    public class Permissions : IPermissionProvider
    {
        // Quản lý quốc gia - Quyền cao nhất (Admin)
        public static readonly Permission ManageCountries = new Permission("ManageCountries", "Manage Countries - Full control over country information");
        
        // Xem thông tin quốc gia - Quyền xem cơ bản (All users)
        public static readonly Permission ViewCountries = new Permission("ViewCountries", "View Countries - View country information and details");
        
        // Chỉnh sửa thông tin quốc gia - Quyền chỉnh sửa (Admin, Editor)
        public static readonly Permission EditCountries = new Permission("EditCountries", "Edit Countries - Modify country information and details");
        
        // Quản lý thông tin thị trường - Quyền quản lý market info (Admin, Editor)
        public static readonly Permission ManageCountryMarkets = new Permission("ManageCountryMarkets", "Manage Country Markets - Manage market information for countries");
        
        // Xem thông tin thị trường - Quyền xem market info (All users)
        public static readonly Permission ViewCountryMarkets = new Permission("ViewCountryMarkets", "View Country Markets - View market information and opportunities");
        
        // Quản lý yêu cầu visa - Quyền quản lý visa requirements (Admin, HR Manager)
        public static readonly Permission ManageVisaRequirements = new Permission("ManageVisaRequirements", "Manage Visa Requirements - Manage visa and immigration requirements");
        
        // Xem yêu cầu visa - Quyền xem visa requirements (All users)
        public static readonly Permission ViewVisaRequirements = new Permission("ViewVisaRequirements", "View Visa Requirements - View visa and immigration information");
        
        // Xuất thông tin quốc gia - Quyền xuất dữ liệu (Admin, Editor)
        public static readonly Permission ExportCountryData = new Permission("ExportCountryData", "Export Country Data - Export country information and statistics");

        /// <summary>
        /// Trả về danh sách tất cả permissions của module
        /// </summary>
        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[]
            {
                ManageCountries,
                ViewCountries,
                EditCountries,
                ManageCountryMarkets,
                ViewCountryMarkets,
                ManageVisaRequirements,
                ViewVisaRequirements,
                ExportCountryData
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