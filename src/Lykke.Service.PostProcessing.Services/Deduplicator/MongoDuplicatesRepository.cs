using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Deduplication;
using MongoDB.Driver;

namespace Lykke.Service.PostProcessing.Services.Deduplicator
{
    public class MongoDuplicatesRepository
    {
        private readonly IMongoCollection<MongoDuplicateEntity> _collection;

        public MongoDuplicatesRepository(IMongoClient mongoClient, string dbName, string collectionName)
        {
            IMongoDatabase db = mongoClient.GetDatabase(dbName);
            _collection = db.GetCollection<MongoDuplicateEntity>(collectionName);
        }

        public async Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            try
            {
                string id = HashHelper.GetMd5Hash(value);
                await _collection.InsertOneAsync(new MongoDuplicateEntity {BsonId = id});
                return true;
            }
            catch (MongoWriteException ex)
            {
                if (ex.Message.Contains("duplicate key error"))
                    return false;

                throw;
            }
        }
    }
}
