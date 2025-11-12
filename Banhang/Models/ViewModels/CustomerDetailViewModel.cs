using System.Collections.Generic;
using Banhang.Models;

namespace Banhang.Models.ViewModels
{
    public class CustomerDetailViewModel
    {
        public User Customer { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
    }
}
