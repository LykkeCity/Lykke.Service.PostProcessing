using Autofac;
using Common;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Deduplication;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Events;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Models;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Models.Enums;
using Lykke.Service.PostProcessing.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.PostProcessing.Core;
using Common.Log;
using Google.Protobuf.WellKnownTypes;
using Lykke.Mailerlite.ApiClient;
using Lykke.Mailerlite.ApiContract;
using OrderStatus = Lykke.Service.PostProcessing.Contracts.Cqrs.Models.Enums.OrderStatus;
using OrderType = Lykke.MatchingEngine.Connector.Models.Events.OrderType;
using TradeRole = Lykke.Service.PostProcessing.Contracts.Cqrs.Models.Enums.TradeRole;

namespace Lykke.Service.PostProcessing.RabbitSubscribers
{
    [UsedImplicitly]
    public class MeRabbitSubscriber : IStartable, IStopable
    {
        [NotNull] private readonly ILogFactory _logFactory;
        private readonly RabbitMqSettings _rabbitMqSettings;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly List<IStopable> _subscribers = new List<IStopable>();
        private readonly IDeduplicator _deduplicator;
        private readonly IReadOnlyList<string> _walletIds;
        private readonly ILykkeMailerliteClient _lykkeMailerliteClient;
        private readonly ILog _log;

        private const string QueueName = "lykke.spot.matching.engine.out.events.post-processing";
        private const bool QueueDurable = true;

        public MeRabbitSubscriber(
            [NotNull] ILogFactory logFactory,
            [NotNull] RabbitMqSettings rabbitMqSettings,
            [NotNull] ICqrsEngine cqrsEngine,
            [NotNull] IDeduplicator deduplicator,
            [NotNull] ILykkeMailerliteClient lykkeMailerliteClient,
            IReadOnlyList<string> walletIds)
        {
            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
            _log = _logFactory.CreateLog(this);
            _rabbitMqSettings = rabbitMqSettings ?? throw new ArgumentNullException(nameof(rabbitMqSettings));
            _cqrsEngine = cqrsEngine ?? throw new ArgumentNullException(nameof(cqrsEngine));
            _deduplicator = deduplicator ?? throw new ArgumentNullException(nameof(deduplicator));
            _lykkeMailerliteClient = lykkeMailerliteClient ?? throw new ArgumentNullException(nameof(lykkeMailerliteClient));
            _walletIds = walletIds;
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
                IsDurable = QueueDurable,
                DeadLetterExchangeName = $"{QueueName}.{messageType}.dlx"
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
                .SetAlternativeExchange(_rabbitMqSettings.AlternativeConnectionString)
                .SetDeduplicator(_deduplicator)
                .Start();
        }


        private async Task ProcessMessageAsync(CashInEvent message)
        {
            var operation = TelemetryHelper.InitTelemetryOperation($"Processing {nameof(CashInEvent)} message", message.Header.RequestId);
            var fees = message.CashIn.Fees;
            var fee = fees?.FirstOrDefault()?.Transfer;
            var @event = new CashInProcessedEvent
            {
                OperationId = Guid.Parse(message.Header.RequestId),
                RequestId = Guid.Parse(message.Header.MessageId),
                WalletId = Guid.Parse(message.CashIn.WalletId),
                Volume = decimal.Parse(message.CashIn.Volume),
                AssetId = message.CashIn.AssetId,
                Timestamp = message.Header.Timestamp,
                FeeSize = ParseNullabe(fee?.Volume)
            };
            _cqrsEngine.PublishEvent(@event, BoundedContext.Name);

            if (fees != null)
            {
                var feeEvent = new FeeChargedEvent
                {
                    OperationId = message.Header.MessageId,
                    OperationType = FeeOperationType.CashInOut,
                    Fee = fees.Where(x => x.Transfer != null).Select(x => x.Transfer).ToJson()
                };
                _cqrsEngine.PublishEvent(feeEvent, BoundedContext.Name);
            }

            await _lykkeMailerliteClient.Customers.UpdateDepositAsync(new UpdateCustomerDepositRequest
            {
                CustomerId = message.CashIn.WalletId, RequestId = Guid.NewGuid().ToString(), Timestamp = message.Header.Timestamp.ToTimestamp()
            });
        }

