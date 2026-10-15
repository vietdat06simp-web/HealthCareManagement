using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.Entities;

namespace HealthCareManagement.Web.Controllers;

public class MembersController : Controller
{
    private readonly ApplicationDbContext _context;

    public MembersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var familyGroupId = GetCurrentFamilyGroupId();
        var currentUserId = GetCurrentUserId();

        if ((!familyGroupId.HasValue || familyGroupId.Value == 0) && currentUserId.HasValue)
        {
            var fg = await _context.FamilyGroups.FirstOrDefaultAsync(f => f.CreatedByUserId == currentUserId.Value);
            if (fg != null)
            {
                familyGroupId = fg.Id;
            }
            else
            {
                var m = await _context.Members.FirstOrDefaultAsync(x => x.UserId == currentUserId.Value);
                if (m != null)
                {
                    familyGroupId = m.FamilyGroupId;
                }
            }
        }

        if (!familyGroupId.HasValue || familyGroupId.Value <= 0)
        {
            return View(new List<Member>());
        }

        var members = await _context.Members
            .Where(m => m.FamilyGroupId == familyGroupId.Value)
            .Include(m => m.HealthMetrics)
            .Include(m => m.MemberAllergies)
            .Include(m => m.MemberChronicDiseases)
            .OrderBy(m => m.Id)
            .ToListAsync();

        return View(members);
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

    private int? GetCurrentFamilyGroupId()
    {
        var claimVal = User.FindFirst("FamilyGroupId")?.Value;
        if (int.TryParse(claimVal, out int fgId) && fgId > 0)
        {
            return fgId;
        }
        return null;
    }

    private int? GetCurrentUserId()
    {
        var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claimVal, out int uId) && uId > 0)
        {
            return uId;
        }
        return null;
    }
}
