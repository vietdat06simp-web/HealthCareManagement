using Microsoft.AspNetCore.Mvc;

namespace HealthCareManagement.Web.Controllers;

public class AppointmentsController : Controller
{
    [HttpGet]
    public IActionResult Index(string tab = "appointments")
    {
        ViewBag.ActiveTab = tab;
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        return RedirectToAction(nameof(Index), new { tab = "appointments" });
    }

    [HttpGet]
    public IActionResult Calendar()
    {
        return RedirectToAction(nameof(Index), new { tab = "appointments" });
    }
}
