# 📁 **Media & File Management Patterns trong OrchardCore**

## 🎯 **TỔNG QUAN**

**Media & File Management** trong OrchardCore cung cấp hệ thống quản lý files toàn diện với khả năng:
- **File Upload & Storage**: Local, cloud storage (AWS S3, Azure Blob)
- **Image Processing**: Resize, crop, format conversion với ImageSharp
- **Security**: Token-based access, secure media handling
- **Performance**: Chunked upload, caching, background processing
- **Extensibility**: Custom providers, media profiles

---

## 🏗️ **KIẾN TRÚC CORE COMPONENTS**

### **1. 📂 IMediaFileProvider - File Storage Abstraction**

```csharp
// Core interface cho media file storage
public interface IMediaFileProvider : IFileProvider
{
    PathString VirtualPathBase { get; }
}

// Implementation cho local storage
public class MediaFileProvider : PhysicalFileProvider, IMediaFileProvider
{
    public MediaFileProvider(PathString virtualPathBase, string root) : base(root)
    {
        VirtualPathBase = virtualPathBase;
    }

    public PathString VirtualPathBase { get; }
}
```

### **2. 🔐 MediaTokenService - Security & URL Protection**

```csharp
// Service bảo vệ media URLs với tokens
public class MediaTokenService : IMediaTokenService
{
    private readonly IMemoryCache _memoryCache;
    private readonly HashSet<string> _knownCommands;
    private readonly byte[] _hashKey;

    public string AddTokenToPath(string path)
    {
        // Parse query parameters
        var pathIndex = path.IndexOf('?');
        if (pathIndex != -1)
        {
            ParseQuery(path[(pathIndex + 1)..], out var processingCommands, out var otherCommands);
            
            // Generate secure token
            var queryStringTokenKey = CreateQueryStringTokenKey(processingCommands);
            var queryStringToken = GetHash(queryStringTokenKey);
            
            processingCommands["token"] = queryStringToken;
            return AddQueryString(path.AsSpan(0, pathIndex), processingCommands);
        }
        
        return path;
    }

    public bool TryValidateToken(
        KeyedCollection<string, KeyValuePair<string, string>> commands, 
        string token)
    {
        var queryStringTokenKey = CreateCommandCollectionTokenKey(commands);
        var queryStringToken = GetHash(queryStringTokenKey);
        
        return string.Equals(queryStringToken, token, StringComparison.OrdinalIgnoreCase);
    }
}
```

### **3. 📤 ChunkFileUploadService - Large File Upload**

```csharp
// Service xử lý upload file lớn theo chunks
public class ChunkFileUploadService : IChunkFileUploadService
{
    private const string UploadIdFormKey = "__chunkedFileUploadId";
    private const string TempFolderPrefix = "ChunkedFileUploads";

    public async Task<IActionResult> ProcessRequestAsync(
        HttpRequest request,
        Func<Guid, IFormFile, ContentRangeHeaderValue, Task<IActionResult>> chunkAsync,
        Func<IEnumerable<IFormFile>, Task<IActionResult>> completedAsync)
    {
        var contentRangeHeader = request.Headers.ContentRange;

        // Check if chunked upload
        if (_options.Value.MaxUploadChunkSize <= 0 ||
            contentRangeHeader.Count is 0 ||
            !request.Form.TryGetValue(UploadIdFormKey, out var uploadIdValue))
        {
            // Regular upload
            return await completedAsync(request.Form.Files);
        }

        // Process chunk
        if (ContentRangeHeaderValue.TryParse(contentRangeHeader, out var contentRange))
        {
            var uploadId = Guid.Parse(uploadIdValue);
            var file = request.Form.Files[0];
            
            return await chunkAsync(uploadId, file, contentRange);
        }

        return new BadRequestResult();
    }
}
```

### **4. 🖼️ Image Processing với ImageSharp**

```csharp
// Media token options cho image processing
public class MediaTokenOptions
{
    public byte[] HashKey { get; set; } = [];
}

// Image processing với secure URLs
public class ImageVersionProcessor : IImageWebProcessor
{
    public const string VersionCommand = "v";
    
    public IEnumerable<string> Commands => [VersionCommand];

    public FormattedImage Process(
        FormattedImage image,
        ILogger logger,
        CommandCollection commands,
        CommandParser parser,
        CultureInfo culture)
    {
        // Version processing logic
        if (commands.Contains(VersionCommand))
        {
            // Apply versioning for cache busting
            return image;
        }

        return image;
    }
}
```

---

## 🛠️ **CUSTOM MEDIA PROVIDER PATTERNS**

### **1. 📦 Custom File Storage Provider**

```csharp
// Custom media provider cho external storage
public class CustomMediaFileProvider : IMediaFileProvider
{
    private readonly IExternalStorageService _storageService;
    private readonly PathString _virtualPathBase;

    public CustomMediaFileProvider(
        IExternalStorageService storageService,
        PathString virtualPathBase)
    {
        _storageService = storageService;
        _virtualPathBase = virtualPathBase;
    }

    public PathString VirtualPathBase => _virtualPathBase;

    public IFileInfo GetFileInfo(string subpath)
    {
        // Get file info from external storage
        var fileInfo = _storageService.GetFileInfoAsync(subpath).Result;
        
        return new CustomFileInfo
        {
            Name = Path.GetFileName(subpath),
            PhysicalPath = null,
            Length = fileInfo.Size,
            LastModified = fileInfo.LastModified,
            IsDirectory = fileInfo.IsDirectory,
            Exists = fileInfo.Exists
        };
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var contents = _storageService.GetDirectoryContentsAsync(subpath).Result;
        
        return new CustomDirectoryContents(contents);
    }

    public IChangeToken Watch(string filter)
    {
        // Implement change tracking if needed
        return NullChangeToken.Singleton;
    }
}

// Custom file info implementation
public class CustomFileInfo : IFileInfo
{
    public bool Exists { get; set; }
    public long Length { get; set; }
    public string PhysicalPath { get; set; }
    public string Name { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public bool IsDirectory { get; set; }

    public Stream CreateReadStream()
    {
        // Return stream from external storage
        return _storageService.OpenReadStreamAsync(PhysicalPath).Result;
    }
}
```

### **2. ☁️ Cloud Storage Integration Pattern**

```csharp
// AWS S3 Media Provider
public class S3MediaFileProvider : IMediaFileProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly PathString _virtualPathBase;

    public S3MediaFileProvider(
        IAmazonS3 s3Client,
        string bucketName,
        PathString virtualPathBase)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
        _virtualPathBase = virtualPathBase;
    }

    public PathString VirtualPathBase => _virtualPathBase;

    public IFileInfo GetFileInfo(string subpath)
    {
        try
        {
            var key = NormalizePath(subpath);
            var response = _s3Client.GetObjectMetadataAsync(_bucketName, key).Result;
            
            return new S3FileInfo
            {
                Name = Path.GetFileName(subpath),
                Length = response.ContentLength,
                LastModified = response.LastModified,
                Exists = true,
                IsDirectory = false,
                S3Key = key
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundFileInfo(subpath);
        }
    }

    private string NormalizePath(string path)
    {
        return path.TrimStart('/').Replace('\\', '/');
    }
}

// S3 File Info
public class S3FileInfo : IFileInfo
{
    public string S3Key { get; set; }
    public bool Exists { get; set; }
    public long Length { get; set; }
    public string PhysicalPath => null;
    public string Name { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public bool IsDirectory { get; set; }

    public Stream CreateReadStream()
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = S3Key
        };
        
        var response = _s3Client.GetObjectAsync(request).Result;
        return response.ResponseStream;
    }
}
```

### **3. 🔄 Media Processing Pipeline**

