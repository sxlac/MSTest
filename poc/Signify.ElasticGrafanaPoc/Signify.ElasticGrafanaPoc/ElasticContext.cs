using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Signify.ElasticGrafanaPoc
{
    public class ElasticContext
    {        
        private ElasticsearchClient Client { get; set; }

        // An index is the equivalent of a database. 
        // Each type that we save in that index can be thought of as a SQL Table.
        // Each individual item we save can be thought of as a row.
        // https://www.elastic.co/blog/what-is-an-elasticsearch-index
        private const string Index = "poc";

        public ElasticContext()
        {
            var settings = new ElasticsearchClientSettings(new Uri(Config.ElasticUri))
                    .CertificateFingerprint(Config.CertificateFingerprint) 
                    .Authentication(new BasicAuthentication(Config.Username, Config.Password)); 

            Client = new ElasticsearchClient(settings);
        }

        public async Task<CreateResponse> CreateEventAsync(SigEvent entity)
        {
            var response = await Client.CreateAsync(entity, request => request.Index(Index));
            return response;
        }
        public async Task<IndexResponse> IndexEventAsync(SigEvent entity)
        {
            var response = await Client.IndexAsync(entity, request => request.Index(Index));           
            return response;
        }

        public async Task<GetResponse<SigEvent>> GetEventAsync(int id)
        {
            var response = await Client.GetAsync<SigEvent>(id, idx => idx.Index(Index));
            return response;
        }

        public async Task<UpdateResponse<SigEvent>> UpdateEventAsync(SigEvent entity)
        {
            var response = await Client.UpdateAsync<SigEvent, object>
                (Index, entity.Id, u => u.Doc(entity));
            return response;
        }

        public async Task<DeleteResponse> DeleteEventAsync(SigEvent entity)
        {
            var response = await Client.DeleteAsync(Index, entity.Id);
            return response;
        }

        public async Task<SearchResponse<SigEvent>> FindByProductAsync(string product)
        {
            var response = await Client.SearchAsync<SigEvent>(s => s
            .Index(Index)
            .From(0)
            .Size(50)
            .Query(q => q.Term(t => t.Product, product))
            );

            return response;
        }

        public async Task<bool> IsHealthyAsync()
        {
            var ping = await Client.PingAsync();
            return ping.IsSuccess();
        }

        public async Task SeedData()
        {
            Random r = new Random();
            var products = new string[] { "CKD", "eGFR", "CKD", "eGFR", "PAD", "DEE" };
            var now = DateTime.Now;

            for (int i = 0; i < 50; i++)
            {
                var evt = new SigEvent()
                {
                    Id = i,
                    EventType = "EvaluationFinalized",
                    Date = now.AddMinutes(-r.Next(0, 60)),
                    Product = products[r.Next(0, 6)],
                    Guid = Guid.NewGuid()
                };
                await IndexEventAsync(evt);
            }
        }
    }
}
