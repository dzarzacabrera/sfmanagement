using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Web.Controllers;
using Serilog;

namespace SFManagement.Web.Controllers;

public class SetupController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.PageTitle = "Setup";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>>
        {
            new("Setup", "")
        };
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ClearDatabase(
        [FromServices] ICommandHandler<ClearDatabaseCommand> handler)
    {
        try
        {
            await handler.HandleAsync(new ClearDatabaseCommand());
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to clear database");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ImportSeedData(
        [FromServices] ICommandHandler<ImportSeedDataCommand> handler)
    {
        try
        {
            await handler.HandleAsync(new ImportSeedDataCommand());
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to import seed data");
            return Json(new { success = false, message = ex.Message });
        }
    }
}
