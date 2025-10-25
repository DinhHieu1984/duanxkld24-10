using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "NhanViet Recruitment",
    Author = "NhanViet Group",
    Website = "https://nhanvietgroup.com",
    Version = "1.0.0",
    Description = "Recruitment and job posting module for NhanViet website",
    Dependencies = ["OrchardCore.ContentManagement", "OrchardCore.Title", "OrchardCore.Html"],
    Category = "Recruitment"
)]