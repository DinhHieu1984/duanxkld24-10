using NhanViet.News.Models;
using OrchardCore.ContentManagement;

namespace NhanViet.News.ViewModels;

public class NewsPartViewModel
{
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; } = DateTime.Now;
    public bool IsFeatured { get; set; } = false;
    public string Author { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    
    public NewsPart NewsPart { get; set; } = new();
    public ContentItem ContentItem { get; set; } = new();
}