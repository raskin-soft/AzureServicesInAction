// Triggers/Http/CreateOrderFunction.cs

using Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Functions.Triggers.Http
{
    public class CreateOrderFunction(ILogger<CreateOrderFunction> logger)
    {
        /* TESTING THE FUNCTION
         >> Azure portal > function app > click on function(CreateOrder) > Get Function URL
         >> Postman
                - Method: POST
                - URL: https://yourapp.azurewebsites.net/api/orders?code=...
                - Headers: Content-Type: application/json
                - Body: raw JSON
                            {
                              "customerName": "Raskinur",
                              "productId": "P123",
                              "quantity": 2
                            }

            >> Now check the Invocations in the Azure portal to see the logs
         */


        [Function("CreateOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
        {

            logger.LogInformation("Order received: {Customer} ordered {Qty} of {Product}", "Raskin", "4", "11111111");

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync($"Order confirmed for Raskin");
            return response;
        }


        // ----------------- This is an alternative implementation that uses a queue to process orders asynchronously -----------------
        //[Function("CreateOrder")]
        //public async Task Run(
        //    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req,
        //    [Queue("orders-queue", Connection = "AzureWebJobsStorage")] IAsyncCollector<string> queueCollector)
        //{
        //    var order = await req.ReadFromJsonAsync<OrderMessage>();
        //    var json = JsonSerializer.Serialize(order);
        //    await queueCollector.AddAsync(json);
        //}



        //[Function("CreateOrder")]
        //public async Task<HttpResponseData> Run(
        //    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
        //{
        //    var body = await req.ReadAsStringAsync();
        //    var order = JsonSerializer.Deserialize<OrderDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //    if (order is null || string.IsNullOrWhiteSpace(order.CustomerName) || string.IsNullOrWhiteSpace(order.ProductId) || order.Quantity <= 0)
        //    {
        //        logger.LogWarning("Invalid order received: {Body}", body);
        //        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        //        await badResponse.WriteStringAsync("Invalid order payload.");
        //        return badResponse;
        //    }

        //    logger.LogInformation("Order received: {Customer} ordered {Qty} of {Product}", order.CustomerName, order.Quantity, order.ProductId);

        //    var response = req.CreateResponse(HttpStatusCode.Created);
        //    await response.WriteStringAsync($"Order confirmed for {order.CustomerName}");
        //    return response;
        //}

    }
}
