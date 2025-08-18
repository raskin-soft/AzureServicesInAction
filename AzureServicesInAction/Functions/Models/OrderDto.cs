// Models/OrderDto.cs

namespace Functions.Models
{
    public sealed class OrderDto
    {
        public string CustomerName { get; set; } = default!;
        public string ProductId { get; set; } = default!;
        public int Quantity { get; set; }
    }
}
