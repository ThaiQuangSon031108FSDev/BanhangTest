using Microsoft.AspNetCore.Mvc;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminBaseController : Controller
    {
        protected bool IsAdmin => HttpContext.Session.GetInt32("RoleID") == 1;
        protected bool IsEmployee => HttpContext.Session.GetInt32("RoleID") == 2;

        protected IActionResult? GuardAdminOrEmployee()
        {
            if (!(IsAdmin || IsEmployee))
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = string.Empty, returnUrl = Request.Path.ToString() });
            }

            return null;
        }

        protected IActionResult? GuardAdminOnly()
        {
            if (!IsAdmin)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = string.Empty, returnUrl = Request.Path.ToString() });
            }

            return null;
        }
    }
}
