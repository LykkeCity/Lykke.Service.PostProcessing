using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Deduplication;
using MongoDB.Driver;

namespace Lykke.Service.PostProcessing.Services.Deduplicator
{
    public class MongoStorageDeduplicator : IDeduplicator
    {
        private readonly MongoDuplicatesRepository _repository;
        private const string DbName = "MongoDeduplicator";

        public MongoStorageDeduplicator(IMongoClient mongoClient, string tableName)
        {
            _repository = new MongoDuplicatesRepository(mongoClient, DbName, tableName);
        }

        public static MongoStorageDeduplicator Create(string connString, string tableName)
        {
            return new MongoStorageDeduplicator(new MongoClient(connString), tableName);
        }
        
        public Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            return _repository.EnsureNotDuplicateAsync(value);
        }
    }
}
