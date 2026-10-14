using Microsoft.AspNetCore.Mvc;

namespace HealthCareManagement.Web.Controllers;

public class MedicalRecordsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Details(int? id)
    {
        return RedirectToAction(nameof(Index));
    }
}
