using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Banhang.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận hàng")] 
        [Display(Name = "Họ và tên")]
        public string ShipName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShipAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string ShipPhone { get; set; } = string.Empty;

        [Display(Name = "Ghi chú")] 
        public string? Notes { get; set; }

        public List<Banhang.Models.CartItem> CartItems { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }
}
