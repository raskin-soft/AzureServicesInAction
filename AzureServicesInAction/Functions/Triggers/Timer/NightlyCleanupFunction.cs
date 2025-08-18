// Triggers/Timer/NightlyCleanupFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Functions.Triggers.Timer
{
    public class NightlyCleanupFunction(ILogger<NightlyCleanupFunction> logger)
    {
        [Function("NightlyCleanup")]
        public void Run([Microsoft.Azure.Functions.Worker.TimerTrigger("0 */5 * * * *")] Microsoft.Azure.Functions.Worker.TimerInfo timer)
        {
            logger.LogInformation("Nightly cleanup started at: {Time}", DateTime.UtcNow);

            // Simulate cleanup logic
            // e.g., delete old blobs, archive logs, reset counters
            logger.LogInformation("Cleanup completed successfully.");
        }
    }
}
