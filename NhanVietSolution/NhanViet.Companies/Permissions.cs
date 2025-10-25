using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace NhanViet.Companies
{
    public sealed class Permissions : IPermissionProvider
    {
        // Define permissions
        public static readonly Permission ManageCompanies = new("ManageCompanies", "Manage companies");
        public static readonly Permission ViewCompanies = new("ViewCompanies", "View companies");
        public static readonly Permission CreateCompany = new("CreateCompany", "Create company");
        public static readonly Permission EditCompany = new("EditCompany", "Edit company");
        public static readonly Permission DeleteCompany = new("DeleteCompany", "Delete company");
        public static readonly Permission PublishCompany = new("PublishCompany", "Publish company");
        public static readonly Permission ExportCompanyReports = new("ExportCompanyReports", "Export company reports");
        public static readonly Permission ManageCompanyCategories = new("ManageCompanyCategories", "Manage company categories");
        public static readonly Permission ViewCompanyDetails = new("ViewCompanyDetails", "View company details");

        private readonly IEnumerable<Permission> _allPermissions = new[]
        {
            ManageCompanies,
            ViewCompanies,
            CreateCompany,
            EditCompany,
            DeleteCompany,
            PublishCompany,
            ExportCompanyReports,
            ManageCompanyCategories,
            ViewCompanyDetails
        };

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(_allPermissions);
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                // Administrator - Full access
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = _allPermissions
                },
                
                // Editor - Can manage content
                new PermissionStereotype
                {
                    Name = "Editor",
                    Permissions = new[]
                    {
                        ManageCompanies,
                        ViewCompanies,
                        CreateCompany,
                        EditCompany,
                        PublishCompany,
                        ExportCompanyReports,
                        ManageCompanyCategories,
                        ViewCompanyDetails
                    }
                },
                
                // Author - Can create and edit own content
                new PermissionStereotype
                {
                    Name = "Author",
                    Permissions = new[]
                    {
                        ViewCompanies,
                        CreateCompany,
                        EditCompany,
                        ViewCompanyDetails
                    }
                },
                
                // Contributor - Can create content
                new PermissionStereotype
                {
                    Name = "Contributor",
                    Permissions = new[]
                    {
                        ViewCompanies,
                        CreateCompany,
                        ViewCompanyDetails
                    }
                },
                
                // Company Manager - Quan ly cong ty
                new PermissionStereotype
                {
                    Name = "Company Manager",
                    Permissions = new[]
                    {
                        ManageCompanies,
                        ViewCompanies,
                        CreateCompany,
                        EditCompany,
                        PublishCompany,
                        DeleteCompany,
                        ExportCompanyReports,
                        ManageCompanyCategories,
                        ViewCompanyDetails
                    }
                },
                
                // Business Development - Phat trien kinh doanh
                new PermissionStereotype
                {
                    Name = "Business Development",
                    Permissions = new[]
                    {
                        ViewCompanies,
                        CreateCompany,
                        EditCompany,
                        ViewCompanyDetails,
                        ExportCompanyReports
                    }
                },
                
                // Sales Manager - Quan ly ban hang
                new PermissionStereotype
                {
                    Name = "Sales Manager",
                    Permissions = new[]
                    {
                        ViewCompanies,
                        CreateCompany,
                        EditCompany,
                        ViewCompanyDetails,
                        ExportCompanyReports
                    }
                },
                
                // HR Manager - Quan ly nhan su (co the xem thong tin cong ty)
                new PermissionStereotype
                {
                    Name = "HR Manager",
                    Permissions = new[]
                    {
                        ViewCompanies,
                        ViewCompanyDetails
                    }
                },
                
                // Authenticated - User da dang nhap
                new PermissionStereotype
                {
                    Name = "Authenticated",
                    Permissions = new[]
                    {
                        ViewCompanies,
                        ViewCompanyDetails
                    }
                },
                
                // Anonymous - Khach vang lai
                new PermissionStereotype
                {
                    Name = "Anonymous",
                    Permissions = new[]
                    {
                        ViewCompanies
                    }
                }
            };
        }
    }
}