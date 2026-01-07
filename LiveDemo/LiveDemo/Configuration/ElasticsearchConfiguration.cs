using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elasticsearch;
using ElasticSearch;
using Elasticsearch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LiveDemo.Configuration;

public static class ElasticsearchConfiguration
{
    public static void ConfigureElasticsearchService(IServiceCollection services, IConfiguration configuration)
    {
        ElasticsearchOptions elasticsearchOptions = GetElasticsearchOptions(configuration);
        Uri connectionString = new (elasticsearchOptions.ConnectionString);

        ElasticsearchClientSettings adminSettings = new ElasticsearchClientSettings(connectionString)
            .DefaultIndex(elasticsearchOptions.IndexName)
            .Authentication(new BasicAuthentication(elasticsearchOptions.Admin.Username, elasticsearchOptions.Admin.Password));

        adminSettings = SetVerboseLogging(adminSettings, configuration);
        AdminElasticsearchClient adminClient = new (adminSettings);
        services.AddSingleton(adminClient);

        ElasticsearchClientSettings userSettings = new ElasticsearchClientSettings(connectionString)
            .DefaultIndex(elasticsearchOptions.IndexName)
            .Authentication(new BasicAuthentication(elasticsearchOptions.User.Username, elasticsearchOptions.User.Password));

        userSettings = SetVerboseLogging(userSettings, configuration);
        UserElasticsearchClient userClient = new (userSettings);
        services.AddSingleton(userClient);

        services.AddSingleton<ElasticsearchService>();
    }

    private static ElasticsearchClientSettings SetVerboseLogging(ElasticsearchClientSettings settings, IConfiguration configuration)
    {
        return configuration.GetValue<bool>("Elasticsearch:VerboseLogging")
            ? settings.DisableDirectStreaming().PrettyJson()
            : settings;
    }
    
    private static ElasticsearchOptions GetElasticsearchOptions(IConfiguration configuration)
    {
        var options = new ElasticsearchOptions();
        configuration.GetSection("Elasticsearch").Bind(options);
        return options;
    }
}
