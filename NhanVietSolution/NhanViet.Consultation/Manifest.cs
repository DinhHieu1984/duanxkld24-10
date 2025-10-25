using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "NhanViet Consultation",
    Author = "NhanViet Group",
    Website = "https://nhanvietgroup.com",
    Version = "1.0.0",
    Description = "Consultation services module for NhanViet website",
    Dependencies = ["OrchardCore.ContentManagement", "OrchardCore.Title"],
    Category = "Services"
)]