// Models/OrderMessage.cs

namespace Functions.Models
{
    public sealed class OrderMessage
    {
        public string OrderId { get; set; } = default!;
        public string CustomerName { get; set; } = default!;
        public string ProductId { get; set; } = default!;
        public int Quantity { get; set; }
    }
}
