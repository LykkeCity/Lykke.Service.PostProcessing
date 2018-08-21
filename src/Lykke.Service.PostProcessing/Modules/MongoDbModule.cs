using Autofac;
using Lykke.RabbitMq.Mongo.Deduplicator;
using Lykke.RabbitMqBroker.Deduplication;
using Lykke.Service.PostProcessing.Settings;
using Lykke.SettingsReader;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Lykke.Service.PostProcessing.Modules
{
    internal class MongoDbModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public MongoDbModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(x =>
                {
                    ConventionRegistry.Register("Ignore extra", new ConventionPack { new IgnoreExtraElementsConvention(true) }, _ => true);

                    var mongoUrl = new MongoUrl(_settings.CurrentValue.PostProcessingService.Db.DeduplicationConnectionString);
                    MongoDefaults.GuidRepresentation = GuidRepresentation.Standard;
                    return new MongoClient(mongoUrl);
                })
                .As<IMongoClient>()
                .SingleInstance();

            builder.RegisterType<MongoStorageDeduplicator>()
                .As<IDeduplicator>()
                .WithParameter(TypedParameter.From("postProcessing.ME"))
                .SingleInstance();
        }
    }
}
