using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Environment.Shell;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace NhanViet.Tests.Performance;

public class LoadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public LoadTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Homepage_ShouldLoadWithinAcceptableTime()
    {
        // Arrange
        var client = _factory.CreateClient();
        var stopwatch = Stopwatch.StartNew();
        const int acceptableLoadTimeMs = 2000; // 2 seconds

        // Act
        var response = await client.GetAsync("/");
        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(stopwatch.ElapsedMilliseconds < acceptableLoadTimeMs, 
            $"Homepage took {stopwatch.ElapsedMilliseconds}ms to load, which exceeds {acceptableLoadTimeMs}ms");
        
        _output.WriteLine($"Homepage loaded in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task AdminPanel_ShouldLoadWithinAcceptableTime()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Warm up the application first (OrchardCore cold start can be slow)
        await client.GetAsync("/");
        await Task.Delay(500); // Give OrchardCore time to initialize
        
        var stopwatch = Stopwatch.StartNew();
        const int acceptableLoadTimeMs = 10000; // 10 seconds for admin (more realistic for OrchardCore)

        // Act
        var response = await client.GetAsync("/admin");
        stopwatch.Stop();

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect);
        Assert.True(stopwatch.ElapsedMilliseconds < acceptableLoadTimeMs,
            $"Admin panel took {stopwatch.ElapsedMilliseconds}ms to load, which exceeds {acceptableLoadTimeMs}ms");
        
        _output.WriteLine($"Admin panel loaded in {stopwatch.ElapsedMilliseconds}ms");
        
        // Additional performance metrics
        if (stopwatch.ElapsedMilliseconds > 5000)
        {
            _output.WriteLine("⚠️  Admin panel load time is above 5 seconds - consider optimization");
        }
        else if (stopwatch.ElapsedMilliseconds < 2000)
        {
            _output.WriteLine("✅ Excellent admin panel performance!");
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/admin")]
    public async Task ConcurrentRequests_ShouldHandleLoad(string endpoint)
    {
        // Arrange
        var client = _factory.CreateClient();
        const int concurrentRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(client.GetAsync(endpoint));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        foreach (var response in responses)
        {
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect);
        }

        var averageTime = stopwatch.ElapsedMilliseconds / concurrentRequests;
        _output.WriteLine($"{concurrentRequests} concurrent requests to {endpoint} completed in {stopwatch.ElapsedMilliseconds}ms (avg: {averageTime}ms per request)");
        
        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task MemoryUsage_ShouldBeReasonable()
    {
        // Arrange
        var client = _factory.CreateClient();
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Make several requests to simulate usage
        for (int i = 0; i < 50; i++)
        {
            var response = await client.GetAsync("/");
            response.Dispose();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        const long maxMemoryIncreaseMB = 50 * 1024 * 1024; // 50MB
        Assert.True(memoryIncrease < maxMemoryIncreaseMB,
            $"Memory increased by {memoryIncrease / 1024 / 1024}MB, which exceeds the limit of {maxMemoryIncreaseMB / 1024 / 1024}MB");

        _output.WriteLine($"Memory usage increased by {memoryIncrease / 1024 / 1024}MB after 50 requests");
    }
}