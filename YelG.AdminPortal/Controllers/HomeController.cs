using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.AdminPortal.Infrastructure;
using YelG.AdminPortal.Models;
using YelG.BLL.Services.Evaluations;
using YelG.Model.Enums;

namespace YelG.AdminPortal.Controllers;

public class HomeController : Controller
{
    private readonly IEvaluationService _evaluations;

    public HomeController(IEvaluationService evaluations)
    {
        _evaluations = evaluations;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var mine = await _evaluations.ListMineAsync(ct);
        var pendingApproval = User.IsAdmin()
            ? (await _evaluations.ListAllAsync(ct)).Count(e => e.Status == EvaluationStatus.Submitted)
            : 0;

        return View(new HomeViewModel(mine, pendingApproval));
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
