using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Autoroute.Models;

namespace NhanViet.Analytics
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
            // Create AnalyticsReport Content Type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("AnalyticsReport", type => type
                .DisplayedAs("Analytics Report")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable()
                .WithPart("TitlePart", part => part
                    .WithPosition("1"))
                .WithPart("AnalyticsReport", part => part
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
                        Pattern = "/analytics/{{ Model.ContentItem | display_text | slugify }}",
                        ShowHomepageOption = false
                    }))
            );

            // Create AnalyticsReport Content Part with Fields
            await _contentDefinitionManager.AlterPartDefinitionAsync("AnalyticsReport", part => part
                .Attachable()
                .WithDescription("Provides analytics report specific functionality")
                .WithField("ReportType", field => field
                    .OfType("TextField")
                    .WithDisplayName("Report Type")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Type of analytics report (Monthly, Quarterly, Annual, Custom)"
                    }))
                .WithField("ReportPeriod", field => field
                    .OfType("TextField")
                    .WithDisplayName("Report Period")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Period covered by this report (e.g., Q1 2024, January 2024)"
                    }))
                .WithField("StartDate", field => field
                    .OfType("DateTimeField")
                    .WithDisplayName("Start Date")
                    .WithSettings(new DateTimeFieldSettings
                    {
                        Hint = "Start date of the reporting period"
                    }))
                .WithField("EndDate", field => field
                    .OfType("DateTimeField")
                    .WithDisplayName("End Date")
                    .WithSettings(new DateTimeFieldSettings
                    {
                        Hint = "End date of the reporting period"
                    }))
                .WithField("TotalJobOrders", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Total Job Orders")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Total number of job orders in this period"
                    }))
                .WithField("TotalWorkers", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Total Workers")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Total number of workers registered in this period"
                    }))
                .WithField("TotalCompanies", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Total Companies")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Total number of companies registered in this period"
                    }))
                .WithField("TotalConsultations", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Total Consultations")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Total number of consultation requests in this period"
                    }))
                .WithField("SuccessfulPlacements", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Successful Placements")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Number of successful worker placements"
                    }))
                .WithField("Revenue", field => field
                    .OfType("NumericField")
                    .WithDisplayName("Revenue")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Total revenue generated in USD"
                    }))
                .WithField("TopCountries", field => field
                    .OfType("TextField")
                    .WithDisplayName("Top Countries")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Top destination countries for workers"
                    }))
                .WithField("TopIndustries", field => field
                    .OfType("TextField")
                    .WithDisplayName("Top Industries")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Top industries for job placements"
                    }))
                .WithField("GeneratedBy", field => field
                    .OfType("TextField")
                    .WithDisplayName("Generated By")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Staff member who generated this report"
                    }))
                .WithField("IsPublic", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Public")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Should this report be visible to the public"
                    }))
            );

            return 1;
        }
    }
}