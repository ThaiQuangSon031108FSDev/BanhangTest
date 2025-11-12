using System.ComponentModel.DataAnnotations;

namespace Banhang.Models.ValidationAttributes
{
    public class PriceValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is decimal price)
            {
                if (price < 0)
                {
                    return new ValidationResult("Giá không được âm");
                }

                if (price > 1000000000)
                {
                    return new ValidationResult("Giá quá lớn");
                }
            }

            return ValidationResult.Success;
        }
    }
}
