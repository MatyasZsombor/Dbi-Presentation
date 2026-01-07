namespace Elasticsearch;

using Elastic.Clients.Elasticsearch;

public class AdminElasticsearchClient(ElasticsearchClientSettings settings)
{ 
    public ElasticsearchClient Client { get; } = new(settings);
}