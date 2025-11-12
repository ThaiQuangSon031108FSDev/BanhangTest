using System;
using System.ComponentModel.DataAnnotations;
using Banhang.Models.ValidationAttributes;

namespace Banhang.Models
{
    public class Product
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Tên phải từ 3-200 ký tự")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá không được để trống")]
        [Range(0, 999999999, ErrorMessage = "Giá từ 0 - 999,999,999")]
        [PriceValidation]
        public decimal Price { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string? Description { get; set; }

        [Url(ErrorMessage = "URL ảnh không hợp lệ")]
        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm")]
        public int Stock { get; set; }

        public int CategoryID { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? CategoryName { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
    }
}
