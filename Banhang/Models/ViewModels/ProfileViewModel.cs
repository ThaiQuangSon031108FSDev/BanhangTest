using System;
using System.Collections.Generic;
using Banhang.Models;

namespace Banhang.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }
}
