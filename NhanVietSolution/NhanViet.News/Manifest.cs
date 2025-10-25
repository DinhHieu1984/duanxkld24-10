using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "NhanViet News",
    Author = "NhanViet Group",
    Website = "https://nhanvietgroup.com",
    Version = "1.0.0",
    Description = "News and articles management module for NhanViet website",
    Dependencies = ["OrchardCore.ContentManagement", "OrchardCore.Title", "OrchardCore.Html"],
    Category = "Content Management"
)]