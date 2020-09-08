// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics
{
    internal class LinuxAppServiceEventGenerator : LinuxEventGenerator
    {
        private readonly LinuxAppServiceFileLoggerFactory _loggerFactory;
        private readonly HostNameProvider _hostNameProvider;
        private LinuxAppServiceFileLogger _traceLogger;
        private LinuxAppServiceFileLogger _metricsLogger;
        private LinuxAppServiceFileLogger _detailsLogger;
        private LinuxAppServiceFileLogger _executionLogger;

        public LinuxAppServiceEventGenerator(LinuxAppServiceFileLoggerFactory loggerFactory, HostNameProvider hostNameProvider)
        {
            _loggerFactory = loggerFactory;
            _hostNameProvider = hostNameProvider ?? throw new ArgumentNullException(nameof(hostNameProvider));
            _traceLogger = _loggerFactory.GetOrCreate(FunctionsLogsCategory);
            _metricsLogger = _loggerFactory.GetOrCreate(FunctionsMetricsCategory);
            _detailsLogger = _loggerFactory.GetOrCreate(FunctionsDetailsCategory);
            _executionLogger = _loggerFactory.GetOrCreate(FunctionsExecutionEventsCategory);
        }

        public static string TraceEventRegex { get; } = $"(?<Level>[0-6]),(?<SubscriptionId>[^,]*),(?<HostName>[^,]*),(?<AppName>[^,]*),(?<FunctionName>[^,]*),(?<EventName>[^,]*),(?<Source>[^,]*),\"(?<Details>.*)\",\"(?<Summary>.*)\",(?<HostVersion>[^,]*),(?<EventTimestamp>[^,]+),(?<ExceptionType>[^,]*),\"(?<ExceptionMessage>.*)\",(?<FunctionInvocationId>[^,]*),(?<HostInstanceId>[^,]*),(?<ActivityId>[^,\"]*)";

        public static string MetricEventRegex { get; } = $"(?<SubscriptionId>[^,]*),(?<AppName>[^,]*),(?<FunctionName>[^,]*),(?<EventName>[^,]*),(?<Average>\\d*),(?<Min>\\d*),(?<Max>\\d*),(?<Count>\\d*),(?<HostVersion>[^,]*),(?<EventTimestamp>[^,]+),(?<Details>[^,\"]*)";

        public static string DetailsEventRegex { get; } = $"(?<AppName>[^,]*),(?<FunctionName>[^,]*),\"(?<InputBindings>.*)\",\"(?<OutputBindings>.*)\",(?<ScriptType>[^,]*),(?<IsDisabled>[0|1])";

        public override void LogFunctionTraceEvent(LogLevel level, string subscriptionId, string appName, string functionName, string eventName,
            string source, string details, string summary, string exceptionType, string exceptionMessage,
            string functionInvocationId, string hostInstanceId, string activityId, string runtimeSiteName, string slotName, DateTime eventTimestamp)
        {
            _traceLogger.Log($"{(int)ToEventLevel(level)},{subscriptionId},{_hostNameProvider.Value},{appName},{functionName},{eventName},{source},{NormalizeString(details)},{NormalizeString(summary)},{ScriptHost.Version},{eventTimestamp.ToString(EventTimestampFormat)},{exceptionType},{NormalizeString(exceptionMessage)},{functionInvocationId},{hostInstanceId},{activityId}");
        }

        public override void LogFunctionMetricEvent(string subscriptionId, string appName, string functionName, string eventName, long average,
            long minimum, long maximum, long count, DateTime eventTimestamp, string data, string runtimeSiteName, string slotName)
        {
            _metricsLogger.Log($"{subscriptionId},{appName},{functionName},{eventName},{average},{minimum},{maximum},{count},{ScriptHost.Version},{eventTimestamp.ToString(EventTimestampFormat)},{data}");
        }

        public override void LogFunctionDetailsEvent(string siteName, string functionName, string inputBindings, string outputBindings,
            string scriptType, bool isDisabled)
        {
            _detailsLogger.Log($"{siteName},{functionName},{NormalizeString(inputBindings)},{NormalizeString(outputBindings)},{scriptType},{(isDisabled ? 1 : 0)}");
        }

        public override void LogFunctionExecutionAggregateEvent(string siteName, string functionName, long executionTimeInMs,
            long functionStartedCount, long functionCompletedCount, long functionFailedCount)
        {
        }

        public override void LogFunctionExecutionEvent(string executionId, string siteName, int concurrency, string functionName,
            string invocationId, string executionStage, long executionTimeSpan, bool success)
        {
            _executionLogger.Log(DateTime.UtcNow.ToString());
        }

        public override void LogAzureMonitorDiagnosticLogEvent(LogLevel level, string resourceId, string operationName, string category, string regionName, string properties)
        {
        }
    }
}
