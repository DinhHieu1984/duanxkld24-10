# 🔧 **Advanced Patterns trong OrchardCore**

## 🎯 **TỔNG QUAN**

**Advanced Patterns** trong OrchardCore cung cấp các kỹ thuật nâng cao cho việc mở rộng và tùy chỉnh hệ thống:
- **Custom Content Fields**: Tạo field types hoàn toàn mới với logic phức tạp
- **Dynamic Content Builders**: Xây dựng content types và parts động
- **Advanced Recipe Patterns**: Recipe steps phức tạp với conditional logic
- **Extensibility Patterns**: Plugin architecture và module composition
- **Meta-Programming**: Code generation và reflection-based solutions
- **Advanced Integration Patterns**: Third-party system integration với complex workflows

---

## 🏗️ **KIẾN TRÚC CORE COMPONENTS**

### **1. 🎨 Custom Content Fields - Advanced Field Types**

```csharp
// Advanced custom field với complex validation và processing
public class GeoLocationField : ContentField
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Address { get; set; }
    public string PlaceId { get; set; }
    public GeoLocationAccuracy Accuracy { get; set; }
    public DateTime? LastUpdated { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum GeoLocationAccuracy
{
    Unknown = 0,
    Country = 1,
    Region = 2,
    City = 3,
    Street = 4,
    Building = 5,
    Exact = 6
}

// Advanced field display driver với multiple display modes
public class GeoLocationFieldDisplayDriver : ContentFieldDisplayDriver<GeoLocationField>
{
    private readonly IGeocodingService _geocodingService;
    private readonly IMapService _mapService;
    private readonly ILogger<GeoLocationFieldDisplayDriver> _logger;

    public GeoLocationFieldDisplayDriver(
        IGeocodingService geocodingService,
        IMapService mapService,
        ILogger<GeoLocationFieldDisplayDriver> logger)
    {
        _geocodingService = geocodingService;
        _mapService = mapService;
        _logger = logger;
    }

    public override IDisplayResult Display(GeoLocationField field, BuildFieldDisplayContext context)
    {
        return Initialize<DisplayGeoLocationFieldViewModel>(GetDisplayShapeType(context), async model =>
        {
            model.Field = field;
            model.Part = context.ContentPart;
            model.PartFieldDefinition = context.PartFieldDefinition;
            
            var settings = context.PartFieldDefinition.GetSettings<GeoLocationFieldSettings>();
            
            // Generate map embed based on settings
            if (settings.ShowMap && field.Latitude.HasValue && field.Longitude.HasValue)
            {
                model.MapEmbedUrl = await _mapService.GenerateMapEmbedUrlAsync(
                    field.Latitude.Value, 
                    field.Longitude.Value, 
                    settings.MapZoomLevel,
                    settings.MapType);
            }

            // Get nearby places if enabled
            if (settings.ShowNearbyPlaces && field.Latitude.HasValue && field.Longitude.HasValue)
            {
                model.NearbyPlaces = await _geocodingService.GetNearbyPlacesAsync(
                    field.Latitude.Value, 
                    field.Longitude.Value, 
                    settings.NearbyPlacesRadius);
            }

            // Calculate distance from reference point if configured
            if (settings.ReferencePoint != null)
            {
                model.DistanceFromReference = CalculateDistance(
                    field.Latitude ?? 0, field.Longitude ?? 0,
                    settings.ReferencePoint.Latitude, settings.ReferencePoint.Longitude);
            }
        })
        .Location("Detail", "Content")
        .Location("Summary", "Meta");
    }

    public override IDisplayResult Edit(GeoLocationField field, BuildFieldEditorContext context)
    {
        return Initialize<EditGeoLocationFieldViewModel>(GetEditorShapeType(context), async model =>
        {
            var settings = context.PartFieldDefinition.GetSettings<GeoLocationFieldSettings>();
            
            model.Latitude = field.Latitude;
            model.Longitude = field.Longitude;
            model.Address = field.Address;
            model.PlaceId = field.PlaceId;
            model.Accuracy = field.Accuracy;
            model.Field = field;
            model.Part = context.ContentPart;
            model.PartFieldDefinition = context.PartFieldDefinition;
            
            // Load map configuration
            model.MapApiKey = settings.MapApiKey;
            model.DefaultZoom = settings.DefaultZoomLevel;
            model.AllowManualCoordinates = settings.AllowManualCoordinates;
            model.RequireAddress = settings.RequireAddress;
            
            // Load saved places for quick selection
            if (settings.ShowSavedPlaces)
            {
                model.SavedPlaces = await GetSavedPlacesAsync(context.ContentPart.ContentItem.ContentType);
            }
        });
    }

    public override async Task<IDisplayResult> UpdateAsync(GeoLocationField field, UpdateFieldEditorContext context)
    {
        var model = new EditGeoLocationFieldViewModel();
        await context.Updater.TryUpdateModelAsync(model, Prefix);

        var settings = context.PartFieldDefinition.GetSettings<GeoLocationFieldSettings>();

        // Validate coordinates
        if (model.Latitude.HasValue && model.Longitude.HasValue)
        {
            if (!IsValidCoordinate(model.Latitude.Value, model.Longitude.Value))
            {
                context.Updater.ModelState.AddModelError(Prefix, nameof(model.Latitude), 
                    S["Invalid coordinates provided."]);
                return Edit(field, context);
            }
        }

        // Geocode address if provided and coordinates are missing
        if (!string.IsNullOrEmpty(model.Address) && (!model.Latitude.HasValue || !model.Longitude.HasValue))
        {
            try
            {
                var geocodeResult = await _geocodingService.GeocodeAsync(model.Address);
                if (geocodeResult.Success)
                {
                    model.Latitude = geocodeResult.Latitude;
                    model.Longitude = geocodeResult.Longitude;
                    model.PlaceId = geocodeResult.PlaceId;
                    model.Accuracy = geocodeResult.Accuracy;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to geocode address: {Address}", model.Address);
                
                if (settings.RequireValidCoordinates)
                {
                    context.Updater.ModelState.AddModelError(Prefix, nameof(model.Address), 
                        S["Could not find coordinates for the provided address."]);
                    return Edit(field, context);
                }
            }
        }

        // Reverse geocode coordinates if address is missing
        if (model.Latitude.HasValue && model.Longitude.HasValue && string.IsNullOrEmpty(model.Address))
        {
            try
            {
                var reverseGeocodeResult = await _geocodingService.ReverseGeocodeAsync(
                    model.Latitude.Value, model.Longitude.Value);
                
                if (reverseGeocodeResult.Success)
                {
                    model.Address = reverseGeocodeResult.Address;
                    model.PlaceId = reverseGeocodeResult.PlaceId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reverse geocode coordinates: {Lat}, {Lng}", 
                    model.Latitude, model.Longitude);
            }
        }

        // Update field
        field.Latitude = model.Latitude;
        field.Longitude = model.Longitude;
        field.Address = model.Address;
        field.PlaceId = model.PlaceId;
        field.Accuracy = model.Accuracy;
        field.LastUpdated = DateTime.UtcNow;

        // Store additional metadata
        field.Metadata["UpdatedBy"] = context.Updater.ModelState.Keys.FirstOrDefault();
        field.Metadata["ValidationPassed"] = !context.Updater.ModelState.HasError();

        return Edit(field, context);
    }

    private bool IsValidCoordinate(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double R = 6371; // Earth's radius in kilometers
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180;
}

// Advanced field settings với complex configuration
public class GeoLocationFieldSettings
{
    public string MapApiKey { get; set; }
    public MapProvider MapProvider { get; set; } = MapProvider.GoogleMaps;
    public MapType MapType { get; set; } = MapType.Roadmap;
    public int DefaultZoomLevel { get; set; } = 15;
    public int MapZoomLevel { get; set; } = 15;
    public bool ShowMap { get; set; } = true;
    public bool AllowManualCoordinates { get; set; } = true;
    public bool RequireAddress { get; set; } = false;
    public bool RequireValidCoordinates { get; set; } = true;
    public bool ShowNearbyPlaces { get; set; } = false;
    public int NearbyPlacesRadius { get; set; } = 1000; // meters
    public bool ShowSavedPlaces { get; set; } = false;
    public GeoPoint ReferencePoint { get; set; }
    public List<string> AllowedCountries { get; set; } = new();
    public List<PlaceType> AllowedPlaceTypes { get; set; } = new();
}

public enum MapProvider
{
    GoogleMaps,
    OpenStreetMap,
    Mapbox,
    BingMaps
}

public enum MapType
{
    Roadmap,
    Satellite,
    Hybrid,
    Terrain
}

public enum PlaceType
{
    Restaurant,
    Hospital,
    School,
    Bank,
    GasStation,
    Hotel,
    Store
}

// Advanced field indexing handler
public class GeoLocationFieldIndexHandler : ContentFieldIndexHandler<GeoLocationField>
{
    public override Task BuildIndexAsync(GeoLocationField field, BuildFieldIndexContext context)
    {
        var options = DocumentIndexOptions.Store;

        // Index coordinates for spatial queries
        if (field.Latitude.HasValue && field.Longitude.HasValue)
        {
            context.DocumentIndex.Set($"{context.Keys.FirstOrDefault()}.Latitude", field.Latitude.Value, options);
            context.DocumentIndex.Set($"{context.Keys.FirstOrDefault()}.Longitude", field.Longitude.Value, options);
            
            // Create geohash for efficient spatial indexing
            var geohash = GenerateGeohash(field.Latitude.Value, field.Longitude.Value);
            context.DocumentIndex.Set($"{context.Keys.FirstOrDefault()}.Geohash", geohash, options);
            
            // Index different precision levels for hierarchical spatial queries
            for (int precision = 1; precision <= 12; precision++)
            {
                var precisionGeohash = geohash.Substring(0, Math.Min(precision, geohash.Length));
                context.DocumentIndex.Set($"{context.Keys.FirstOrDefault()}.Geohash{precision}", precisionGeohash, options);
            }
        }

        // Index address components for text search
        if (!string.IsNullOrEmpty(field.Address))
        {
            context.DocumentIndex.Set($"{context.Keys.FirstOrDefault()}.Address", field.Address, 
                DocumentIndexOptions.Analyze | DocumentIndexOptions.Store);
        }

        // Index place ID for exact matching
        if (!string.IsNullOrEmpty(field.PlaceId))
        {
            context.DocumentIndex.Set($"{context.Keys.FirstOrDefault()}.PlaceId", field.PlaceId, options);
        }

        // Index accuracy for filtering
        context.DocumentIndex.Set($"{context.Keys.FirstOrDefault()}.Accuracy", (int)field.Accuracy, options);

        return Task.CompletedTask;
    }

    private string GenerateGeohash(double latitude, double longitude)
    {
        // Simplified geohash implementation
        // In production, use a proper geohash library
        const string base32 = "0123456789bcdefghjkmnpqrstuvwxyz";
        var latRange = new[] { -90.0, 90.0 };
        var lonRange = new[] { -180.0, 180.0 };
        var geohash = "";
        var bits = 0;
        var bit = 0;
        var even = true;

        while (geohash.Length < 12)
        {
            double mid;
            if (even)
            {
                mid = (lonRange[0] + lonRange[1]) / 2;
                if (longitude >= mid)
                {
                    bit = (bit << 1) + 1;
                    lonRange[0] = mid;
                }
                else
                {
                    bit = bit << 1;
                    lonRange[1] = mid;
                }
            }
            else
            {
                mid = (latRange[0] + latRange[1]) / 2;
                if (latitude >= mid)
                {
                    bit = (bit << 1) + 1;
                    latRange[0] = mid;
                }
                else
                {
                    bit = bit << 1;
                    latRange[1] = mid;
                }
            }

            even = !even;
            if (++bits == 5)
            {
                geohash += base32[bit];
                bits = 0;
                bit = 0;
            }
        }

        return geohash;
    }
}
```

