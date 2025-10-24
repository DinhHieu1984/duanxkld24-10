using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement.Metadata.Builders;

namespace NhanViet.JobOrders
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
            // Create JobOrder Content Type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("JobOrder", type => type
                .DisplayedAs("Job Order")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable()
                .WithPart("TitlePart", part => part
                    .WithPosition("1"))
                .WithPart("JobOrder", part => part
                    .WithPosition("2"))
                .WithPart("CommonPart", part => part
                    .WithPosition("3"))
                .WithPart("PublishLaterPart", part => part
                    .WithPosition("4"))
                .WithPart("AutoroutePart", part => part
                    .WithPosition("5")
                    .WithSettings(new AutoroutePartSettings
                    {
                        AllowCustomPath = true,
                        Pattern = "{{ Model.ContentItem | display_text | slugify }}",
                        ShowHomepageOption = false
                    }))
            );

            // Create JobOrder Content Part with Fields
            await _contentDefinitionManager.AlterPartDefinitionAsync("JobOrder", part => part
                .WithField("JobTitle", field => field
                    .OfType("TextField")
                    .WithDisplayName("Job Title")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Enter the job title"
                    }))
                .WithField("JobDescription", field => field
                    .OfType("HtmlField")
                    .WithDisplayName("Job Description")
                    .WithSettings(new HtmlFieldSettings
                    {
                        Hint = "Enter detailed job description"
                    }))
                .WithField("Company", field => field
                    .OfType("TextField")
                    .WithDisplayName("Company")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Enter company name"
                    }))
                .WithField("Location", field => field
                    .OfType("TextField")
                    .WithDisplayName("Location")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Enter job location"
                    }))
                .WithField("Salary", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Salary")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Enter salary amount",
                        Scale = 2
                    }))
                .WithField("PostedDate", field => field
                    .OfType("DateTimeField")
                    .WithDisplayName("Posted Date")
                    .WithSettings(new DateTimeFieldSettings
                    {
                        Hint = "When was this job posted"
                    }))
                .WithField("ExpiryDate", field => field
                    .OfType("DateTimeField")
                    .WithDisplayName("Expiry Date")
                    .WithSettings(new DateTimeFieldSettings
                    {
                        Hint = "When does this job expire"
                    }))
                .WithField("IsActive", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Active")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Is this job currently active"
                    }))
                .WithField("Requirements", field => field
                    .OfType("HtmlField")
                    .WithDisplayName("Requirements")
                    .WithSettings(new HtmlFieldSettings
                    {
                        Hint = "Enter job requirements"
                    }))
                .WithField("Benefits", field => field
                    .OfType("HtmlField")
                    .WithDisplayName("Benefits")
                    .WithSettings(new HtmlFieldSettings
                    {
                        Hint = "Enter job benefits"
                    }))
            );

            return 1;
        }
    }
}