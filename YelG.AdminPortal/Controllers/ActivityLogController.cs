using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.BLL.Services.Audit;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class ActivityLogController : Controller
    {
        private readonly IActivityLogService _log;

        public ActivityLogController(IActivityLogService log)
        {
            _log = log;
        }

        public async Task<IActionResult> Index([FromQuery] ActivityLogFilter filter, CancellationToken ct)
        {
            ViewData["Filter"] = filter;
            return View(await _log.SearchAsync(filter, ct));
        }
    }
}