```csharp
// Custom image processor
public class WatermarkProcessor : IImageWebProcessor
{
    public IEnumerable<string> Commands => ["watermark"];

    public FormattedImage Process(
        FormattedImage image,
        ILogger logger,
        CommandCollection commands,
        CommandParser parser,
        CultureInfo culture)
    {
        if (commands.Contains("watermark"))
        {
            using var watermarkImage = Image.Load("watermark.png");
            
            image.Image.Mutate(ctx =>
            {
                var position = new Point(
                    image.Image.Width - watermarkImage.Width - 10,
                    image.Image.Height - watermarkImage.Height - 10
                );
                
                ctx.DrawImage(watermarkImage, position, 0.7f);
            });
        }

        return image;
    }
}

// Media profile service
public class CustomMediaProfileService : IMediaProfileService
{
    private readonly IMediaProfilesManager _profilesManager;

    public async Task<string> GetMediaProfileUrlAsync(
        string path,
        string profileName,
        string token = null)
    {
        var profiles = await _profilesManager.GetMediaProfilesDocumentAsync();
        
        if (profiles.MediaProfiles.TryGetValue(profileName, out var profile))
        {
            var queryString = BuildQueryString(profile);
            var url = $"{path}?{queryString}";
            
            if (!string.IsNullOrEmpty(token))
            {
                url += $"&token={token}";
            }
            
            return url;
        }
        
        return path;
    }

    private string BuildQueryString(MediaProfile profile)
    {
        var parameters = new List<string>();
        
        if (profile.Width > 0)
            parameters.Add($"width={profile.Width}");
            
        if (profile.Height > 0)
            parameters.Add($"height={profile.Height}");
            
        if (!string.IsNullOrEmpty(profile.Mode))
            parameters.Add($"rmode={profile.Mode}");
            
        return string.Join("&", parameters);
    }
}
```

---

## 🔒 **SECURITY PATTERNS**

### **1. 🛡️ Secure Media Middleware**

```csharp
// Middleware bảo vệ media files
public class SecureMediaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMediaFileStore _mediaFileStore;
    private readonly IAuthorizationService _authorizationService;

    public SecureMediaMiddleware(
        RequestDelegate next,
        IMediaFileStore mediaFileStore,
        IAuthorizationService authorizationService)
    {
        _next = next;
        _mediaFileStore = mediaFileStore;
        _authorizationService = authorizationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        
        // Check if this is a media request
        if (IsMediaRequest(path))
        {
            var fileInfo = await _mediaFileStore.GetFileInfoAsync(path);
            
            if (fileInfo != null && IsSecureFile(fileInfo))
            {
                // Check authorization
                var authResult = await _authorizationService.AuthorizeAsync(
                    context.User, 
                    fileInfo, 
                    "ViewSecureMedia");
                
                if (!authResult.Succeeded)
                {
                    context.Response.StatusCode = 403;
                    return;
                }
            }
        }
        
        await _next(context);
    }

    private bool IsMediaRequest(string path)
    {
        return path.StartsWith("/media/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSecureFile(IFileStoreEntry fileInfo)
    {
        // Check if file is in secure folder or has secure metadata
        return fileInfo.Path.Contains("/secure/") || 
               fileInfo.Path.Contains("/private/");
    }
}
```

### **2. 🔐 File Access Authorization**

```csharp
// Authorization handler cho media files
public class MediaFileAuthorizationHandler : 
    AuthorizationHandler<ViewMediaRequirement, IFileStoreEntry>
{
    private readonly IContentManager _contentManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ViewMediaRequirement requirement,
        IFileStoreEntry resource)
    {
        var user = context.User;
        var filePath = resource.Path;

        // Check if user owns the file
        if (await IsFileOwner(user, filePath))
        {
            context.Succeed(requirement);
            return;
        }

        // Check if file is associated with content user can view
        var contentItems = await GetAssociatedContent(filePath);
        foreach (var contentItem in contentItems)
        {
            var authResult = await _authorizationService.AuthorizeAsync(
                user, contentItem, CommonPermissions.ViewContent);
                
            if (authResult.Succeeded)
            {
                context.Succeed(requirement);
                return;
            }
        }

        // Check role-based access
        if (user.IsInRole("MediaManager") || user.IsInRole("Administrator"))
        {
            context.Succeed(requirement);
        }
    }

    private async Task<bool> IsFileOwner(ClaimsPrincipal user, string filePath)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return false;

        // Check if file is in user's folder
        return filePath.Contains($"/users/{userId}/");
    }
}
```

---

## 📤 **FILE UPLOAD PATTERNS**

### **1. 🚀 Advanced Upload Handler**

```csharp
// Custom upload handler với validation
public class CustomFileUploadHandler : IFileUploadHandler
{
    private readonly IMediaFileStore _mediaFileStore;
    private readonly ILogger<CustomFileUploadHandler> _logger;
    private readonly MediaOptions _mediaOptions;

    public async Task<FileUploadResult> HandleUploadAsync(
        IFormFile file,
        string targetPath,
        FileUploadOptions options = null)
    {
        // Validate file
        var validationResult = await ValidateFileAsync(file, options);
        if (!validationResult.IsValid)
        {
            return FileUploadResult.Failed(validationResult.Errors);
        }

        // Generate unique filename
        var fileName = await GenerateUniqueFileNameAsync(file.FileName, targetPath);
        var fullPath = _mediaFileStore.Combine(targetPath, fileName);

        try
        {
            // Process file before saving
            using var processedStream = await ProcessFileAsync(file, options);
            
            // Save to media store
            var savedPath = await _mediaFileStore.CreateFileFromStreamAsync(
                fullPath, processedStream);

            // Create thumbnail if image
            if (IsImageFile(file))
            {
                await CreateThumbnailAsync(savedPath, options?.ThumbnailSize);
            }

            // Log upload
            _logger.LogInformation("File uploaded: {FilePath} by {User}", 
                savedPath, GetCurrentUser());

            return FileUploadResult.Success(savedPath, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", file.FileName);
            return FileUploadResult.Failed($"Upload failed: {ex.Message}");
        }
    }

    private async Task<FileValidationResult> ValidateFileAsync(
        IFormFile file, 
        FileUploadOptions options)
    {
        var errors = new List<string>();

        // Size validation
        if (file.Length > _mediaOptions.MaxFileSize)
        {
            errors.Add($"File size exceeds limit of {_mediaOptions.MaxFileSize} bytes");
        }

        // Extension validation
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_mediaOptions.AllowedFileExtensions.Contains(extension))
        {
            errors.Add($"File extension '{extension}' is not allowed");
        }

        // Content type validation
        if (!IsValidContentType(file.ContentType, extension))
        {
            errors.Add("File content type does not match extension");
        }

        // Virus scan (if enabled)
        if (options?.ScanForVirus == true)
        {
            var scanResult = await ScanFileForVirusAsync(file);
            if (!scanResult.IsClean)
            {
                errors.Add("File failed virus scan");
            }
        }

        return new FileValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private async Task<Stream> ProcessFileAsync(IFormFile file, FileUploadOptions options)
    {
        var stream = file.OpenReadStream();

        // Image processing
        if (IsImageFile(file) && options?.ProcessImages == true)
        {
            return await ProcessImageAsync(stream, options);
        }

        // Document processing
        if (IsDocumentFile(file) && options?.ProcessDocuments == true)
        {
            return await ProcessDocumentAsync(stream, options);
        }

        return stream;
    }
}

// Upload options
public class FileUploadOptions
{
    public long? MaxFileSize { get; set; }
    public string[] AllowedExtensions { get; set; }
    public bool ProcessImages { get; set; } = true;
    public bool ProcessDocuments { get; set; } = false;
    public bool ScanForVirus { get; set; } = false;
    public Size? ThumbnailSize { get; set; }
    public ImageFormat? ConvertToFormat { get; set; }
    public int? ImageQuality { get; set; }
}
```

### **2. 📊 Progress Tracking cho Chunked Upload**

