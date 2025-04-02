// https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/examples.html

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Signify.ElasticGrafanaPoc;

namespace Signify.ElasticGrafanaPoc
{
    class Program
    {
        static async Task Main(string[] args_)
        { 
            var ctx = new ElasticContext();
            var isHealthy = await ctx.IsHealthyAsync();
            if (!isHealthy)
            {
                throw new HttpRequestException("Connection to ElasticSearch is unhealthy.");
            }

            await ctx.SeedData();

            Console.WriteLine("Hello, World!");
        }
    }        
}
