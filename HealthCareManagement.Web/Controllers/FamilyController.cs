using Microsoft.AspNetCore.Mvc;

namespace HealthCareManagement.Web.Controllers;

public class FamilyController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "FamilyGroups");
    }
}
