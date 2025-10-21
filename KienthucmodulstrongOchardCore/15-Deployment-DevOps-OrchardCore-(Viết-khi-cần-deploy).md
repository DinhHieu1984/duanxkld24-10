# 🚀 **Deployment & DevOps Patterns trong OrchardCore**

## 🎯 **TỔNG QUAN**

**Deployment & DevOps** trong OrchardCore cung cấp hệ thống triển khai và vận hành mạnh mẽ với khả năng:
- **Deployment Plans**: Automated deployment strategies với step-by-step execution
- **Recipe System**: Infrastructure as Code cho configuration management
- **Docker Support**: Containerization cho scalable deployments
- **CI/CD Integration**: GitHub Actions, Azure DevOps, Jenkins support
- **Environment Management**: Multi-environment deployment với configuration isolation
- **Monitoring & Logging**: Health checks, metrics collection, distributed tracing

---

## 🏗️ **KIẾN TRÚC CORE COMPONENTS**

### **1. 📦 Deployment Plans - Automated Deployment Strategy**

```csharp
// Core deployment plan service
public class DeploymentPlanService : IDeploymentPlanService
{
    private readonly ISession _session;
    private readonly IAuthorizationService _authorizationService;
    private readonly IEnumerable<IDeploymentStepFactory> _deploymentStepFactories;
    private readonly ILogger<DeploymentPlanService> _logger;

    public async Task<DeploymentPlan> CreateDeploymentPlanAsync(string name, string description)
    {
        var deploymentPlan = new DeploymentPlan
        {
            Name = name,
            Description = description,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
            DeploymentSteps = new List<DeploymentStep>()
        };

        _session.Save(deploymentPlan);
        await _session.SaveChangesAsync();

        _logger.LogInformation("Created deployment plan: {PlanName}", name);
        return deploymentPlan;
    }

    public async Task<string> ExecuteDeploymentPlanAsync(
        string planName, 
        IDictionary<string, object> parameters = null)
    {
        var plan = await GetDeploymentPlanAsync(planName);
        if (plan == null)
        {
            throw new InvalidOperationException($"Deployment plan '{planName}' not found");
        }

        var executionId = Guid.NewGuid().ToString();
        var context = new DeploymentPlanExecutionContext
        {
            ExecutionId = executionId,
            Plan = plan,
            Parameters = parameters ?? new Dictionary<string, object>(),
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting deployment plan execution: {PlanName} ({ExecutionId})", 
                planName, executionId);

            // Execute deployment steps in sequence
            foreach (var step in plan.DeploymentSteps.OrderBy(s => s.Position))
            {
                await ExecuteDeploymentStepAsync(step, context);
            }

            context.EndTime = DateTime.UtcNow;
            context.Status = DeploymentStatus.Completed;

            _logger.LogInformation("Completed deployment plan execution: {PlanName} ({ExecutionId}) in {Duration}ms", 
                planName, executionId, (context.EndTime - context.StartTime).TotalMilliseconds);

            return executionId;
        }
        catch (Exception ex)
        {
            context.EndTime = DateTime.UtcNow;
            context.Status = DeploymentStatus.Failed;
            context.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Failed deployment plan execution: {PlanName} ({ExecutionId})", 
                planName, executionId);

            throw;
        }
    }

    private async Task ExecuteDeploymentStepAsync(DeploymentStep step, DeploymentPlanExecutionContext context)
    {
        var stepFactory = _deploymentStepFactories.FirstOrDefault(f => f.Name == step.Type);
        if (stepFactory == null)
        {
            throw new InvalidOperationException($"Deployment step factory '{step.Type}' not found");
        }

        var stepContext = new DeploymentStepContext
        {
            ExecutionId = context.ExecutionId,
            Step = step,
            Parameters = context.Parameters,
            Logger = _logger
        };

        _logger.LogInformation("Executing deployment step: {StepName} ({StepType})", 
            step.Name, step.Type);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await stepFactory.ExecuteAsync(stepContext);
            stopwatch.Stop();

            _logger.LogInformation("Completed deployment step: {StepName} in {Duration}ms", 
                step.Name, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed deployment step: {StepName} after {Duration}ms", 
                step.Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

// Custom deployment step factory
public class DatabaseMigrationDeploymentStepFactory : IDeploymentStepFactory
{
    public string Name => "DatabaseMigration";
    public string DisplayName => "Database Migration";
    public string Description => "Executes database migrations during deployment";

    private readonly IDataMigrationManager _dataMigrationManager;
    private readonly ILogger<DatabaseMigrationDeploymentStepFactory> _logger;

    public DatabaseMigrationDeploymentStepFactory(
        IDataMigrationManager dataMigrationManager,
        ILogger<DatabaseMigrationDeploymentStepFactory> logger)
    {
        _dataMigrationManager = dataMigrationManager;
        _logger = logger;
    }

    public async Task ExecuteAsync(DeploymentStepContext context)
    {
        var settings = JsonSerializer.Deserialize<DatabaseMigrationSettings>(context.Step.Settings);
        
        _logger.LogInformation("Starting database migrations for features: {Features}", 
            string.Join(", ", settings.Features));

        foreach (var feature in settings.Features)
        {
            try
            {
                await _dataMigrationManager.UpdateAsync(feature);
                _logger.LogInformation("Completed database migration for feature: {Feature}", feature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed database migration for feature: {Feature}", feature);
                
                if (settings.FailOnError)
                {
                    throw;
                }
            }
        }
    }
}

// Application deployment step
public class ApplicationDeploymentStepFactory : IDeploymentStepFactory
{
    public string Name => "ApplicationDeployment";
    public string DisplayName => "Application Deployment";
    public string Description => "Deploys application files and configurations";

    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApplicationDeploymentStepFactory> _logger;

    public async Task ExecuteAsync(DeploymentStepContext context)
    {
        var settings = JsonSerializer.Deserialize<ApplicationDeploymentSettings>(context.Step.Settings);
        
        // 1. Backup current application
        if (settings.CreateBackup)
        {
            await CreateApplicationBackupAsync(settings.BackupPath);
        }

        // 2. Deploy new application files
        await DeployApplicationFilesAsync(settings.SourcePath, settings.TargetPath);

        // 3. Update configuration files
        await UpdateConfigurationFilesAsync(settings.ConfigurationUpdates);

        // 4. Restart application if required
        if (settings.RestartApplication)
        {
            await RestartApplicationAsync();
        }

        // 5. Run health checks
        if (settings.RunHealthChecks)
        {
            await RunPostDeploymentHealthChecksAsync();
        }
    }

    private async Task CreateApplicationBackupAsync(string backupPath)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupDirectory = Path.Combine(backupPath, $"backup_{timestamp}");
        
        Directory.CreateDirectory(backupDirectory);
        
        // Copy current application files
        await CopyDirectoryAsync(_webHostEnvironment.ContentRootPath, backupDirectory);
        
        _logger.LogInformation("Created application backup at: {BackupPath}", backupDirectory);
    }

    private async Task DeployApplicationFilesAsync(string sourcePath, string targetPath)
    {
        if (Directory.Exists(sourcePath))
        {
            await CopyDirectoryAsync(sourcePath, targetPath, overwrite: true);
            _logger.LogInformation("Deployed application files from {Source} to {Target}", 
                sourcePath, targetPath);
        }
    }

    private async Task RestartApplicationAsync()
    {
        // Create a file that triggers application restart
        var restartFile = Path.Combine(_webHostEnvironment.ContentRootPath, "app_offline.htm");
        await File.WriteAllTextAsync(restartFile, "<html><body>Application is restarting...</body></html>");
        
        // Wait a moment then remove the file
        await Task.Delay(2000);
        
        if (File.Exists(restartFile))
        {
            File.Delete(restartFile);
        }
        
        _logger.LogInformation("Application restart triggered");
    }
}
```

### **2. 🍳 Recipe System - Infrastructure as Code**

```csharp
// Recipe executor với advanced features
public class AdvancedRecipeExecutor : IRecipeExecutor
{
    private readonly IRecipeStepHandler _recipeStepHandler;
    private readonly IRecipeEnvironmentProvider _environmentProvider;
    private readonly ILogger<AdvancedRecipeExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public async Task<string> ExecuteAsync(
        string executionId, 
        RecipeDescriptor recipeDescriptor, 
        IDictionary<string, object> environment, 
        CancellationToken cancellationToken)
    {
        var context = new RecipeExecutionContext
        {
            ExecutionId = executionId,
            Recipe = recipeDescriptor,
            Environment = environment,
            StartTime = DateTime.UtcNow,
            CancellationToken = cancellationToken
        };

        try
        {
            _logger.LogInformation("Starting recipe execution: {RecipeName} ({ExecutionId})", 
                recipeDescriptor.Name, executionId);

            // Load and parse recipe
            var recipe = await LoadRecipeAsync(recipeDescriptor);
            
            // Validate recipe steps
            await ValidateRecipeStepsAsync(recipe, context);

            // Execute recipe steps
            foreach (var step in recipe.Steps)
            {
                await ExecuteRecipeStepAsync(step, context);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Recipe execution cancelled: {RecipeName} ({ExecutionId})", 
                        recipeDescriptor.Name, executionId);
                    break;
                }
            }

            context.EndTime = DateTime.UtcNow;
            context.Status = RecipeExecutionStatus.Completed;

            _logger.LogInformation("Completed recipe execution: {RecipeName} ({ExecutionId}) in {Duration}ms", 
                recipeDescriptor.Name, executionId, (context.EndTime - context.StartTime).TotalMilliseconds);

            return executionId;
        }
        catch (Exception ex)
        {
            context.EndTime = DateTime.UtcNow;
            context.Status = RecipeExecutionStatus.Failed;
            context.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Failed recipe execution: {RecipeName} ({ExecutionId})", 
                recipeDescriptor.Name, executionId);

            throw;
        }
    }

    private async Task ExecuteRecipeStepAsync(RecipeStep step, RecipeExecutionContext context)
    {
        var stepHandler = _serviceProvider.GetService<IRecipeStepHandler>(step.Name);
        if (stepHandler == null)
        {
            throw new InvalidOperationException($"Recipe step handler '{step.Name}' not found");
        }

        var stepContext = new RecipeStepContext
        {
            ExecutionId = context.ExecutionId,
            Step = step,
            Environment = context.Environment,
            CancellationToken = context.CancellationToken
        };

        _logger.LogInformation("Executing recipe step: {StepName}", step.Name);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await stepHandler.ExecuteAsync(stepContext);
            stopwatch.Stop();

            _logger.LogInformation("Completed recipe step: {StepName} in {Duration}ms", 
                step.Name, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed recipe step: {StepName} after {Duration}ms", 
                step.Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

// Custom recipe step handlers
public class FeatureRecipeStepHandler : IRecipeStepHandler
{
    public string Name => "feature";

    private readonly IShellFeaturesManager _shellFeaturesManager;
    private readonly ILogger<FeatureRecipeStepHandler> _logger;

    public async Task ExecuteAsync(RecipeStepContext context)
    {
        var features = context.Step.Data.ToObject<FeatureStepModel>();
        
        // Enable features
        if (features.Enable?.Any() == true)
        {
            _logger.LogInformation("Enabling features: {Features}", string.Join(", ", features.Enable));
            
            var enabledFeatures = await _shellFeaturesManager.EnableFeaturesAsync(
                features.Enable.Select(f => new ShellFeature(f)).ToArray());
            
            foreach (var feature in enabledFeatures)
            {
                _logger.LogInformation("Enabled feature: {FeatureName}", feature.Id);
            }
        }

        // Disable features
        if (features.Disable?.Any() == true)
        {
            _logger.LogInformation("Disabling features: {Features}", string.Join(", ", features.Disable));
            
            var disabledFeatures = await _shellFeaturesManager.DisableFeaturesAsync(
                features.Disable.Select(f => new ShellFeature(f)).ToArray());
            
            foreach (var feature in disabledFeatures)
            {
                _logger.LogInformation("Disabled feature: {FeatureName}", feature.Id);
            }
        }
    }
}

public class ContentRecipeStepHandler : IRecipeStepHandler
{
    public string Name => "content";

    private readonly IContentManager _contentManager;
    private readonly IContentItemIdGenerator _contentItemIdGenerator;
    private readonly ILogger<ContentRecipeStepHandler> _logger;

    public async Task ExecuteAsync(RecipeStepContext context)
    {
        var contentItems = context.Step.Data.ToObject<ContentItem[]>();
        
        foreach (var contentItem in contentItems)
        {
            // Generate content item ID if not provided
            if (string.IsNullOrEmpty(contentItem.ContentItemId))
            {
                contentItem.ContentItemId = _contentItemIdGenerator.GenerateUniqueId(contentItem);
            }

            // Check if content item already exists
            var existingItem = await _contentManager.GetAsync(contentItem.ContentItemId);
            
            if (existingItem != null)
            {
                _logger.LogInformation("Updating existing content item: {ContentItemId} ({ContentType})", 
                    contentItem.ContentItemId, contentItem.ContentType);
                
                // Update existing content item
                existingItem.Merge(contentItem);
                await _contentManager.UpdateAsync(existingItem);
            }
            else
            {
                _logger.LogInformation("Creating new content item: {ContentItemId} ({ContentType})", 
                    contentItem.ContentItemId, contentItem.ContentType);
                
                // Create new content item
                await _contentManager.CreateAsync(contentItem);
            }

            // Publish if required
            if (contentItem.Published)
            {
                await _contentManager.PublishAsync(contentItem);
                _logger.LogInformation("Published content item: {ContentItemId}", contentItem.ContentItemId);
            }
        }
    }
}

// Environment-specific recipe step
public class EnvironmentConfigRecipeStepHandler : IRecipeStepHandler
{
    public string Name => "environment-config";

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EnvironmentConfigRecipeStepHandler> _logger;

    public async Task ExecuteAsync(RecipeStepContext context)
    {
        var config = context.Step.Data.ToObject<EnvironmentConfigModel>();
        
        // Apply environment-specific configurations
        var environmentName = _environment.EnvironmentName;
        
        if (config.Environments.TryGetValue(environmentName, out var envConfig))
        {
            _logger.LogInformation("Applying configuration for environment: {Environment}", environmentName);
            
            // Update app settings
            if (envConfig.AppSettings?.Any() == true)
            {
                await UpdateAppSettingsAsync(envConfig.AppSettings);
            }

            // Update connection strings
            if (envConfig.ConnectionStrings?.Any() == true)
            {
                await UpdateConnectionStringsAsync(envConfig.ConnectionStrings);
            }

            // Set environment variables
            if (envConfig.EnvironmentVariables?.Any() == true)
            {
                SetEnvironmentVariables(envConfig.EnvironmentVariables);
            }
        }
        else
        {
            _logger.LogWarning("No configuration found for environment: {Environment}", environmentName);
        }
    }

    private async Task UpdateAppSettingsAsync(Dictionary<string, object> appSettings)
    {
        var appSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        
        if (File.Exists(appSettingsPath))
        {
            var json = await File.ReadAllTextAsync(appSettingsPath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            foreach (var setting in appSettings)
            {
                settings[setting.Key] = setting.Value;
                _logger.LogInformation("Updated app setting: {Key}", setting.Key);
            }
            
            var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(appSettingsPath, updatedJson);
        }
    }
}
```

