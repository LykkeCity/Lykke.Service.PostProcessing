using MongoDB.Bson.Serialization.Attributes;

namespace Lykke.Service.PostProcessing.Services.Deduplicator
{
    public class MongoDuplicateEntity
    {
        [BsonId]
        public string BsonId { get; set; }

    }
}
