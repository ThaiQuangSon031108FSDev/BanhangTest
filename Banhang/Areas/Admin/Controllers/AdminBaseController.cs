using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public abstract class AdminBaseController : Controller
    {
        protected bool IsAdmin => User.IsInRole("Admin");
        protected bool IsEmployee => User.IsInRole("Employee");
    }
}