### **3. 🐳 Docker Integration**

```csharp
// Docker deployment service
public class DockerDeploymentService : IDockerDeploymentService
{
    private readonly ILogger<DockerDeploymentService> _logger;
    private readonly IConfiguration _configuration;

    public async Task<DockerDeploymentResult> DeployAsync(DockerDeploymentOptions options)
    {
        var result = new DockerDeploymentResult
        {
            StartTime = DateTime.UtcNow,
            DeploymentId = Guid.NewGuid().ToString()
        };

        try
        {
            _logger.LogInformation("Starting Docker deployment: {DeploymentId}", result.DeploymentId);

            // 1. Build Docker image
            if (options.BuildImage)
            {
                await BuildDockerImageAsync(options);
            }

            // 2. Push to registry
            if (options.PushToRegistry)
            {
                await PushToRegistryAsync(options);
            }

            // 3. Deploy to target environment
            await DeployToEnvironmentAsync(options);

            // 4. Run health checks
            if (options.RunHealthChecks)
            {
                await RunHealthChecksAsync(options);
            }

            result.EndTime = DateTime.UtcNow;
            result.Status = DeploymentStatus.Completed;
            result.Success = true;

            _logger.LogInformation("Completed Docker deployment: {DeploymentId} in {Duration}ms", 
                result.DeploymentId, (result.EndTime - result.StartTime).TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.Status = DeploymentStatus.Failed;
            result.Success = false;
            result.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Failed Docker deployment: {DeploymentId}", result.DeploymentId);
            throw;
        }
    }

    private async Task BuildDockerImageAsync(DockerDeploymentOptions options)
    {
        _logger.LogInformation("Building Docker image: {ImageName}:{Tag}", options.ImageName, options.Tag);

        var buildArgs = new List<string>
        {
            "docker", "build",
            "-t", $"{options.ImageName}:{options.Tag}",
            "-f", options.DockerfilePath
        };

        // Add build arguments
        foreach (var buildArg in options.BuildArgs)
        {
            buildArgs.AddRange(new[] { "--build-arg", $"{buildArg.Key}={buildArg.Value}" });
        }

        // Add target platform
        if (!string.IsNullOrEmpty(options.Platform))
        {
            buildArgs.AddRange(new[] { "--platform", options.Platform });
        }

        buildArgs.Add(options.BuildContext);

        var processInfo = new ProcessStartInfo
        {
            FileName = buildArgs[0],
            Arguments = string.Join(" ", buildArgs.Skip(1)),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(processInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker build failed: {error}");
        }

        _logger.LogInformation("Docker image built successfully: {ImageName}:{Tag}", options.ImageName, options.Tag);
    }

    private async Task PushToRegistryAsync(DockerDeploymentOptions options)
    {
        _logger.LogInformation("Pushing Docker image to registry: {Registry}", options.Registry);

        // Login to registry if credentials provided
        if (!string.IsNullOrEmpty(options.RegistryUsername))
        {
            await ExecuteDockerCommandAsync("login", options.Registry, 
                "-u", options.RegistryUsername, 
                "-p", options.RegistryPassword);
        }

        // Tag image for registry
        var registryImage = $"{options.Registry}/{options.ImageName}:{options.Tag}";
        await ExecuteDockerCommandAsync("tag", $"{options.ImageName}:{options.Tag}", registryImage);

        // Push image
        await ExecuteDockerCommandAsync("push", registryImage);

        _logger.LogInformation("Docker image pushed successfully: {RegistryImage}", registryImage);
    }

    private async Task DeployToEnvironmentAsync(DockerDeploymentOptions options)
    {
        switch (options.DeploymentTarget.ToLower())
        {
            case "docker-compose":
                await DeployWithDockerComposeAsync(options);
                break;
            case "kubernetes":
                await DeployToKubernetesAsync(options);
                break;
            case "azure-container-instances":
                await DeployToAzureContainerInstancesAsync(options);
                break;
            case "aws-ecs":
                await DeployToAwsEcsAsync(options);
                break;
            default:
                throw new NotSupportedException($"Deployment target '{options.DeploymentTarget}' is not supported");
        }
    }

    private async Task DeployWithDockerComposeAsync(DockerDeploymentOptions options)
    {
        _logger.LogInformation("Deploying with Docker Compose: {ComposeFile}", options.DockerComposeFile);

        // Update docker-compose.yml with new image
        await UpdateDockerComposeImageAsync(options.DockerComposeFile, options.ServiceName, 
            $"{options.ImageName}:{options.Tag}");

        // Deploy with docker-compose
        await ExecuteDockerComposeCommandAsync(options.DockerComposeFile, "up", "-d", options.ServiceName);

        _logger.LogInformation("Docker Compose deployment completed");
    }

    private async Task DeployToKubernetesAsync(DockerDeploymentOptions options)
    {
        _logger.LogInformation("Deploying to Kubernetes: {Namespace}", options.KubernetesNamespace);

        // Update Kubernetes deployment manifest
        await UpdateKubernetesManifestAsync(options.KubernetesManifestPath, 
            $"{options.Registry}/{options.ImageName}:{options.Tag}");

        // Apply Kubernetes manifest
        await ExecuteKubectlCommandAsync("apply", "-f", options.KubernetesManifestPath, 
            "-n", options.KubernetesNamespace);

        // Wait for rollout to complete
        await ExecuteKubectlCommandAsync("rollout", "status", 
            $"deployment/{options.ServiceName}", "-n", options.KubernetesNamespace);

        _logger.LogInformation("Kubernetes deployment completed");
    }

    private async Task RunHealthChecksAsync(DockerDeploymentOptions options)
    {
        _logger.LogInformation("Running post-deployment health checks");

        var healthCheckUrl = $"{options.ApplicationUrl}/health";
        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(30);

        using var httpClient = new HttpClient();
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(healthCheckUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Health check passed: {Url}", healthCheckUrl);
                    return;
                }
                
                _logger.LogWarning("Health check failed (attempt {Attempt}/{MaxRetries}): {StatusCode}", 
                    i + 1, maxRetries, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check error (attempt {Attempt}/{MaxRetries})", 
                    i + 1, maxRetries);
            }

            if (i < maxRetries - 1)
            {
                await Task.Delay(retryDelay);
            }
        }

        throw new InvalidOperationException($"Health checks failed after {maxRetries} attempts");
    }
}
```

---

## 🔧 **CI/CD INTEGRATION PATTERNS**

### **1. 🔄 GitHub Actions Workflow**

```yaml
# .github/workflows/deploy.yml
name: Deploy to Production

on:
  push:
    branches: [ main ]
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        default: 'staging'
        type: choice
        options:
        - staging
        - production

env:
  DOTNET_VERSION: '8.0.x'
  NODE_VERSION: '18'
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    outputs:
      image-tag: ${{ steps.meta.outputs.tags }}
      image-digest: ${{ steps.build.outputs.digest }}
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: ${{ env.NODE_VERSION }}
        cache: 'npm'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build application
      run: dotnet build --no-restore --configuration Release
      
    - name: Run unit tests
      run: dotnet test --no-build --configuration Release --verbosity normal
      
    - name: Run integration tests
      run: |
        cd test/OrchardCore.Tests.Functional
        npm install
        npm run test
        
    - name: Setup Docker Buildx
      uses: docker/setup-buildx-action@v3
      
    - name: Login to Container Registry
      uses: docker/login-action@v3
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
        
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v5
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=ref,event=pr
          type=sha,prefix={{branch}}-
          type=raw,value=latest,enable={{is_default_branch}}
          
    - name: Build and push Docker image
      id: build
      uses: docker/build-push-action@v5
      with:
        context: .
        platforms: linux/amd64,linux/arm64
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max
        build-args: |
          BUILDKIT_INLINE_CACHE=1
          
  deploy-staging:
    needs: build-and-test
    runs-on: ubuntu-latest
    environment: staging
    if: github.ref == 'refs/heads/main' || github.event.inputs.environment == 'staging'
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup kubectl
      uses: azure/setup-kubectl@v3
      with:
        version: 'v1.28.0'
        
    - name: Configure kubectl
      run: |
        echo "${{ secrets.KUBE_CONFIG_STAGING }}" | base64 -d > kubeconfig
        export KUBECONFIG=kubeconfig
        kubectl config current-context
        
    - name: Deploy to staging
      run: |
        export KUBECONFIG=kubeconfig
        envsubst < k8s/staging/deployment.yaml | kubectl apply -f -
        kubectl rollout status deployment/orchardcore-app -n staging
      env:
        IMAGE_TAG: ${{ needs.build-and-test.outputs.image-tag }}
        
    - name: Run smoke tests
      run: |
        # Wait for deployment to be ready
        sleep 30
        
        # Run smoke tests against staging environment
        curl -f ${{ secrets.STAGING_URL }}/health || exit 1
        curl -f ${{ secrets.STAGING_URL }}/api/health || exit 1
        
    - name: Notify deployment
      uses: 8398a7/action-slack@v3
      with:
        status: ${{ job.status }}
        channel: '#deployments'
        text: 'Staging deployment completed: ${{ needs.build-and-test.outputs.image-tag }}'
      env:
        SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
        
  deploy-production:
    needs: [build-and-test, deploy-staging]
    runs-on: ubuntu-latest
    environment: production
    if: github.ref == 'refs/heads/main' || github.event.inputs.environment == 'production'
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup kubectl
      uses: azure/setup-kubectl@v3
      with:
        version: 'v1.28.0'
        
    - name: Configure kubectl
      run: |
        echo "${{ secrets.KUBE_CONFIG_PRODUCTION }}" | base64 -d > kubeconfig
        export KUBECONFIG=kubeconfig
        kubectl config current-context
        
    - name: Create deployment backup
      run: |
        export KUBECONFIG=kubeconfig
        kubectl get deployment orchardcore-app -n production -o yaml > backup-$(date +%Y%m%d-%H%M%S).yaml
        
    - name: Deploy to production
      run: |
        export KUBECONFIG=kubeconfig
        envsubst < k8s/production/deployment.yaml | kubectl apply -f -
        kubectl rollout status deployment/orchardcore-app -n production --timeout=600s
      env:
        IMAGE_TAG: ${{ needs.build-and-test.outputs.image-tag }}
        
    - name: Run production health checks
      run: |
        # Wait for deployment to be ready
        sleep 60
        
        # Run comprehensive health checks
        curl -f ${{ secrets.PRODUCTION_URL }}/health || exit 1
        curl -f ${{ secrets.PRODUCTION_URL }}/api/health || exit 1
        
        # Check database connectivity
        curl -f ${{ secrets.PRODUCTION_URL }}/api/health/database || exit 1
        
        # Check external dependencies
        curl -f ${{ secrets.PRODUCTION_URL }}/api/health/dependencies || exit 1
        
    - name: Update deployment status
      run: |
        # Update deployment tracking system
        curl -X POST "${{ secrets.DEPLOYMENT_TRACKER_URL }}/deployments" \
          -H "Authorization: Bearer ${{ secrets.DEPLOYMENT_TRACKER_TOKEN }}" \
          -H "Content-Type: application/json" \
          -d '{
            "environment": "production",
            "version": "${{ needs.build-and-test.outputs.image-tag }}",
            "status": "completed",
            "deployedAt": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'",
            "deployedBy": "${{ github.actor }}"
          }'
          
    - name: Notify production deployment
      uses: 8398a7/action-slack@v3
      with:
        status: ${{ job.status }}
        channel: '#production'
        text: '🚀 Production deployment completed: ${{ needs.build-and-test.outputs.image-tag }}'
      env:
        SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
```

