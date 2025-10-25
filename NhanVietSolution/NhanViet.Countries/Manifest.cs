using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "NhanViet Countries",
    Author = "NhanViet Group",
    Website = "https://nhanvietgroup.com",
    Version = "1.0.0",
    Description = "Countries and locations data module for NhanViet website",
    Dependencies = ["OrchardCore.ContentManagement", "OrchardCore.Title"],
    Category = "Data"
)]