### **2. 🏗️ Dynamic Content Builders - Runtime Content Type Creation**

```csharp
// Advanced dynamic content type builder
public class DynamicContentTypeBuilder : IDynamicContentTypeBuilder
{
    private readonly IContentDefinitionManager _contentDefinitionManager;
    private readonly IContentFieldProviders _fieldProviders;
    private readonly ILogger<DynamicContentTypeBuilder> _logger;

    public async Task<ContentTypeDefinition> BuildContentTypeAsync(DynamicContentTypeRequest request)
    {
        var contentTypeBuilder = new ContentTypeDefinitionBuilder()
            .Named(request.Name)
            .DisplayedAs(request.DisplayName)
            .WithDescription(request.Description);

        // Configure content type settings
        if (request.Settings != null)
        {
            contentTypeBuilder = ConfigureContentTypeSettings(contentTypeBuilder, request.Settings);
        }

        // Add parts dynamically
        foreach (var partRequest in request.Parts)
        {
            contentTypeBuilder = await AddPartToContentTypeAsync(contentTypeBuilder, partRequest);
        }

        // Build and register the content type
        var contentTypeDefinition = contentTypeBuilder.Build();
        await _contentDefinitionManager.StoreTypeDefinitionAsync(contentTypeDefinition);

        _logger.LogInformation("Created dynamic content type: {ContentType}", request.Name);
        return contentTypeDefinition;
    }

    private async Task<ContentTypeDefinitionBuilder> AddPartToContentTypeAsync(
        ContentTypeDefinitionBuilder builder, 
        DynamicPartRequest partRequest)
    {
        if (partRequest.IsCustomPart)
        {
            // Create custom part definition
            var partDefinition = await CreateCustomPartDefinitionAsync(partRequest);
            await _contentDefinitionManager.StorePartDefinitionAsync(partDefinition);
        }

        var partBuilder = builder.WithPart(partRequest.Name, partRequest.DisplayName, part =>
        {
            // Configure part settings
            if (partRequest.Settings != null)
            {
                foreach (var setting in partRequest.Settings)
                {
                    part.WithSetting(setting.Key, setting.Value?.ToString());
                }
            }

            // Add fields to part
            foreach (var fieldRequest in partRequest.Fields)
            {
                part = AddFieldToPartAsync(part, fieldRequest);
            }

            return part;
        });

        return partBuilder;
    }

    private ContentPartDefinitionBuilder AddFieldToPartAsync(
        ContentPartDefinitionBuilder partBuilder, 
        DynamicFieldRequest fieldRequest)
    {
        return partBuilder.WithField(fieldRequest.Name, field =>
        {
            field.OfType(fieldRequest.FieldType)
                 .WithDisplayName(fieldRequest.DisplayName);

            // Configure field settings
            if (fieldRequest.Settings != null)
            {
                foreach (var setting in fieldRequest.Settings)
                {
                    field.WithSetting(setting.Key, setting.Value?.ToString());
                }
            }

            // Configure field position
            if (fieldRequest.Position.HasValue)
            {
                field.WithPosition(fieldRequest.Position.Value.ToString());
            }

            return field;
        });
    }

    private async Task<ContentPartDefinition> CreateCustomPartDefinitionAsync(DynamicPartRequest partRequest)
    {
        var partBuilder = new ContentPartDefinitionBuilder()
            .Named(partRequest.Name)
            .WithDescription(partRequest.Description);

        // Add fields to custom part
        foreach (var fieldRequest in partRequest.Fields)
        {
            partBuilder = partBuilder.WithField(fieldRequest.Name, field =>
            {
                field.OfType(fieldRequest.FieldType)
                     .WithDisplayName(fieldRequest.DisplayName);

                if (fieldRequest.Settings != null)
                {
                    foreach (var setting in fieldRequest.Settings)
                    {
                        field.WithSetting(setting.Key, setting.Value?.ToString());
                    }
                }

                return field;
            });
        }

        return partBuilder.Build();
    }

    private ContentTypeDefinitionBuilder ConfigureContentTypeSettings(
        ContentTypeDefinitionBuilder builder, 
        Dictionary<string, object> settings)
    {
        foreach (var setting in settings)
        {
            builder = builder.WithSetting(setting.Key, setting.Value?.ToString());
        }

        // Configure common settings
        if (settings.ContainsKey("Creatable"))
        {
            builder = builder.Creatable(Convert.ToBoolean(settings["Creatable"]));
        }

        if (settings.ContainsKey("Listable"))
        {
            builder = builder.Listable(Convert.ToBoolean(settings["Listable"]));
        }

        if (settings.ContainsKey("Draftable"))
        {
            builder = builder.Draftable(Convert.ToBoolean(settings["Draftable"]));
        }

        if (settings.ContainsKey("Versionable"))
        {
            builder = builder.Versionable(Convert.ToBoolean(settings["Versionable"]));
        }

        if (settings.ContainsKey("Securable"))
        {
            builder = builder.Securable(Convert.ToBoolean(settings["Securable"]));
        }

        return builder;
    }
}

// Advanced content builder với template system
public class TemplatedContentBuilder : ITemplatedContentBuilder
{
    private readonly IDynamicContentTypeBuilder _contentTypeBuilder;
    private readonly IContentTemplateProvider _templateProvider;
    private readonly ILiquidTemplateManager _liquidTemplateManager;

    public async Task<ContentTypeDefinition> BuildFromTemplateAsync(string templateName, Dictionary<string, object> parameters)
    {
        // Load template
        var template = await _templateProvider.GetTemplateAsync(templateName);
        if (template == null)
        {
            throw new InvalidOperationException($"Template '{templateName}' not found");
        }

        // Process template với Liquid
        var processedTemplate = await _liquidTemplateManager.RenderStringAsync(template.Content, parameters);
        
        // Parse processed template to content type request
        var contentTypeRequest = JsonSerializer.Deserialize<DynamicContentTypeRequest>(processedTemplate);
        
        // Build content type
        return await _contentTypeBuilder.BuildContentTypeAsync(contentTypeRequest);
    }
}

// Content type template example
public class ContentTypeTemplate
{
    public string Name { get; set; }
    public string Content { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> DefaultParameters { get; set; } = new();
    public List<string> RequiredParameters { get; set; } = new();
}

// Example template content (Liquid template)
/*
{
  "name": "{{ name | default: 'DynamicContent' }}",
  "displayName": "{{ displayName | default: name }}",
  "description": "{{ description | default: 'Dynamically generated content type' }}",
  "settings": {
    "Creatable": {{ creatable | default: true }},
    "Listable": {{ listable | default: true }},
    "Draftable": {{ draftable | default: true }}
  },
  "parts": [
    {
      "name": "{{ name }}Part",
      "displayName": "{{ displayName }} Content",
      "isCustomPart": true,
      "fields": [
        {% for field in fields %}
        {
          "name": "{{ field.name }}",
          "displayName": "{{ field.displayName | default: field.name }}",
          "fieldType": "{{ field.type | default: 'TextField' }}",
          "settings": {{ field.settings | json }}
        }{% unless forloop.last %},{% endunless %}
        {% endfor %}
      ]
    }
  ]
}
*/
```

### **3. 🍳 Advanced Recipe Patterns - Complex Recipe Steps**

