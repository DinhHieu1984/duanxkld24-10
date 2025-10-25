using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace NhanViet.Recruitment
{
    public sealed class Permissions : IPermissionProvider
    {
        // Define permissions
        public static readonly Permission ManageRecruitment = new("ManageRecruitment", "Manage recruitment posts");
        public static readonly Permission ViewRecruitment = new("ViewRecruitment", "View recruitment posts");
        public static readonly Permission CreateRecruitment = new("CreateRecruitment", "Create recruitment posts");
        public static readonly Permission EditRecruitment = new("EditRecruitment", "Edit recruitment posts");
        public static readonly Permission DeleteRecruitment = new("DeleteRecruitment", "Delete recruitment posts");
        public static readonly Permission PublishRecruitment = new("PublishRecruitment", "Publish recruitment posts");
        public static readonly Permission ApplyRecruitment = new("ApplyRecruitment", "Apply for recruitment");
        public static readonly Permission ExportRecruitmentReports = new("ExportRecruitmentReports", "Export recruitment reports");
        public static readonly Permission ManageCandidateProfiles = new("ManageCandidateProfiles", "Manage candidate profiles");

        private readonly IEnumerable<Permission> _allPermissions = new[]
        {
            ManageRecruitment,
            ViewRecruitment,
            CreateRecruitment,
            EditRecruitment,
            DeleteRecruitment,
            PublishRecruitment,
            ApplyRecruitment,
            ExportRecruitmentReports,
            ManageCandidateProfiles
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
                        ManageRecruitment,
                        ViewRecruitment,
                        CreateRecruitment,
                        EditRecruitment,
                        PublishRecruitment,
                        ExportRecruitmentReports
                    }
                },
                
                // Author - Can create and edit own content
                new PermissionStereotype
                {
                    Name = "Author",
                    Permissions = new[]
                    {
                        ViewRecruitment,
                        CreateRecruitment,
                        EditRecruitment
                    }
                },
                
                // Contributor - Can create content
                new PermissionStereotype
                {
                    Name = "Contributor",
                    Permissions = new[]
                    {
                        ViewRecruitment,
                        CreateRecruitment
                    }
                },
                
                // HR Manager - Quan ly tuyen dung
                new PermissionStereotype
                {
                    Name = "HR Manager",
                    Permissions = new[]
                    {
                        ManageRecruitment,
                        ViewRecruitment,
                        CreateRecruitment,
                        EditRecruitment,
                        PublishRecruitment,
                        DeleteRecruitment,
                        ExportRecruitmentReports,
                        ManageCandidateProfiles
                    }
                },
                
                // HR Staff - Nhan vien nhan su
                new PermissionStereotype
                {
                    Name = "HR Staff",
                    Permissions = new[]
                    {
                        ViewRecruitment,
                        CreateRecruitment,
                        EditRecruitment,
                        ManageCandidateProfiles
                    }
                },
                
                // Recruiter - Chuyen vien tuyen dung
                new PermissionStereotype
                {
                    Name = "Recruiter",
                    Permissions = new[]
                    {
                        ViewRecruitment,
                        CreateRecruitment,
                        EditRecruitment,
                        PublishRecruitment,
                        ManageCandidateProfiles
                    }
                },
                
                // Department Manager - Quan ly phong ban
                new PermissionStereotype
                {
                    Name = "Department Manager",
                    Permissions = new[]
                    {
                        ViewRecruitment,
                        CreateRecruitment,
                        EditRecruitment
                    }
                },
                
                // Authenticated - User da dang nhap
                new PermissionStereotype
                {
                    Name = "Authenticated",
                    Permissions = new[]
                    {
                        ViewRecruitment,
                        ApplyRecruitment
                    }
                },
                
                // Anonymous - Khach vang lai
                new PermissionStereotype
                {
                    Name = "Anonymous",
                    Permissions = new[]
                    {
                        ViewRecruitment
                    }
                }
            };
        }
    }
}