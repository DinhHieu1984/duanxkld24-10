using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Autoroute.Models;

namespace NhanViet.Consultation
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
            // Create ConsultationRequest Content Type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("ConsultationRequest", type => type
                .DisplayedAs("Consultation Request")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable()
                .WithPart("TitlePart", part => part
                    .WithPosition("1"))
                .WithPart("ConsultationRequest", part => part
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
                        Pattern = "/consultations/{{ Model.ContentItem | display_text | slugify }}",
                        ShowHomepageOption = false
                    }))
            );

            // Create ConsultationRequest Content Part with Fields
            await _contentDefinitionManager.AlterPartDefinitionAsync("ConsultationRequest", part => part
                .Attachable()
                .WithDescription("Provides consultation request specific functionality")
                .WithField("CustomerName", field => field
                    .OfType("TextField")
                    .WithDisplayName("Customer Name")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Full name of the customer requesting consultation"
                    }))
                .WithField("CustomerPhone", field => field
                    .OfType("TextField")
                    .WithDisplayName("Customer Phone")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Customer's contact phone number"
                    }))
                .WithField("CustomerEmail", field => field
                    .OfType("TextField")
                    .WithDisplayName("Customer Email")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Customer's contact email address"
                    }))
                .WithField("ServiceType", field => field
                    .OfType("TextField")
                    .WithDisplayName("Service Type")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Type of service requested (e.g., Labor Export, Visa Consultation, etc.)"
                    }))
                .WithField("PreferredCountry", field => field
                    .OfType("TextField")
                    .WithDisplayName("Preferred Country")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Country where customer wants to work"
                    }))
                .WithField("Message", field => field
                    .OfType("TextField")
                    .WithDisplayName("Message")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Detailed message or questions from customer"
                    }))
                .WithField("RequestDate", field => field
                    .OfType("DateTimeField")
                    .WithDisplayName("Request Date")
                    .WithSettings(new DateTimeFieldSettings
                    {
                        Hint = "When was this consultation request submitted"
                    }))
                .WithField("Status", field => field
                    .OfType("TextField")
                    .WithDisplayName("Status")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Current status (New, In Progress, Completed, Cancelled)"
                    }))
                .WithField("AssignedTo", field => field
                    .OfType("TextField")
                    .WithDisplayName("Assigned To")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Staff member assigned to handle this request"
                    }))
                .WithField("Priority", field => field
                    .OfType("TextField")
                    .WithDisplayName("Priority")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Priority level (Low, Medium, High, Urgent)"
                    }))
                .WithField("IsUrgent", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Urgent")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Mark as urgent consultation request"
                    }))
                .WithField("FollowUpDate", field => field
                    .OfType("DateTimeField")
                    .WithDisplayName("Follow Up Date")
                    .WithSettings(new DateTimeFieldSettings
                    {
                        Hint = "When to follow up with the customer"
                    }))
            );

            return 1;
        }
    }
}