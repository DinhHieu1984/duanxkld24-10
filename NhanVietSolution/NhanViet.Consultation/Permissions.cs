using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace NhanViet.Consultation
{
    public sealed class Permissions : IPermissionProvider
    {
        // Define permissions
        public static readonly Permission ManageConsultation = new("ManageConsultation", "Manage consultation requests");
        public static readonly Permission ViewConsultation = new("ViewConsultation", "View consultation requests");
        public static readonly Permission CreateConsultationRequest = new("CreateConsultationRequest", "Create consultation request");
        public static readonly Permission AssignConsultation = new("AssignConsultation", "Assign consultation to consultant");
        public static readonly Permission RespondConsultation = new("RespondConsultation", "Respond to consultation");
        public static readonly Permission EditConsultation = new("EditConsultation", "Edit consultation");
        public static readonly Permission DeleteConsultation = new("DeleteConsultation", "Delete consultation");
        public static readonly Permission ExportConsultationReports = new("ExportConsultationReports", "Export consultation reports");
        public static readonly Permission ManageConsultationCategories = new("ManageConsultationCategories", "Manage consultation categories");
        public static readonly Permission TrackConsultationProgress = new("TrackConsultationProgress", "Track consultation progress");

        private readonly IEnumerable<Permission> _allPermissions = new[]
        {
            ManageConsultation,
            ViewConsultation,
            CreateConsultationRequest,
            AssignConsultation,
            RespondConsultation,
            EditConsultation,
            DeleteConsultation,
            ExportConsultationReports,
            ManageConsultationCategories,
            TrackConsultationProgress
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
                        ManageConsultation,
                        ViewConsultation,
                        AssignConsultation,
                        RespondConsultation,
                        EditConsultation,
                        ExportConsultationReports,
                        ManageConsultationCategories,
                        TrackConsultationProgress
                    }
                },
                
                // Author - Can create and edit own content
                new PermissionStereotype
                {
                    Name = "Author",
                    Permissions = new[]
                    {
                        ViewConsultation,
                        CreateConsultationRequest,
                        RespondConsultation,
                        EditConsultation
                    }
                },
                
                // Contributor - Can create content
                new PermissionStereotype
                {
                    Name = "Contributor",
                    Permissions = new[]
                    {
                        ViewConsultation,
                        CreateConsultationRequest
                    }
                },
                
                // Consultation Manager - Quan ly tu van
                new PermissionStereotype
                {
                    Name = "Consultation Manager",
                    Permissions = new[]
                    {
                        ManageConsultation,
                        ViewConsultation,
                        AssignConsultation,
                        RespondConsultation,
                        EditConsultation,
                        DeleteConsultation,
                        ExportConsultationReports,
                        ManageConsultationCategories,
                        TrackConsultationProgress
                    }
                },
                
                // Consultant - Tu van vien
                new PermissionStereotype
                {
                    Name = "Consultant",
                    Permissions = new[]
                    {
                        ManageConsultation,
                        ViewConsultation,
                        RespondConsultation,
                        EditConsultation,
                        TrackConsultationProgress
                    }
                },
                
                // Senior Consultant - Tu van vien cap cao
                new PermissionStereotype
                {
                    Name = "Senior Consultant",
                    Permissions = new[]
                    {
                        ManageConsultation,
                        ViewConsultation,
                        AssignConsultation,
                        RespondConsultation,
                        EditConsultation,
                        ExportConsultationReports,
                        TrackConsultationProgress
                    }
                },
                
                // HR Manager - Quan ly nhan su (co the xem de phoi hop)
                new PermissionStereotype
                {
                    Name = "HR Manager",
                    Permissions = new[]
                    {
                        ViewConsultation,
                        TrackConsultationProgress
                    }
                },
                
                // Authenticated - User da dang nhap
                new PermissionStereotype
                {
                    Name = "Authenticated",
                    Permissions = new[]
                    {
                        CreateConsultationRequest
                    }
                },
                
                // Anonymous - Khach vang lai (co the tao yeu cau tu van)
                new PermissionStereotype
                {
                    Name = "Anonymous",
                    Permissions = new[]
                    {
                        CreateConsultationRequest
                    }
                }
            };
        }
    }
}