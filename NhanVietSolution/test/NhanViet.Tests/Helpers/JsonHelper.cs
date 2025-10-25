using Newtonsoft.Json;

namespace NhanViet.Tests.Helpers;

public static class JsonHelper
{
    public static readonly JsonSerializerSettings OrchardCoreSettings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        DateFormatHandling = DateFormatHandling.IsoDateFormat
    };

    public static string SerializeObject(object value)
    {
        return JsonConvert.SerializeObject(value, OrchardCoreSettings);
    }

    public static T? DeserializeObject<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, OrchardCoreSettings);
    }
}