        private Task ProcessMessageAsync(CashOutEvent message)
        {
            var operation = TelemetryHelper.InitTelemetryOperation($"Processing {nameof(CashOutEvent)} message", message.Header.RequestId);
            var fees = message.CashOut.Fees;
            var fee = fees?.FirstOrDefault()?.Transfer;
            var @event = new CashOutProcessedEvent
            {
                OperationId = Guid.Parse(message.Header.RequestId),
                RequestId = Guid.Parse(message.Header.MessageId),
                WalletId = Guid.Parse(message.CashOut.WalletId),
                Volume = decimal.Parse(message.CashOut.Volume),
                AssetId = message.CashOut.AssetId,
                Timestamp = message.Header.Timestamp,
                FeeSize = ParseNullabe(fee?.Volume)
            };
            _cqrsEngine.PublishEvent(@event, BoundedContext.Name);

            if (fees != null)
            {
                var feeEvent = new FeeChargedEvent
                {
                    OperationId = message.Header.MessageId,
                    OperationType = FeeOperationType.CashInOut,
                    Fee = fees.Where(x => x.Transfer != null).Select(x => x.Transfer).ToJson()
                };
                _cqrsEngine.PublishEvent(feeEvent, BoundedContext.Name);
            }

            TelemetryHelper.SubmitOperationResult(operation);

            return Task.CompletedTask;
        }

        private Task ProcessMessageAsync(CashTransferEvent message)
        {
            var operation = TelemetryHelper.InitTelemetryOperation($"Processing {nameof(CashTransferEvent)} message", message.Header.RequestId);
            var fees = message.CashTransfer.Fees;
            var fee = fees?.FirstOrDefault()?.Transfer;
            var @event = new CashTransferProcessedEvent
            {
                OperationId = Guid.Parse(message.Header.RequestId),
                RequestId = Guid.Parse(message.Header.MessageId),
                FromWalletId = Guid.Parse(message.CashTransfer.FromWalletId),
                ToWalletId = Guid.Parse(message.CashTransfer.ToWalletId),
                Volume = decimal.Parse(message.CashTransfer.Volume),
                AssetId = message.CashTransfer.AssetId,
                Timestamp = message.Header.Timestamp,
                FeeSourceWalletId = fee != null ? Guid.Parse(fee.SourceWalletId) : (Guid?)null,
                FeeSize = ParseNullabe(fee?.Volume)
            };
            _cqrsEngine.PublishEvent(@event, BoundedContext.Name);

            if (fees != null)
            {
                var feeEvent = new FeeChargedEvent
                {
                    OperationId = message.Header.MessageId,
                    OperationType = FeeOperationType.Transfer,
                    Fee = fees.Where(x => x.Transfer != null).Select(x => x.Transfer).ToJson()
                };
                _cqrsEngine.PublishEvent(feeEvent, BoundedContext.Name);
            }

            TelemetryHelper.SubmitOperationResult(operation);

            return Task.CompletedTask;
        }

