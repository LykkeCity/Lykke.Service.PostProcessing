﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Deduplication;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.PostProcessing.Settings;

namespace Lykke.Service.PostProcessing.RabbitSubscribers
{
    [UsedImplicitly]
    public class MeRabbitSubscriber : IStartable, IStopable
    {
        [NotNull] private readonly ILogFactory _logFactory;
        private readonly RabbitMqSettings _rabbitMqSettings;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly List<IStopable> _subscribers = new List<IStopable>();

        private const string QueueName = "lykke.spot.matching.engine.out.events.post-processing";
        private const bool QueueDurable = true;

        public MeRabbitSubscriber(
            [NotNull] ILogFactory logFactory,
            [NotNull] RabbitMqSettings rabbitMqSettings,
            [NotNull] ICqrsEngine cqrsEngine)
        {
            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
            _rabbitMqSettings = rabbitMqSettings ?? throw new ArgumentNullException(nameof(rabbitMqSettings));
            _cqrsEngine = cqrsEngine ?? throw new ArgumentNullException(nameof(cqrsEngine));
        }

        public void Start()
        {
            _subscribers.Add(Subscribe<CashInEvent>(MatchingEngine.Connector.Models.Events.Common.MessageType.CashIn, ProcessMessageAsync));
            _subscribers.Add(Subscribe<CashOutEvent>(MatchingEngine.Connector.Models.Events.Common.MessageType.CashOut, ProcessMessageAsync));
            _subscribers.Add(Subscribe<CashTransferEvent>(MatchingEngine.Connector.Models.Events.Common.MessageType.CashTransfer, ProcessMessageAsync));
            _subscribers.Add(Subscribe<ExecutionEvent>(MatchingEngine.Connector.Models.Events.Common.MessageType.Order, ProcessMessageAsync));
        }

        private RabbitMqSubscriber<T> Subscribe<T>(MatchingEngine.Connector.Models.Events.Common.MessageType messageType, Func<T, Task> func)
        {
            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _rabbitMqSettings.ConnectionString,
                QueueName = $"{QueueName}.{messageType}",
                ExchangeName = _rabbitMqSettings.Exchange,
                RoutingKey = ((int)messageType).ToString(),
                IsDurable = QueueDurable
            };

            return new RabbitMqSubscriber<T>(
                    _logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_logFactory, settings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<T>())
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .Subscribe(func)
                .CreateDefaultBinding()
                .SetAlternativeExchange(_rabbitMqSettings.AlternativeExchange)
                .SetDeduplicator(new InMemoryDeduplcator(TimeSpan.FromDays(7)))
                .Start();
        }


        private Task ProcessMessageAsync(CashInEvent message)
        {
            var fee = message.CashIn.Fees?.FirstOrDefault()?.Transfer;
            var command = new Lykke.Service.History.Contracts.Cqrs.Commands.SaveCashinCommand
            {
                Id = Guid.Parse(message.Header.MessageId),
                WalletId = Guid.Parse(message.CashIn.WalletId),
                Volume = decimal.Parse(message.CashIn.Volume),
                AssetId = message.CashIn.AssetId,
                Timestamp = message.Header.Timestamp,
                FeeSize = fee != null ? decimal.Parse(fee.Volume) : 0m
            };
            _cqrsEngine.SendCommand(command, "PostProcessing", "history");
            return Task.CompletedTask;
        }

        private Task ProcessMessageAsync(CashOutEvent message)
        {
            var fee = message.CashOut.Fees?.FirstOrDefault()?.Transfer;
            var command = new Lykke.Service.History.Contracts.Cqrs.Commands.SaveCashoutCommand
            {
                Id = Guid.Parse(message.Header.MessageId),
                WalletId = Guid.Parse(message.CashOut.WalletId),
                Volume = decimal.Parse(message.CashOut.Volume),
                AssetId = message.CashOut.AssetId,
                Timestamp = message.Header.Timestamp,
                FeeSize = fee != null ? decimal.Parse(fee.Volume) : 0m
            };
            _cqrsEngine.SendCommand(command, "PostProcessing", "history");
            return Task.CompletedTask;
        }

