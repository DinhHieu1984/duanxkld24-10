using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Autoroute.Models;
using OrchardCore.Media.Settings;

namespace NhanViet.Countries
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
            // Create Country Content Type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("Country", type => type
                .DisplayedAs("Country")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable()
                .WithPart("TitlePart", part => part
                    .WithPosition("1"))
                .WithPart("Country", part => part
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
                        Pattern = "/countries/{{ Model.ContentItem | display_text | slugify }}",
                        ShowHomepageOption = false
                    }))
            );

            // Create Country Content Part with Fields
            await _contentDefinitionManager.AlterPartDefinitionAsync("Country", part => part
                .Attachable()
                .WithDescription("Provides country specific functionality for labor export")
                .WithField("CountryCode", field => field
                    .OfType("TextField")
                    .WithDisplayName("Country Code")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "ISO country code (e.g., JP, KR, SG)"
                    }))
                .WithField("Region", field => field
                    .OfType("TextField")
                    .WithDisplayName("Region")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Geographic region (e.g., East Asia, Southeast Asia, Middle East)"
                    }))
                .WithField("Currency", field => field
                    .OfType("TextField")
                    .WithDisplayName("Currency")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Local currency (e.g., JPY, KRW, SGD)"
                    }))
                .WithField("Language", field => field
                    .OfType("TextField")
                    .WithDisplayName("Primary Language")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Primary language spoken in the country"
                    }))
                .WithField("Flag", field => field
                    .OfType("MediaField")
                    .WithDisplayName("Country Flag")
                    .WithSettings(new MediaFieldSettings
                    {
                        Hint = "Country flag image",
                        Multiple = false
                    }))
                .WithField("MinimumWage", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Minimum Wage")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Minimum wage in USD per month"
                    }))
                .WithField("AverageWage", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Average Wage")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Average wage in USD per month"
                    }))
                .WithField("WorkingHours", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Working Hours")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Standard working hours per week"
                    }))
                .WithField("VisaRequirements", field => field
                    .OfType("TextField")
                    .WithDisplayName("Visa Requirements")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Visa requirements for Vietnamese workers"
                    }))
                .WithField("PopularIndustries", field => field
                    .OfType("TextField")
                    .WithDisplayName("Popular Industries")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Industries that commonly hire foreign workers"
                    }))
                .WithField("CostOfLiving", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Cost of Living Index")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Cost of living index (100 = average)"
                    }))
                .WithField("IsActive", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Active")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Is this country currently accepting workers"
                    }))
                .WithField("IsPopular", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Popular")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Is this a popular destination for Vietnamese workers"
                    }))
                .WithField("ContactEmbassy", field => field
                    .OfType("TextField")
                    .WithDisplayName("Embassy Contact")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Vietnamese embassy contact information in this country"
                    }))
            );

            return 1;
        }
    }
}