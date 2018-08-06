using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.History.Contracts.Cqrs.Commands;
using Lykke.Service.PostProcessing.Settings;
using Lykke.SettingsReader;

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
                var messagingEngine = new MessagingEngine(
                    logFactory,
                    new TransportResolver(
                        new Dictionary<string, TransportInfo>
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
                return CreateEngine(ctx, messagingEngine, logFactory);
            })
                .As<ICqrsEngine>()
                .AutoActivate()
                .SingleInstance();
        }

        private CqrsEngine CreateEngine(
            IComponentContext ctx,
            IMessagingEngine messagingEngine,
            ILogFactory logFactory)
        {
            const string defaultRoute = "commands";

            return new CqrsEngine(
                logFactory,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                    "RabbitMq",
                    Messaging.Serialization.SerializationFormat.ProtoBuf,
                    environment: "lykke")),

                Register.DefaultRouting
                    .PublishingCommands(
                        typeof(SaveCashinCommand),
                        typeof(SaveCashoutCommand),
                        typeof(SaveTransferCommand),
                        typeof(SaveExecutionCommand))
                    .To("history").With(defaultRoute));
        }
    }
}