### **2. 🔧 Azure DevOps Pipeline**

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
    - main
    - develop
  paths:
    exclude:
    - README.md
    - docs/*

variables:
  buildConfiguration: 'Release'
  dotnetVersion: '8.0.x'
  nodeVersion: '18.x'
  vmImageName: 'ubuntu-latest'
  
  # Container registry variables
  containerRegistry: 'myregistry.azurecr.io'
  imageRepository: 'orchardcore-app'
  dockerfilePath: '$(Build.SourcesDirectory)/Dockerfile'
  tag: '$(Build.BuildId)'

stages:
- stage: Build
  displayName: Build and Test
  jobs:
  - job: Build
    displayName: Build
    pool:
      vmImage: $(vmImageName)
    
    steps:
    - task: UseDotNet@2
      displayName: 'Setup .NET SDK'
      inputs:
        packageType: 'sdk'
        version: $(dotnetVersion)
        
    - task: NodeTool@0
      displayName: 'Setup Node.js'
      inputs:
        versionSpec: $(nodeVersion)
        
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        projects: '**/*.csproj'
        
    - task: DotNetCoreCLI@2
      displayName: 'Build application'
      inputs:
        command: 'build'
        projects: '**/*.csproj'
        arguments: '--configuration $(buildConfiguration) --no-restore'
        
    - task: DotNetCoreCLI@2
      displayName: 'Run unit tests'
      inputs:
        command: 'test'
        projects: '**/OrchardCore.Tests.csproj'
        arguments: '--configuration $(buildConfiguration) --no-build --collect:"XPlat Code Coverage" --logger trx --results-directory $(Agent.TempDirectory)'
        
    - task: PublishTestResults@2
      displayName: 'Publish test results'
      condition: succeededOrFailed()
      inputs:
        testResultsFormat: 'VSTest'
        testResultsFiles: '**/*.trx'
        searchFolder: '$(Agent.TempDirectory)'
        
    - task: PublishCodeCoverageResults@1
      displayName: 'Publish code coverage'
      inputs:
        codeCoverageTool: 'Cobertura'
        summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
        
    - task: Docker@2
      displayName: 'Build Docker image'
      inputs:
        containerRegistry: 'MyContainerRegistry'
        repository: $(imageRepository)
        command: 'build'
        Dockerfile: $(dockerfilePath)
        tags: |
          $(tag)
          latest
          
    - task: Docker@2
      displayName: 'Push Docker image'
      inputs:
        containerRegistry: 'MyContainerRegistry'
        repository: $(imageRepository)
        command: 'push'
        tags: |
          $(tag)
          latest

- stage: DeployStaging
  displayName: Deploy to Staging
  dependsOn: Build
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/develop'))
  
  jobs:
  - deployment: DeployStaging
    displayName: Deploy to Staging
    pool:
      vmImage: $(vmImageName)
    environment: 'staging'
    
    strategy:
      runOnce:
        deploy:
          steps:
          - task: KubernetesManifest@0
            displayName: 'Deploy to Kubernetes'
            inputs:
              action: 'deploy'
              kubernetesServiceConnection: 'staging-k8s'
              namespace: 'staging'
              manifests: |
                $(Pipeline.Workspace)/k8s/staging/deployment.yaml
                $(Pipeline.Workspace)/k8s/staging/service.yaml
                $(Pipeline.Workspace)/k8s/staging/ingress.yaml
              containers: '$(containerRegistry)/$(imageRepository):$(tag)'
              
          - task: PowerShell@2
            displayName: 'Run smoke tests'
            inputs:
              targetType: 'inline'
              script: |
                # Wait for deployment
                Start-Sleep -Seconds 30
                
                # Test health endpoints
                $healthUrl = "https://staging.myapp.com/health"
                $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing
                
                if ($response.StatusCode -ne 200) {
                  throw "Health check failed: $($response.StatusCode)"
                }
                
                Write-Host "Staging deployment health check passed"

- stage: DeployProduction
  displayName: Deploy to Production
  dependsOn: [Build, DeployStaging]
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  
  jobs:
  - deployment: DeployProduction
    displayName: Deploy to Production
    pool:
      vmImage: $(vmImageName)
    environment: 'production'
    
    strategy:
      runOnce:
        deploy:
          steps:
          - task: KubernetesManifest@0
            displayName: 'Create backup'
            inputs:
              action: 'deploy'
              kubernetesServiceConnection: 'production-k8s'
              namespace: 'production'
              manifests: '$(Pipeline.Workspace)/k8s/backup/backup-job.yaml'
              
          - task: KubernetesManifest@0
            displayName: 'Deploy to Production'
            inputs:
              action: 'deploy'
              kubernetesServiceConnection: 'production-k8s'
              namespace: 'production'
              manifests: |
                $(Pipeline.Workspace)/k8s/production/deployment.yaml
                $(Pipeline.Workspace)/k8s/production/service.yaml
                $(Pipeline.Workspace)/k8s/production/ingress.yaml
              containers: '$(containerRegistry)/$(imageRepository):$(tag)'
              
          - task: PowerShell@2
            displayName: 'Run production health checks'
            inputs:
              targetType: 'inline'
              script: |
                # Comprehensive health checks
                $baseUrl = "https://myapp.com"
                $endpoints = @("/health", "/api/health", "/api/health/database")
                
                foreach ($endpoint in $endpoints) {
                  $url = "$baseUrl$endpoint"
                  $response = Invoke-WebRequest -Uri $url -UseBasicParsing
                  
                  if ($response.StatusCode -ne 200) {
                    throw "Health check failed for $url: $($response.StatusCode)"
                  }
                  
                  Write-Host "Health check passed: $url"
                }
                
          - task: PowerShell@2
            displayName: 'Send deployment notification'
            inputs:
              targetType: 'inline'
              script: |
                $webhookUrl = "$(SLACK_WEBHOOK_URL)"
                $message = @{
                  text = "🚀 Production deployment completed: $(imageRepository):$(tag)"
                  channel = "#production"
                } | ConvertTo-Json
                
                Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $message -ContentType "application/json"
```

---

## 🔧 **ENVIRONMENT MANAGEMENT**

### **1. 🌍 Multi-Environment Configuration**

```csharp
// Environment-specific configuration service
public class EnvironmentConfigurationService : IEnvironmentConfigurationService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnvironmentConfigurationService> _logger;

    public async Task<EnvironmentConfiguration> GetEnvironmentConfigurationAsync()
    {
        var environmentName = _environment.EnvironmentName;
        
        var config = new EnvironmentConfiguration
        {
            EnvironmentName = environmentName,
            IsProduction = _environment.IsProduction(),
            IsStaging = _environment.IsStaging(),
            IsDevelopment = _environment.IsDevelopment(),
            
            // Database configuration
            DatabaseConfiguration = GetDatabaseConfiguration(),
            
            // Cache configuration
            CacheConfiguration = GetCacheConfiguration(),
            
            // Logging configuration
            LoggingConfiguration = GetLoggingConfiguration(),
            
            // Security configuration
            SecurityConfiguration = GetSecurityConfiguration(),
            
            // Feature flags
            FeatureFlags = await GetFeatureFlagsAsync(),
            
            // External services
            ExternalServices = GetExternalServicesConfiguration()
        };

        _logger.LogInformation("Loaded configuration for environment: {Environment}", environmentName);
        return config;
    }

    private DatabaseConfiguration GetDatabaseConfiguration()
    {
        return new DatabaseConfiguration
        {
            ConnectionString = _configuration.GetConnectionString("Default"),
            Provider = _configuration["Database:Provider"] ?? "SqlServer",
            CommandTimeout = _configuration.GetValue<int>("Database:CommandTimeout", 30),
            EnableRetryOnFailure = _configuration.GetValue<bool>("Database:EnableRetryOnFailure", true),
            MaxRetryCount = _configuration.GetValue<int>("Database:MaxRetryCount", 3),
            EnableSensitiveDataLogging = _configuration.GetValue<bool>("Database:EnableSensitiveDataLogging", false)
        };
    }

    private CacheConfiguration GetCacheConfiguration()
    {
        var cacheType = _configuration["Cache:Type"] ?? "Memory";
        
        return new CacheConfiguration
        {
            Type = cacheType,
            ConnectionString = _configuration.GetConnectionString("Redis"),
            DefaultExpiration = _configuration.GetValue<TimeSpan>("Cache:DefaultExpiration", TimeSpan.FromMinutes(30)),
            EnableCompression = _configuration.GetValue<bool>("Cache:EnableCompression", true),
            KeyPrefix = _configuration["Cache:KeyPrefix"] ?? $"oc_{_environment.EnvironmentName}_"
        };
    }

    private LoggingConfiguration GetLoggingConfiguration()
    {
        return new LoggingConfiguration
        {
            LogLevel = _configuration["Logging:LogLevel:Default"] ?? "Information",
            EnableStructuredLogging = _configuration.GetValue<bool>("Logging:EnableStructuredLogging", true),
            EnableFileLogging = _configuration.GetValue<bool>("Logging:EnableFileLogging", false),
            LogFilePath = _configuration["Logging:LogFilePath"] ?? "logs/app.log",
            
            // Application Insights
            ApplicationInsightsKey = _configuration["ApplicationInsights:InstrumentationKey"],
            
            // Serilog sinks
            SerilogSinks = _configuration.GetSection("Serilog:WriteTo").Get<List<SerilogSinkConfiguration>>() ?? new()
        };
    }

    private SecurityConfiguration GetSecurityConfiguration()
    {
        return new SecurityConfiguration
        {
            RequireHttps = _configuration.GetValue<bool>("Security:RequireHttps", _environment.IsProduction()),
            EnableHsts = _configuration.GetValue<bool>("Security:EnableHsts", _environment.IsProduction()),
            HstsMaxAge = _configuration.GetValue<TimeSpan>("Security:HstsMaxAge", TimeSpan.FromDays(365)),
            
            // Authentication
            JwtSecretKey = _configuration["Authentication:Jwt:SecretKey"],
            JwtIssuer = _configuration["Authentication:Jwt:Issuer"],
            JwtAudience = _configuration["Authentication:Jwt:Audience"],
            JwtExpiration = _configuration.GetValue<TimeSpan>("Authentication:Jwt:Expiration", TimeSpan.FromHours(24)),
            
            // CORS
            CorsOrigins = _configuration.GetSection("Security:Cors:Origins").Get<string[]>() ?? Array.Empty<string>(),
            
            // Rate limiting
            EnableRateLimiting = _configuration.GetValue<bool>("Security:RateLimiting:Enabled", true),
            RateLimitRequests = _configuration.GetValue<int>("Security:RateLimiting:Requests", 100),
            RateLimitWindow = _configuration.GetValue<TimeSpan>("Security:RateLimiting:Window", TimeSpan.FromMinutes(1))
        };
    }

    private async Task<Dictionary<string, bool>> GetFeatureFlagsAsync()
    {
        var featureFlags = new Dictionary<string, bool>();
        
        // Load from configuration
        var configFlags = _configuration.GetSection("FeatureFlags").Get<Dictionary<string, bool>>();
        if (configFlags != null)
        {
            foreach (var flag in configFlags)
            {
                featureFlags[flag.Key] = flag.Value;
            }
        }

        // Load from external feature flag service (e.g., Azure App Configuration, LaunchDarkly)
        var externalFlags = await LoadExternalFeatureFlagsAsync();
        foreach (var flag in externalFlags)
        {
            featureFlags[flag.Key] = flag.Value;
        }

        return featureFlags;
    }

    private ExternalServicesConfiguration GetExternalServicesConfiguration()
    {
        return new ExternalServicesConfiguration
        {
            // Email service
            EmailService = new EmailServiceConfiguration
            {
                Provider = _configuration["ExternalServices:Email:Provider"] ?? "SendGrid",
                ApiKey = _configuration["ExternalServices:Email:ApiKey"],
                FromEmail = _configuration["ExternalServices:Email:FromEmail"],
                FromName = _configuration["ExternalServices:Email:FromName"]
            },
            
            // Storage service
            StorageService = new StorageServiceConfiguration
            {
                Provider = _configuration["ExternalServices:Storage:Provider"] ?? "Local",
                ConnectionString = _configuration["ExternalServices:Storage:ConnectionString"],
                ContainerName = _configuration["ExternalServices:Storage:ContainerName"] ?? "media",
                CdnUrl = _configuration["ExternalServices:Storage:CdnUrl"]
            },
            
            // Search service
            SearchService = new SearchServiceConfiguration
            {
                Provider = _configuration["ExternalServices:Search:Provider"] ?? "Lucene",
                ConnectionString = _configuration["ExternalServices:Search:ConnectionString"],
                IndexPrefix = _configuration["ExternalServices:Search:IndexPrefix"] ?? $"oc_{_environment.EnvironmentName}_"
            }
        };
    }
}

