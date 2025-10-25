using OrchardCore.ContentManagement;

namespace NhanViet.Analytics.Models;

public class AnalyticsPart : ContentPart
{
    public string TrackingCode { get; set; } = string.Empty;
    public bool EnableTracking { get; set; } = true;
    public string AnalyticsProvider { get; set; } = "GoogleAnalytics";
}