using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Core.Tools;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.Vendors.Serilog;
using Datadog.Trace.Vendors.Serilog.Configuration;
using Datadog.Trace.Vendors.Serilog.Core;
using Datadog.Trace.Vendors.Serilog.Events;
using Datadog.Trace.Vendors.Serilog.Formatting;
using Datadog.Trace.Vendors.Serilog.Formatting.Display;
using Datadog.Trace.Vendors.Serilog.Sinks.File;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Datadog.Trace.IntegrationTests
{
    public class DatadogHttpClientTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public DatadogHttpClientTests(ITestOutputHelper output)
        {
            _output = output;
            var logger = new LoggerConfiguration()
                        .WriteTo.TestOutput(output)
                        .CreateLogger()
                        .ForContext<DatadogHttpClientTests>();
            (DatadogLogging.GetLoggerFor<DatadogHttpClientTests>() as DatadogSerilogLogger)?.SetLogger(logger);
        }

        public void Dispose()
        {
#pragma warning disable 618
            var original = DatadogLogging.For<DatadogHttpClientTests>();
            var logger = DatadogLogging.GetLoggerFor<DatadogHttpClientTests>() as DatadogSerilogLogger;
            logger?.SetLogger(original);
#pragma warning restore 618
        }

        [Fact]
        public async Task DatadogHttpClient_CanSendTracesToAgent()
        {
            var agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            {
                agent.RequestReceived += (sender, args) =>
                {
                    _output.WriteLine("Request received");
                };

                agent.RequestDeserialized += (sender, args) =>
                {
                    var spanIds = args.Value
                                      .SelectMany(trace => trace.Select(x => x.SpanId.ToString()));
                    _output.WriteLine($"Spans received: {string.Join(",", spanIds)}");
                };

                var settings = new TracerSettings { AgentUri = new Uri($"http://localhost:{agent.Port}"), TracesTransport = TransportStrategy.DatadogTcp, };
                var tracer = new Tracer(settings);

                using (var scope = tracer.StartActive("operationName"))
                {
                    scope.Span.ResourceName = "resourceName";
                }

                await tracer.FlushAsync();

                var spans = agent.WaitForSpans(1);
                Assert.Equal(1, spans.Count);
            }
        }
    }

#pragma warning disable SA1402

    /// <summary>
    /// From https://github.com/trbenning/serilog-sinks-xunit/blob/master/src/Serilog.Sinks.XUnit/XUnitLoggerConfigurationExtensions.cs
    /// </summary>
    internal static class TestOutputLoggerConfigurationExtensions
    {
        private const string DefaultConsoleOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}";

        public static LoggerConfiguration TestOutput(
            this LoggerSinkConfiguration sinkConfiguration,
            ITestOutputHelper testOutputHelper,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = DefaultConsoleOutputTemplate,
            IFormatProvider formatProvider = null,
            LoggingLevelSwitch levelSwitch = null)
        {
            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            return sinkConfiguration.Sink(new TestOutputSink(testOutputHelper, formatter), restrictedToMinimumLevel, levelSwitch);
        }

        public static LoggerConfiguration TestOutput(
            this LoggerSinkConfiguration sinkConfiguration,
            ITestOutputHelper testOutputHelper,
            ITextFormatter formatter,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null)
        {
            return sinkConfiguration.Sink(new TestOutputSink(testOutputHelper, formatter), restrictedToMinimumLevel, levelSwitch);
        }
    }

    /// <summary>
    /// From: https://github.com/trbenning/serilog-sinks-xunit/blob/master/src/Serilog.Sinks.XUnit/Sinks/XUnit/TestOutputSink.cs
    /// </summary>
    internal class TestOutputSink : ILogEventSink
    {
        private readonly IMessageSink _messageSink;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ITextFormatter _textFormatter;

        public TestOutputSink(IMessageSink messageSink, ITextFormatter textFormatter)
        {
            _messageSink = messageSink ?? throw new ArgumentNullException(nameof(messageSink));
            _textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
        }

        public TestOutputSink(ITestOutputHelper testOutputHelper, ITextFormatter textFormatter)
        {
            _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
            _textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);
            var message = renderSpace.ToString().Trim();
            _messageSink?.OnMessage(new DiagnosticMessage(message));
            _testOutputHelper?.WriteLine(message);
        }
    }
}
