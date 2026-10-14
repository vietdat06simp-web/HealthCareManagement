using Microsoft.AspNetCore.Mvc;

namespace HealthCareManagement.Web.Controllers;

public class PrescriptionsController : Controller
{
    [HttpGet]
    public IActionResult Index(string tab = "medications")
    {
        ViewBag.ActiveTab = tab;
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        return RedirectToAction(nameof(Index), new { tab = "medications" });
    }

    [HttpGet]
    public IActionResult Details(int? id)
    {
        return RedirectToAction(nameof(Index), new { tab = "medications" });
    }
}
