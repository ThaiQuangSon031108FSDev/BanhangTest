namespace Banhang.Models
{
    public class Cart
    {
        public int CartID { get; set; }
        public int UserID { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte Status { get; set; } // 0: Open, 1: Ordered, 2: Cancelled
    }
}
