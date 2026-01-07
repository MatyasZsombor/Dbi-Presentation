using ElasticSearch.Models;

namespace Elasticsearch.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ElasticsearchInitializerService(ILogger<ElasticsearchInitializerService> logger,
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    List<WebPage> pages) : IHostedService
{
    private static readonly Action<ILogger, Exception?> LogElasticsearchSetupStarted =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(StartAsync)),
            "Started setting up Elasticsearch");

    private static readonly Action<ILogger, Exception?> LogElasticsearchSetupFinished =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2, nameof(StartAsync)),
            "Finished setting up Elasticsearch");
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ElasticsearchService elasticsearchService = scope.ServiceProvider.GetRequiredService<ElasticsearchService>();

        LogElasticsearchSetupStarted(logger, null);
        string? onStartup = configuration["Elasticsearch:OnStartUp"];

        if (!await elasticsearchService.IsAlive())
        {
            return;
        }

        switch (onStartup)
        {
            case "recreate":
                await elasticsearchService.RecreateIndex(pages);
                break;
            case "create":
                await elasticsearchService.CreateIndexAndSync(pages);
                break;
            case "resync":
                await elasticsearchService.SyncIndex(pages);
                break;
        }

        LogElasticsearchSetupFinished(logger, null);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
