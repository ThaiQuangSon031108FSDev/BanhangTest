using System.Collections.Generic;
using Banhang.Models;

namespace Banhang.Models.ViewModels
{
    public class AdminOrderListViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public Dictionary<string, int> StatusCounts { get; set; } = new();
        public decimal TotalRevenue { get; set; }

        public int TotalOrders => Orders.Count;

        public int PendingCount => GetStatusCount("Pending");

        public int ProcessingCount => GetStatusCount("Processing");

        public int ShippedCount => GetStatusCount("Shipped");

        public int DeliveredCount => GetStatusCount("Delivered");

        public int CancelledCount => GetStatusCount("Cancelled");

        private int GetStatusCount(string status)
        {
            return StatusCounts.TryGetValue(status, out var count) ? count : 0;
        }
    }
}
