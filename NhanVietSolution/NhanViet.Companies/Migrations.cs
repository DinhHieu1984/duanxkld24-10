using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Autoroute.Models;
using OrchardCore.Media.Settings;

namespace NhanViet.Companies
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
            // Create Company Content Type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("Company", type => type
                .DisplayedAs("Company")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable()
                .WithPart("TitlePart", part => part
                    .WithPosition("1"))
                .WithPart("Company", part => part
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
                        Pattern = "/companies/{{ Model.ContentItem | display_text | slugify }}",
                        ShowHomepageOption = false
                    }))
            );

            // Create Company Content Part with Fields
            await _contentDefinitionManager.AlterPartDefinitionAsync("Company", part => part
                .Attachable()
                .WithDescription("Provides company specific functionality")
                .WithField("CompanyCode", field => field
                    .OfType("TextField")
                    .WithDisplayName("Company Code")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Unique company identifier code"
                    }))
                .WithField("Industry", field => field
                    .OfType("TextField")
                    .WithDisplayName("Industry")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Industry sector (e.g., Manufacturing, Construction, etc.)"
                    }))
                .WithField("Country", field => field
                    .OfType("TextField")
                    .WithDisplayName("Country")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Country where company is located"
                    }))
                .WithField("City", field => field
                    .OfType("TextField")
                    .WithDisplayName("City")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "City where company is located"
                    }))
                .WithField("Address", field => field
                    .OfType("TextField")
                    .WithDisplayName("Address")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Full company address"
                    }))
                .WithField("ContactEmail", field => field
                    .OfType("TextField")
                    .WithDisplayName("Contact Email")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Primary contact email address"
                    }))
                .WithField("ContactPhone", field => field
                    .OfType("TextField")
                    .WithDisplayName("Contact Phone")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Primary contact phone number"
                    }))
                .WithField("Website", field => field
                    .OfType("TextField")
                    .WithDisplayName("Website")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Company website URL"
                    }))
                .WithField("Logo", field => field
                    .OfType("MediaField")
                    .WithDisplayName("Company Logo")
                    .WithSettings(new MediaFieldSettings
                    {
                        Hint = "Company logo image",
                        Multiple = false
                    }))
                .WithField("EstablishedYear", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Established Year")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Year when company was established"
                    }))
                .WithField("EmployeeCount", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Employee Count")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Number of employees"
                    }))
                .WithField("IsPartner", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Partner")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Is this company a partner of NhanViet Group"
                    }))
                .WithField("IsActive", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Active")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Is this company currently active"
                    }))
            );

            return 1;
        }
    }
}