using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "NhanViet Core",
    Author = "NhanViet Team",
    Website = "https://nhanviet.com",
    Version = "1.0.0",
    Description = "Core functionality for NhanViet Labor Export Management System",
    Category = "Core",
    Dependencies = new[] { "OrchardCore.Navigation", "OrchardCore.Routing" }
)]