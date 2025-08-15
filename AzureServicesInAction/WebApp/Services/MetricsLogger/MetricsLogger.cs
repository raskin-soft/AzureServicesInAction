using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;

namespace WebApp.Services.MetricsLogger
{
    public class MetricsLogger : IMetricsLogger
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly TelemetryOptions _options;

        public MetricsLogger(IOptions<TelemetryOptions> options, TelemetryClient telemetryClient)
        {
            _options = options.Value;
            _telemetryClient = telemetryClient;
        }

        public void TrackPerformance(string name, double value)
        {
            if (_options.EnableLogging)
            {
                _telemetryClient.TrackMetric(name, value);
            }
        }

        public void TrackEvent(string name, IDictionary<string, string> properties = null)
        {
            if (_options.EnableLogging)
            {
                _telemetryClient.TrackEvent(name, properties);
            }
        }
    }

    public class TelemetryOptions
    {
        public bool EnableLogging { get; set; }
    }
}