// Environment-specific startup configuration
public class EnvironmentStartup : StartupBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public EnvironmentStartup(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        // Environment-specific service registration
        if (_environment.IsDevelopment())
        {
            ConfigureDevelopmentServices(services);
        }
        else if (_environment.IsStaging())
        {
            ConfigureStagingServices(services);
        }
        else if (_environment.IsProduction())
        {
            ConfigureProductionServices(services);
        }

        // Common services
        services.AddScoped<IEnvironmentConfigurationService, EnvironmentConfigurationService>();
    }

    private void ConfigureDevelopmentServices(IServiceCollection services)
    {
        // Development-specific services
        services.AddDatabaseDeveloperPageExceptionFilter();
        
        // Use in-memory cache for development
        services.AddMemoryCache();
        
        // Use local file storage
        services.Configure<MediaOptions>(options =>
        {
            options.AssetsUseCdn = false;
            options.AssetsVersionHash = string.Empty;
        });
        
        // Enable detailed errors
        services.Configure<OrchardCoreOptions>(options =>
        {
            options.UseDeveloperExceptionPage = true;
        });
    }

    private void ConfigureStagingServices(IServiceCollection services)
    {
        // Staging-specific services
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = _configuration.GetConnectionString("Redis");
        });
        
        // Use Azure Blob Storage for staging
        services.Configure<AzureBlobStorageOptions>(options =>
        {
            options.ConnectionString = _configuration.GetConnectionString("AzureStorage");
            options.ContainerName = "staging-media";
        });
        
        // Enable Application Insights
        services.AddApplicationInsightsTelemetry(_configuration["ApplicationInsights:InstrumentationKey"]);
    }

    private void ConfigureProductionServices(IServiceCollection services)
    {
        // Production-specific services
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = _configuration.GetConnectionString("Redis");
            options.InstanceName = "OrchardCore_Production";
        });
        
        // Use Azure Blob Storage with CDN
        services.Configure<AzureBlobStorageOptions>(options =>
        {
            options.ConnectionString = _configuration.GetConnectionString("AzureStorage");
            options.ContainerName = "production-media";
            options.BasePath = _configuration["Storage:CdnUrl"];
        });
        
        // Enable comprehensive monitoring
        services.AddApplicationInsightsTelemetry(_configuration["ApplicationInsights:InstrumentationKey"]);
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>()
            .AddRedis(_configuration.GetConnectionString("Redis"))
            .AddAzureBlobStorage(_configuration.GetConnectionString("AzureStorage"));
        
        // Enable security headers
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        if (_environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // Environment-specific middleware
        if (_environment.IsProduction())
        {
            // Production security middleware
            app.UseSecurityHeaders();
            app.UseRateLimiting();
        }

        // Health check endpoints
        routes.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        
        routes.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        
        routes.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    }
}
```

---

## 📊 **MONITORING & OBSERVABILITY**

### **1. 📈 Application Monitoring Service**

```csharp
// Comprehensive monitoring service
public class ApplicationMonitoringService : IApplicationMonitoringService
{
    private readonly IMetricsCollector _metricsCollector;
    private readonly ILogger<ApplicationMonitoringService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public async Task<HealthReport> GetApplicationHealthAsync()
    {
        var healthReport = new HealthReport
        {
            Timestamp = DateTime.UtcNow,
            Environment = _configuration["Environment"] ?? "Unknown",
            Version = GetApplicationVersion(),
            Checks = new List<HealthCheck>()
        };

        // Database health check
        var dbHealth = await CheckDatabaseHealthAsync();
        healthReport.Checks.Add(dbHealth);

        // Cache health check
        var cacheHealth = await CheckCacheHealthAsync();
        healthReport.Checks.Add(cacheHealth);

        // External services health check
        var externalServicesHealth = await CheckExternalServicesHealthAsync();
        healthReport.Checks.AddRange(externalServicesHealth);

        // System resources health check
        var systemHealth = await CheckSystemResourcesAsync();
        healthReport.Checks.Add(systemHealth);

        // Application-specific health checks
        var appHealth = await CheckApplicationHealthAsync();
        healthReport.Checks.AddRange(appHealth);

        // Overall status
        healthReport.Status = healthReport.Checks.All(c => c.Status == HealthStatus.Healthy) 
            ? HealthStatus.Healthy 
            : healthReport.Checks.Any(c => c.Status == HealthStatus.Unhealthy)
                ? HealthStatus.Unhealthy
                : HealthStatus.Degraded;

        // Collect metrics
        _metricsCollector.Gauge("application.health.status", (int)healthReport.Status);
        _metricsCollector.Gauge("application.health.checks.total", healthReport.Checks.Count);
        _metricsCollector.Gauge("application.health.checks.healthy", 
            healthReport.Checks.Count(c => c.Status == HealthStatus.Healthy));

        return healthReport;
    }

    private async Task<HealthCheck> CheckDatabaseHealthAsync()
    {
        var healthCheck = new HealthCheck
        {
            Name = "Database",
            Category = "Infrastructure"
        };

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var stopwatch = Stopwatch.StartNew();
            await dbContext.Database.CanConnectAsync();
            stopwatch.Stop();

            healthCheck.Status = HealthStatus.Healthy;
            healthCheck.ResponseTime = stopwatch.Elapsed;
            healthCheck.Details = new Dictionary<string, object>
            {
                ["ConnectionString"] = dbContext.Database.GetConnectionString()?.MaskConnectionString(),
                ["Provider"] = dbContext.Database.ProviderName,
                ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds
            };

            _metricsCollector.Histogram("database.health.response_time", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            healthCheck.Status = HealthStatus.Unhealthy;
            healthCheck.ErrorMessage = ex.Message;
            healthCheck.Details = new Dictionary<string, object>
            {
                ["Exception"] = ex.GetType().Name,
                ["Message"] = ex.Message
            };

            _logger.LogError(ex, "Database health check failed");
            _metricsCollector.Increment("database.health.failures");
        }

        return healthCheck;
    }

    private async Task<HealthCheck> CheckCacheHealthAsync()
    {
        var healthCheck = new HealthCheck
        {
            Name = "Cache",
            Category = "Infrastructure"
        };

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
            
            var testKey = $"health_check_{Guid.NewGuid()}";
            var testValue = "test_value";
            
            var stopwatch = Stopwatch.StartNew();
            
            // Test write
            await cache.SetStringAsync(testKey, testValue);
            
            // Test read
            var retrievedValue = await cache.GetStringAsync(testKey);
            
            // Test delete
            await cache.RemoveAsync(testKey);
            
            stopwatch.Stop();

            if (retrievedValue == testValue)
            {
                healthCheck.Status = HealthStatus.Healthy;
                healthCheck.ResponseTime = stopwatch.Elapsed;
                healthCheck.Details = new Dictionary<string, object>
                {
                    ["CacheType"] = cache.GetType().Name,
                    ["ResponseTimeMs"] = stopwatch.ElapsedMilliseconds,
                    ["Operations"] = "Write, Read, Delete"
                };

                _metricsCollector.Histogram("cache.health.response_time", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                healthCheck.Status = HealthStatus.Unhealthy;
                healthCheck.ErrorMessage = "Cache read/write test failed";
            }
        }
        catch (Exception ex)
        {
            healthCheck.Status = HealthStatus.Unhealthy;
            healthCheck.ErrorMessage = ex.Message;
            healthCheck.Details = new Dictionary<string, object>
            {
                ["Exception"] = ex.GetType().Name,
                ["Message"] = ex.Message
            };

            _logger.LogError(ex, "Cache health check failed");
            _metricsCollector.Increment("cache.health.failures");
        }

        return healthCheck;
    }

    private async Task<IEnumerable<HealthCheck>> CheckExternalServicesHealthAsync()
    {
        var healthChecks = new List<HealthCheck>();

        // Email service health check
        var emailHealth = await CheckEmailServiceHealthAsync();
        healthChecks.Add(emailHealth);

        // Storage service health check
        var storageHealth = await CheckStorageServiceHealthAsync();
        healthChecks.Add(storageHealth);

        // Search service health check
        var searchHealth = await CheckSearchServiceHealthAsync();
        healthChecks.Add(searchHealth);

        return healthChecks;
    }

