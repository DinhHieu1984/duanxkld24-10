using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Autoroute.Models;
using OrchardCore.Media.Settings;

namespace NhanViet.Recruitment
{
    public sealed class Migrations : DataMigration
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public Migrations(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;
        }

        public async Task<int> CreateAsync()
        {
            // Create WorkerProfile Content Type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("WorkerProfile", type => type
                .DisplayedAs("Worker Profile")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable()
                .WithPart("TitlePart", part => part
                    .WithPosition("1"))
                .WithPart("WorkerProfile", part => part
                    .WithPosition("2"))
                .WithPart("HtmlBodyPart", part => part
                    .WithPosition("3"))
                .WithPart("CommonPart", part => part
                    .WithPosition("4"))
                .WithPart("PublishLaterPart", part => part
                    .WithPosition("5"))
                .WithPart("AutoroutePart", part => part
                    .WithPosition("6")
                    .WithSettings(new AutoroutePartSettings
                    {
                        AllowCustomPath = true,
                        Pattern = "/workers/{{ Model.ContentItem | display_text | slugify }}",
                        ShowHomepageOption = false
                    }))
            );

            // Create WorkerProfile Content Part with Fields
            await _contentDefinitionManager.AlterPartDefinitionAsync("WorkerProfile", part => part
                .Attachable()
                .WithDescription("Provides worker profile specific functionality")
                .WithField("FullName", field => field
                    .OfType("TextField")
                    .WithDisplayName("Full Name")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Worker's full name"
                    }))
                .WithField("Age", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Age")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Worker's age"
                    }))
                .WithField("Gender", field => field
                    .OfType("TextField")
                    .WithDisplayName("Gender")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Worker's gender (Male/Female/Other)"
                    }))
                .WithField("Nationality", field => field
                    .OfType("TextField")
                    .WithDisplayName("Nationality")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Worker's nationality"
                    }))
                .WithField("Skills", field => field
                    .OfType("TextField")
                    .WithDisplayName("Skills")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Comma-separated list of skills"
                    }))
                .WithField("Experience", field => field
                    .OfType("TextField")
                    .WithDisplayName("Experience")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Work experience description"
                    }))
                .WithField("Education", field => field
                    .OfType("TextField")
                    .WithDisplayName("Education")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Educational background"
                    }))
                .WithField("Languages", field => field
                    .OfType("TextField")
                    .WithDisplayName("Languages")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Languages spoken"
                    }))
                .WithField("Photo", field => field
                    .OfType("MediaField")
                    .WithDisplayName("Photo")
                    .WithSettings(new MediaFieldSettings
                    {
                        Hint = "Worker's profile photo",
                        Multiple = false
                    }))
                .WithField("ContactPhone", field => field
                    .OfType("TextField")
                    .WithDisplayName("Contact Phone")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Worker's contact phone number"
                    }))
                .WithField("ContactEmail", field => field
                    .OfType("TextField")
                    .WithDisplayName("Contact Email")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Worker's contact email address"
                    }))
                .WithField("IsAvailable", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Available")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Is this worker currently available for work"
                    }))
                .WithField("PreferredCountries", field => field
                    .OfType("TextField")
                    .WithDisplayName("Preferred Countries")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Countries where worker prefers to work"
                    }))
                .WithField("ExpectedSalary", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Expected Salary")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Expected monthly salary in USD"
                    }))
            );

            return 1;
        }
    }
}