```csharp
// Advanced recipe step với conditional logic
public class ConditionalRecipeStep : IRecipeStepHandler
{
    public string Name => "conditional";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConditionalRecipeStep> _logger;

    public async Task ExecuteAsync(RecipeStepContext context)
    {
        var model = context.Step.Data.ToObject<ConditionalRecipeStepModel>();
        
        foreach (var condition in model.Conditions)
        {
            if (await EvaluateConditionAsync(condition, context))
            {
                _logger.LogInformation("Condition '{Condition}' evaluated to true, executing steps", condition.Name);
                
                foreach (var step in condition.Steps)
                {
                    await ExecuteNestedStepAsync(step, context);
                }
                
                // If condition is met and breakOnMatch is true, stop processing
                if (condition.BreakOnMatch)
                {
                    break;
                }
            }
        }

        // Execute default steps if no conditions were met
        if (model.DefaultSteps?.Any() == true && !model.Conditions.Any(c => c.WasExecuted))
        {
            _logger.LogInformation("No conditions were met, executing default steps");
            
            foreach (var step in model.DefaultSteps)
            {
                await ExecuteNestedStepAsync(step, context);
            }
        }
    }

    private async Task<bool> EvaluateConditionAsync(ConditionalStep condition, RecipeStepContext context)
    {
        try
        {
            switch (condition.Type.ToLower())
            {
                case "feature":
                    return await EvaluateFeatureConditionAsync(condition, context);
                
                case "setting":
                    return await EvaluateSettingConditionAsync(condition, context);
                
                case "environment":
                    return EvaluateEnvironmentCondition(condition);
                
                case "content":
                    return await EvaluateContentConditionAsync(condition, context);
                
                case "custom":
                    return await EvaluateCustomConditionAsync(condition, context);
                
                default:
                    _logger.LogWarning("Unknown condition type: {Type}", condition.Type);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition: {Condition}", condition.Name);
            return false;
        }
    }

    private async Task<bool> EvaluateFeatureConditionAsync(ConditionalStep condition, RecipeStepContext context)
    {
        var shellFeaturesManager = _serviceProvider.GetRequiredService<IShellFeaturesManager>();
        var enabledFeatures = await shellFeaturesManager.GetEnabledFeaturesAsync();
        
        var featureName = condition.Value?.ToString();
        var isEnabled = enabledFeatures.Any(f => f.Id == featureName);
        
        return condition.Operator switch
        {
            "equals" => isEnabled == Convert.ToBoolean(condition.ExpectedValue),
            "enabled" => isEnabled,
            "disabled" => !isEnabled,
            _ => false
        };
    }

    private async Task<bool> EvaluateSettingConditionAsync(ConditionalStep condition, RecipeStepContext context)
    {
        var siteService = _serviceProvider.GetRequiredService<ISiteService>();
        var site = await siteService.GetSiteSettingsAsync();
        
        var settingPath = condition.Value?.ToString();
        var actualValue = GetNestedPropertyValue(site, settingPath);
        
        return condition.Operator switch
        {
            "equals" => Equals(actualValue, condition.ExpectedValue),
            "contains" => actualValue?.ToString()?.Contains(condition.ExpectedValue?.ToString()) == true,
            "exists" => actualValue != null,
            "empty" => string.IsNullOrEmpty(actualValue?.ToString()),
            _ => false
        };
    }

    private bool EvaluateEnvironmentCondition(ConditionalStep condition)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
        return condition.Operator switch
        {
            "equals" => string.Equals(environment, condition.ExpectedValue?.ToString(), StringComparison.OrdinalIgnoreCase),
            "contains" => environment.Contains(condition.ExpectedValue?.ToString(), StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private async Task<bool> EvaluateContentConditionAsync(ConditionalStep condition, RecipeStepContext context)
    {
        var contentManager = _serviceProvider.GetRequiredService<IContentManager>();
        var session = _serviceProvider.GetRequiredService<ISession>();
        
        switch (condition.Operator)
        {
            case "exists":
                var contentType = condition.Value?.ToString();
                var count = await session.Query<ContentItem>()
                    .Where(ci => ci.ContentType == contentType)
                    .CountAsync();
                return count > 0;
            
            case "count":
                var contentType2 = condition.Value?.ToString();
                var actualCount = await session.Query<ContentItem>()
                    .Where(ci => ci.ContentType == contentType2)
                    .CountAsync();
                var expectedCount = Convert.ToInt32(condition.ExpectedValue);
                return actualCount >= expectedCount;
            
            default:
                return false;
        }
    }

    private async Task<bool> EvaluateCustomConditionAsync(ConditionalStep condition, RecipeStepContext context)
    {
        var evaluatorName = condition.Value?.ToString();
        var evaluator = _serviceProvider.GetService<ICustomConditionEvaluator>(evaluatorName);
        
        if (evaluator == null)
        {
            _logger.LogWarning("Custom condition evaluator not found: {Evaluator}", evaluatorName);
            return false;
        }

        return await evaluator.EvaluateAsync(condition, context);
    }

    private async Task ExecuteNestedStepAsync(RecipeStep step, RecipeStepContext parentContext)
    {
        var stepHandler = _serviceProvider.GetService<IRecipeStepHandler>(step.Name);
        if (stepHandler == null)
        {
            _logger.LogWarning("Recipe step handler not found: {StepName}", step.Name);
            return;
        }

        var nestedContext = new RecipeStepContext
        {
            ExecutionId = parentContext.ExecutionId,
            Step = step,
            Environment = parentContext.Environment,
            CancellationToken = parentContext.CancellationToken
        };

        await stepHandler.ExecuteAsync(nestedContext);
    }

    private object GetNestedPropertyValue(object obj, string propertyPath)
    {
        var properties = propertyPath.Split('.');
        var current = obj;

        foreach (var property in properties)
        {
            if (current == null) return null;
            
            var propertyInfo = current.GetType().GetProperty(property);
            if (propertyInfo == null) return null;
            
            current = propertyInfo.GetValue(current);
        }

        return current;
    }
}

// Loop recipe step for batch operations
public class LoopRecipeStep : IRecipeStepHandler
{
    public string Name => "loop";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoopRecipeStep> _logger;

    public async Task ExecuteAsync(RecipeStepContext context)
    {
        var model = context.Step.Data.ToObject<LoopRecipeStepModel>();
        
        var items = await GetLoopItemsAsync(model, context);
        
        _logger.LogInformation("Starting loop with {ItemCount} items", items.Count());
        
        var index = 0;
        foreach (var item in items)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Loop cancelled at index {Index}", index);
                break;
            }

            _logger.LogDebug("Processing loop item {Index}: {Item}", index, item);
            
            // Create loop context with current item
            var loopEnvironment = new Dictionary<string, object>(context.Environment)
            {
                ["LoopItem"] = item,
                ["LoopIndex"] = index,
                ["LoopIsFirst"] = index == 0,
                ["LoopIsLast"] = index == items.Count() - 1
            };

            // Execute steps for current item
            foreach (var step in model.Steps)
            {
                var loopContext = new RecipeStepContext
                {
                    ExecutionId = context.ExecutionId,
                    Step = step,
                    Environment = loopEnvironment,
                    CancellationToken = context.CancellationToken
                };

                var stepHandler = _serviceProvider.GetService<IRecipeStepHandler>(step.Name);
                if (stepHandler != null)
                {
                    await stepHandler.ExecuteAsync(loopContext);
                }
            }

            index++;
            
            // Add delay between iterations if specified
            if (model.DelayBetweenIterations > 0)
            {
                await Task.Delay(model.DelayBetweenIterations, context.CancellationToken);
            }
        }
        
        _logger.LogInformation("Completed loop with {ProcessedCount} items", index);
    }

    private async Task<IEnumerable<object>> GetLoopItemsAsync(LoopRecipeStepModel model, RecipeStepContext context)
    {
        switch (model.Source.Type.ToLower())
        {
            case "array":
                return model.Source.Items ?? new List<object>();
            
            case "range":
                var start = Convert.ToInt32(model.Source.Start);
                var end = Convert.ToInt32(model.Source.End);
                return Enumerable.Range(start, end - start + 1).Cast<object>();
            
            case "content":
                return await GetContentItemsAsync(model.Source, context);
            
            case "query":
                return await ExecuteQueryAsync(model.Source, context);
            
            default:
                _logger.LogWarning("Unknown loop source type: {Type}", model.Source.Type);
                return new List<object>();
        }
    }

    private async Task<IEnumerable<object>> GetContentItemsAsync(LoopSource source, RecipeStepContext context)
    {
        var session = _serviceProvider.GetRequiredService<ISession>();
        
        var query = session.Query<ContentItem>();
        
        if (!string.IsNullOrEmpty(source.ContentType))
        {
            query = query.Where(ci => ci.ContentType == source.ContentType);
        }

        if (source.Published.HasValue)
        {
            query = query.Where(ci => ci.Published == source.Published.Value);
        }

        if (source.MaxItems.HasValue)
        {
            query = query.Take(source.MaxItems.Value);
        }

        var items = await query.ListAsync();
        return items.Cast<object>();
    }
}

// Parallel recipe step for concurrent execution
public class ParallelRecipeStep : IRecipeStepHandler
{
    public string Name => "parallel";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ParallelRecipeStep> _logger;

    public async Task ExecuteAsync(RecipeStepContext context)
    {
        var model = context.Step.Data.ToObject<ParallelRecipeStepModel>();
        
        var maxDegreeOfParallelism = model.MaxDegreeOfParallelism ?? Environment.ProcessorCount;
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        
        _logger.LogInformation("Starting parallel execution of {StepCount} steps with max parallelism {MaxParallelism}", 
            model.Steps.Count, maxDegreeOfParallelism);

        var tasks = model.Steps.Select(async step =>
        {
            await semaphore.WaitAsync(context.CancellationToken);
            
            try
            {
                var stepHandler = _serviceProvider.GetService<IRecipeStepHandler>(step.Name);
                if (stepHandler != null)
                {
                    var parallelContext = new RecipeStepContext
                    {
                        ExecutionId = context.ExecutionId,
                        Step = step,
                        Environment = context.Environment,
                        CancellationToken = context.CancellationToken
                    };

                    await stepHandler.ExecuteAsync(parallelContext);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        if (model.WaitForAll)
        {
            await Task.WhenAll(tasks);
        }
        else
        {
            // Fire and forget
            _ = Task.WhenAll(tasks);
        }
        
        _logger.LogInformation("Completed parallel execution setup");
    }
}
```

### **4. 🔌 Extensibility Patterns - Plugin Architecture**