```csharp
// Service theo dõi tiến độ upload
public class UploadProgressService : IUploadProgressService
{
    private readonly IMemoryCache _cache;
    private readonly IHubContext<UploadProgressHub> _hubContext;

    public async Task<ChunkUploadResult> ProcessChunkAsync(
        Guid uploadId,
        IFormFile chunk,
        ContentRangeHeaderValue contentRange)
    {
        var uploadInfo = GetOrCreateUploadInfo(uploadId);
        
        // Save chunk to temp file
        var chunkPath = Path.Combine(uploadInfo.TempDirectory, $"chunk_{contentRange.From}");
        using (var fileStream = File.Create(chunkPath))
        {
            await chunk.CopyToAsync(fileStream);
        }

        // Update progress
        uploadInfo.ReceivedChunks.Add(new ChunkInfo
        {
            Start = contentRange.From.Value,
            End = contentRange.To.Value,
            FilePath = chunkPath
        });

        var progress = CalculateProgress(uploadInfo, contentRange.Length.Value);
        
        // Notify clients
        await _hubContext.Clients.Group(uploadId.ToString())
            .SendAsync("UploadProgress", new { uploadId, progress });

        // Check if upload complete
        if (IsUploadComplete(uploadInfo, contentRange.Length.Value))
        {
            var finalPath = await AssembleChunksAsync(uploadInfo);
            CleanupTempFiles(uploadInfo);
            
            return ChunkUploadResult.Completed(finalPath);
        }

        return ChunkUploadResult.InProgress(progress);
    }

    private async Task<string> AssembleChunksAsync(UploadInfo uploadInfo)
    {
        var finalPath = Path.Combine(_mediaOptions.TempDirectory, uploadInfo.FileName);
        
        using var finalStream = File.Create(finalPath);
        
        // Sort chunks by start position
        var sortedChunks = uploadInfo.ReceivedChunks
            .OrderBy(c => c.Start)
            .ToList();

        foreach (var chunk in sortedChunks)
        {
            using var chunkStream = File.OpenRead(chunk.FilePath);
            await chunkStream.CopyToAsync(finalStream);
        }

        return finalPath;
    }
}

// SignalR Hub cho real-time progress
public class UploadProgressHub : Hub
{
    public async Task JoinUploadGroup(string uploadId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, uploadId);
    }

    public async Task LeaveUploadGroup(string uploadId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, uploadId);
    }
}
```

---

## 🖼️ **IMAGE PROCESSING PATTERNS**

### **1. 🎨 Advanced Image Processing**

```csharp
// Custom image processor với multiple effects
public class AdvancedImageProcessor : IImageProcessor
{
    public async Task<ProcessedImage> ProcessImageAsync(
        Stream inputStream,
        ImageProcessingOptions options)
    {
        using var image = await Image.LoadAsync(inputStream);
        
        // Apply transformations
        image.Mutate(ctx =>
        {
            // Resize
            if (options.Width.HasValue || options.Height.HasValue)
            {
                var resizeOptions = new ResizeOptions
                {
                    Size = new Size(options.Width ?? 0, options.Height ?? 0),
                    Mode = options.ResizeMode ?? ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                };
                ctx.Resize(resizeOptions);
            }

            // Crop
            if (options.CropRectangle.HasValue)
            {
                ctx.Crop(options.CropRectangle.Value);
            }

            // Rotate
            if (options.RotationAngle.HasValue)
            {
                ctx.Rotate(options.RotationAngle.Value);
            }

            // Filters
            if (options.Brightness.HasValue)
            {
                ctx.Brightness(options.Brightness.Value);
            }

            if (options.Contrast.HasValue)
            {
                ctx.Contrast(options.Contrast.Value);
            }

            if (options.Saturation.HasValue)
            {
                ctx.Saturate(options.Saturation.Value);
            }

            // Watermark
            if (options.Watermark != null)
            {
                ApplyWatermark(ctx, options.Watermark);
            }
        });

        // Convert format if needed
        var outputStream = new MemoryStream();
        var encoder = GetEncoder(options.OutputFormat ?? ImageFormat.Jpeg, options.Quality);
        
        await image.SaveAsync(outputStream, encoder);
        outputStream.Position = 0;

        return new ProcessedImage
        {
            Stream = outputStream,
            Width = image.Width,
            Height = image.Height,
            Format = options.OutputFormat ?? ImageFormat.Jpeg,
            Size = outputStream.Length
        };
    }

    private void ApplyWatermark(IImageProcessingContext ctx, WatermarkOptions watermark)
    {
        using var watermarkImage = Image.Load(watermark.ImagePath);
        
        // Calculate position
        var position = CalculateWatermarkPosition(
            ctx.GetCurrentSize(),
            watermarkImage.Size,
            watermark.Position,
            watermark.Margin);

        // Apply watermark
        ctx.DrawImage(watermarkImage, position, watermark.Opacity);
    }
}

// Image processing options
public class ImageProcessingOptions
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public ResizeMode? ResizeMode { get; set; }
    public Rectangle? CropRectangle { get; set; }
    public float? RotationAngle { get; set; }
    public float? Brightness { get; set; }
    public float? Contrast { get; set; }
    public float? Saturation { get; set; }
    public ImageFormat? OutputFormat { get; set; }
    public int? Quality { get; set; }
    public WatermarkOptions Watermark { get; set; }
}

public class WatermarkOptions
{
    public string ImagePath { get; set; }
    public WatermarkPosition Position { get; set; } = WatermarkPosition.BottomRight;
    public int Margin { get; set; } = 10;
    public float Opacity { get; set; } = 0.7f;
}
```

### **2. 📱 Responsive Image Generation**

```csharp
// Service tạo responsive images
public class ResponsiveImageService : IResponsiveImageService
{
    private readonly IImageProcessor _imageProcessor;
    private readonly IMediaFileStore _mediaFileStore;

    public async Task<ResponsiveImageSet> GenerateResponsiveImagesAsync(
        string imagePath,
        ResponsiveImageOptions options)
    {
        var originalFile = await _mediaFileStore.GetFileInfoAsync(imagePath);
        if (originalFile == null)
        {
            throw new FileNotFoundException($"Image not found: {imagePath}");
        }

        var imageSet = new ResponsiveImageSet
        {
            OriginalPath = imagePath,
            Images = new List<ResponsiveImage>()
        };

        using var originalStream = await _mediaFileStore.GetFileStreamAsync(imagePath);
        
        // Generate different sizes
        foreach (var breakpoint in options.Breakpoints)
        {
            var processOptions = new ImageProcessingOptions
            {
                Width = breakpoint.Width,
                Height = breakpoint.Height,
                ResizeMode = breakpoint.ResizeMode,
                Quality = breakpoint.Quality,
                OutputFormat = breakpoint.Format
            };

            var processed = await _imageProcessor.ProcessImageAsync(originalStream, processOptions);
            
            // Generate filename
            var fileName = GenerateResponsiveFileName(imagePath, breakpoint);
            var responsivePath = await _mediaFileStore.CreateFileFromStreamAsync(fileName, processed.Stream);

            imageSet.Images.Add(new ResponsiveImage
            {
                Path = responsivePath,
                Width = processed.Width,
                Height = processed.Height,
                Breakpoint = breakpoint.Name,
                Size = processed.Size
            });

            originalStream.Position = 0; // Reset for next iteration
        }

        return imageSet;
    }

    public string GenerateResponsiveHtml(ResponsiveImageSet imageSet, string altText = "")
    {
        var srcSet = string.Join(", ", 
            imageSet.Images.Select(img => $"{img.Path} {img.Width}w"));

        var sizes = string.Join(", ",
            imageSet.Images.Select(img => $"(max-width: {img.Width}px) {img.Width}px"));

        return $@"
            <img src=""{imageSet.Images.First().Path}""
                 srcset=""{srcSet}""
                 sizes=""{sizes}""
                 alt=""{altText}""
                 loading=""lazy"" />";
    }
}

// Responsive image options
public class ResponsiveImageOptions
{
    public List<ImageBreakpoint> Breakpoints { get; set; } = new();
}

public class ImageBreakpoint
{
    public string Name { get; set; }
    public int Width { get; set; }
    public int? Height { get; set; }
    public ResizeMode ResizeMode { get; set; } = ResizeMode.Max;
    public int Quality { get; set; } = 85;
    public ImageFormat Format { get; set; } = ImageFormat.Jpeg;
}
```

---

## ⚡ **PERFORMANCE OPTIMIZATION PATTERNS**

### **1. 🚀 Media Caching Strategy**

