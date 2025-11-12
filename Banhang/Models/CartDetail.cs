namespace Banhang.Models
{
    public class CartDetail
    {
        public int CartDetailID { get; set; }
        public int CartID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }

        // optional view helpers
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}
