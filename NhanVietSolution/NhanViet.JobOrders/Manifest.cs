using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "NhanViet Job Orders",
    Author = "NhanViet Group",
    Website = "https://nhanvietgroup.com",
    Version = "1.0.0",
    Description = "Job orders and recruitment management module for NhanViet website",
    Dependencies = ["OrchardCore.ContentManagement", "OrchardCore.Title"],
    Category = "Recruitment"
)]