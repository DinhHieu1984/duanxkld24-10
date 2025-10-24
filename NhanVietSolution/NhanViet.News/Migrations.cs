using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentFields.Settings;
using OrchardCore.Autoroute.Models;
using OrchardCore.Media.Settings;

namespace NhanViet.News
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
            // Create NewsArticle Content Type
            await _contentDefinitionManager.AlterTypeDefinitionAsync("NewsArticle", type => type
                .DisplayedAs("News Article")
                .Creatable()
                .Listable()
                .Draftable()
                .Versionable()
                .Securable()
                .WithPart("TitlePart", part => part
                    .WithPosition("1"))
                .WithPart("NewsArticle", part => part
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
                        Pattern = "/news/{{ Model.ContentItem | display_text | slugify }}",
                        ShowHomepageOption = false
                    }))
            );

            // Create NewsArticle Content Part with Fields
            await _contentDefinitionManager.AlterPartDefinitionAsync("NewsArticle", part => part
                .Attachable()
                .WithDescription("Provides news article specific functionality")
                .WithField("Summary", field => field
                    .OfType("TextField")
                    .WithDisplayName("Summary")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Brief summary of the news article"
                    }))
                .WithField("Category", field => field
                    .OfType("TextField")
                    .WithDisplayName("Category")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "News category (e.g., Labor Export, Company News, etc.)"
                    }))
                .WithField("Tags", field => field
                    .OfType("TextField")
                    .WithDisplayName("Tags")
                    .WithSettings(new TextFieldSettings
                    {
                        Hint = "Comma-separated tags"
                    }))
                .WithField("FeaturedImage", field => field
                    .OfType("MediaField")
                    .WithDisplayName("Featured Image")
                    .WithSettings(new MediaFieldSettings
                    {
                        Hint = "Main image for the news article",
                        Multiple = false
                    }))
                .WithField("PublishDate", field => field
                    .OfType("DateTimeField")
                    .WithDisplayName("Publish Date")
                    .WithSettings(new DateTimeFieldSettings
                    {
                        Hint = "When should this article be published"
                    }))
                .WithField("IsFeatured", field => field
                    .OfType("BooleanField")
                    .WithDisplayName("Is Featured")
                    .WithSettings(new BooleanFieldSettings
                    {
                        Hint = "Should this article be featured on homepage"
                    }))
                .WithField("ViewCount", field => field
                    .OfType("NumericField")
                    .WithDisplayName("View Count")
                    .WithSettings(new NumericFieldSettings
                    {
                        Hint = "Number of times this article has been viewed",
                        DefaultValue = "0"
                    }))
            );

            return 1;
        }
    }
}