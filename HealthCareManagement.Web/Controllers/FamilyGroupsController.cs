using Microsoft.AspNetCore.Mvc;
using HealthCareManagement.Web.Models.ViewModels;

namespace HealthCareManagement.Web.Controllers;

public class FamilyGroupsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = new FamilyGroupViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(FamilyGroupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        TempData["SuccessMessage"] = "Cập nhật thông tin tổ ấm thành công!";
        return RedirectToAction(nameof(Index));
    }
}
