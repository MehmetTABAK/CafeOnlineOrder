namespace ProjectCafeWeb.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
        public int? StockCount { get; set; }
        public bool IsThereDiscount { get; set; }
        public double? DiscountRate { get; set; }
        public int Quantity { get; set; }
        public string? Image { get; set; }
        public double TotalPrice => Price * Quantity;
    }
}