```csharp
// Advanced plugin system với dependency injection
public interface IPluginManager
{
    Task<IEnumerable<IPlugin>> GetPluginsAsync();
    Task<T> GetPluginAsync<T>(string pluginId) where T : class, IPlugin;
    Task LoadPluginAsync(string pluginPath);
    Task UnloadPluginAsync(string pluginId);
    Task<bool> IsPluginLoadedAsync(string pluginId);
}

public class AdvancedPluginManager : IPluginManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdvancedPluginManager> _logger;
    private readonly ConcurrentDictionary<string, PluginContext> _loadedPlugins = new();
    private readonly IPluginAssemblyLoadContext _assemblyLoadContext;

    public async Task LoadPluginAsync(string pluginPath)
    {
        try
        {
            _logger.LogInformation("Loading plugin from: {PluginPath}", pluginPath);

            // Load plugin assembly
            var assembly = await _assemblyLoadContext.LoadFromPathAsync(pluginPath);
            
            // Find plugin types
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var pluginType in pluginTypes)
            {
                await LoadPluginTypeAsync(pluginType, assembly);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from: {PluginPath}", pluginPath);
            throw;
        }
    }

    private async Task LoadPluginTypeAsync(Type pluginType, Assembly assembly)
    {
        // Get plugin metadata
        var pluginAttribute = pluginType.GetCustomAttribute<PluginAttribute>();
        if (pluginAttribute == null)
        {
            _logger.LogWarning("Plugin type {PluginType} missing PluginAttribute", pluginType.Name);
            return;
        }

        var pluginId = pluginAttribute.Id ?? pluginType.Name;
        
        if (_loadedPlugins.ContainsKey(pluginId))
        {
            _logger.LogWarning("Plugin {PluginId} is already loaded", pluginId);
            return;
        }

        // Create plugin instance
        var plugin = await CreatePluginInstanceAsync(pluginType);
        
        // Create plugin context
        var pluginContext = new PluginContext
        {
            Id = pluginId,
            Name = pluginAttribute.Name ?? pluginType.Name,
            Version = pluginAttribute.Version ?? "1.0.0",
            Description = pluginAttribute.Description,
            Author = pluginAttribute.Author,
            Plugin = plugin,
            Assembly = assembly,
            Type = pluginType,
            LoadedAt = DateTime.UtcNow,
            Dependencies = pluginAttribute.Dependencies ?? Array.Empty<string>()
        };

        // Validate dependencies
        if (!await ValidatePluginDependenciesAsync(pluginContext))
        {
            _logger.LogError("Plugin {PluginId} has unmet dependencies", pluginId);
            return;
        }

        // Initialize plugin
        await InitializePluginAsync(pluginContext);
        
        // Register plugin
        _loadedPlugins.TryAdd(pluginId, pluginContext);
        
        _logger.LogInformation("Successfully loaded plugin: {PluginId} v{Version}", pluginId, pluginContext.Version);
    }

    private async Task<IPlugin> CreatePluginInstanceAsync(Type pluginType)
    {
        // Use dependency injection to create plugin instance
        var constructors = pluginType.GetConstructors();
        var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
        
        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];
        
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            args[i] = _serviceProvider.GetService(parameterType);
            
            if (args[i] == null && !parameters[i].HasDefaultValue)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve dependency {parameterType.Name} for plugin {pluginType.Name}");
            }
        }

        var instance = Activator.CreateInstance(pluginType, args) as IPlugin;
        return instance;
    }

    private async Task<bool> ValidatePluginDependenciesAsync(PluginContext pluginContext)
    {
        foreach (var dependency in pluginContext.Dependencies)
        {
            if (!_loadedPlugins.ContainsKey(dependency))
            {
                // Try to load dependency
                var dependencyPath = await FindPluginPathAsync(dependency);
                if (dependencyPath != null)
                {
                    await LoadPluginAsync(dependencyPath);
                }
                
                if (!_loadedPlugins.ContainsKey(dependency))
                {
                    _logger.LogError("Plugin {PluginId} requires dependency {Dependency} which could not be loaded", 
                        pluginContext.Id, dependency);
                    return false;
                }
            }
        }

        return true;
    }

    private async Task InitializePluginAsync(PluginContext pluginContext)
    {
        try
        {
            await pluginContext.Plugin.InitializeAsync(_serviceProvider);
            pluginContext.IsInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize plugin: {PluginId}", pluginContext.Id);
            throw;
        }
    }

    public async Task UnloadPluginAsync(string pluginId)
    {
        if (!_loadedPlugins.TryRemove(pluginId, out var pluginContext))
        {
            _logger.LogWarning("Plugin {PluginId} is not loaded", pluginId);
            return;
        }

        try
        {
            // Dispose plugin if it implements IDisposable
            if (pluginContext.Plugin is IDisposable disposable)
            {
                disposable.Dispose();
            }

            // Unload assembly if possible
            await _assemblyLoadContext.UnloadAsync(pluginContext.Assembly);
            
            _logger.LogInformation("Successfully unloaded plugin: {PluginId}", pluginId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading plugin: {PluginId}", pluginId);
        }
    }

    public async Task<IEnumerable<IPlugin>> GetPluginsAsync()
    {
        return _loadedPlugins.Values.Select(ctx => ctx.Plugin);
    }

    public async Task<T> GetPluginAsync<T>(string pluginId) where T : class, IPlugin
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var pluginContext))
        {
            return pluginContext.Plugin as T;
        }

        return null;
    }

    public async Task<bool> IsPluginLoadedAsync(string pluginId)
    {
        return _loadedPlugins.ContainsKey(pluginId);
    }
}

// Plugin base interface và attributes
public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    Task InitializeAsync(IServiceProvider serviceProvider);
}

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public string[] Dependencies { get; set; } = Array.Empty<string>();
}

// Plugin context for managing loaded plugins
public class PluginContext
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public IPlugin Plugin { get; set; }
    public Assembly Assembly { get; set; }
    public Type Type { get; set; }
    public DateTime LoadedAt { get; set; }
    public bool IsInitialized { get; set; }
    public string[] Dependencies { get; set; } = Array.Empty<string>();
}

// Example plugin implementation
[Plugin(
    Id = "CustomContentProcessor",
    Name = "Custom Content Processor",
    Version = "1.0.0",
    Description = "Processes content with custom logic",
    Author = "Your Company",
    Dependencies = new[] { "OrchardCore.ContentManagement" }
)]
public class CustomContentProcessorPlugin : IPlugin, IContentHandler
{
    public string Id => "CustomContentProcessor";
    public string Name => "Custom Content Processor";
    public string Version => "1.0.0";

    private IContentManager _contentManager;
    private ILogger<CustomContentProcessorPlugin> _logger;

    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        _contentManager = serviceProvider.GetRequiredService<IContentManager>();
        _logger = serviceProvider.GetRequiredService<ILogger<CustomContentProcessorPlugin>>();
        
        _logger.LogInformation("Custom Content Processor Plugin initialized");
    }

    public async Task CreatedAsync(CreateContentContext context)
    {
        // Custom content processing logic
        if (context.ContentItem.ContentType == "Article")
        {
            await ProcessArticleAsync(context.ContentItem);
        }
    }

    private async Task ProcessArticleAsync(ContentItem article)
    {
        // Custom processing logic
        _logger.LogInformation("Processing article: {ArticleId}", article.ContentItemId);
        
        // Example: Auto-generate tags based on content
        var titlePart = article.As<TitlePart>();
        if (titlePart != null && !string.IsNullOrEmpty(titlePart.Title))
        {
            var autoTags = await GenerateAutoTagsAsync(titlePart.Title);
            // Apply auto tags to article
        }
    }

    private async Task<string[]> GenerateAutoTagsAsync(string title)
    {
        // Implement auto-tag generation logic
        return new[] { "auto-generated", "article" };
    }
}
```

---

## 🎯 **KẾT LUẬN**

**Advanced Patterns** trong OrchardCore cung cấp foundation mạnh mẽ cho:

- **🎨 Custom Content Fields**: Field types phức tạp với validation, processing, và indexing
- **🏗️ Dynamic Content Builders**: Runtime content type creation với template system
- **🍳 Advanced Recipe Patterns**: Conditional logic, loops, parallel execution
- **🔌 Extensibility Patterns**: Plugin architecture với dependency injection
- **🔧 Meta-Programming**: Code generation và reflection-based solutions
- **🌐 Advanced Integration**: Complex third-party system integration

**Advanced Patterns giúp mở rộng OrchardCore beyond standard capabilities để xây dựng highly customized và scalable solutions! 🚀**

---

## 🎯 **KHI NÀO CẦN ADVANCED PATTERNS - VÍ DỤ THỰC TẾ**

### **1. 🏥 Hospital Management System - Custom Medical Fields**

#### **Tình huống:**
Anh xây dựng **hệ thống quản lý bệnh viện** cần custom fields cho medical data như vital signs, medical history, prescription tracking với complex validation và integration với medical devices.

#### **❌ TRƯỚC KHI DÙNG ADVANCED PATTERNS:**
```csharp
// Sử dụng TextField đơn giản - không đủ cho medical data
public class PatientRecord : ContentPart
{
    public string BloodPressure { get; set; } // "120/80" - string không validate được
    public string HeartRate { get; set; }     // "72" - không có unit validation
    public string Temperature { get; set; }   // "36.5" - không convert unit được
    public string MedicalHistory { get; set; } // Plain text - không structured
    public string Prescriptions { get; set; } // JSON string - không validate drug interactions
}

// Vấn đề:
// - Không validate medical values (blood pressure range, heart rate limits)
// - Không convert units (Celsius/Fahrenheit, mmHg/kPa)
// - Không check drug interactions
// - Không integrate với medical devices
// - Không có medical data visualization
// - Không có audit trail cho medical changes
```