```csharp
// Advanced media caching service
public class MediaCacheService : IMediaCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IMediaFileStore _mediaFileStore;
    private readonly ILogger<MediaCacheService> _logger;

    public async Task<CachedMediaItem> GetCachedMediaAsync(
        string path,
        MediaCacheOptions options = null)
    {
        var cacheKey = GenerateCacheKey(path, options);
        
        // Try memory cache first
        if (_memoryCache.TryGetValue(cacheKey, out CachedMediaItem cachedItem))
        {
            return cachedItem;
        }

        // Try distributed cache
        var distributedData = await _distributedCache.GetAsync(cacheKey);
        if (distributedData != null)
        {
            cachedItem = JsonSerializer.Deserialize<CachedMediaItem>(distributedData);
            
            // Add back to memory cache
            _memoryCache.Set(cacheKey, cachedItem, TimeSpan.FromMinutes(30));
            return cachedItem;
        }

        // Load from storage and cache
        return await LoadAndCacheMediaAsync(path, cacheKey, options);
    }

    private async Task<CachedMediaItem> LoadAndCacheMediaAsync(
        string path,
        string cacheKey,
        MediaCacheOptions options)
    {
        var fileInfo = await _mediaFileStore.GetFileInfoAsync(path);
        if (fileInfo == null)
        {
            return null;
        }

        var cachedItem = new CachedMediaItem
        {
            Path = path,
            ContentType = GetContentType(path),
            LastModified = fileInfo.LastModified,
            Size = fileInfo.Length,
            ETag = GenerateETag(fileInfo)
        };

        // Load content if small enough for memory caching
        if (options?.CacheContent == true && fileInfo.Length <= options.MaxCacheSize)
        {
            using var stream = await _mediaFileStore.GetFileStreamAsync(path);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            cachedItem.Content = memoryStream.ToArray();
        }

        // Cache in memory
        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
            Size = cachedItem.Content?.Length ?? 1
        };
        _memoryCache.Set(cacheKey, cachedItem, memoryCacheOptions);

        // Cache in distributed cache
        var distributedData = JsonSerializer.SerializeToUtf8Bytes(cachedItem);
        var distributedOptions = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(2)
        };
        await _distributedCache.SetAsync(cacheKey, distributedData, distributedOptions);

        return cachedItem;
    }
}

// Background service để cleanup cache
public class MediaCacheCleanupService : BackgroundService
{
    private readonly IMediaCacheService _cacheService;
    private readonly ILogger<MediaCacheCleanupService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredCacheAsync();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }
    }

    private async Task CleanupExpiredCacheAsync()
    {
        // Cleanup logic
        _logger.LogInformation("Starting media cache cleanup");
        
        // Remove expired items from distributed cache
        await _cacheService.RemoveExpiredItemsAsync();
        
        // Cleanup temp files
        await CleanupTempFilesAsync();
        
        _logger.LogInformation("Media cache cleanup completed");
    }
}
```

### **2. 📊 Media Analytics & Monitoring**

```csharp
// Media usage analytics service
public class MediaAnalyticsService : IMediaAnalyticsService
{
    private readonly ILogger<MediaAnalyticsService> _logger;
    private readonly IMetricsCollector _metricsCollector;

    public async Task TrackMediaAccessAsync(MediaAccessEvent accessEvent)
    {
        // Log access
        _logger.LogInformation("Media accessed: {Path} by {User} from {IP}",
            accessEvent.Path, accessEvent.UserId, accessEvent.IpAddress);

        // Collect metrics
        _metricsCollector.Increment("media.access.count", new Dictionary<string, string>
        {
            ["path"] = accessEvent.Path,
            ["type"] = GetMediaType(accessEvent.Path),
            ["user"] = accessEvent.UserId ?? "anonymous"
        });

        // Track bandwidth usage
        if (accessEvent.BytesServed > 0)
        {
            _metricsCollector.Histogram("media.bandwidth.bytes", accessEvent.BytesServed);
        }

        // Store in analytics database
        await StoreAnalyticsEventAsync(accessEvent);
    }

    public async Task<MediaUsageReport> GenerateUsageReportAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var report = new MediaUsageReport
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalAccesses = await GetTotalAccessesAsync(startDate, endDate),
            TotalBandwidth = await GetTotalBandwidthAsync(startDate, endDate),
            TopFiles = await GetTopFilesAsync(startDate, endDate, 10),
            AccessByType = await GetAccessByTypeAsync(startDate, endDate),
            AccessByUser = await GetAccessByUserAsync(startDate, endDate)
        };

        return report;
    }
}

// Media health check
public class MediaHealthCheck : IHealthCheck
{
    private readonly IMediaFileStore _mediaFileStore;
    private readonly IMediaCacheService _cacheService;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check media store accessibility
            var testFile = "health-check.txt";
            await _mediaFileStore.CreateFileFromStreamAsync(testFile, 
                new MemoryStream(Encoding.UTF8.GetBytes("health check")));
            
            var fileInfo = await _mediaFileStore.GetFileInfoAsync(testFile);
            if (fileInfo == null)
            {
                return HealthCheckResult.Unhealthy("Cannot read test file from media store");
            }

            await _mediaFileStore.TryDeleteFileAsync(testFile);

            // Check cache service
            var cacheHealth = await _cacheService.CheckHealthAsync();
            if (!cacheHealth.IsHealthy)
            {
                return HealthCheckResult.Degraded($"Cache service issues: {cacheHealth.Message}");
            }

            return HealthCheckResult.Healthy("Media system is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Media system error: {ex.Message}");
        }
    }
}
```

---

## 🔧 **MODULE REGISTRATION & STARTUP**

### **1. 📦 Media Module Startup**

```csharp
// Startup class cho custom media module
public class CustomMediaStartup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddScoped<ICustomMediaFileProvider, CustomMediaFileProvider>();
        services.AddScoped<IFileUploadHandler, CustomFileUploadHandler>();
        services.AddScoped<IImageProcessor, AdvancedImageProcessor>();
        services.AddScoped<IResponsiveImageService, ResponsiveImageService>();
        
        // Caching
        services.AddScoped<IMediaCacheService, MediaCacheService>();
        services.AddHostedService<MediaCacheCleanupService>();
        
        // Analytics
        services.AddScoped<IMediaAnalyticsService, MediaAnalyticsService>();
        
        // Health checks
        services.AddHealthChecks()
            .AddCheck<MediaHealthCheck>("media");

        // Configuration
        services.Configure<MediaOptions>(options =>
        {
            options.MaxFileSize = 100 * 1024 * 1024; // 100MB
            options.AllowedFileExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
            options.EnableChunkedUpload = true;
            options.MaxUploadChunkSize = 5 * 1024 * 1024; // 5MB chunks
        });

        // Image processing
        services.Configure<ImageProcessingOptions>(options =>
        {
            options.DefaultQuality = 85;
            options.EnableWebPConversion = true;
            options.MaxImageDimension = 4096;
        });
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        // Media middleware
        app.UseMiddleware<SecureMediaMiddleware>();
        app.UseMiddleware<MediaAnalyticsMiddleware>();
        
        // SignalR for upload progress
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<UploadProgressHub>("/uploadProgress");
        });
    }
}

// Module manifest
[assembly: Module(
    Name = "Custom Media Management",
    Author = "Your Name",
    Website = "https://yourwebsite.com",
    Version = "1.0.0",
    Description = "Advanced media management with cloud storage and processing capabilities",
    Category = "Media",
    Dependencies = new[] { "OrchardCore.Media" }
)]
```

### **2. ⚙️ Configuration Options**

```csharp
// Media configuration options
public class CustomMediaOptions
{
    public long MaxFileSize { get; set; } = 50 * 1024 * 1024; // 50MB
    public string[] AllowedFileExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif" };
    public bool EnableChunkedUpload { get; set; } = true;
    public long MaxUploadChunkSize { get; set; } = 5 * 1024 * 1024; // 5MB
    public bool EnableVirusScanning { get; set; } = false;
    public bool EnableImageProcessing { get; set; } = true;
    public bool EnableResponsiveImages { get; set; } = true;
    public CloudStorageOptions CloudStorage { get; set; } = new();
    public CacheOptions Cache { get; set; } = new();
}

public class CloudStorageOptions
{
    public string Provider { get; set; } // "S3", "Azure", "Local"
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
    public bool EnableCdn { get; set; } = false;
    public string CdnUrl { get; set; }
}

public class CacheOptions
{
    public bool EnableMemoryCache { get; set; } = true;
    public bool EnableDistributedCache { get; set; } = true;
    public long MaxCacheSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromHours(2);
}
```

