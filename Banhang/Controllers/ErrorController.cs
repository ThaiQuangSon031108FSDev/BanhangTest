using Microsoft.AspNetCore.Mvc;

namespace Banhang.Controllers
{
    public class ErrorController : Controller
    {
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

            return View("Error");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
