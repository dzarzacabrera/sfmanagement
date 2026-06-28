using Microsoft.AspNetCore.Mvc;

namespace SFManagement.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewBag.PageTitle = "Home";
        return RedirectToAction("Index", "Project");
    }
}