---

## 🎯 **USE CASES THỰC TẾ**

### **1. 🏥 Hospital Medical Records System**

```csharp
// Medical image management system
public class MedicalImageService : IMedicalImageService
{
    private readonly IMediaFileStore _mediaFileStore;
    private readonly IImageProcessor _imageProcessor;
    private readonly IEncryptionService _encryptionService;

    public async Task<MedicalImageResult> UploadMedicalImageAsync(
        IFormFile imageFile,
        string patientId,
        MedicalImageType imageType)
    {
        // Validate medical image
        var validation = await ValidateMedicalImageAsync(imageFile, imageType);
        if (!validation.IsValid)
        {
            return MedicalImageResult.Failed(validation.Errors);
        }

        // Generate secure path
        var imagePath = GenerateSecurePath(patientId, imageType);
        
        // Process DICOM or standard image
        using var processedStream = await ProcessMedicalImageAsync(imageFile, imageType);
        
        // Encrypt sensitive data
        using var encryptedStream = await _encryptionService.EncryptStreamAsync(processedStream);
        
        // Save with metadata
        var savedPath = await _mediaFileStore.CreateFileFromStreamAsync(imagePath, encryptedStream);
        
        // Create audit log
        await LogMedicalImageAccessAsync(patientId, savedPath, "UPLOAD");
        
        return MedicalImageResult.Success(savedPath);
    }

    private async Task<Stream> ProcessMedicalImageAsync(IFormFile file, MedicalImageType type)
    {
        switch (type)
        {
            case MedicalImageType.XRay:
                return await ProcessXRayImageAsync(file);
            case MedicalImageType.MRI:
                return await ProcessMRIImageAsync(file);
            case MedicalImageType.CTScan:
                return await ProcessCTScanAsync(file);
            default:
                return file.OpenReadStream();
        }
    }
}
```

### **2. 🎓 E-learning Platform**

```csharp
// Educational content media service
public class EducationalMediaService : IEducationalMediaService
{
    public async Task<CourseMediaResult> UploadCourseMediaAsync(
        IFormFile mediaFile,
        string courseId,
        MediaContentType contentType)
    {
        // Validate educational content
        var validation = await ValidateEducationalContentAsync(mediaFile, contentType);
        if (!validation.IsValid)
        {
            return CourseMediaResult.Failed(validation.Errors);
        }

        var mediaPath = $"courses/{courseId}/{contentType.ToString().ToLower()}";
        
        switch (contentType)
        {
            case MediaContentType.Video:
                return await ProcessVideoContentAsync(mediaFile, mediaPath);
            case MediaContentType.Audio:
                return await ProcessAudioContentAsync(mediaFile, mediaPath);
            case MediaContentType.Document:
                return await ProcessDocumentContentAsync(mediaFile, mediaPath);
            case MediaContentType.Image:
                return await ProcessImageContentAsync(mediaFile, mediaPath);
            default:
                throw new NotSupportedException($"Content type {contentType} not supported");
        }
    }

    private async Task<CourseMediaResult> ProcessVideoContentAsync(IFormFile video, string basePath)
    {
        // Generate multiple quality versions
        var qualities = new[] { 240, 480, 720, 1080 };
        var processedVideos = new List<ProcessedVideo>();

        foreach (var quality in qualities)
        {
            var outputPath = $"{basePath}/video_{quality}p.mp4";
            var processedVideo = await ConvertVideoQualityAsync(video, quality);
            
            var savedPath = await _mediaFileStore.CreateFileFromStreamAsync(outputPath, processedVideo);
            processedVideos.Add(new ProcessedVideo
            {
                Path = savedPath,
                Quality = quality,
                Size = processedVideo.Length
            });
        }

        // Generate thumbnail
        var thumbnailPath = await GenerateVideoThumbnailAsync(video, $"{basePath}/thumbnail.jpg");
        
        // Generate subtitles if enabled
        var subtitlesPath = await GenerateSubtitlesAsync(video, $"{basePath}/subtitles.vtt");

        return CourseMediaResult.Success(new CourseMedia
        {
            Videos = processedVideos,
            ThumbnailPath = thumbnailPath,
            SubtitlesPath = subtitlesPath
        });
    }
}
```

### **3. 🏪 E-commerce Product Images**

```csharp
// Product image management service
public class ProductImageService : IProductImageService
{
    public async Task<ProductImageResult> UploadProductImagesAsync(
        IEnumerable<IFormFile> images,
        string productId,
        ProductImageOptions options)
    {
        var processedImages = new List<ProductImage>();
        
        foreach (var image in images)
        {
            // Validate product image
            var validation = await ValidateProductImageAsync(image);
            if (!validation.IsValid)
            {
                continue; // Skip invalid images
            }

            var basePath = $"products/{productId}";
            
            // Generate multiple sizes for e-commerce
            var imageSizes = new[]
            {
                new { Name = "thumbnail", Width = 150, Height = 150 },
                new { Name = "small", Width = 300, Height = 300 },
                new { Name = "medium", Width = 600, Height = 600 },
                new { Name = "large", Width = 1200, Height = 1200 },
                new { Name = "zoom", Width = 2400, Height = 2400 }
            };

            var productImage = new ProductImage
            {
                OriginalFileName = image.FileName,
                Variants = new List<ImageVariant>()
            };

            foreach (var size in imageSizes)
            {
                var processOptions = new ImageProcessingOptions
                {
                    Width = size.Width,
                    Height = size.Height,
                    ResizeMode = ResizeMode.Max,
                    Quality = GetQualityForSize(size.Name),
                    OutputFormat = ImageFormat.Jpeg
                };

                // Add watermark for large images
                if (size.Name == "large" || size.Name == "zoom")
                {
                    processOptions.Watermark = new WatermarkOptions
                    {
                        ImagePath = "watermark.png",
                        Position = WatermarkPosition.BottomRight,
                        Opacity = 0.3f
                    };
                }

                var processed = await _imageProcessor.ProcessImageAsync(image.OpenReadStream(), processOptions);
                var imagePath = $"{basePath}/{size.Name}_{Path.GetFileNameWithoutExtension(image.FileName)}.jpg";
                var savedPath = await _mediaFileStore.CreateFileFromStreamAsync(imagePath, processed.Stream);

                productImage.Variants.Add(new ImageVariant
                {
                    Name = size.Name,
                    Path = savedPath,
                    Width = processed.Width,
                    Height = processed.Height,
                    Size = processed.Size
                });
            }

            processedImages.Add(productImage);
        }

        return ProductImageResult.Success(processedImages);
    }
}
```

---

## 📊 **MONITORING & ANALYTICS**

### **1. 📈 Media Usage Metrics**

```csharp
// Media metrics collector
public class MediaMetricsCollector : IMediaMetricsCollector
{
    private readonly IMetricsLogger _metricsLogger;
    private readonly ILogger<MediaMetricsCollector> _logger;

    public void TrackFileUpload(string fileName, long fileSize, TimeSpan uploadTime)
    {
        _metricsLogger.Counter("media_uploads_total")
            .WithTag("file_extension", Path.GetExtension(fileName))
            .Increment();

        _metricsLogger.Histogram("media_upload_size_bytes")
            .Record(fileSize);

        _metricsLogger.Histogram("media_upload_duration_seconds")
            .Record(uploadTime.TotalSeconds);
    }

    public void TrackFileAccess(string filePath, long bytesServed, TimeSpan responseTime)
    {
        _metricsLogger.Counter("media_requests_total")
            .WithTag("file_type", GetFileType(filePath))
            .Increment();

        _metricsLogger.Histogram("media_response_size_bytes")
            .Record(bytesServed);

        _metricsLogger.Histogram("media_response_duration_seconds")
            .Record(responseTime.TotalSeconds);
    }

    public void TrackImageProcessing(string operation, TimeSpan processingTime, bool success)
    {
        _metricsLogger.Counter("image_processing_total")
            .WithTag("operation", operation)
            .WithTag("status", success ? "success" : "failure")
            .Increment();

        if (success)
        {
            _metricsLogger.Histogram("image_processing_duration_seconds")
                .WithTag("operation", operation)
                .Record(processingTime.TotalSeconds);
        }
    }
}
```

