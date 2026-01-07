using Elastic.Clients.Elasticsearch;

namespace ElasticSearch;

public class UserElasticsearchClient(ElasticsearchClientSettings settings)
{
    public ElasticsearchClient Client { get; } = new(settings);
}