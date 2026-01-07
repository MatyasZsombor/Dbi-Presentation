using System.Text.Json;
using ElasticSearch.Models;
using Elasticsearch.Services;
using LiveDemo.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LiveDemo;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    private static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                services.AddSingleton(configuration);
                
                var webPages = LoadWebpages();
                services.AddSingleton(webPages);
                
                AddLogging(services);
                ElasticsearchConfiguration.ConfigureElasticsearchService(services, configuration);
                services.AddHostedService<ElasticsearchInitializerService>();
                services.AddHostedService<DemoRunner>();
            })
            .Build();
        
        await host.RunAsync();
    }

    private static List<WebPage> LoadWebpages()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "webpages.json");
        
        if (!File.Exists(path))
        {
            Console.WriteLine($"Warning: webpages.json not found at {path}");
            return new List<WebPage>();
        }
        
        string jsonString = File.ReadAllText(path);
        
        var webPages = JsonSerializer.Deserialize<List<WebPage>>(jsonString, JsonSerializerOptions) ?? [];
        Console.WriteLine($"Loaded {webPages.Count} webpages from JSON.");
        return webPages;
    }
    
    private static void AddLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }
}