### **2. 🔍 Media Audit Logging**

```csharp
// Media audit service
public class MediaAuditService : IMediaAuditService
{
    private readonly ILogger<MediaAuditService> _logger;
    private readonly IAuditEventStore _auditStore;

    public async Task LogMediaEventAsync(MediaAuditEvent auditEvent)
    {
        // Structure log for easy querying
        _logger.LogInformation("Media Event: {EventType} - {FilePath} by {UserId} from {IpAddress}",
            auditEvent.EventType,
            auditEvent.FilePath,
            auditEvent.UserId,
            auditEvent.IpAddress);

        // Store in audit database
        await _auditStore.StoreEventAsync(new AuditEvent
        {
            EventType = auditEvent.EventType,
            EntityType = "Media",
            EntityId = auditEvent.FilePath,
            UserId = auditEvent.UserId,
            IpAddress = auditEvent.IpAddress,
            UserAgent = auditEvent.UserAgent,
            Timestamp = auditEvent.Timestamp,
            Details = JsonSerializer.Serialize(auditEvent.Details)
        });

        // Alert on suspicious activity
        if (IsSuspiciousActivity(auditEvent))
        {
            await SendSecurityAlertAsync(auditEvent);
        }
    }

    private bool IsSuspiciousActivity(MediaAuditEvent auditEvent)
    {
        // Check for suspicious patterns
        return auditEvent.EventType == "UNAUTHORIZED_ACCESS" ||
               auditEvent.EventType == "BULK_DOWNLOAD" ||
               (auditEvent.EventType == "UPLOAD" && auditEvent.Details.ContainsKey("large_file"));
    }
}
```

---

## 💡 **BEST PRACTICES**

### **✅ DO's:**

1. **🔒 Security First**
   - Always validate file types and content
   - Use secure file paths and names
   - Implement proper authorization
   - Scan for malware when possible

2. **⚡ Performance Optimization**
   - Use chunked upload for large files
   - Implement proper caching strategies
   - Generate multiple image sizes
   - Use CDN for static content

3. **🛡️ Error Handling**
   - Graceful degradation for processing failures
   - Proper cleanup of temporary files
   - Comprehensive logging and monitoring
   - User-friendly error messages

4. **📊 Monitoring**
   - Track upload/download metrics
   - Monitor storage usage
   - Log security events
   - Health checks for storage systems

### **❌ DON'Ts:**

1. **🚫 Security Issues**
   - Don't trust file extensions
   - Don't store files in web-accessible locations without protection
   - Don't skip virus scanning for user uploads
   - Don't expose internal file paths

2. **⚠️ Performance Problems**
   - Don't process large images synchronously
   - Don't cache everything in memory
   - Don't skip image optimization
   - Don't ignore cleanup tasks

3. **🔧 Architecture Mistakes**
   - Don't tightly couple to specific storage providers
   - Don't skip abstraction layers
   - Don't ignore scalability requirements
   - Don't forget about backup strategies

---

## 🎯 **KẾT LUẬN**

**Media & File Management** trong OrchardCore cung cấp foundation mạnh mẽ cho:

- **🔧 Extensible Architecture**: Dễ dàng tích hợp custom providers
- **☁️ Cloud-Ready**: Hỗ trợ AWS S3, Azure Blob Storage
- **🖼️ Advanced Processing**: ImageSharp integration cho image manipulation
- **🔒 Security-Focused**: Token-based protection, secure file handling
- **⚡ Performance-Optimized**: Caching, chunked upload, background processing

**Media & File Management là component thiết yếu cho mọi ứng dụng web hiện đại cần xử lý files và media content! 🚀**

---

## 🎯 **KHI NÀO CẦN MEDIA & FILE MANAGEMENT - VÍ DỤ THỰC TẾ**

### **1. 📸 Social Media Platform - Instagram Clone**

#### **Tình huống:**
Anh xây dựng **platform chia sẻ ảnh** như Instagram. Users upload ảnh, cần resize multiple sizes, apply filters, và serve nhanh cho millions users.

#### **❌ TRƯỚC KHI DÙNG MEDIA MANAGEMENT:**
```csharp
// Upload ảnh thủ công - không tối ưu
[HttpPost]
public async Task<IActionResult> UploadPhoto(IFormFile photo)
{
    // Không validate - nguy hiểm
    var fileName = photo.FileName;
    var path = Path.Combine("wwwroot/uploads", fileName);
    
    // Save trực tiếp - không resize
    using (var stream = new FileStream(path, FileMode.Create))
    {
        await photo.CopyToAsync(stream);
    }
    
    // Không có thumbnail, không có CDN
    // Không có security, không có backup
    // Load chậm, bandwidth cao
    
    return Ok(new { url = $"/uploads/{fileName}" });
}
```

#### **✅ SAU KHI DÙNG MEDIA MANAGEMENT:**
```csharp
// Professional photo upload system
public class SocialMediaPhotoService : ISocialMediaPhotoService
{
    private readonly IMediaFileStore _mediaFileStore;
    private readonly IResponsiveImageService _responsiveImageService;
    private readonly IImageProcessor _imageProcessor;
    private readonly IMediaCacheService _cacheService;

    [HttpPost]
    public async Task<IActionResult> UploadPhoto(IFormFile photo, PhotoUploadOptions options)
    {
        // 1. Validate ảnh
        var validation = await ValidatePhotoAsync(photo);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors);
        }

        var userId = GetCurrentUserId();
        var photoId = Guid.NewGuid();
        
        // 2. Generate multiple sizes cho different devices
        var photoSizes = new[]
        {
            new { Name = "thumbnail", Width = 150, Height = 150 },    // Profile grid
            new { Name = "small", Width = 320, Height = 320 },       // Mobile feed
            new { Name = "medium", Width = 640, Height = 640 },      // Desktop feed
            new { Name = "large", Width = 1080, Height = 1080 },     // Full view
            new { Name = "original", Width = 0, Height = 0 }         // Original backup
        };

        var processedPhotos = new List<ProcessedPhoto>();

        foreach (var size in photoSizes)
        {
            var processOptions = new ImageProcessingOptions
            {
                Width = size.Width > 0 ? size.Width : null,
                Height = size.Height > 0 ? size.Height : null,
                ResizeMode = ResizeMode.Crop, // Square crop như Instagram
                Quality = GetQualityForSize(size.Name),
                OutputFormat = ImageFormat.Jpeg
            };

            // 3. Apply Instagram-style filters
            if (options.FilterName != "none")
            {
                processOptions.Filters = GetInstagramFilter(options.FilterName);
            }

            // 4. Process image
            var processed = await _imageProcessor.ProcessImageAsync(photo.OpenReadStream(), processOptions);
            
            // 5. Save to cloud storage với CDN
            var imagePath = $"photos/{userId}/{photoId}/{size.Name}.jpg";
            var savedPath = await _mediaFileStore.CreateFileFromStreamAsync(imagePath, processed.Stream);
            
            processedPhotos.Add(new ProcessedPhoto
            {
                Size = size.Name,
                Path = savedPath,
                Width = processed.Width,
                Height = processed.Height,
                FileSize = processed.Size,
                CdnUrl = GenerateCdnUrl(savedPath)
            });
        }

        // 6. Generate responsive HTML
        var responsiveHtml = GenerateResponsivePhotoHtml(processedPhotos);
        
        // 7. Cache popular photos
        await _cacheService.PrewarmCacheAsync(processedPhotos.Select(p => p.Path));
        
        // 8. Analytics tracking
        await TrackPhotoUploadAsync(userId, photoId, photo.Length);

        return Ok(new PhotoUploadResult
        {
            PhotoId = photoId,
            Photos = processedPhotos,
            ResponsiveHtml = responsiveHtml,
            UploadTime = DateTime.UtcNow
        });
    }

    private ImageFilterOptions GetInstagramFilter(string filterName)
    {
        return filterName switch
        {
            "vintage" => new ImageFilterOptions
            {
                Sepia = 0.3f,
                Contrast = 1.2f,
                Brightness = 0.9f,
                Vignette = 0.2f
            },
            "dramatic" => new ImageFilterOptions
            {
                Contrast = 1.5f,
                Saturation = 1.3f,
                Brightness = 0.8f
            },
            "cool" => new ImageFilterOptions
            {
                Temperature = -200, // Cooler
                Tint = 10,
                Saturation = 1.1f
            },
            _ => new ImageFilterOptions()
        };
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Media Management** | **Sau Media Management** |
|---------------------------|--------------------------|
| ❌ 1 size duy nhất, load chậm | ✅ 5 sizes responsive, load nhanh |
| ❌ Không có filters | ✅ Instagram-style filters |
| ❌ Không validate, unsafe | ✅ Comprehensive validation |
| ❌ Local storage only | ✅ Cloud storage + CDN |
| ❌ Không có caching | ✅ Multi-level caching |
| ❌ Bandwidth cao | ✅ Optimized bandwidth |

---

### **2. 🏢 Corporate Document Management - SharePoint Clone**

#### **Tình huống:**
Anh xây dựng **hệ thống quản lý tài liệu** cho công ty với 1000+ nhân viên. Cần upload, preview, version control, và security cho documents.

#### **❌ TRƯỚC KHI DÙNG MEDIA MANAGEMENT:**
```csharp
// Document upload cơ bản - thiếu tính năng
[HttpPost]
public async Task<IActionResult> UploadDocument(IFormFile document)
{
    // Không check permissions
    // Không có version control
    // Không có preview generation
    // Không có virus scan
    
    var fileName = document.FileName;
    var path = Path.Combine("Documents", fileName);
    
    using (var stream = new FileStream(path, FileMode.Create))
    {
        await document.CopyToAsync(stream);
    }
    
    return Ok($"File saved: {fileName}");
}
```

#### **✅ SAU KHI DÙNG MEDIA MANAGEMENT:**
```csharp
// Enterprise document management system
public class CorporateDocumentService : ICorporateDocumentService
{
    private readonly IMediaFileStore _mediaFileStore;
    private readonly IDocumentProcessor _documentProcessor;
    private readonly IVirusScanService _virusScanService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IVersionControlService _versionControlService;

