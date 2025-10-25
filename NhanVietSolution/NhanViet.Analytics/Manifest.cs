using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "NhanViet Analytics",
    Author = "NhanViet Group",
    Website = "https://nhanvietgroup.com",
    Version = "1.0.0",
    Description = "Analytics and tracking module for NhanViet website",
    Dependencies = ["OrchardCore.ContentManagement"],
    Category = "Analytics"
)]