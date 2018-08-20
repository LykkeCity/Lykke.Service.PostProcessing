using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Events;
using Lykke.Service.PostProcessing.Settings;
using Lykke.SettingsReader;
using System.Collections.Generic;

namespace Lykke.Service.PostProcessing.Modules
{
    public class CqrsModule : Module
    {
        private readonly CqrsSettings _settings;

        public CqrsModule(IReloadingManager<AppSettings> settingsManager)
        {
            _settings = settingsManager.CurrentValue.PostProcessingService.Cqrs;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.RabbitConnString
            };
            var rabbitMqEndpoint = rabbitMqSettings.Endpoint.ToString();

            builder.Register(ctx =>
            {
                var logFactory = ctx.Resolve<ILogFactory>();
                return new MessagingEngine(
                    logFactory,
                    new TransportResolver(new Dictionary<string, TransportInfo>
                    {
                        {
                            "RabbitMq",
                            new TransportInfo(
                                rabbitMqEndpoint,
                                rabbitMqSettings.UserName,
                                rabbitMqSettings.Password, "None", "RabbitMq")
                        }
                    }),
                    new RabbitMqTransportFactory(logFactory));
            });

            builder.Register(ctx =>
            {
                const string defaultRoute = "self";

                return new CqrsEngine(
                    ctx.Resolve<ILogFactory>(),
                    ctx.Resolve<IDependencyResolver>(),
                    ctx.Resolve<MessagingEngine>(),
                    new DefaultEndpointProvider(),
                    true,
                    Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                        "RabbitMq",
                        Messaging.Serialization.SerializationFormat.ProtoBuf,
                        environment: "lykke")),

                    Register.BoundedContext(BoundedContext.Name)
                        .PublishingEvents(
                            typeof(FeeChargedEvent),
                            typeof(CashInProcessedEvent),
                            typeof(CashOutProcessedEvent),
                            typeof(CashTransferProcessedEvent),
                            typeof(ExecutionProcessedEvent),
                            typeof(ManualOrderTradeProcessedEvent))
                        .With(defaultRoute),

                    Register.DefaultRouting);
            })
                .As<ICqrsEngine>()
                .SingleInstance();
        }
    }
}
