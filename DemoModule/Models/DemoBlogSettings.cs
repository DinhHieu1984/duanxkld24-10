namespace OrchardCoreLearning.DemoBlogModule.Models
{
    /// <summary>
    /// ✅ ĐÚNG: Settings model tuân thủ OrchardCore conventions
    /// - Naming convention: {Module}Settings
    /// - Properties với default values
    /// - Serializable
    /// </summary>
    public class DemoBlogSettings
    {
        /// <summary>
        /// Whether the module has been initialized
        /// </summary>
        public bool IsInitialized { get; set; } = false;
        
        /// <summary>
        /// Number of posts to display per page
        /// </summary>
        public int PostsPerPage { get; set; } = 10;
        
        /// <summary>
        /// Whether comments are allowed on blog posts
        /// </summary>
        public bool AllowComments { get; set; } = true;
        
        /// <summary>
        /// Whether comments require approval before being displayed
        /// </summary>
        public bool RequireCommentApproval { get; set; } = false;
        
        /// <summary>
        /// Whether to show author information on blog posts
        /// </summary>
        public bool ShowAuthor { get; set; } = true;
        
        /// <summary>
        /// Whether to show published date on blog posts
        /// </summary>
        public bool ShowPublishedDate { get; set; } = true;
        
        /// <summary>
        /// Whether to show categories on blog posts
        /// </summary>
        public bool ShowCategories { get; set; } = true;
        
        /// <summary>
        /// Whether to show tags on blog posts
        /// </summary>
        public bool ShowTags { get; set; } = true;
        
        /// <summary>
        /// Whether to show reading time on blog posts
        /// </summary>
        public bool ShowReadingTime { get; set; } = true;
        
        /// <summary>
        /// Whether to show view count on blog posts
        /// </summary>
        public bool ShowViewCount { get; set; } = false;
        
        /// <summary>
        /// Default category for new blog posts
        /// </summary>
        public string DefaultCategory { get; set; } = "General";
        
        /// <summary>
        /// Whether to automatically generate excerpts from content
        /// </summary>
        public bool AutoGenerateExcerpts { get; set; } = true;
        
        /// <summary>
        /// Maximum length for auto-generated excerpts
        /// </summary>
        public int ExcerptLength { get; set; } = 200;
        
        /// <summary>
        /// Whether to enable SEO features
        /// </summary>
        public bool EnableSeo { get; set; } = true;
        
        /// <summary>
        /// Whether to enable RSS feed
        /// </summary>
        public bool EnableRssFeed { get; set; } = true;
        
        /// <summary>
        /// RSS feed title
        /// </summary>
        public string RssFeedTitle { get; set; } = "Blog Posts";
        
        /// <summary>
        /// RSS feed description
        /// </summary>
        public string RssFeedDescription { get; set; } = "Latest blog posts";
        
        /// <summary>
        /// Number of posts to include in RSS feed
        /// </summary>
        public int RssFeedPostCount { get; set; } = 20;
        
        /// <summary>
        /// Custom CSS for blog styling
        /// </summary>
        public string CustomCss { get; set; } = string.Empty;
        
        /// <summary>
        /// Custom JavaScript for blog functionality
        /// </summary>
        public string CustomJavaScript { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether to enable social sharing buttons
        /// </summary>
        public bool EnableSocialSharing { get; set; } = true;
        
        /// <summary>
        /// Social sharing platforms to enable
        /// </summary>
        public string[] EnabledSocialPlatforms { get; set; } = new[] { "Facebook", "Twitter", "LinkedIn" };
        
        /// <summary>
        /// Whether to enable related posts feature
        /// </summary>
        public bool EnableRelatedPosts { get; set; } = true;
        
        /// <summary>
        /// Number of related posts to show
        /// </summary>
        public int RelatedPostsCount { get; set; } = 3;
        
        /// <summary>
        /// Whether to enable search functionality
        /// </summary>
        public bool EnableSearch { get; set; } = true;
        
        /// <summary>
        /// Whether to enable archive functionality
        /// </summary>
        public bool EnableArchive { get; set; } = true;
        
        /// <summary>
        /// Archive grouping method (Monthly, Yearly)
        /// </summary>
        public string ArchiveGrouping { get; set; } = "Monthly";
    }
}