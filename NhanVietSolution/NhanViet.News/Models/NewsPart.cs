using OrchardCore.ContentManagement;
using System.Text.Json.Serialization;

namespace NhanViet.News.Models;

public class NewsPart : ContentPart
{
    
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; } = DateTime.Now;
    public bool IsFeatured { get; set; } = false;
    public string Author { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    
    public void Apply(NewsPart other)
    {
        if (other == null) return;
        
        if (!string.IsNullOrEmpty(other.Summary))
            Summary = other.Summary;
        if (!string.IsNullOrEmpty(other.Category))
            Category = other.Category;
        if (!string.IsNullOrEmpty(other.Tags))
            Tags = other.Tags;
        if (!string.IsNullOrEmpty(other.Author))
            Author = other.Author;
        if (!string.IsNullOrEmpty(other.ImageUrl))
            ImageUrl = other.ImageUrl;
            
        // Update date properties
        if (other.PublishedDate != default(DateTime))
            PublishedDate = other.PublishedDate;
            
        // Update boolean properties
        IsFeatured = other.IsFeatured;
    }
}