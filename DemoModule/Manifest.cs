using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Demo Blog Module",
    Author = "OrchardCore Learning",
    Website = "https://github.com/khpt1976-cloud/HocOrchardCore",
    Version = "1.0.0",
    Description = "A demonstration blog module following OrchardCore standards and best practices",
    Category = "Content Management",
    Dependencies = new[] 
    { 
        "OrchardCore.Contents",
        "OrchardCore.ContentTypes",
        "OrchardCore.Autoroute",
        "OrchardCore.Html",
        "OrchardCore.Title",
        "OrchardCore.Taxonomies"
    }
)]

[assembly: Feature(
    Id = "OrchardCoreLearning.DemoBlogModule",
    Name = "Demo Blog Module",
    Description = "Core blog functionality with automated setup",
    Category = "Content Management"
)]

[assembly: Feature(
    Id = "OrchardCoreLearning.DemoBlogModule.Admin",
    Name = "Demo Blog Module Admin",
    Description = "Admin interface for blog management",
    Category = "Content Management",
    Dependencies = new[] { "OrchardCoreLearning.DemoBlogModule", "OrchardCore.Admin" }
)]