    private async Task<HealthCheck> CheckSystemResourcesAsync()
    {
        var healthCheck = new HealthCheck
        {
            Name = "System Resources",
            Category = "System"
        };

        try
        {
            var process = Process.GetCurrentProcess();
            
            // Memory usage
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            
            // CPU usage (approximate)
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            await Task.Delay(500);
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            var cpuUsagePercentage = cpuUsageTotal * 100;

            // Disk space
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            var diskInfo = drives.Select(d => new
            {
                Name = d.Name,
                TotalSize = d.TotalSize,
                AvailableSpace = d.AvailableFreeSpace,
                UsedPercentage = (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100
            }).ToList();

            healthCheck.Status = HealthStatus.Healthy;
            healthCheck.Details = new Dictionary<string, object>
            {
                ["WorkingSetMB"] = workingSet / (1024 * 1024),
                ["PrivateMemoryMB"] = privateMemory / (1024 * 1024),
                ["CpuUsagePercentage"] = Math.Round(cpuUsagePercentage, 2),
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["DiskInfo"] = diskInfo
            };

            // Collect system metrics
            _metricsCollector.Gauge("system.memory.working_set_mb", workingSet / (1024 * 1024));
            _metricsCollector.Gauge("system.memory.private_mb", privateMemory / (1024 * 1024));
            _metricsCollector.Gauge("system.cpu.usage_percentage", cpuUsagePercentage);

            // Check for resource warnings
            if (workingSet > 1024 * 1024 * 1024) // > 1GB
            {
                healthCheck.Status = HealthStatus.Degraded;
                healthCheck.WarningMessage = "High memory usage detected";
            }

            if (cpuUsagePercentage > 80)
            {
                healthCheck.Status = HealthStatus.Degraded;
                healthCheck.WarningMessage = "High CPU usage detected";
            }
        }
        catch (Exception ex)
        {
            healthCheck.Status = HealthStatus.Unhealthy;
            healthCheck.ErrorMessage = ex.Message;
            _logger.LogError(ex, "System resources health check failed");
        }

        return healthCheck;
    }

    private async Task<IEnumerable<HealthCheck>> CheckApplicationHealthAsync()
    {
        var healthChecks = new List<HealthCheck>();

        // Content management health
        var contentHealth = await CheckContentManagementHealthAsync();
        healthChecks.Add(contentHealth);

        // Background tasks health
        var backgroundTasksHealth = await CheckBackgroundTasksHealthAsync();
        healthChecks.Add(backgroundTasksHealth);

        // Module health checks
        var moduleHealthChecks = await CheckModulesHealthAsync();
        healthChecks.AddRange(moduleHealthChecks);

        return healthChecks;
    }

    public async Task CollectApplicationMetricsAsync()
    {
        try
        {
            // Collect content metrics
            await CollectContentMetricsAsync();

            // Collect user metrics
            await CollectUserMetricsAsync();

            // Collect performance metrics
            await CollectPerformanceMetricsAsync();

            // Collect business metrics
            await CollectBusinessMetricsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect application metrics");
        }
    }

    private async Task CollectContentMetricsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
        var session = scope.ServiceProvider.GetRequiredService<ISession>();

        // Total content items
        var totalContentItems = await session.Query<ContentItem>().CountAsync();
        _metricsCollector.Gauge("content.items.total", totalContentItems);

        // Published content items
        var publishedContentItems = await session.Query<ContentItem>()
            .Where(ci => ci.Published)
            .CountAsync();
        _metricsCollector.Gauge("content.items.published", publishedContentItems);

        // Content items by type
        var contentItemsByType = await session.Query<ContentItem>()
            .GroupBy(ci => ci.ContentType)
            .Select(g => new { ContentType = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var item in contentItemsByType)
        {
            _metricsCollector.Gauge("content.items.by_type", item.Count, 
                new Dictionary<string, string> { ["content_type"] = item.ContentType });
        }
    }
}
```

---

## 🎯 **KẾT LUẬN**

**Deployment & DevOps** trong OrchardCore cung cấp foundation mạnh mẽ cho:

- **📦 Deployment Plans**: Automated, step-by-step deployment strategies
- **🍳 Recipe System**: Infrastructure as Code với environment-specific configurations
- **🐳 Docker Support**: Containerization cho scalable deployments
- **🔄 CI/CD Integration**: GitHub Actions, Azure DevOps workflows
- **🌍 Environment Management**: Multi-environment với configuration isolation
- **📊 Monitoring & Observability**: Health checks, metrics, distributed tracing

**Deployment & DevOps patterns giúp đảm bảo deployments reliable, scalable và maintainable cho OrchardCore applications! 🚀**

---

## 🎯 **KHI NÀO CẦN DEPLOYMENT & DEVOPS - VÍ DỤ THỰC TẾ**

### **1. 🏢 Enterprise SaaS Platform - Multi-tenant Deployment**

#### **Tình huống:**
Anh xây dựng **SaaS platform** với hàng nghìn tenants, cần deploy frequent updates mà không downtime, auto-scaling, và monitoring comprehensive.

#### **❌ TRƯỚC KHI DÙNG DEPLOYMENT & DEVOPS:**
```bash
# Manual deployment - rủi ro cao, downtime lâu
# 1. SSH vào server production
ssh user@production-server

# 2. Stop application manually
sudo systemctl stop myapp

# 3. Backup database manually
mysqldump -u root -p myapp > backup_$(date +%Y%m%d).sql

# 4. Copy files manually
scp -r ./build/* user@production-server:/var/www/myapp/

# 5. Update database manually
dotnet ef database update

# 6. Start application
sudo systemctl start myapp

# 7. Pray it works! 🙏
curl http://production-server/health

# Vấn đề:
# - Downtime 10-15 phút mỗi lần deploy
# - Không có rollback strategy
# - Không có health checks
# - Không có monitoring
# - Manual process = human errors
# - Không có environment consistency
```

#### **✅ SAU KHI DÙNG DEPLOYMENT & DEVOPS:**
```csharp
// Enterprise SaaS Deployment Service
public class SaaSDeploymentService : ISaaSDeploymentService
{
    private readonly IKubernetesClient _k8sClient;
    private readonly IDockerRegistryClient _registryClient;
    private readonly IMonitoringService _monitoringService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SaaSDeploymentService> _logger;

    public async Task<DeploymentResult> DeployToProductionAsync(SaaSDeploymentRequest request)
    {
        var deploymentId = Guid.NewGuid().ToString();
        var result = new DeploymentResult { DeploymentId = deploymentId };

        try
        {
            _logger.LogInformation("Starting SaaS production deployment: {DeploymentId}", deploymentId);

            // 1. Pre-deployment validation
            await ValidateDeploymentAsync(request);

            // 2. Blue-Green deployment strategy
            await ExecuteBlueGreenDeploymentAsync(request, deploymentId);

            // 3. Database migration với zero-downtime
            await ExecuteZeroDowntimeDatabaseMigrationAsync(request);

            // 4. Tenant-specific configurations
            await UpdateTenantConfigurationsAsync(request);

            // 5. Health checks và smoke tests
            await RunComprehensiveHealthChecksAsync(request);

            // 6. Traffic switching
            await SwitchTrafficToNewVersionAsync(request, deploymentId);

            // 7. Post-deployment monitoring
            await StartPostDeploymentMonitoringAsync(deploymentId);

            result.Status = DeploymentStatus.Completed;
            result.DeploymentTime = DateTime.UtcNow;
            result.DowntimeMinutes = 0; // Zero downtime!

            await _notificationService.NotifyDeploymentSuccessAsync(deploymentId, result);

            _logger.LogInformation("Completed SaaS production deployment: {DeploymentId}", deploymentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed SaaS production deployment: {DeploymentId}", deploymentId);
            
            // Auto-rollback on failure
            await ExecuteAutomaticRollbackAsync(deploymentId);
            
            result.Status = DeploymentStatus.Failed;
            result.ErrorMessage = ex.Message;
            
            await _notificationService.NotifyDeploymentFailureAsync(deploymentId, ex);
            throw;
        }
    }

    private async Task ExecuteBlueGreenDeploymentAsync(SaaSDeploymentRequest request, string deploymentId)
    {
        _logger.LogInformation("Executing Blue-Green deployment for: {DeploymentId}", deploymentId);

        // 1. Deploy to Green environment (inactive)
        var greenDeployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"saas-app-green-{deploymentId}",
                NamespaceProperty = "production",
                Labels = new Dictionary<string, string>
                {
                    ["app"] = "saas-app",
                    ["version"] = request.Version,
                    ["environment"] = "green",
                    ["deployment-id"] = deploymentId
                }
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = request.ReplicaCount,
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        ["app"] = "saas-app",
                        ["environment"] = "green"
                    }
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string>
                        {
                            ["app"] = "saas-app",
                            ["version"] = request.Version,
                            ["environment"] = "green"
                        }
                    },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "saas-app",
                                Image = $"{request.ImageRegistry}/{request.ImageName}:{request.Version}",
                                Ports = new List<V1ContainerPort>
                                {
                                    new V1ContainerPort { ContainerPort = 80 }
                                },
                                Env = BuildEnvironmentVariables(request, "green"),
                                Resources = new V1ResourceRequirements
                                {
                                    Requests = new Dictionary<string, ResourceQuantity>
                                    {
                                        ["cpu"] = new ResourceQuantity("500m"),
                                        ["memory"] = new ResourceQuantity("1Gi")
                                    },
                                    Limits = new Dictionary<string, ResourceQuantity>
                                    {
                                        ["cpu"] = new ResourceQuantity("2000m"),
                                        ["memory"] = new ResourceQuantity("4Gi")
                                    }
                                },
                                LivenessProbe = new V1Probe
                                {
                                    HttpGet = new V1HTTPGetAction
                                    {
                                        Path = "/health/live",
                                        Port = 80
                                    },
                                    InitialDelaySeconds = 30,
                                    PeriodSeconds = 10
                                },
                                ReadinessProbe = new V1Probe
                                {
                                    HttpGet = new V1HTTPGetAction
                                    {
                                        Path = "/health/ready",
                                        Port = 80
                                    },
                                    InitialDelaySeconds = 10,
                                    PeriodSeconds = 5
                                }
                            }
                        }
                    }
                }
            }
        };

        // Deploy to Kubernetes
        await _k8sClient.CreateNamespacedDeploymentAsync(greenDeployment, "production");

        // Wait for Green deployment to be ready
        await WaitForDeploymentReadyAsync("saas-app-green", "production", TimeSpan.FromMinutes(10));

        _logger.LogInformation("Green deployment ready: {DeploymentId}", deploymentId);
    }

    private async Task ExecuteZeroDowntimeDatabaseMigrationAsync(SaaSDeploymentRequest request)
    {
        _logger.LogInformation("Executing zero-downtime database migration");

        // 1. Create migration job
        var migrationJob = new V1Job
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"db-migration-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                NamespaceProperty = "production"
            },
            Spec = new V1JobSpec
            {
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "db-migration",
                                Image = $"{request.ImageRegistry}/{request.ImageName}:{request.Version}",
                                Command = new List<string> { "dotnet", "MyApp.dll", "--migrate-database" },
                                Env = BuildDatabaseEnvironmentVariables(request)
                            }
                        }
                    }
                }
            }
        };

        await _k8sClient.CreateNamespacedJobAsync(migrationJob, "production");

        // Wait for migration to complete
        await WaitForJobCompletionAsync(migrationJob.Metadata.Name, "production", TimeSpan.FromMinutes(30));

        _logger.LogInformation("Database migration completed successfully");
    }

    private async Task UpdateTenantConfigurationsAsync(SaaSDeploymentRequest request)
    {
        _logger.LogInformation("Updating tenant configurations");

        // Update ConfigMaps for each tenant
        foreach (var tenant in request.TenantConfigurations)
        {
            var configMap = new V1ConfigMap
            {
                Metadata = new V1ObjectMeta
                {
                    Name = $"tenant-config-{tenant.TenantId}",
                    NamespaceProperty = "production"
                },
                Data = new Dictionary<string, string>
                {
                    ["appsettings.json"] = JsonSerializer.Serialize(tenant.Configuration, new JsonSerializerOptions { WriteIndented = true }),
                    ["tenant-id"] = tenant.TenantId,
                    ["tenant-name"] = tenant.TenantName,
                    ["database-connection"] = tenant.DatabaseConnection.Encrypt(),
                    ["feature-flags"] = JsonSerializer.Serialize(tenant.FeatureFlags)
                }
            };

            await _k8sClient.ReplaceNamespacedConfigMapAsync(configMap, configMap.Metadata.Name, "production");
        }

        _logger.LogInformation("Updated configurations for {TenantCount} tenants", request.TenantConfigurations.Count);
    }

    private async Task RunComprehensiveHealthChecksAsync(SaaSDeploymentRequest request)
    {
        _logger.LogInformation("Running comprehensive health checks");

        var healthChecks = new List<Task<HealthCheckResult>>
        {
            // Application health
            CheckApplicationHealthAsync("green"),
            
            // Database connectivity
            CheckDatabaseConnectivityAsync(),
            
            // Cache connectivity
            CheckCacheConnectivityAsync(),
            
            // External services
            CheckExternalServicesAsync(),
            
            // Tenant-specific health checks
            CheckTenantHealthAsync(request.TenantConfigurations.Take(5)) // Sample tenants
        };

        var results = await Task.WhenAll(healthChecks);

        if (results.Any(r => !r.IsHealthy))
        {
            var failedChecks = results.Where(r => !r.IsHealthy).Select(r => r.CheckName);
            throw new DeploymentException($"Health checks failed: {string.Join(", ", failedChecks)}");
        }

        _logger.LogInformation("All health checks passed");
    }

    private async Task SwitchTrafficToNewVersionAsync(SaaSDeploymentRequest request, string deploymentId)
    {
        _logger.LogInformation("Switching traffic to new version: {DeploymentId}", deploymentId);

        // 1. Update service selector to point to Green environment
        var service = await _k8sClient.ReadNamespacedServiceAsync("saas-app-service", "production");
        service.Spec.Selector["environment"] = "green";
        
        await _k8sClient.ReplaceNamespacedServiceAsync(service, "saas-app-service", "production");

        // 2. Wait for traffic to stabilize
        await Task.Delay(TimeSpan.FromSeconds(30));

        // 3. Verify traffic is flowing correctly
        await VerifyTrafficFlowAsync();

        // 4. Scale down Blue environment (old version)
        await ScaleDownBlueEnvironmentAsync();

        _logger.LogInformation("Traffic successfully switched to new version");
    }

    private async Task StartPostDeploymentMonitoringAsync(string deploymentId)
    {
        _logger.LogInformation("Starting post-deployment monitoring: {DeploymentId}", deploymentId);

        // Monitor for 30 minutes after deployment
        var monitoringTask = Task.Run(async () =>
        {
            var endTime = DateTime.UtcNow.AddMinutes(30);
            
            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    // Check error rates
                    var errorRate = await _monitoringService.GetErrorRateAsync(TimeSpan.FromMinutes(5));
                    if (errorRate > 0.05) // > 5% error rate
                    {
                        _logger.LogWarning("High error rate detected: {ErrorRate}% - Deployment: {DeploymentId}", 
                            errorRate * 100, deploymentId);
                        
                        await _notificationService.NotifyHighErrorRateAsync(deploymentId, errorRate);
                    }

                    // Check response times
                    var avgResponseTime = await _monitoringService.GetAverageResponseTimeAsync(TimeSpan.FromMinutes(5));
                    if (avgResponseTime > TimeSpan.FromSeconds(2))
                    {
                        _logger.LogWarning("High response time detected: {ResponseTime}ms - Deployment: {DeploymentId}", 
                            avgResponseTime.TotalMilliseconds, deploymentId);
                    }

                    // Check tenant health
                    var unhealthyTenants = await _monitoringService.GetUnhealthyTenantsAsync();
                    if (unhealthyTenants.Any())
                    {
                        _logger.LogWarning("Unhealthy tenants detected: {TenantCount} - Deployment: {DeploymentId}", 
                            unhealthyTenants.Count(), deploymentId);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during post-deployment monitoring: {DeploymentId}", deploymentId);
                }
            }
        });

        // Don't await - let it run in background
        _ = monitoringTask;
    }
}

// Automated CI/CD Pipeline Configuration
public class SaaSCiCdPipelineService : ISaaSCiCdPipelineService
{
    public async Task<PipelineResult> ExecutePipelineAsync(PipelineRequest request)
    {
        var pipeline = new DeploymentPipeline
        {
            Name = "SaaS Production Pipeline",
            Stages = new List<PipelineStage>
            {
                // Stage 1: Build & Test
                new PipelineStage
                {
                    Name = "Build & Test",
                    Steps = new List<PipelineStep>
                    {
                        new BuildStep { DockerImage = "mcr.microsoft.com/dotnet/sdk:8.0" },
                        new UnitTestStep { TestProjects = "**/*Tests.csproj" },
                        new IntegrationTestStep { TestEnvironment = "staging" },
                        new SecurityScanStep { ScanType = "SAST" },
                        new DockerBuildStep { 
                            ImageName = "saas-app", 
                            Tags = new[] { request.Version, "latest" } 
                        }
                    }
                },

                // Stage 2: Deploy to Staging
                new PipelineStage
                {
                    Name = "Deploy to Staging",
                    Steps = new List<PipelineStep>
                    {
                        new KubernetesDeployStep 
                        { 
                            Environment = "staging",
                            Namespace = "staging",
                            ManifestPath = "k8s/staging/"
                        },
                        new DatabaseMigrationStep { Environment = "staging" },
                        new SmokeTestStep { BaseUrl = "https://staging.saas-app.com" },
                        new LoadTestStep { 
                            Duration = TimeSpan.FromMinutes(10),
                            ConcurrentUsers = 100
                        }
                    }
                },

                // Stage 3: Production Approval
                new PipelineStage
                {
                    Name = "Production Approval",
                    Steps = new List<PipelineStep>
                    {
                        new ManualApprovalStep 
                        { 
                            Approvers = new[] { "tech-lead@company.com", "devops@company.com" },
                            TimeoutMinutes = 60
                        }
                    }
                },

                // Stage 4: Deploy to Production
                new PipelineStage
                {
                    Name = "Deploy to Production",
                    Steps = new List<PipelineStep>
                    {
                        new BlueGreenDeploymentStep
                        {
                            Environment = "production",
                            Namespace = "production",
                            HealthCheckUrl = "https://api.saas-app.com/health"
                        },
                        new DatabaseMigrationStep { Environment = "production" },
                        new TenantConfigurationStep { TenantCount = 1000 },
                        new TrafficSwitchStep { SwitchPercentage = 100 },
                        new PostDeploymentMonitoringStep { DurationMinutes = 30 }
                    }
                }
            }
        };

        return await ExecutePipelineAsync(pipeline, request);
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Deployment & DevOps** | **Sau Deployment & DevOps** |
|--------------------------------|------------------------------|
| ❌ Manual deployment - 15 phút downtime | ✅ Blue-Green deployment - 0 downtime |
| ❌ Không có rollback strategy | ✅ Automatic rollback on failure |
| ❌ Không có health checks | ✅ Comprehensive health monitoring |
| ❌ Manual database migration | ✅ Zero-downtime database migration |
| ❌ Không có environment consistency | ✅ Infrastructure as Code với Kubernetes |
| ❌ Human errors trong deployment | ✅ Fully automated CI/CD pipeline |

