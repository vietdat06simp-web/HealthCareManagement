using Microsoft.AspNetCore.Mvc;

namespace HealthCareManagement.Web.Controllers;

public class MembersController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        // Điều hướng sang form tạo thành viên hoặc hiển thị thông báo
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Details(int? id)
    {
        // Điều hướng sang chi tiết thành viên
        return RedirectToAction(nameof(Index));
    }
}
