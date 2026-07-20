using Microsoft.AspNetCore.Mvc;

namespace SFManagement.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewBag.PageTitle = "Home";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.PageTitle = "Error";
        return View();
    }
}