---

### **2. 🏦 Banking System - High-Security Deployment**

#### **Tình huống:**
Anh phát triển **hệ thống ngân hàng** cần compliance strict, audit trails, security scanning, và disaster recovery capabilities.

#### **❌ TRƯỚC KHI DÙNG DEPLOYMENT & DEVOPS:**
```bash
# Manual security-focused deployment - chậm và rủi ro
# 1. Manual security review
echo "Please review code manually for security issues..."

# 2. Manual compliance check
echo "Please verify compliance with banking regulations..."

# 3. Manual backup
pg_dump banking_db > backup_$(date +%Y%m%d).sql

# 4. Manual deployment với downtime
systemctl stop banking-app
cp -r ./build/* /opt/banking-app/
systemctl start banking-app

# 5. Manual verification
curl -k https://banking-app.com/health

# Vấn đề:
# - Không có automated security scanning
# - Không có compliance validation
# - Không có audit trails
# - Không có disaster recovery testing
# - Manual process = security risks
# - Không có encrypted deployment artifacts
```

#### **✅ SAU KHI DÙNG DEPLOYMENT & DEVOPS:**
```csharp
// Banking-Grade Secure Deployment Service
public class BankingDeploymentService : IBankingDeploymentService
{
    private readonly ISecurityScanningService _securityScanner;
    private readonly IComplianceValidationService _complianceValidator;
    private readonly IAuditLoggingService _auditLogger;
    private readonly IEncryptionService _encryptionService;
    private readonly IDisasterRecoveryService _disasterRecovery;

    public async Task<SecureDeploymentResult> DeployToBankingProductionAsync(BankingDeploymentRequest request)
    {
        var deploymentId = Guid.NewGuid().ToString();
        var auditContext = new AuditContext
        {
            DeploymentId = deploymentId,
            InitiatedBy = request.InitiatedBy,
            Timestamp = DateTime.UtcNow,
            Environment = "production"
        };

        try
        {
            // 1. Pre-deployment security validation
            await ValidateBankingSecurityRequirementsAsync(request, auditContext);

            // 2. Compliance verification
            await VerifyRegulatoryComplianceAsync(request, auditContext);

            // 3. Encrypted artifact preparation
            await PrepareEncryptedArtifactsAsync(request, auditContext);

            // 4. Disaster recovery backup
            await CreateDisasterRecoveryBackupAsync(auditContext);

            // 5. Secure deployment execution
            await ExecuteSecureBankingDeploymentAsync(request, auditContext);

            // 6. Post-deployment security validation
            await ValidatePostDeploymentSecurityAsync(auditContext);

            // 7. Compliance reporting
            await GenerateComplianceReportAsync(auditContext);

            return new SecureDeploymentResult
            {
                DeploymentId = deploymentId,
                Status = DeploymentStatus.Completed,
                SecurityValidationPassed = true,
                ComplianceValidationPassed = true,
                AuditTrailGenerated = true
            };
        }
        catch (Exception ex)
        {
            await _auditLogger.LogSecurityIncidentAsync(auditContext, ex);
            throw;
        }
    }

    private async Task ValidateBankingSecurityRequirementsAsync(
        BankingDeploymentRequest request, 
        AuditContext auditContext)
    {
        await _auditLogger.LogAuditEventAsync(auditContext, "Starting security validation");

        // 1. Static Application Security Testing (SAST)
        var sastResults = await _securityScanner.RunSastScanAsync(request.SourceCodePath);
        if (sastResults.HighSeverityIssues.Any())
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext, 
                $"SAST scan found {sastResults.HighSeverityIssues.Count} high-severity issues");
            throw new SecurityValidationException("High-severity security issues found in SAST scan");
        }

        // 2. Dynamic Application Security Testing (DAST)
        var dastResults = await _securityScanner.RunDastScanAsync(request.StagingUrl);
        if (dastResults.CriticalVulnerabilities.Any())
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext,
                $"DAST scan found {dastResults.CriticalVulnerabilities.Count} critical vulnerabilities");
            throw new SecurityValidationException("Critical vulnerabilities found in DAST scan");
        }

        // 3. Container security scanning
        var containerScanResults = await _securityScanner.ScanContainerImageAsync(request.ContainerImage);
        if (containerScanResults.HighRiskVulnerabilities.Any())
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext,
                $"Container scan found {containerScanResults.HighRiskVulnerabilities.Count} high-risk vulnerabilities");
            throw new SecurityValidationException("High-risk vulnerabilities found in container image");
        }

        // 4. Dependency vulnerability scanning
        var dependencyScanResults = await _securityScanner.ScanDependenciesAsync(request.SourceCodePath);
        if (dependencyScanResults.KnownVulnerabilities.Any(v => v.Severity == "Critical"))
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext,
                "Critical vulnerabilities found in dependencies");
            throw new SecurityValidationException("Critical vulnerabilities found in dependencies");
        }

        // 5. Secrets scanning
        var secretsScanResults = await _securityScanner.ScanForSecretsAsync(request.SourceCodePath);
        if (secretsScanResults.ExposedSecrets.Any())
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext,
                $"Found {secretsScanResults.ExposedSecrets.Count} exposed secrets");
            throw new SecurityValidationException("Exposed secrets found in source code");
        }

        await _auditLogger.LogAuditEventAsync(auditContext, "Security validation completed successfully");
    }

    private async Task VerifyRegulatoryComplianceAsync(
        BankingDeploymentRequest request, 
        AuditContext auditContext)
    {
        await _auditLogger.LogAuditEventAsync(auditContext, "Starting regulatory compliance verification");

        // 1. PCI DSS Compliance
        var pciComplianceResult = await _complianceValidator.ValidatePciDssComplianceAsync(request);
        if (!pciComplianceResult.IsCompliant)
        {
            await _auditLogger.LogComplianceViolationAsync(auditContext, 
                $"PCI DSS compliance failed: {string.Join(", ", pciComplianceResult.Violations)}");
            throw new ComplianceException("PCI DSS compliance validation failed");
        }

        // 2. SOX Compliance (Sarbanes-Oxley)
        var soxComplianceResult = await _complianceValidator.ValidateSoxComplianceAsync(request);
        if (!soxComplianceResult.IsCompliant)
        {
            await _auditLogger.LogComplianceViolationAsync(auditContext,
                $"SOX compliance failed: {string.Join(", ", soxComplianceResult.Violations)}");
            throw new ComplianceException("SOX compliance validation failed");
        }

        // 3. Basel III Compliance
        var baselComplianceResult = await _complianceValidator.ValidateBaselIiiComplianceAsync(request);
        if (!baselComplianceResult.IsCompliant)
        {
            await _auditLogger.LogComplianceViolationAsync(auditContext,
                $"Basel III compliance failed: {string.Join(", ", baselComplianceResult.Violations)}");
            throw new ComplianceException("Basel III compliance validation failed");
        }

        // 4. Data Privacy Compliance (GDPR, CCPA)
        var privacyComplianceResult = await _complianceValidator.ValidateDataPrivacyComplianceAsync(request);
        if (!privacyComplianceResult.IsCompliant)
        {
            await _auditLogger.LogComplianceViolationAsync(auditContext,
                $"Data privacy compliance failed: {string.Join(", ", privacyComplianceResult.Violations)}");
            throw new ComplianceException("Data privacy compliance validation failed");
        }

        await _auditLogger.LogAuditEventAsync(auditContext, "Regulatory compliance verification completed");
    }

    private async Task PrepareEncryptedArtifactsAsync(
        BankingDeploymentRequest request, 
        AuditContext auditContext)
    {
        await _auditLogger.LogAuditEventAsync(auditContext, "Preparing encrypted deployment artifacts");

        // 1. Encrypt application binaries
        var encryptedBinaries = await _encryptionService.EncryptFileAsync(
            request.ApplicationBinariesPath, 
            request.EncryptionKey);

        // 2. Encrypt configuration files
        var encryptedConfigs = await _encryptionService.EncryptConfigurationAsync(
            request.ConfigurationFiles,
            request.EncryptionKey);

        // 3. Encrypt database migration scripts
        var encryptedMigrations = await _encryptionService.EncryptFileAsync(
            request.DatabaseMigrationsPath,
            request.EncryptionKey);

        // 4. Create encrypted deployment package
        var deploymentPackage = new EncryptedDeploymentPackage
        {
            EncryptedBinaries = encryptedBinaries,
            EncryptedConfigurations = encryptedConfigs,
            EncryptedMigrations = encryptedMigrations,
            PackageHash = await _encryptionService.ComputeHashAsync(encryptedBinaries),
            DigitalSignature = await _encryptionService.SignPackageAsync(encryptedBinaries, request.SigningKey)
        };

        // 5. Store in secure artifact repository
        await StoreSecureArtifactAsync(deploymentPackage, auditContext);

        await _auditLogger.LogAuditEventAsync(auditContext, "Encrypted artifacts prepared successfully");
    }

    private async Task CreateDisasterRecoveryBackupAsync(AuditContext auditContext)
    {
        await _auditLogger.LogAuditEventAsync(auditContext, "Creating disaster recovery backup");

        // 1. Full database backup với encryption
        var dbBackupResult = await _disasterRecovery.CreateEncryptedDatabaseBackupAsync(new DatabaseBackupOptions
        {
            BackupType = BackupType.Full,
            EncryptionEnabled = true,
            CompressionEnabled = true,
            VerifyIntegrity = true,
            BackupLocation = "secure-backup-storage"
        });

        // 2. Application state backup
        var appStateBackup = await _disasterRecovery.CreateApplicationStateBackupAsync();

        // 3. Configuration backup
        var configBackup = await _disasterRecovery.CreateConfigurationBackupAsync();

        // 4. Test backup integrity
        var backupIntegrityResult = await _disasterRecovery.ValidateBackupIntegrityAsync(
            new[] { dbBackupResult.BackupId, appStateBackup.BackupId, configBackup.BackupId });

        if (!backupIntegrityResult.IsValid)
        {
            throw new DisasterRecoveryException("Backup integrity validation failed");
        }

        // 5. Store backup metadata
        await _disasterRecovery.StoreBackupMetadataAsync(new BackupMetadata
        {
            DeploymentId = auditContext.DeploymentId,
            BackupIds = new[] { dbBackupResult.BackupId, appStateBackup.BackupId, configBackup.BackupId },
            CreatedAt = DateTime.UtcNow,
            RetentionPeriod = TimeSpan.FromDays(2555), // 7 years for banking compliance
            EncryptionKeyId = "banking-backup-key-2024"
        });

        await _auditLogger.LogAuditEventAsync(auditContext, "Disaster recovery backup created successfully");
    }

    private async Task ExecuteSecureBankingDeploymentAsync(
        BankingDeploymentRequest request, 
        AuditContext auditContext)
    {
        await _auditLogger.LogAuditEventAsync(auditContext, "Starting secure banking deployment");

        // 1. Maintenance window notification
        await NotifyMaintenanceWindowAsync(request.MaintenanceWindow);

        // 2. Gradual traffic reduction
        await GraduallyReduceTrafficAsync();

        // 3. Secure database migration
        await ExecuteSecureDatabaseMigrationAsync(request, auditContext);

        // 4. Deploy encrypted application
        await DeployEncryptedApplicationAsync(request, auditContext);

        // 5. Update security configurations
        await UpdateSecurityConfigurationsAsync(request, auditContext);

        // 6. Restart services với security validation
        await RestartServicesWithSecurityValidationAsync(auditContext);

        // 7. Gradual traffic restoration
        await GraduallyRestoreTrafficAsync();

        await _auditLogger.LogAuditEventAsync(auditContext, "Secure banking deployment completed");
    }

    private async Task ValidatePostDeploymentSecurityAsync(AuditContext auditContext)
    {
        await _auditLogger.LogAuditEventAsync(auditContext, "Starting post-deployment security validation");

        // 1. Runtime security scanning
        var runtimeScanResults = await _securityScanner.RunRuntimeSecurityScanAsync();
        if (runtimeScanResults.SecurityIssues.Any())
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext,
                $"Runtime security scan found {runtimeScanResults.SecurityIssues.Count} issues");
            throw new SecurityValidationException("Runtime security issues detected");
        }

        // 2. Network security validation
        var networkSecurityResults = await _securityScanner.ValidateNetworkSecurityAsync();
        if (!networkSecurityResults.IsSecure)
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext,
                "Network security validation failed");
            throw new SecurityValidationException("Network security validation failed");
        }

        // 3. Access control validation
        var accessControlResults = await _securityScanner.ValidateAccessControlsAsync();
        if (!accessControlResults.IsValid)
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext,
                "Access control validation failed");
            throw new SecurityValidationException("Access control validation failed");
        }

        // 4. Encryption validation
        var encryptionResults = await _securityScanner.ValidateEncryptionAsync();
        if (!encryptionResults.IsValid)
        {
            await _auditLogger.LogSecurityViolationAsync(auditContext,
                "Encryption validation failed");
            throw new SecurityValidationException("Encryption validation failed");
        }

        await _auditLogger.LogAuditEventAsync(auditContext, "Post-deployment security validation completed");
    }

    private async Task GenerateComplianceReportAsync(AuditContext auditContext)
    {
        var complianceReport = new BankingComplianceReport
        {
            DeploymentId = auditContext.DeploymentId,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = auditContext.InitiatedBy,
            
            SecurityValidation = new SecurityValidationReport
            {
                SastScanPassed = true,
                DastScanPassed = true,
                ContainerScanPassed = true,
                DependencyScanPassed = true,
                SecretsScanPassed = true
            },
            
            ComplianceValidation = new ComplianceValidationReport
            {
                PciDssCompliant = true,
                SoxCompliant = true,
                BaselIiiCompliant = true,
                DataPrivacyCompliant = true
            },
            
            DeploymentDetails = new DeploymentDetailsReport
            {
                EncryptionUsed = true,
                BackupCreated = true,
                AuditTrailGenerated = true,
                DisasterRecoveryTested = true
            }
        };

        // Store compliance report
        await StoreComplianceReportAsync(complianceReport);

        // Send to regulatory authorities if required
        if (auditContext.RequiresRegulatoryReporting)
        {
            await SubmitRegulatoryReportAsync(complianceReport);
        }

        await _auditLogger.LogAuditEventAsync(auditContext, "Compliance report generated and stored");
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Deployment & DevOps** | **Sau Deployment & DevOps** |
|--------------------------------|------------------------------|
| ❌ Manual security review | ✅ Automated SAST/DAST/Container scanning |
| ❌ Không có compliance validation | ✅ PCI DSS, SOX, Basel III compliance |
| ❌ Không có audit trails | ✅ Comprehensive audit logging |
| ❌ Không có encrypted deployment | ✅ End-to-end encryption |
| ❌ Không có disaster recovery | ✅ Automated backup & recovery testing |
| ❌ Manual compliance reporting | ✅ Automated compliance reports |

---

### **3. 🌐 Global E-commerce - Multi-Region Deployment**

#### **Tình huống:**
Anh vận hành **e-commerce platform** phục vụ global customers, cần deploy đồng thời multiple regions với data consistency và performance optimization.

#### **❌ TRƯỚC KHI DÙNG DEPLOYMENT & DEVOPS:**
```bash
# Manual multi-region deployment - phức tạp và dễ lỗi
# 1. Deploy to US East manually
ssh user@us-east-server
# ... manual deployment steps ...

