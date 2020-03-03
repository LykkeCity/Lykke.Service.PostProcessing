using System;
using Common;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Lykke.Service.PostProcessing.Core
{
    public class TelemetryHelper
    {
        private static readonly TelemetryClient Telemetry = new TelemetryClient();

        public static IOperationHolder<RequestTelemetry> InitTelemetryOperation(
            string name,
            string id = null,
            string parentId = null)
        {

            var requestTelemetry = new RequestTelemetry { Name = name };

            if (!string.IsNullOrEmpty(id))
            {
                requestTelemetry.Id = id;
                requestTelemetry.Context.Operation.Id = id;
            }

            if (!string.IsNullOrEmpty(parentId))
            {
                requestTelemetry.Context.Operation.ParentId = parentId;
            }

            var operation = Telemetry.StartOperation(requestTelemetry);

            var json = new
            {
                operationId = operation.Telemetry.Context.Operation.Id,
                parentId = operation.Telemetry.Context.Operation.ParentId,
                name = operation.Telemetry.Name
            }.ToJson();

            Console.WriteLine($"sending request telemetry: {json}");

            return operation;
        }

        public static void SubmitException(IOperationHolder<RequestTelemetry> telemtryOperation, Exception e)
        {
            telemtryOperation.Telemetry.Success = false;
            Telemetry.TrackException(e);
        }

        public static void SubmitOperationResult(IOperationHolder<RequestTelemetry> telemtryOperation)
        {
            Telemetry.StopOperation(telemtryOperation);
        }
    }
}