#### **✅ SAU KHI DÙNG ADVANCED PATTERNS:**
```csharp
// Advanced Medical Vital Signs Field
public class VitalSignsField : ContentField
{
    public BloodPressure BloodPressure { get; set; }
    public HeartRate HeartRate { get; set; }
    public BodyTemperature Temperature { get; set; }
    public RespiratoryRate RespiratoryRate { get; set; }
    public OxygenSaturation OxygenSaturation { get; set; }
    public DateTime MeasuredAt { get; set; }
    public string MeasuredBy { get; set; }
    public MedicalDevice Device { get; set; }
    public VitalSignsAlert[] Alerts { get; set; } = Array.Empty<VitalSignsAlert>();
    public Dictionary<string, object> DeviceMetadata { get; set; } = new();
}

public class BloodPressure
{
    public int Systolic { get; set; }
    public int Diastolic { get; set; }
    public BloodPressureUnit Unit { get; set; } = BloodPressureUnit.MmHg;
    public BloodPressureCategory Category => CalculateCategory();
    
    private BloodPressureCategory CalculateCategory()
    {
        if (Systolic < 90 || Diastolic < 60) return BloodPressureCategory.Low;
        if (Systolic < 120 && Diastolic < 80) return BloodPressureCategory.Normal;
        if (Systolic < 130 && Diastolic < 80) return BloodPressureCategory.Elevated;
        if (Systolic < 140 || Diastolic < 90) return BloodPressureCategory.Stage1Hypertension;
        if (Systolic < 180 || Diastolic < 120) return BloodPressureCategory.Stage2Hypertension;
        return BloodPressureCategory.HypertensiveCrisis;
    }
}

// Advanced Medical Field Display Driver
public class VitalSignsFieldDisplayDriver : ContentFieldDisplayDriver<VitalSignsField>
{
    private readonly IMedicalDeviceService _deviceService;
    private readonly IMedicalValidationService _validationService;
    private readonly IMedicalAlertService _alertService;
    private readonly IMedicalAuditService _auditService;

    public override IDisplayResult Display(VitalSignsField field, BuildFieldDisplayContext context)
    {
        return Initialize<DisplayVitalSignsFieldViewModel>(GetDisplayShapeType(context), async model =>
        {
            model.Field = field;
            model.Part = context.ContentPart;
            model.PartFieldDefinition = context.PartFieldDefinition;
            
            var settings = context.PartFieldDefinition.GetSettings<VitalSignsFieldSettings>();
            
            // Generate medical charts
            if (settings.ShowTrendChart)
            {
                model.TrendChartData = await GenerateVitalSignsTrendAsync(context.ContentPart.ContentItem);
            }

            // Check for medical alerts
            model.ActiveAlerts = await _alertService.GetActiveAlertsAsync(field);
            
            // Get reference ranges based on patient demographics
            var patient = context.ContentPart.ContentItem.As<PatientPart>();
            model.ReferenceRanges = await GetReferenceRangesAsync(patient);
            
            // Medical interpretation
            model.MedicalInterpretation = await GenerateMedicalInterpretationAsync(field, patient);
        })
        .Location("Detail", "Content")
        .Location("Summary", "Meta");
    }

    public override IDisplayResult Edit(VitalSignsField field, BuildFieldEditorContext context)
    {
        return Initialize<EditVitalSignsFieldViewModel>(GetEditorShapeType(context), async model =>
        {
            var settings = context.PartFieldDefinition.GetSettings<VitalSignsFieldSettings>();
            
            model.BloodPressureSystolic = field.BloodPressure?.Systolic;
            model.BloodPressureDiastolic = field.BloodPressure?.Diastolic;
            model.HeartRate = field.HeartRate?.BeatsPerMinute;
            model.Temperature = field.Temperature?.Value;
            model.TemperatureUnit = field.Temperature?.Unit ?? TemperatureUnit.Celsius;
            model.RespiratoryRate = field.RespiratoryRate?.BreathsPerMinute;
            model.OxygenSaturation = field.OxygenSaturation?.Percentage;
            model.MeasuredAt = field.MeasuredAt;
            model.MeasuredBy = field.MeasuredBy;
            
            // Load connected medical devices
            if (settings.AllowDeviceIntegration)
            {
                model.AvailableDevices = await _deviceService.GetAvailableDevicesAsync();
                model.SelectedDeviceId = field.Device?.DeviceId;
            }
            
            // Load measurement history for context
            if (settings.ShowMeasurementHistory)
            {
                model.RecentMeasurements = await GetRecentMeasurementsAsync(context.ContentPart.ContentItem);
            }
        });
    }

    public override async Task<IDisplayResult> UpdateAsync(VitalSignsField field, UpdateFieldEditorContext context)
    {
        var model = new EditVitalSignsFieldViewModel();
        await context.Updater.TryUpdateModelAsync(model, Prefix);

        var settings = context.PartFieldDefinition.GetSettings<VitalSignsFieldSettings>();
        var patient = context.ContentPart.ContentItem.As<PatientPart>();

        // Validate vital signs ranges
        var validationResult = await _validationService.ValidateVitalSignsAsync(model, patient);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                context.Updater.ModelState.AddModelError(Prefix, error.PropertyName, error.ErrorMessage);
            }
            return Edit(field, context);
        }

        // Update field with validated data
        field.BloodPressure = new BloodPressure
        {
            Systolic = model.BloodPressureSystolic ?? 0,
            Diastolic = model.BloodPressureDiastolic ?? 0,
            Unit = BloodPressureUnit.MmHg
        };

        field.HeartRate = new HeartRate
        {
            BeatsPerMinute = model.HeartRate ?? 0,
            Rhythm = await DetectHeartRhythmAsync(model.HeartRate ?? 0)
        };

        field.Temperature = new BodyTemperature
        {
            Value = model.Temperature ?? 0,
            Unit = model.TemperatureUnit,
            Site = TemperatureSite.Oral // Default, could be configurable
        };

        field.MeasuredAt = model.MeasuredAt ?? DateTime.UtcNow;
        field.MeasuredBy = model.MeasuredBy ?? context.Updater.ModelState.Keys.FirstOrDefault();

        // Device integration
        if (!string.IsNullOrEmpty(model.SelectedDeviceId))
        {
            field.Device = await _deviceService.GetDeviceAsync(model.SelectedDeviceId);
            
            // Import data from device if available
            if (settings.AutoImportFromDevice)
            {
                var deviceData = await _deviceService.ImportVitalSignsAsync(model.SelectedDeviceId);
                if (deviceData != null)
                {
                    field = MergeDeviceData(field, deviceData);
                }
            }
        }

        // Generate medical alerts
        field.Alerts = await _alertService.GenerateAlertsAsync(field, patient);

        // Audit trail
        await _auditService.LogVitalSignsChangeAsync(field, context.ContentPart.ContentItem, 
            $"Vital signs updated by {field.MeasuredBy}");

        return Edit(field, context);
    }

    private async Task<VitalSignsTrendData> GenerateVitalSignsTrendAsync(ContentItem patient)
    {
        // Get historical vital signs data
        var historicalData = await GetHistoricalVitalSignsAsync(patient);
        
        return new VitalSignsTrendData
        {
            BloodPressureTrend = historicalData.Select(d => new DataPoint
            {
                Timestamp = d.MeasuredAt,
                Systolic = d.BloodPressure.Systolic,
                Diastolic = d.BloodPressure.Diastolic
            }).ToList(),
            
            HeartRateTrend = historicalData.Select(d => new DataPoint
            {
                Timestamp = d.MeasuredAt,
                Value = d.HeartRate.BeatsPerMinute
            }).ToList(),
            
            TemperatureTrend = historicalData.Select(d => new DataPoint
            {
                Timestamp = d.MeasuredAt,
                Value = d.Temperature.Value
            }).ToList()
        };
    }

    private async Task<MedicalInterpretation> GenerateMedicalInterpretationAsync(
        VitalSignsField field, PatientPart patient)
    {
        var interpretation = new MedicalInterpretation();
        
        // Blood pressure interpretation
        interpretation.BloodPressureInterpretation = field.BloodPressure.Category switch
        {
            BloodPressureCategory.Low => "Blood pressure is below normal range. Monitor for hypotension symptoms.",
            BloodPressureCategory.Normal => "Blood pressure is within normal range.",
            BloodPressureCategory.Elevated => "Blood pressure is elevated. Lifestyle modifications recommended.",
            BloodPressureCategory.Stage1Hypertension => "Stage 1 hypertension detected. Medical evaluation recommended.",
            BloodPressureCategory.Stage2Hypertension => "Stage 2 hypertension detected. Immediate medical attention required.",
            BloodPressureCategory.HypertensiveCrisis => "CRITICAL: Hypertensive crisis. Emergency medical intervention required.",
            _ => "Unable to interpret blood pressure reading."
        };

        // Heart rate interpretation
        var heartRateCategory = CategorizeHeartRate(field.HeartRate.BeatsPerMinute, patient.Age);
        interpretation.HeartRateInterpretation = heartRateCategory switch
        {
            HeartRateCategory.Bradycardia => "Heart rate is below normal range (bradycardia). Monitor for symptoms.",
            HeartRateCategory.Normal => "Heart rate is within normal range.",
            HeartRateCategory.Tachycardia => "Heart rate is above normal range (tachycardia). Evaluate underlying causes.",
            _ => "Unable to interpret heart rate reading."
        };

        // Temperature interpretation
        var tempInCelsius = field.Temperature.Unit == TemperatureUnit.Fahrenheit 
            ? (field.Temperature.Value - 32) * 5 / 9 
            : field.Temperature.Value;
            
        interpretation.TemperatureInterpretation = tempInCelsius switch
        {
            < 35.0 => "Hypothermia detected. Immediate warming measures required.",
            >= 35.0 and < 36.1 => "Body temperature is below normal range.",
            >= 36.1 and <= 37.2 => "Body temperature is within normal range.",
            > 37.2 and < 38.0 => "Mild fever detected. Monitor and consider antipyretics.",
            >= 38.0 and < 39.0 => "Moderate fever detected. Medical evaluation recommended.",
            >= 39.0 => "High fever detected. Immediate medical attention required.",
            _ => "Unable to interpret temperature reading."
        };

        return interpretation;
    }
}

// Medical Device Integration Service
public class MedicalDeviceIntegrationService : IMedicalDeviceService
{
    private readonly ILogger<MedicalDeviceIntegrationService> _logger;
    private readonly IDeviceConnectionManager _connectionManager;

    public async Task<VitalSignsData> ImportVitalSignsAsync(string deviceId)
    {
        try
        {
            var device = await GetDeviceAsync(deviceId);
            if (device == null || !device.IsConnected)
            {
                throw new InvalidOperationException($"Device {deviceId} is not available");
            }

            _logger.LogInformation("Importing vital signs from device: {DeviceId}", deviceId);

            // Connect to device based on type
            var vitalSigns = device.Type switch
            {
                DeviceType.BloodPressureMonitor => await ImportFromBloodPressureMonitorAsync(device),
                DeviceType.PulseOximeter => await ImportFromPulseOximeterAsync(device),
                DeviceType.Thermometer => await ImportFromThermometerAsync(device),
                DeviceType.ECGMonitor => await ImportFromECGMonitorAsync(device),
                DeviceType.MultiParameterMonitor => await ImportFromMultiParameterMonitorAsync(device),
                _ => throw new NotSupportedException($"Device type {device.Type} is not supported")
            };

            _logger.LogInformation("Successfully imported vital signs from device: {DeviceId}", deviceId);
            return vitalSigns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import vital signs from device: {DeviceId}", deviceId);
            throw;
        }
    }

    private async Task<VitalSignsData> ImportFromMultiParameterMonitorAsync(MedicalDevice device)
    {
        // Connect to multi-parameter monitor (e.g., Philips IntelliVue, GE Carescape)
        var connection = await _connectionManager.ConnectAsync(device);
        
        try
        {
            // Request current vital signs data
            var data = await connection.RequestVitalSignsAsync();
            
            return new VitalSignsData
            {
                BloodPressure = new BloodPressure
                {
                    Systolic = data.BloodPressure.Systolic,
                    Diastolic = data.BloodPressure.Diastolic,
                    Unit = BloodPressureUnit.MmHg
                },
                HeartRate = new HeartRate
                {
                    BeatsPerMinute = data.HeartRate,
                    Rhythm = data.HeartRhythm
                },
                Temperature = new BodyTemperature
                {
                    Value = data.Temperature,
                    Unit = TemperatureUnit.Celsius,
                    Site = data.TemperatureSite
                },
                RespiratoryRate = new RespiratoryRate
                {
                    BreathsPerMinute = data.RespiratoryRate
                },
                OxygenSaturation = new OxygenSaturation
                {
                    Percentage = data.SpO2,
                    PlethysmographIndex = data.PI
                },
                DeviceMetadata = new Dictionary<string, object>
                {
                    ["DeviceModel"] = device.Model,
                    ["SerialNumber"] = device.SerialNumber,
                    ["CalibrationDate"] = device.LastCalibration,
                    ["DataQuality"] = data.SignalQuality,
                    ["AlarmLimits"] = data.AlarmLimits
                }
            };
        }
        finally
        {
            await connection.DisconnectAsync();
        }
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Advanced Patterns** | **Sau Advanced Patterns** |
|------------------------------|----------------------------|
| ❌ Plain text fields - không validate medical data | ✅ Structured medical fields với comprehensive validation |
| ❌ Không convert units | ✅ Automatic unit conversion (Celsius/Fahrenheit, mmHg/kPa) |
| ❌ Không check medical ranges | ✅ Age-specific reference ranges và medical interpretation |
| ❌ Không integrate medical devices | ✅ Real-time device integration với multiple protocols |
| ❌ Không có medical alerts | ✅ Intelligent medical alerts và critical value notifications |
| ❌ Không có audit trail | ✅ Complete medical audit trail với compliance tracking |

---

### **2. 🏗️ E-learning Platform - Dynamic Course Builder**

#### **Tình huống:**
Anh phát triển **nền tảng e-learning** cần tạo course structures động based on learning paths, student progress, và adaptive content delivery.

#### **❌ TRƯỚC KHI DÙNG ADVANCED PATTERNS:**
```csharp
// Static course structure - không flexible
public class Course : ContentPart
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string[] Modules { get; set; } // Fixed module list
    public string[] Prerequisites { get; set; } // Static prerequisites
    public int Duration { get; set; } // Fixed duration
}

