using NhanViet.Analytics.Models;
using OrchardCore.ContentManagement;

namespace NhanViet.Analytics.ViewModels;

public class AnalyticsPartViewModel
{
    public string TrackingCode { get; set; } = string.Empty;
    public bool EnableTracking { get; set; } = true;
    public string AnalyticsProvider { get; set; } = "GoogleAnalytics";
    
    public AnalyticsPart AnalyticsPart { get; set; } = new();
    public ContentItem ContentItem { get; set; } = new();
}