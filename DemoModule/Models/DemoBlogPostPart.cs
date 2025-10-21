using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;

namespace OrchardCoreLearning.DemoBlogModule.Models
{
    /// <summary>
    /// ✅ ĐÚNG: Content Part definition tuân thủ OrchardCore conventions
    /// - Kế thừa từ ContentPart
    /// - Sử dụng ContentFields
    /// - Naming convention: {Purpose}Part
    /// - Properties có getter/setter
    /// </summary>
    public class DemoBlogPostPart : ContentPart
    {
        /// <summary>
        /// Blog post category
        /// </summary>
        public TextField Category { get; set; } = new();
        
        /// <summary>
        /// Comma-separated tags
        /// </summary>
        public TextField Tags { get; set; } = new();
        
        /// <summary>
        /// Short excerpt/summary of the blog post
        /// </summary>
        public TextField Excerpt { get; set; } = new();
        
        /// <summary>
        /// Whether this post is featured
        /// </summary>
        public BooleanField IsFeatured { get; set; } = new();
        
        /// <summary>
        /// Custom published date (can be different from ContentItem.PublishedUtc)
        /// </summary>
        public DateTimeField PublishedDate { get; set; } = new();
        
        /// <summary>
        /// Number of times this post has been viewed
        /// </summary>
        public NumericField ViewCount { get; set; } = new();
        
        /// <summary>
        /// Estimated reading time in minutes
        /// </summary>
        public NumericField ReadingTime { get; set; } = new();
        
        /// <summary>
        /// Author name (can be different from ContentItem.Author)
        /// </summary>
        public TextField AuthorName { get; set; } = new();
        
        /// <summary>
        /// SEO meta description
        /// </summary>
        public TextField MetaDescription { get; set; } = new();
        
        /// <summary>
        /// SEO keywords
        /// </summary>
        public TextField MetaKeywords { get; set; } = new();
    }
}