// Manual course creation - time consuming
public async Task CreateCourseAsync()
{
    // Phải manually tạo từng module, lesson, quiz
    var course = new Course
    {
        Title = "JavaScript Fundamentals",
        Modules = new[] { "Variables", "Functions", "Objects", "Arrays" }
    };
    
    // Phải manually tạo content cho từng module
    // Không có adaptive learning
    // Không có personalization
    // Không có progress tracking
}

// Vấn đề:
// - Static course structure không adapt được theo student level
// - Không có personalized learning paths
// - Không có dynamic content generation
// - Không có adaptive assessment
// - Manual course creation process
// - Không có learning analytics integration
```

#### **✅ SAU KHI DÙNG ADVANCED PATTERNS:**
```csharp
// Dynamic Course Builder với AI-powered content generation
public class AdaptiveCourseBuilder : IDynamicContentTypeBuilder
{
    private readonly ILearningAnalyticsService _analyticsService;
    private readonly IContentGenerationService _contentGenerator;
    private readonly IPersonalizationEngine _personalizationEngine;

    public async Task<ContentTypeDefinition> BuildAdaptiveCourseAsync(AdaptiveCourseRequest request)
    {
        // Analyze student profile và learning preferences
        var studentProfile = await _analyticsService.GetStudentProfileAsync(request.StudentId);
        var learningPath = await _personalizationEngine.GenerateLearningPathAsync(studentProfile, request.Subject);

        // Generate dynamic course structure
        var courseBuilder = new ContentTypeDefinitionBuilder()
            .Named($"AdaptiveCourse_{request.Subject}_{request.StudentId}")
            .DisplayedAs($"Adaptive {request.Subject} Course")
            .WithDescription($"Personalized {request.Subject} course for {studentProfile.Name}");

        // Add adaptive modules based on student level
        foreach (var module in learningPath.Modules)
        {
            await AddAdaptiveModuleAsync(courseBuilder, module, studentProfile);
        }

        // Add assessment components
        await AddAdaptiveAssessmentsAsync(courseBuilder, learningPath, studentProfile);

        // Add progress tracking
        await AddProgressTrackingAsync(courseBuilder, studentProfile);

        var courseDefinition = courseBuilder.Build();
        await _contentDefinitionManager.StoreTypeDefinitionAsync(courseDefinition);

        return courseDefinition;
    }

    private async Task AddAdaptiveModuleAsync(
        ContentTypeDefinitionBuilder courseBuilder, 
        LearningModule module, 
        StudentProfile studentProfile)
    {
        var moduleBuilder = courseBuilder.WithPart($"{module.Name}Module", module.DisplayName, part =>
        {
            // Add content based on learning style
            if (studentProfile.LearningStyle.HasFlag(LearningStyle.Visual))
            {
                part.WithField("VideoContent", field => field
                    .OfType("VideoField")
                    .WithDisplayName("Video Lessons")
                    .WithSetting("VideoQuality", studentProfile.PreferredVideoQuality)
                    .WithSetting("SubtitlesEnabled", studentProfile.RequiresSubtitles.ToString()));

                part.WithField("InfographicContent", field => field
                    .OfType("MediaField")
                    .WithDisplayName("Visual Aids")
                    .WithSetting("AllowedExtensions", "png,jpg,svg,gif"));
            }

            if (studentProfile.LearningStyle.HasFlag(LearningStyle.Auditory))
            {
                part.WithField("AudioContent", field => field
                    .OfType("AudioField")
                    .WithDisplayName("Audio Lessons")
                    .WithSetting("AudioQuality", "high")
                    .WithSetting("PlaybackSpeed", studentProfile.PreferredPlaybackSpeed.ToString()));

                part.WithField("PodcastContent", field => field
                    .OfType("MediaField")
                    .WithDisplayName("Podcast Episodes"));
            }

            if (studentProfile.LearningStyle.HasFlag(LearningStyle.Kinesthetic))
            {
                part.WithField("InteractiveExercises", field => field
                    .OfType("InteractiveContentField")
                    .WithDisplayName("Hands-on Exercises")
                    .WithSetting("ExerciseType", "coding")
                    .WithSetting("DifficultyLevel", studentProfile.CurrentLevel.ToString()));

                part.WithField("SimulationContent", field => field
                    .OfType("SimulationField")
                    .WithDisplayName("Interactive Simulations"));
            }

            // Add adaptive difficulty content
            var difficultyLevel = CalculateModuleDifficulty(module, studentProfile);
            part.WithField("AdaptiveContent", field => field
                .OfType("AdaptiveContentField")
                .WithDisplayName("Adaptive Learning Content")
                .WithSetting("DifficultyLevel", difficultyLevel.ToString())
                .WithSetting("AdaptationStrategy", studentProfile.AdaptationStrategy));

            // Add prerequisite checking
            if (module.Prerequisites.Any())
            {
                part.WithField("PrerequisiteCheck", field => field
                    .OfType("PrerequisiteField")
                    .WithDisplayName("Prerequisites")
                    .WithSetting("RequiredModules", string.Join(",", module.Prerequisites))
                    .WithSetting("MinimumScore", module.MinimumPrerequisiteScore.ToString()));
            }

            return part;
        });
    }

    private async Task AddAdaptiveAssessmentsAsync(
        ContentTypeDefinitionBuilder courseBuilder,
        LearningPath learningPath,
        StudentProfile studentProfile)
    {
        courseBuilder.WithPart("AdaptiveAssessment", "Adaptive Assessment", part =>
        {
            // Question bank field với adaptive selection
            part.WithField("QuestionBank", field => field
                .OfType("AdaptiveQuestionBankField")
                .WithDisplayName("Adaptive Question Bank")
                .WithSetting("TotalQuestions", learningPath.AssessmentQuestionCount.ToString())
                .WithSetting("DifficultyDistribution", JsonSerializer.Serialize(learningPath.DifficultyDistribution))
                .WithSetting("AdaptiveAlgorithm", studentProfile.AssessmentStyle.ToString()));

            // Real-time difficulty adjustment
            part.WithField("DifficultyAdjustment", field => field
                .OfType("DifficultyAdjustmentField")
                .WithDisplayName("Dynamic Difficulty")
                .WithSetting("InitialDifficulty", studentProfile.CurrentLevel.ToString())
                .WithSetting("AdjustmentStrategy", "IRT") // Item Response Theory
                .WithSetting("MinimumQuestions", "5")
                .WithSetting("MaximumQuestions", "20"));

            // Performance analytics
            part.WithField("PerformanceTracking", field => field
                .OfType("PerformanceTrackingField")
                .WithDisplayName("Performance Analytics")
                .WithSetting("TrackResponseTime", "true")
                .WithSetting("TrackAttemptPatterns", "true")
                .WithSetting("GenerateInsights", "true"));

            return part;
        });
    }

    private async Task AddProgressTrackingAsync(
        ContentTypeDefinitionBuilder courseBuilder,
        StudentProfile studentProfile)
    {
        courseBuilder.WithPart("ProgressTracking", "Learning Progress", part =>
        {
            // Completion tracking
            part.WithField("CompletionStatus", field => field
                .OfType("CompletionTrackingField")
                .WithDisplayName("Module Completion")
                .WithSetting("TrackingGranularity", "lesson")
                .WithSetting("RequiredCompletionPercentage", "80"));

            // Time tracking
            part.WithField("TimeTracking", field => field
                .OfType("TimeTrackingField")
                .WithDisplayName("Learning Time")
                .WithSetting("TrackActiveTime", "true")
                .WithSetting("IdleTimeThreshold", "300") // 5 minutes
                .WithSetting("GenerateTimeReports", "true"));

            // Engagement metrics
            part.WithField("EngagementMetrics", field => field
                .OfType("EngagementTrackingField")
                .WithDisplayName("Engagement Analytics")
                .WithSetting("TrackInteractions", "true")
                .WithSetting("TrackScrollDepth", "true")
                .WithSetting("TrackVideoWatchTime", "true"));

            // Learning path adaptation
            part.WithField("PathAdaptation", field => field
                .OfType("PathAdaptationField")
                .WithDisplayName("Adaptive Path")
                .WithSetting("AdaptationTriggers", "performance,engagement,time")
                .WithSetting("RecommendationEngine", "collaborative_filtering")
                .WithSetting("PersonalizationLevel", studentProfile.PersonalizationPreference.ToString()));

            return part;
        });
    }
}