    [HttpPost]
    public async Task<IActionResult> UploadDocument(
        IFormFile document, 
        string folderId, 
        DocumentUploadOptions options)
    {
        var user = GetCurrentUser();
        
        // 1. Check permissions
        var folder = await GetFolderAsync(folderId);
        var authResult = await _authorizationService.AuthorizeAsync(
            user, folder, "UploadDocument");
        
        if (!authResult.Succeeded)
        {
            return Forbid("Insufficient permissions to upload to this folder");
        }

        // 2. Validate document
        var validation = await ValidateDocumentAsync(document, options);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors);
        }

        // 3. Virus scan
        var scanResult = await _virusScanService.ScanFileAsync(document);
        if (!scanResult.IsClean)
        {
            await LogSecurityEventAsync("VIRUS_DETECTED", document.FileName, user.Id);
            return BadRequest("File failed security scan");
        }

        var documentId = Guid.NewGuid();
        var basePath = $"documents/{folderId}/{documentId}";

        // 4. Save original document
        var originalPath = $"{basePath}/original/{document.FileName}";
        var savedPath = await _mediaFileStore.CreateFileFromStreamAsync(
            originalPath, document.OpenReadStream());

        // 5. Generate previews based on document type
        var previews = await GenerateDocumentPreviewsAsync(document, basePath);
        
        // 6. Extract text for search indexing
        var extractedText = await ExtractTextFromDocumentAsync(document);
        
        // 7. Create version entry
        var version = await _versionControlService.CreateVersionAsync(new DocumentVersion
        {
            DocumentId = documentId,
            VersionNumber = 1,
            FileName = document.FileName,
            FilePath = savedPath,
            FileSize = document.Length,
            ContentType = document.ContentType,
            UploadedBy = user.Id,
            UploadedAt = DateTime.UtcNow,
            ExtractedText = extractedText,
            Previews = previews
        });

        // 8. Set up document permissions
        await SetupDocumentPermissionsAsync(documentId, folderId, user.Id);
        
        // 9. Notify stakeholders
        if (options.NotifyTeam)
        {
            await NotifyTeamMembersAsync(folderId, documentId, document.FileName, user);
        }

        // 10. Audit logging
        await LogDocumentEventAsync("DOCUMENT_UPLOADED", documentId, user.Id);

        return Ok(new DocumentUploadResult
        {
            DocumentId = documentId,
            VersionId = version.Id,
            OriginalPath = savedPath,
            Previews = previews,
            SearchableText = extractedText.Substring(0, Math.Min(500, extractedText.Length)),
            UploadedAt = DateTime.UtcNow
        });
    }

    private async Task<List<DocumentPreview>> GenerateDocumentPreviewsAsync(
        IFormFile document, 
        string basePath)
    {
        var previews = new List<DocumentPreview>();
        var extension = Path.GetExtension(document.FileName).ToLower();

        switch (extension)
        {
            case ".pdf":
                // Generate PDF thumbnails
                var pdfPreviews = await _documentProcessor.GeneratePdfPreviewsAsync(
                    document.OpenReadStream(), $"{basePath}/previews");
                previews.AddRange(pdfPreviews);
                break;

            case ".docx":
            case ".doc":
                // Convert to PDF first, then generate previews
                var pdfStream = await _documentProcessor.ConvertToPdfAsync(document.OpenReadStream());
                var docPreviews = await _documentProcessor.GeneratePdfPreviewsAsync(
                    pdfStream, $"{basePath}/previews");
                previews.AddRange(docPreviews);
                break;

            case ".xlsx":
            case ".xls":
                // Generate Excel sheet previews
                var excelPreviews = await _documentProcessor.GenerateExcelPreviewsAsync(
                    document.OpenReadStream(), $"{basePath}/previews");
                previews.AddRange(excelPreviews);
                break;

            case ".pptx":
            case ".ppt":
                // Generate PowerPoint slide previews
                var pptPreviews = await _documentProcessor.GeneratePowerPointPreviewsAsync(
                    document.OpenReadStream(), $"{basePath}/previews");
                previews.AddRange(pptPreviews);
                break;

            default:
                // Generate generic file icon
                previews.Add(new DocumentPreview
                {
                    Type = "icon",
                    Path = GetFileTypeIcon(extension),
                    PageNumber = 0
                });
                break;
        }

        return previews;
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Media Management** | **Sau Media Management** |
|---------------------------|--------------------------|
| ❌ Không có permissions | ✅ Role-based access control |
| ❌ Không có virus scan | ✅ Comprehensive security scanning |
| ❌ Không có preview | ✅ Multi-format preview generation |
| ❌ Không có version control | ✅ Full version history |
| ❌ Không searchable | ✅ Full-text search indexing |
| ❌ Không có audit trail | ✅ Complete audit logging |

---

### **3. 🎬 Video Streaming Platform - YouTube Clone**

#### **Tình huống:**
Anh xây dựng **platform streaming video** như YouTube. Cần upload video lớn, transcode multiple quality, generate thumbnails, và serve cho millions viewers.

#### **❌ TRƯỚC KHI DÙNG MEDIA MANAGEMENT:**
```csharp
// Video upload cơ bản - không scalable
[HttpPost]
public async Task<IActionResult> UploadVideo(IFormFile video)
{
    // Không có chunked upload - timeout với file lớn
    // Không có transcoding - chỉ 1 quality
    // Không có thumbnail generation
    // Không có progress tracking
    
    var fileName = video.FileName;
    var path = Path.Combine("Videos", fileName);
    
    // Upload đồng bộ - block UI
    using (var stream = new FileStream(path, FileMode.Create))
    {
        await video.CopyToAsync(stream); // Có thể timeout
    }
    
    return Ok($"Video uploaded: {fileName}");
}
```

#### **✅ SAU KHI DÙNG MEDIA MANAGEMENT:**
```csharp
// Professional video streaming platform
public class VideoStreamingService : IVideoStreamingService
{
    private readonly IChunkFileUploadService _chunkUploadService;
    private readonly IVideoTranscodingService _transcodingService;
    private readonly IMediaFileStore _mediaFileStore;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IHubContext<VideoUploadHub> _hubContext;

    [HttpPost]
    public async Task<IActionResult> UploadVideo(VideoUploadRequest request)
    {
        var user = GetCurrentUser();
        var videoId = Guid.NewGuid();

        // 1. Process chunked upload với progress tracking
        var uploadResult = await _chunkUploadService.ProcessRequestAsync(
            Request,
            async (uploadId, chunk, contentRange) =>
            {
                // Process each chunk
                var progress = (double)contentRange.To.Value / contentRange.Length.Value * 100;
                
                // Notify client về progress
                await _hubContext.Clients.Group(uploadId.ToString())
                    .SendAsync("UploadProgress", new { 
                        uploadId, 
                        progress, 
                        bytesUploaded = contentRange.To.Value,
                        totalBytes = contentRange.Length.Value 
                    });

                return Ok(new { uploadId, progress });
            },
            async (files) =>
            {
                // Upload completed - start processing
                var videoFile = files.First();
                return await ProcessCompletedVideoUploadAsync(videoFile, videoId, user);
            });

        return uploadResult;
    }

    private async Task<IActionResult> ProcessCompletedVideoUploadAsync(
        IFormFile videoFile, 
        Guid videoId, 
        User user)
    {
        // 1. Validate video file
        var validation = await ValidateVideoFileAsync(videoFile);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors);
        }

        var basePath = $"videos/{user.Id}/{videoId}";
        
        // 2. Save original video
        var originalPath = $"{basePath}/original/{videoFile.FileName}";
        var savedPath = await _mediaFileStore.CreateFileFromStreamAsync(
            originalPath, videoFile.OpenReadStream());

        // 3. Queue background processing tasks
        var processingJob = new VideoProcessingJob
        {
            VideoId = videoId,
            OriginalPath = savedPath,
            UserId = user.Id,
            FileName = videoFile.FileName,
            FileSize = videoFile.Length
        };

        // Queue multiple parallel tasks
        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await ProcessVideoInBackgroundAsync(processingJob, token);
        });

        // 4. Return immediate response
        return Ok(new VideoUploadResult
        {
            VideoId = videoId,
            Status = "Processing",
            Message = "Video uploaded successfully. Processing will complete shortly.",
            EstimatedProcessingTime = EstimateProcessingTime(videoFile.Length),
            OriginalPath = savedPath
        });
    }

    private async Task ProcessVideoInBackgroundAsync(
        VideoProcessingJob job, 
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Generate thumbnail từ video
            var thumbnailTask = GenerateVideoThumbnailsAsync(job);
            
            // 2. Transcode multiple qualities parallel
            var transcodingTasks = new[]
            {
                TranscodeVideoAsync(job, "240p", 240, 400),   // Mobile
                TranscodeVideoAsync(job, "480p", 480, 800),   // SD
                TranscodeVideoAsync(job, "720p", 720, 1200),  // HD
                TranscodeVideoAsync(job, "1080p", 1080, 2500) // Full HD
            };

            // 3. Extract metadata
            var metadataTask = ExtractVideoMetadataAsync(job);

            // 4. Generate video preview (first 30 seconds)
            var previewTask = GenerateVideoPreviewAsync(job);

            // Wait for all tasks
            await Task.WhenAll(
                thumbnailTask,
                Task.WhenAll(transcodingTasks),
                metadataTask,
                previewTask
            );

            var thumbnails = await thumbnailTask;
            var transcodedVideos = await Task.WhenAll(transcodingTasks);
            var metadata = await metadataTask;
            var preview = await previewTask;

            // 5. Update database với processed results
            await UpdateVideoProcessingResultsAsync(job.VideoId, new VideoProcessingResults
            {
                Thumbnails = thumbnails,
                TranscodedVideos = transcodedVideos.ToList(),
                Metadata = metadata,
                Preview = preview,
                ProcessingCompletedAt = DateTime.UtcNow
            });

            // 6. Notify user processing completed
            await _hubContext.Clients.Group($"user_{job.UserId}")
                .SendAsync("VideoProcessingCompleted", new { 
                    videoId = job.VideoId,
                    thumbnails,
                    qualities = transcodedVideos.Select(v => v.Quality).ToArray()
                });

            // 7. Generate adaptive streaming manifest (HLS/DASH)
            await GenerateStreamingManifestAsync(job.VideoId, transcodedVideos);

        }
        catch (Exception ex)
        {
            // Handle processing errors
            await HandleVideoProcessingErrorAsync(job, ex);
        }
    }

    private async Task<TranscodedVideo> TranscodeVideoAsync(
        VideoProcessingJob job, 
        string quality, 
        int height, 
        int bitrate)
    {
        var outputPath = $"videos/{job.UserId}/{job.VideoId}/{quality}.mp4";
        
        // Use FFmpeg for transcoding
        var transcodingOptions = new VideoTranscodingOptions
        {
            InputPath = job.OriginalPath,
            OutputPath = outputPath,
            Height = height,
            Bitrate = bitrate,
            Codec = "h264",
            AudioCodec = "aac",
            Format = "mp4"
        };

        var result = await _transcodingService.TranscodeAsync(transcodingOptions);
        
        return new TranscodedVideo
        {
            Quality = quality,
            Path = result.OutputPath,
            FileSize = result.FileSize,
            Duration = result.Duration,
            Bitrate = bitrate,
            Resolution = $"{result.Width}x{result.Height}"
        };
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Media Management** | **Sau Media Management** |
|---------------------------|--------------------------|
| ❌ Upload timeout với file lớn | ✅ Chunked upload với progress |
| ❌ 1 quality duy nhất | ✅ Multiple qualities (240p-1080p) |
| ❌ Không có thumbnails | ✅ Auto-generated thumbnails |
| ❌ Blocking upload | ✅ Background processing |
| ❌ Không có streaming | ✅ Adaptive streaming (HLS/DASH) |
| ❌ Poor user experience | ✅ Real-time progress updates |

---

## 💡 **TÓM TẮT KHI NÀO CẦN MEDIA & FILE MANAGEMENT**

### **✅ CẦN DÙNG KHI:**

#### **1. 📱 User-Generated Content Platforms**
- **Ví dụ**: Social media, photo sharing, video platforms
- **Lý do**: Cần multiple sizes, filters, fast delivery
- **Lợi ích**: Responsive images, CDN optimization, user experience

#### **2. 🏢 Enterprise Document Systems**
- **Ví dụ**: Document management, file sharing, collaboration
- **Lý do**: Security, permissions, version control, compliance
- **Lợi ích**: Audit trails, virus scanning, preview generation

#### **3. 🎬 Media Streaming Applications**
- **Ví dụ**: Video platforms, e-learning, webinars
- **Lý do**: Large files, multiple formats, adaptive streaming
- **Lợi ích**: Background processing, quality options, thumbnails

#### **4. 🛒 E-commerce Product Catalogs**
- **Ví dụ**: Online stores, marketplaces, catalogs
- **Lý do**: Product images, zoom functionality, fast loading
- **Lợi ích**: Multiple sizes, watermarks, SEO optimization

#### **5. 🏥 Healthcare & Compliance Systems**
- **Ví dụ**: Medical records, legal documents, financial reports
- **Lý do**: Security, encryption, audit requirements
- **Lợi ích**: HIPAA compliance, secure access, retention policies

### **❌ KHÔNG CẦN DÙNG KHI:**

#### **1. 📄 Simple Static Websites**
- **Ví dụ**: Company brochure, landing pages
- **Lý do**: Ít files, không thay đổi thường xuyên
- **Giải pháp**: Static file serving đủ rồi

#### **2. 🔧 Internal Tools với ít media**
- **Ví dụ**: Admin dashboards, reporting tools
- **Lý do**: Chủ yếu text/data, ít hình ảnh
- **Giải pháp**: Basic file upload đủ

#### **3. 📊 Data-Heavy Applications**
- **Ví dụ**: Analytics platforms, monitoring systems
- **Lý do**: Focus vào data processing, không phải media
- **Giải pháp**: Simple file handling

### **🎯 KẾT LUẬN:**
**Media & File Management phù hợp nhất cho các ứng dụng có nhiều user-generated content, cần processing phức tạp, và yêu cầu performance cao!**

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*