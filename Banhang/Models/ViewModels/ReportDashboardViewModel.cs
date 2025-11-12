using System;
using System.Collections.Generic;

namespace Banhang.Models.ViewModels
{
    public class MonthlyRevenuePoint
    {
        public DateTime Month { get; set; }
        public decimal Total { get; set; }
    }

    public class TopProductReportItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ReportDashboardViewModel
    {
        public List<MonthlyRevenuePoint> MonthlyRevenue { get; set; } = new();
        public List<TopProductReportItem> TopProducts { get; set; } = new();
    }
}