# 2. Deploy to EU West manually  
ssh user@eu-west-server
# ... manual deployment steps ...

# 3. Deploy to Asia Pacific manually
ssh user@ap-southeast-server
# ... manual deployment steps ...

# 4. Manual database synchronization
# ... complex manual sync process ...

# 5. Manual CDN cache invalidation
# ... manual cache clearing ...

# Vấn đề:
# - Inconsistent deployment timing
# - Data synchronization issues
# - CDN cache inconsistencies
# - No rollback coordination
# - Manual process = human errors
# - No performance optimization
```

#### **✅ SAU KHI DÙNG DEPLOYMENT & DEVOPS:**
```csharp
// Global Multi-Region Deployment Service
public class GlobalEcommerceDeploymentService : IGlobalDeploymentService
{
    private readonly IMultiRegionOrchestrator _regionOrchestrator;
    private readonly IDataSynchronizationService _dataSyncService;
    private readonly ICdnManagementService _cdnService;
    private readonly IPerformanceOptimizationService _performanceService;

    public async Task<GlobalDeploymentResult> DeployGloballyAsync(GlobalDeploymentRequest request)
    {
        var deploymentId = Guid.NewGuid().ToString();
        var result = new GlobalDeploymentResult { DeploymentId = deploymentId };

        try
        {
            // 1. Pre-deployment global validation
            await ValidateGlobalDeploymentReadinessAsync(request);

            // 2. Coordinated multi-region deployment
            await ExecuteCoordinatedMultiRegionDeploymentAsync(request, deploymentId);

            // 3. Global data synchronization
            await SynchronizeGlobalDataAsync(request, deploymentId);

            // 4. CDN optimization và cache invalidation
            await OptimizeCdnAndInvalidateCacheAsync(request);

            // 5. Global performance validation
            await ValidateGlobalPerformanceAsync(request);

            // 6. Global traffic routing optimization
            await OptimizeGlobalTrafficRoutingAsync(request);

            result.Status = DeploymentStatus.Completed;
            result.RegionsDeployed = request.TargetRegions.Count;
            result.GlobalSyncCompleted = true;

            return result;
        }
        catch (Exception ex)
        {
            // Coordinated global rollback
            await ExecuteGlobalRollbackAsync(deploymentId, request.TargetRegions);
            throw;
        }
    }

    private async Task ExecuteCoordinatedMultiRegionDeploymentAsync(
        GlobalDeploymentRequest request, 
        string deploymentId)
    {
        // Deploy to regions in optimal order (based on traffic patterns)
        var deploymentOrder = OptimizeDeploymentOrder(request.TargetRegions);
        var deploymentTasks = new List<Task<RegionDeploymentResult>>();

        foreach (var region in deploymentOrder)
        {
            var regionTask = DeployToRegionAsync(region, request, deploymentId);
            deploymentTasks.Add(regionTask);

            // Stagger deployments to avoid overwhelming systems
            await Task.Delay(TimeSpan.FromMinutes(2));
        }

        // Wait for all regional deployments to complete
        var regionResults = await Task.WhenAll(deploymentTasks);

        // Validate all regions deployed successfully
        if (regionResults.Any(r => !r.Success))
        {
            var failedRegions = regionResults.Where(r => !r.Success).Select(r => r.Region);
            throw new MultiRegionDeploymentException(
                $"Deployment failed in regions: {string.Join(", ", failedRegions)}");
        }
    }

