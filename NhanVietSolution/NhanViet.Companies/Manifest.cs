using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "NhanViet Companies",
    Author = "NhanViet Group",
    Website = "https://nhanvietgroup.com",
    Version = "1.0.0",
    Description = "Company management module for NhanViet website",
    Dependencies = ["OrchardCore.ContentManagement", "OrchardCore.Title"],
    Category = "Content Management"
)]