using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            ViewBag.StatusCode = statusCode;

            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Trang bạn tìm kiếm không tồn tại.";
                    break;
                case 403:
                    ViewBag.ErrorMessage = "Bạn không có quyền truy cập trang này.";
                    break;
                case 500:
                    ViewBag.ErrorMessage = "Lỗi máy chủ. Vui lòng thử lại sau.";
                    break;
                default:
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi không xác định.";
                    break;
            }

            _logger.LogWarning("Trả về trang lỗi {StatusCode} cho người dùng {Username}", statusCode, User.Identity?.Name);
            return View("Error");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            _logger.LogError("Đã xảy ra lỗi không xác định, chuyển hướng đến trang Error");
            return View();
        }
    }
}