    private async Task<RegionDeploymentResult> DeployToRegionAsync(
        DeploymentRegion region, 
        GlobalDeploymentRequest request, 
        string deploymentId)
    {
        var regionResult = new RegionDeploymentResult { Region = region.Name };

        try
        {
            // 1. Regional pre-deployment checks
            await ValidateRegionalReadinessAsync(region);

            // 2. Deploy to regional Kubernetes cluster
            await DeployToRegionalKubernetesAsync(region, request, deploymentId);

            // 3. Update regional database
            await UpdateRegionalDatabaseAsync(region, request);

            // 4. Configure regional services
            await ConfigureRegionalServicesAsync(region, request);

            // 5. Regional health checks
            await ValidateRegionalHealthAsync(region);

            regionResult.Success = true;
            regionResult.DeploymentTime = DateTime.UtcNow;

            return regionResult;
        }
        catch (Exception ex)
        {
            regionResult.Success = false;
            regionResult.ErrorMessage = ex.Message;
            throw;
        }
    }

    private async Task SynchronizeGlobalDataAsync(GlobalDeploymentRequest request, string deploymentId)
    {
        // 1. Product catalog synchronization
        await SynchronizeProductCatalogAsync(request.TargetRegions);

        // 2. Inventory synchronization
        await SynchronizeInventoryDataAsync(request.TargetRegions);

        // 3. Pricing synchronization (with currency conversion)
        await SynchronizePricingDataAsync(request.TargetRegions);

        // 4. Customer data synchronization (GDPR compliant)
        await SynchronizeCustomerDataAsync(request.TargetRegions);

        // 5. Order data synchronization
        await SynchronizeOrderDataAsync(request.TargetRegions);

        // 6. Validate data consistency across regions
        await ValidateGlobalDataConsistencyAsync(request.TargetRegions);
    }

    private async Task OptimizeCdnAndInvalidateCacheAsync(GlobalDeploymentRequest request)
    {
        var cdnTasks = new List<Task>();

        foreach (var region in request.TargetRegions)
        {
            // Regional CDN optimization
            var cdnTask = Task.Run(async () =>
            {
                // 1. Invalidate regional CDN cache
                await _cdnService.InvalidateRegionalCacheAsync(region.CdnEndpoint, new[]
                {
                    "/api/*",
                    "/static/js/*",
                    "/static/css/*",
                    "/images/products/*"
                });

                // 2. Pre-warm critical content
                await _cdnService.PreWarmCacheAsync(region.CdnEndpoint, new[]
                {
                    "/",
                    "/products/popular",
                    "/categories",
                    "/api/products/featured"
                });

                // 3. Update CDN configuration
                await _cdnService.UpdateCdnConfigurationAsync(region.CdnEndpoint, new CdnConfiguration
                {
                    CacheTtl = TimeSpan.FromHours(24),
                    CompressionEnabled = true,
                    Http2Enabled = true,
                    BrotliCompressionEnabled = true,
                    ImageOptimizationEnabled = true
                });
            });

            cdnTasks.Add(cdnTask);
        }

        await Task.WhenAll(cdnTasks);
    }

    private async Task ValidateGlobalPerformanceAsync(GlobalDeploymentRequest request)
    {
        var performanceTests = new List<Task<PerformanceTestResult>>();

        foreach (var region in request.TargetRegions)
        {
            // Test from multiple locations to each region
            var testTask = _performanceService.RunGlobalPerformanceTestAsync(new PerformanceTestOptions
            {
                TargetRegion = region,
                TestDuration = TimeSpan.FromMinutes(5),
                ConcurrentUsers = 100,
                TestScenarios = new[]
                {
                    "Homepage Load",
                    "Product Search",
                    "Add to Cart",
                    "Checkout Process",
                    "API Response Times"
                }
            });

            performanceTests.Add(testTask);
        }

        var results = await Task.WhenAll(performanceTests);

        // Validate performance meets SLA requirements
        foreach (var result in results)
        {
            if (result.AverageResponseTime > TimeSpan.FromSeconds(2))
            {
                throw new PerformanceException(
                    $"Performance SLA violation in {result.Region}: {result.AverageResponseTime.TotalSeconds}s");
            }

            if (result.ErrorRate > 0.01) // > 1% error rate
            {
                throw new PerformanceException(
                    $"Error rate SLA violation in {result.Region}: {result.ErrorRate * 100}%");
            }
        }
    }

    private async Task OptimizeGlobalTrafficRoutingAsync(GlobalDeploymentRequest request)
    {
        // 1. Update DNS routing policies
        await UpdateDnsRoutingPoliciesAsync(request.TargetRegions);

        // 2. Configure load balancer weights based on capacity
        await ConfigureGlobalLoadBalancerWeightsAsync(request.TargetRegions);

        // 3. Update geo-routing rules
        await UpdateGeoRoutingRulesAsync(request.TargetRegions);

        // 4. Configure failover policies
        await ConfigureFailoverPoliciesAsync(request.TargetRegions);

        // 5. Validate traffic distribution
        await ValidateTrafficDistributionAsync(request.TargetRegions);
    }

    private async Task UpdateDnsRoutingPoliciesAsync(List<DeploymentRegion> regions)
    {
        var dnsUpdates = new List<DnsUpdate>();

        foreach (var region in regions)
        {
            dnsUpdates.Add(new DnsUpdate
            {
                RecordType = "A",
                Name = $"{region.Name}.ecommerce-app.com",
                Value = region.LoadBalancerIp,
                Ttl = 300, // 5 minutes
                RoutingPolicy = new GeolocationRoutingPolicy
                {
                    ContinentCode = region.ContinentCode,
                    CountryCode = region.CountryCode,
                    HealthCheckId = region.HealthCheckId
                }
            });
        }

        await _regionOrchestrator.UpdateDnsRecordsAsync(dnsUpdates);
    }

    private async Task ConfigureGlobalLoadBalancerWeightsAsync(List<DeploymentRegion> regions)
    {
        var weightUpdates = new List<LoadBalancerWeightUpdate>();

        foreach (var region in regions)
        {
            // Calculate weight based on region capacity and current load
            var currentLoad = await GetRegionCurrentLoadAsync(region);
            var capacity = region.MaxCapacity;
            var weight = Math.Max(1, (int)((capacity - currentLoad) / capacity * 100));

            weightUpdates.Add(new LoadBalancerWeightUpdate
            {
                Region = region.Name,
                Weight = weight,
                HealthCheckEnabled = true
            });
        }

        await _regionOrchestrator.UpdateLoadBalancerWeightsAsync(weightUpdates);
    }
}

// Global deployment pipeline configuration
public class GlobalEcommercePipelineService : IGlobalPipelineService
{
    public async Task<PipelineResult> ExecuteGlobalPipelineAsync(GlobalPipelineRequest request)
    {
        var pipeline = new GlobalDeploymentPipeline
        {
            Name = "Global E-commerce Deployment Pipeline",
            Stages = new List<GlobalPipelineStage>
            {
                // Stage 1: Global Build & Test
                new GlobalPipelineStage
                {
                    Name = "Global Build & Test",
                    ParallelExecution = true,
                    Steps = new List<PipelineStep>
                    {
                        new GlobalBuildStep { Regions = request.TargetRegions },
                        new GlobalTestStep { TestSuites = new[] { "Unit", "Integration", "E2E" } },
                        new GlobalSecurityScanStep(),
                        new GlobalPerformanceTestStep()
                    }
                },

                // Stage 2: Staging Deployment (All Regions)
                new GlobalPipelineStage
                {
                    Name = "Global Staging Deployment",
                    ParallelExecution = true,
                    Steps = request.TargetRegions.Select(region => new RegionalDeploymentStep
                    {
                        Region = region,
                        Environment = "staging",
                        HealthCheckRequired = true
                    }).Cast<PipelineStep>().ToList()
                },

                // Stage 3: Global Integration Testing
                new GlobalPipelineStage
                {
                    Name = "Global Integration Testing",
                    Steps = new List<PipelineStep>
                    {
                        new GlobalDataSyncTestStep(),
                        new GlobalPerformanceTestStep(),
                        new GlobalSecurityTestStep(),
                        new GlobalComplianceTestStep()
                    }
                },

                // Stage 4: Production Approval
                new GlobalPipelineStage
                {
                    Name = "Production Approval",
                    Steps = new List<PipelineStep>
                    {
                        new ManualApprovalStep 
                        { 
                            Approvers = new[] { "global-ops@company.com" },
                            RequiredApprovals = 2
                        }
                    }
                },

                // Stage 5: Global Production Deployment
                new GlobalPipelineStage
                {
                    Name = "Global Production Deployment",
                    Steps = new List<PipelineStep>
                    {
                        new CoordinatedMultiRegionDeploymentStep
                        {
                            Regions = request.TargetRegions,
                            DeploymentStrategy = "BlueGreen",
                            StaggerDelayMinutes = 2
                        },
                        new GlobalDataSynchronizationStep(),
                        new GlobalCdnOptimizationStep(),
                        new GlobalTrafficRoutingStep(),
                        new GlobalMonitoringStep { DurationMinutes = 60 }
                    }
                }
            }
        };

        return await ExecuteGlobalPipelineAsync(pipeline, request);
    }
}
```

#### **🎯 Kết quả so sánh:**

| **Trước Deployment & DevOps** | **Sau Deployment & DevOps** |
|--------------------------------|------------------------------|
| ❌ Manual multi-region deployment | ✅ Coordinated automated deployment |
| ❌ Data synchronization issues | ✅ Automated global data sync |
| ❌ CDN cache inconsistencies | ✅ Coordinated CDN optimization |
| ❌ No performance validation | ✅ Global performance testing |
| ❌ Manual traffic routing | ✅ Intelligent geo-routing |
| ❌ No rollback coordination | ✅ Global coordinated rollback |

---

## 💡 **TÓM TẮT KHI NÀO CẦN DEPLOYMENT & DEVOPS**

### **✅ CẦN DÙNG KHI:**

#### **1. 🏢 Enterprise Applications với High Availability**
- **Ví dụ**: SaaS platforms, banking systems, e-commerce
- **Lợi ích**: Zero-downtime deployments, automated rollbacks, comprehensive monitoring

#### **2. 🔒 High-Security Environments**
- **Ví dụ**: Banking, healthcare, government systems
- **Lợi ích**: Security scanning, compliance validation, audit trails, encrypted deployments

#### **3. 🌍 Multi-Region/Global Applications**
- **Ví dụ**: Global e-commerce, CDN-backed applications
- **Lợi ích**: Coordinated deployments, data synchronization, performance optimization

#### **4. 📈 Scalable Applications với Frequent Updates**
- **Ví dụ**: Social media platforms, news websites, mobile app backends
- **Lợi ích**: Automated CI/CD, blue-green deployments, canary releases

#### **5. 🏗️ Complex Multi-Service Architectures**
- **Ví dụ**: Microservices, distributed systems
- **Lợi ích**: Orchestrated deployments, service mesh management, dependency handling

### **❌ KHÔNG CẦN DÙNG KHI:**

#### **1. 📄 Simple Static Websites**
- **Ví dụ**: Company brochure sites, personal blogs
- **Lý do**: Simple FTP upload hoặc static hosting đủ rồi

#### **2. 🔧 Internal Tools với Low Traffic**
- **Ví dụ**: Admin dashboards, internal reporting tools
- **Lý do**: Manual deployment acceptable, ít updates

#### **3. 💰 Small Projects với Limited Resources**
- **Ví dụ**: Startup MVPs, prototype applications
- **Lý do**: Setup cost cao hơn benefit

#### **4. 🎓 Learning/Educational Projects**
- **Ví dụ**: Student projects, coding bootcamp assignments
- **Lý do**: Focus on learning, không cần production-grade deployment

### **🎯 KẾT LUẬN:**
**Deployment & DevOps patterns phù hợp nhất cho enterprise applications cần high availability, security, scalability, và frequent updates với zero-downtime requirements!**

---

*Tài liệu này được tạo dựa trên phân tích source code OrchardCore và best practices.*