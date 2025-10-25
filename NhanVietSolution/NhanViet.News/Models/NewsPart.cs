using OrchardCore.ContentManagement;

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
}