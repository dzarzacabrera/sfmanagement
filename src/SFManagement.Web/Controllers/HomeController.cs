using Microsoft.AspNetCore.Mvc;

namespace SFManagement.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Project");
    }
}