        private Task ProcessMessageAsync(ExecutionEvent message)
        {
            try
            {
                var orders = message.Orders.Select(x => new OrderModel
                {
                    Id = Guid.Parse(x.ExternalId),
                    WalletId = Guid.Parse(x.WalletId),
                    Volume = decimal.Parse(x.Volume),
                    AssetPairId = x.AssetPairId,
                    CreateDt = x.CreatedAt,
                    LowerLimitPrice = ParseNullabe(x.LowerLimitPrice),
                    LowerPrice = ParseNullabe(x.LowerPrice),
                    MatchDt = x.LastMatchTime,
                    MatchingId = Guid.Parse(x.Id),
                    Price = ParseNullabe(x.Price),
                    RegisterDt = x.Registered,
                    RejectReason = x.RejectReason,
                    RemainingVolume = ParseNullabe(x.RemainingVolume),
                    Side = (Contracts.Cqrs.Models.Enums.OrderSide)(int)x.Side,
                    Status = (Contracts.Cqrs.Models.Enums.OrderStatus)(int)x.Status,
                    StatusDt = x.StatusDate,
                    Straight = x.OrderType == OrderType.Limit || x.OrderType == OrderType.StopLimit || x.Straight,
                    Type = (Contracts.Cqrs.Models.Enums.OrderType)(int)x.OrderType,
                    UpperLimitPrice = ParseNullabe(x.UpperLimitPrice),
                    UpperPrice = ParseNullabe(x.UpperPrice),
                    Trades = x.Trades?.Select(t => new TradeModel
                    {
                        Id = Guid.Parse(t.TradeId),
                        WalletId = Guid.Parse(x.WalletId),
                        AssetPairId = x.AssetPairId,
                        BaseAssetId = t.BaseAssetId,
                        BaseVolume = decimal.Parse(t.BaseVolume),
                        Price = decimal.Parse(t.Price),
                        Timestamp = t.Timestamp,
                        QuotingAssetId = t.QuotingAssetId,
                        QuotingVolume = decimal.Parse(t.QuotingVolume),
                        Index = t.Index,
                        Role = (Contracts.Cqrs.Models.Enums.TradeRole)(int)t.Role,
                        FeeSize = ParseNullabe(t.Fees?.FirstOrDefault()?.Volume),
                        FeeAssetId = t.Fees?.FirstOrDefault()?.AssetId,
                        OppositeWalletId = Guid.Parse(t.OppositeWalletId),
                    }).ToList()
                }).ToList();

                foreach (var order in orders.Where(x => _walletIds.Contains(x.WalletId.ToString())))
                {
                    _log.Info("Order from ME", $"order: {new { order.Id, order.Status, message.Header.SequenceNumber }.ToJson()}");
                }

                var @event = new ExecutionProcessedEvent { SequenceNumber = message.Header.SequenceNumber, Orders = orders };
                _cqrsEngine.PublishEvent(@event, BoundedContext.Name);

                foreach (var order in orders.Where(x => x.Trades != null && x.Trades.Any()))
                {
                    var tradeProcessedEvent = new ManualOrderTradeProcessedEvent { Order = order };

                    _cqrsEngine.PublishEvent(tradeProcessedEvent, BoundedContext.Name);
                }

                foreach (var order in message.Orders.Where(x => x.Trades != null && x.Trades.Count > 0))
                {
                    var orderType = order.OrderType == OrderType.Market ? FeeOperationType.Trade : FeeOperationType.LimitTrade;
                    var orderId = order.Id;
                    foreach (var trade in order.Trades)
                    {
                        if (trade.Fees != null)
                        {
                            var feeEvent = new FeeChargedEvent { OperationId = orderId, OperationType = orderType, Fee = trade.Fees.ToJson() };

                            _cqrsEngine.PublishEvent(feeEvent, BoundedContext.Name);
                        }
                    }
                }

                var limitOrders = orders.Where(x => x.Type == Contracts.Cqrs.Models.Enums.OrderType.Limit ||
                                                    x.Type == Contracts.Cqrs.Models.Enums.OrderType.StopLimit).ToList();
                foreach (var order in limitOrders.Where(x => x.Status == OrderStatus.Cancelled))
                {
                    var orderCancelledEvent = new OrderCancelledEvent
                    {
                        OrderId = order.Id,
                        Status = order.Status,
                        AssetPairId = order.AssetPairId,
                        Price = order.Price,
                        Timestamp = order.StatusDt,
                        Volume = order.Volume,
                        WalletId = order.WalletId
                    };
                    _cqrsEngine.PublishEvent(orderCancelledEvent, BoundedContext.Name);
                }

                foreach (var order in limitOrders.Where(x => x.Status == OrderStatus.Placed))
                {
                    var orderPlacedEvent = new OrderPlacedEvent
                    {
                        OrderId = order.Id,
                        Status = order.Status,
                        AssetPairId = order.AssetPairId,
                        Price = order.Price,
                        Timestamp = order.StatusDt,
                        Volume = order.Volume,
                        WalletId = order.WalletId,
                        CreateDt = order.CreateDt
                    };
                    _cqrsEngine.PublishEvent(orderPlacedEvent, BoundedContext.Name);
                }

                foreach (var order in limitOrders.Where(x =>
                    (x.Status == OrderStatus.Matched || x.Status == OrderStatus.PartiallyMatched)
                    && x.Trades.Any(t => t.Role == TradeRole.Taker)))
                {
                    var orderPlacedEvent = new OrderPlacedEvent
                    {
                        OrderId = order.Id,
                        Status = order.Status,
                        AssetPairId = order.AssetPairId,
                        Price = order.Price,
                        Timestamp = order.StatusDt,
                        Volume = order.Volume,
                        WalletId = order.WalletId,
                        CreateDt = order.CreateDt
                    };
                    _cqrsEngine.PublishEvent(orderPlacedEvent, BoundedContext.Name);
                }
            }
            catch (Exception ex)
            {
                _log.Error(message: "Error processing orders", exception:ex, context: message.Orders.Select(x => new { Id = x.ExternalId, ClientId = x.WalletId, Status = x.Status }).ToJson());
            }

            return Task.CompletedTask;
        }

        private decimal? ParseNullabe(string value)
        {
            return !string.IsNullOrEmpty(value) ? decimal.Parse(value) : (decimal?)null;
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