// Advanced Recipe for Course Template Generation
public class CourseTemplateRecipeStep : IRecipeStepHandler
{
    public string Name => "course-template";

    private readonly IAdaptiveCourseBuilder _courseBuilder;
    private readonly ILearningContentService _contentService;

    public async Task ExecuteAsync(RecipeStepContext context)
    {
        var model = context.Step.Data.ToObject<CourseTemplateRecipeModel>();
        
        foreach (var template in model.CourseTemplates)
        {
            // Generate course for different student profiles
            foreach (var profileType in template.TargetProfiles)
            {
                var courseRequest = new AdaptiveCourseRequest
                {
                    Subject = template.Subject,
                    StudentProfile = profileType,
                    LearningObjectives = template.LearningObjectives,
                    Duration = template.EstimatedDuration,
                    DifficultyLevel = template.DifficultyLevel
                };

                // Build adaptive course
                var courseDefinition = await _courseBuilder.BuildAdaptiveCourseAsync(courseRequest);
                
                // Generate content using AI
                if (template.AutoGenerateContent)
                {
                    await GenerateCourseContentAsync(courseDefinition, template);
                }

                // Create course instances for different languages
                if (template.MultiLanguageSupport)
                {
                    foreach (var language in template.SupportedLanguages)
                    {
                        await CreateLocalizedCourseAsync(courseDefinition, language, template);
                    }
                }
            }
        }
    }

    private async Task GenerateCourseContentAsync(ContentTypeDefinition courseDefinition, CourseTemplate template)
    {
        // AI-powered content generation
        var contentGenerator = new AIContentGenerator();
        
        foreach (var part in courseDefinition.Parts)
        {
            foreach (var field in part.PartDefinition.Fields)
            {
                switch (field.FieldDefinition.Name)
                {
                    case "VideoContent":
                        await GenerateVideoScriptsAsync(contentGenerator, field, template);
                        break;
                    
                    case "InteractiveExercises":
                        await GenerateCodeExercisesAsync(contentGenerator, field, template);
                        break;
                    
                    case "QuestionBank":
                        await GenerateAssessmentQuestionsAsync(contentGenerator, field, template);
                        break;
                    
                    case "AdaptiveContent":
                        await GenerateAdaptiveContentVariationsAsync(contentGenerator, field, template);
                        break;
                }
            }
        }
    }

    private async Task GenerateCodeExercisesAsync(
        AIContentGenerator generator, 
        ContentPartFieldDefinition field, 
        CourseTemplate template)
    {
        var exercises = await generator.GenerateCodeExercisesAsync(new CodeExerciseRequest
        {
            Subject = template.Subject,
            DifficultyLevel = template.DifficultyLevel,
            ProgrammingLanguage = template.PrimaryLanguage,
            ExerciseCount = template.ExercisesPerModule,
            IncludeUnitTests = true,
            IncludeSolution = true,
            IncludeHints = true
        });

        // Store generated exercises
        foreach (var exercise in exercises)
        {
            await _contentService.CreateExerciseContentAsync(exercise, field);
        }
    }
}