        private Task ProcessMessageAsync(CashTransferEvent message)
        {
            var fee = message.CashTransfer.Fees?.FirstOrDefault()?.Transfer;
            var command = new Lykke.Service.History.Contracts.Cqrs.Commands.SaveTransferCommand
            {
                Id = Guid.Parse(message.Header.MessageId),
                FromWalletId = Guid.Parse(message.CashTransfer.FromWalletId),
                ToWalletId = Guid.Parse(message.CashTransfer.ToWalletId),
                Volume = decimal.Parse(message.CashTransfer.Volume),
                AssetId = message.CashTransfer.AssetId,
                Timestamp = message.Header.Timestamp,
                FeeSourceWalletId = fee != null ? Guid.Parse(fee.SourceWalletId) : (Guid?)null,
                FeeSize = fee != null ? decimal.Parse(fee.Volume) : 0m
            };
            _cqrsEngine.SendCommand(command, "PostProcessing", "history");
            return Task.CompletedTask;
        }

        private Task ProcessMessageAsync(ExecutionEvent message)
        {
            var command = new Lykke.Service.History.Contracts.Cqrs.Commands.SaveExecutionCommand
            {
                SequenceNumber = message.Header.SequenceNumber,
                Orders = message.Orders.Select(x => new Lykke.Service.History.Contracts.Cqrs.Models.OrderModel
                {
                    Id = Guid.Parse(x.ExternalId),
                    WalletId = Guid.Parse(x.WalletId),
                    Volume = decimal.Parse(x.Volume),
                    AssetPairId = x.AssetPairId,
                    CreateDt = x.CreatedAt,
                    LowerLimitPrice = decimal.Parse(x.LowerLimitPrice),
                    LowerPrice = decimal.Parse(x.LowerPrice),
                    MatchDt = x.LastMatchTime,
                    MatchingId = Guid.Parse(x.Id),
                    Price = decimal.Parse(x.Price),
                    RegisterDt = x.Registered,
                    RejectReason = x.RejectReason,
                    RemainingVolume = decimal.Parse(x.RemainingVolume),
                    Side = (Lykke.Service.History.Contracts.Cqrs.Models.Enums.OrderSide)(int)x.Side,
                    Status = (Lykke.Service.History.Contracts.Cqrs.Models.Enums.OrderStatus)(int)x.Status,
                    StatusDt = x.StatusDate,
                    Straight = x.Straight ?? true,
                    Type = (Lykke.Service.History.Contracts.Cqrs.Models.Enums.OrderType)(int)x.OrderType,
                    UpperLimitPrice = decimal.Parse(x.UpperLimitPrice),
                    UpperPrice = decimal.Parse(x.UpperPrice),
                    Trades = x.Trades.Select(t => new Lykke.Service.History.Contracts.Cqrs.Models.TradeModel
                    {
                        WalletId = Guid.Parse(x.WalletId),
                        Volume = decimal.Parse(t.Volume),
                        Id = Guid.Parse(t.TradeId),
                        AssetId = t.AssetId,
                        Timestamp = t.Timestamp,
                        AssetPairId = x.AssetPairId,
                        Price = decimal.Parse(t.Price),
                        FeeSize = decimal.Parse(t.Fees?.FirstOrDefault()?.Volume ?? "0"),
                        FeeAssetId = t.Fees?.FirstOrDefault()?.AssetId,
                        Index = t.Index,
                        OppositeAssetId = t.OppositeAssetId,
                        OppositeVolume = decimal.Parse(t.OppositeVolume),
                        Role = (Lykke.Service.History.Contracts.Cqrs.Commands.Models.TradeRole)(int)t.Role
                    })
                }).ToList()
            };
            _cqrsEngine.SendCommand(command, "PostProcessing", "history");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber?.Dispose();
            }

        }

        public void Stop()
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber?.Stop();
            }
        }
    }
}