namespace WebApp.Services.MetricsLogger
{
    public interface IMetricsLogger
    {
        void TrackPerformance(string name, double value);
        void TrackEvent(string eventName, IDictionary<string, string> properties);
    }
}