// Plugin for Learning Analytics Integration
[Plugin(
    Id = "LearningAnalyticsIntegration",
    Name = "Learning Analytics Integration",
    Version = "2.0.0",
    Description = "Integrates with learning analytics platforms for advanced insights",
    Dependencies = new[] { "OrchardCore.ContentManagement", "OrchardCore.Workflows" }
)]
public class LearningAnalyticsPlugin : IPlugin, IContentHandler
{
    private readonly ILearningAnalyticsService _analyticsService;
    private readonly IPersonalizationEngine _personalizationEngine;

    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        _analyticsService = serviceProvider.GetRequiredService<ILearningAnalyticsService>();
        _personalizationEngine = serviceProvider.GetRequiredService<IPersonalizationEngine>();
    }

    public async Task PublishedAsync(PublishContentContext context)
    {
        // Track content interaction
        if (context.ContentItem.ContentType.StartsWith("AdaptiveCourse"))
        {
            await TrackLearningInteractionAsync(context.ContentItem);
        }
    }

    private async Task TrackLearningInteractionAsync(ContentItem courseContent)
    {
        var studentId = GetCurrentStudentId();
        var interactionData = new LearningInteraction
        {
            StudentId = studentId,
            ContentId = courseContent.ContentItemId,
            ContentType = courseContent.ContentType,
            InteractionType = "content_access",
            Timestamp = DateTime.UtcNow,
            SessionId = GetCurrentSessionId(),
            Metadata = ExtractContentMetadata(courseContent)
        };

        await _analyticsService.TrackInteractionAsync(interactionData);

        // Trigger personalization update
        await _personalizationEngine.UpdateStudentProfileAsync(studentId, interactionData);
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Advanced Patterns** | **Sau Advanced Patterns** |
|------------------------------|----------------------------|
| ❌ Static course structure | ✅ Dynamic adaptive course generation based on student profile |
| ❌ One-size-fits-all content | ✅ Personalized content cho visual/auditory/kinesthetic learners |
| ❌ Manual course creation | ✅ AI-powered automated content generation |
| ❌ Fixed assessments | ✅ Adaptive assessments với Item Response Theory |
| ❌ No learning analytics | ✅ Real-time learning analytics và progress tracking |
| ❌ No personalization | ✅ Advanced personalization engine với ML recommendations |

---

### **3. 🏪 Multi-vendor Marketplace - Complex Recipe Orchestration**

#### **Tình huống:**
Anh xây dựng **marketplace platform** cần setup complex vendor onboarding process với conditional workflows, parallel processing, và integration với multiple external services.

#### **❌ TRƯỚC KHI DÙNG ADVANCED PATTERNS:**
```json
// Simple linear recipe - không flexible
{
  "name": "VendorOnboarding",
  "steps": [
    {
      "name": "feature",
      "data": {
        "enable": ["OrchardCore.Commerce", "OrchardCore.Users"]
      }
    },
    {
      "name": "content",
      "data": [
        {
          "ContentType": "Vendor",
          "DisplayText": "New Vendor"
        }
      ]
    }
  ]
}

// Vấn đề:
// - Linear process không handle complex business logic
// - Không có conditional steps based on vendor type
// - Không có parallel processing cho time-consuming tasks
// - Không có error handling và retry logic
// - Không có integration với external services
// - Không có approval workflows
```

#### **✅ SAU KHI DÙNG ADVANCED PATTERNS:**
```json
// Advanced Recipe với Complex Orchestration
{
  "name": "AdvancedVendorOnboarding",
  "description": "Complex vendor onboarding với conditional logic và parallel processing",
  "steps": [
    {
      "name": "conditional",
      "data": {
        "conditions": [
          {
            "name": "EnterpriseVendor",
            "type": "custom",
            "value": "VendorTypeEvaluator",
            "operator": "equals",
            "expectedValue": "Enterprise",
            "breakOnMatch": false,
            "steps": [
              {
                "name": "parallel",
                "data": {
                  "maxDegreeOfParallelism": 3,
                  "waitForAll": true,
                  "steps": [
                    {
                      "name": "enterprise-verification",
                      "data": {
                        "verificationType": "business_registration",
                        "requiredDocuments": ["business_license", "tax_certificate", "bank_statement"],
                        "verificationService": "DunsAndBradstreet",
                        "timeout": 300
                      }
                    },
                    {
                      "name": "credit-check",
                      "data": {
                        "creditService": "Experian",
                        "minimumScore": 650,
                        "reportType": "comprehensive"
                      }
                    },
                    {
                      "name": "compliance-check",
                      "data": {
                        "complianceServices": ["AML", "KYC", "GDPR"],
                        "jurisdictions": ["US", "EU", "UK"]
                      }
                    }
                  ]
                }
              },
              {
                "name": "enterprise-setup",
                "data": {
                  "contentType": "EnterpriseVendor",
                  "features": ["AdvancedAnalytics", "BulkOperations", "APIAccess"],
                  "limits": {
                    "products": 10000,
                    "orders": "unlimited",
                    "storage": "1TB"
                  }
                }
              }
            ]
          },
          {
            "name": "SMBVendor",
            "type": "custom", 
            "value": "VendorTypeEvaluator",
            "operator": "equals",
            "expectedValue": "SMB",
            "breakOnMatch": false,
            "steps": [
              {
                "name": "loop",
                "data": {
                  "source": {
                    "type": "array",
                    "items": ["basic_verification", "payment_setup", "product_catalog"]
                  },
                  "delayBetweenIterations": 5000,
                  "steps": [
                    {
                      "name": "smb-onboarding-step",
                      "data": {
                        "stepType": "{{ LoopItem }}",
                        "automated": true,
                        "notificationRequired": true
                      }
                    }
                  ]
                }
              }
            ]
          },
          {
            "name": "IndividualSeller",
            "type": "custom",
            "value": "VendorTypeEvaluator", 
            "operator": "equals",
            "expectedValue": "Individual",
            "breakOnMatch": false,
            "steps": [
              {
                "name": "individual-verification",
                "data": {
                  "verificationType": "identity_only",
                  "requiredDocuments": ["government_id", "address_proof"],
                  "autoApproval": true,
                  "limits": {
                    "products": 100,
                    "monthlyRevenue": 5000
                  }
                }
              }
            ]
          }
        ],
        "defaultSteps": [
          {
            "name": "manual-review",
            "data": {
              "reviewType": "comprehensive",
              "assignTo": "vendor-review-team",
              "priority": "high"
            }
          }
        ]
      }
    },
    {
      "name": "parallel",
      "data": {
        "maxDegreeOfParallelism": 5,
        "waitForAll": false,
        "steps": [
          {
            "name": "payment-integration",
            "data": {
              "paymentProviders": ["Stripe", "PayPal", "Square"],
              "setupWebhooks": true,
              "testTransactions": true
            }
          },
          {
            "name": "shipping-integration", 
            "data": {
              "shippingProviders": ["FedEx", "UPS", "DHL"],
              "rateCalculation": true,
              "trackingIntegration": true
            }
          },
          {
            "name": "tax-calculation-setup",
            "data": {
              "taxService": "Avalara",
              "jurisdictions": "auto-detect",
              "productTaxCodes": true
            }
          },
          {
            "name": "inventory-sync",
            "data": {
              "inventorySystem": "auto-detect",
              "syncFrequency": "real-time",
              "lowStockAlerts": true
            }
          },
          {
            "name": "analytics-setup",
            "data": {
              "analyticsProvider": "GoogleAnalytics",
              "ecommerceTracking": true,
              "customDashboard": true
            }
          }
        ]
      }
    },
    {
      "name": "conditional",
      "data": {
        "conditions": [
          {
            "name": "RequiresApproval",
            "type": "setting",
            "value": "VendorSettings.RequireManualApproval",
            "operator": "equals",
            "expectedValue": true,
            "steps": [
              {
                "name": "approval-workflow",
                "data": {
                  "workflowType": "VendorApproval",
                  "approvers": ["vendor-manager", "compliance-officer"],
                  "requiredApprovals": 2,
                  "timeoutDays": 5,
                  "escalationRules": [
                    {
                      "condition": "timeout",
                      "action": "escalate_to_director"
                    }
                  ]
                }
              }
            ]
          }
        ],
        "defaultSteps": [
          {
            "name": "auto-approval",
            "data": {
              "approvalType": "automatic",
              "notifyVendor": true,
              "activateAccount": true
            }
          }
        ]
      }
    },
    {
      "name": "post-onboarding",
      "data": {
        "tasks": [
          {
            "name": "welcome-email",
            "template": "VendorWelcome",
            "personalized": true
          },
          {
            "name": "training-enrollment",
            "courses": ["PlatformBasics", "BestPractices", "PolicyCompliance"]
          },
          {
            "name": "support-ticket-creation",
            "type": "onboarding_support",
            "assignTo": "vendor-success-team"
          },
          {
            "name": "performance-monitoring-setup",
            "metrics": ["sales", "customer_satisfaction", "compliance_score"],
            "alertThresholds": {
              "low_sales": 30,
              "poor_rating": 3.0,
              "compliance_issues": 1
            }
          }
        ]
      }
    }
  ]
}
```

```csharp
// Custom Recipe Step Handlers cho Marketplace
public class VendorVerificationRecipeStep : IRecipeStepHandler
{
    public string Name => "vendor-verification";

    private readonly IVendorVerificationService _verificationService;
    private readonly IExternalServiceIntegrator _serviceIntegrator;

    public async Task ExecuteAsync(RecipeStepContext context)
    {
        var model = context.Step.Data.ToObject<VendorVerificationModel>();
        var vendorId = context.Environment["VendorId"]?.ToString();

        try
        {
            // Parallel verification processes
            var verificationTasks = new List<Task<VerificationResult>>();

            if (model.RequiredDocuments?.Any() == true)
            {
                verificationTasks.Add(VerifyDocumentsAsync(vendorId, model.RequiredDocuments));
            }

            if (!string.IsNullOrEmpty(model.VerificationService))
            {
                verificationTasks.Add(VerifyBusinessRegistrationAsync(vendorId, model.VerificationService));
            }

            if (model.CreditCheckRequired)
            {
                verificationTasks.Add(PerformCreditCheckAsync(vendorId, model.CreditService));
            }

            // Wait for all verifications với timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(model.Timeout ?? 300));
            var completedTask = await Task.WhenAny(Task.WhenAll(verificationTasks), timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Vendor verification timed out");
            }

            var results = await Task.WhenAll(verificationTasks);
            
            // Evaluate overall verification result
            var overallResult = EvaluateVerificationResults(results);
            
            // Update vendor status
            await UpdateVendorVerificationStatusAsync(vendorId, overallResult);

            // Trigger next steps based on result
            if (overallResult.IsApproved)
            {
                context.Environment["VerificationPassed"] = true;
                await TriggerApprovedVendorWorkflowAsync(vendorId);
            }
            else
            {
                context.Environment["VerificationPassed"] = false;
                await TriggerRejectedVendorWorkflowAsync(vendorId, overallResult.RejectionReasons);
            }
        }
        catch (Exception ex)
        {
            await HandleVerificationErrorAsync(vendorId, ex);
            throw;
        }
    }

    private async Task<VerificationResult> VerifyBusinessRegistrationAsync(string vendorId, string service)
    {
        switch (service.ToLower())
        {
            case "dunsandbradstreet":
                return await _serviceIntegrator.VerifyWithDunsAndBradstreetAsync(vendorId);
            
            case "opencorporates":
                return await _serviceIntegrator.VerifyWithOpenCorporatesAsync(vendorId);
            
            default:
                throw new NotSupportedException($"Verification service '{service}' is not supported");
        }
    }

    private async Task<VerificationResult> PerformCreditCheckAsync(string vendorId, string creditService)
    {
        var creditResult = await _serviceIntegrator.PerformCreditCheckAsync(vendorId, creditService);
        
        return new VerificationResult
        {
            IsApproved = creditResult.Score >= 650, // Configurable threshold
            Score = creditResult.Score,
            Details = creditResult.Details,
            VerificationType = "CreditCheck"
        };
    }
}

// Marketplace Integration Plugin
[Plugin(
    Id = "MarketplaceIntegration",
    Name = "Advanced Marketplace Integration",
    Version = "3.0.0",
    Description = "Comprehensive marketplace integration với external services",
    Dependencies = new[] { "OrchardCore.Commerce", "OrchardCore.Workflows", "OrchardCore.Recipes" }
)]
public class MarketplaceIntegrationPlugin : IPlugin
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketplaceIntegrationPlugin> _logger;

    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<MarketplaceIntegrationPlugin>>();

        // Register custom recipe steps
        RegisterCustomRecipeSteps();
        
        // Setup external service integrations
        await SetupExternalIntegrationsAsync();
        
        // Initialize monitoring và alerting
        await InitializeMonitoringAsync();

        _logger.LogInformation("Marketplace Integration Plugin initialized successfully");
    }

    private void RegisterCustomRecipeSteps()
    {
        var serviceCollection = new ServiceCollection();
        
        serviceCollection.AddScoped<IRecipeStepHandler, VendorVerificationRecipeStep>();
        serviceCollection.AddScoped<IRecipeStepHandler, PaymentIntegrationRecipeStep>();
        serviceCollection.AddScoped<IRecipeStepHandler, ShippingIntegrationRecipeStep>();
        serviceCollection.AddScoped<IRecipeStepHandler, ComplianceCheckRecipeStep>();
        serviceCollection.AddScoped<IRecipeStepHandler, AnalyticsSetupRecipeStep>();
        
        // Register với DI container
        foreach (var service in serviceCollection)
        {
            _serviceProvider.GetRequiredService<IServiceCollection>().Add(service);
        }
    }

    private async Task SetupExternalIntegrationsAsync()
    {
        var integrationManager = _serviceProvider.GetRequiredService<IExternalServiceIntegrator>();
        
        // Setup payment provider integrations
        await integrationManager.RegisterPaymentProviderAsync("Stripe", new StripeIntegration());
        await integrationManager.RegisterPaymentProviderAsync("PayPal", new PayPalIntegration());
        await integrationManager.RegisterPaymentProviderAsync("Square", new SquareIntegration());
        
        // Setup shipping provider integrations
        await integrationManager.RegisterShippingProviderAsync("FedEx", new FedExIntegration());
        await integrationManager.RegisterShippingProviderAsync("UPS", new UPSIntegration());
        await integrationManager.RegisterShippingProviderAsync("DHL", new DHLIntegration());
        
        // Setup verification service integrations
        await integrationManager.RegisterVerificationServiceAsync("DunsAndBradstreet", new DnBIntegration());
        await integrationManager.RegisterVerificationServiceAsync("Experian", new ExperianIntegration());
        
        _logger.LogInformation("External service integrations configured");
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Advanced Patterns** | **Sau Advanced Patterns** |
|------------------------------|----------------------------|
| ❌ Linear recipe execution | ✅ Complex conditional logic với parallel processing |
| ❌ One-size-fits-all onboarding | ✅ Vendor type-specific onboarding workflows |
| ❌ Manual external integrations | ✅ Automated integration với 15+ external services |
| ❌ No error handling | ✅ Comprehensive error handling với retry logic |
| ❌ No approval workflows | ✅ Multi-level approval workflows với escalation |
| ❌ Static process | ✅ Dynamic process adaptation based on business rules |

---

## 💡 **TÓM TẮT KHI NÀO CẦN ADVANCED PATTERNS**

### **✅ CẦN DÙNG KHI:**

#### **1. 🏥 Specialized Domain Requirements**
- **Ví dụ**: Medical systems, financial platforms, scientific applications
- **Lợi ích**: Domain-specific validation, specialized data types, compliance requirements

#### **2. 🎓 Dynamic Content Generation**
- **Ví dụ**: E-learning platforms, personalized content, adaptive systems
- **Lợi ích**: Runtime content creation, personalization engines, AI-powered generation

#### **3. 🏪 Complex Business Workflows**
- **Ví dụ**: Multi-vendor marketplaces, enterprise onboarding, approval processes
- **Lợi ích**: Conditional logic, parallel processing, external service integration

#### **4. 🔌 Extensible Plugin Architecture**
- **Ví dụ**: Multi-tenant SaaS, customizable platforms, third-party integrations
- **Lợi ích**: Runtime plugin loading, dependency management, isolated execution

#### **5. 🤖 AI/ML Integration Requirements**
- **Ví dụ**: Recommendation engines, predictive analytics, automated content generation
- **Lợi ích**: Model integration, real-time inference, adaptive algorithms

### **❌ KHÔNG CẦN DÙNG KHI:**

#### **1. 📄 Simple Content Websites**
- **Ví dụ**: Corporate websites, blogs, portfolios
- **Lý do**: Standard OrchardCore fields đủ rồi

#### **2. 🏪 Basic E-commerce**
- **Ví dụ**: Simple online stores, single-vendor shops
- **Lý do**: OrchardCore.Commerce module đủ functionality

#### **3. 💰 Limited Development Resources**
- **Ví dụ**: Small projects, tight budgets, short timelines
- **Lý do**: Advanced patterns require significant development investment

#### **4. 🎯 Prototype/MVP Projects**
- **Ví dụ**: Proof of concepts, early-stage startups
- **Lý do**: Focus on core functionality first, optimize later

### **🎯 KẾT LUẬN:**
**Advanced Patterns phù hợp nhất cho specialized domains, dynamic content generation, complex workflows, và extensible architectures requiring beyond-standard OrchardCore capabilities!**

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*