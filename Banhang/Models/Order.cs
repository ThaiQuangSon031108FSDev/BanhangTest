namespace Banhang.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string ShipName { get; set; } = string.Empty;
        public string ShipAddress { get; set; } = string.Empty;
        public string ShipPhone { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<OrderDetail> Items { get; set; } = new();
        public string? Username { get; set; }
        public string? Email { get; set; }
    }
}
