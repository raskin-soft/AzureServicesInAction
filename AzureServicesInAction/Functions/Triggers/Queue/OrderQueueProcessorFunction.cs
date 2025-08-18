// Triggers/Queue/OrderQueueProcessorFunction.cs

using Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Functions.Triggers.Queue
{


    public class OrderQueueProcessorFunction(ILogger<OrderQueueProcessorFunction> logger)
    {
        [Function("OrderQueueProcessor")]
        public async Task Run(
            [Microsoft.Azure.Functions.Worker.QueueTrigger("orders-queue", Connection = "AzureWebJobsStorage")] string message)
        {
            logger.LogInformation("Queue message received: {Message}", message);

            var order = JsonSerializer.Deserialize<OrderMessage>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (order is null)
            {
                logger.LogWarning("Invalid order message format.");
                return;
            }

            logger.LogInformation("Processing order {OrderId} for {Customer}", order.OrderId, order.CustomerName);

            // Simulate processing
            await Task.Delay(500); // pretend to call DB, API, etc.

            logger.LogInformation("Order {OrderId} processed successfully.");
        }
    }
}
