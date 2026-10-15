using System.Security.Claims;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareManagement.Web.Controllers;

public class MedicalRecordsController : Controller
{
    private readonly ApplicationDbContext _context;

    public MedicalRecordsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (familyGroupId <= 0)
        {
            return View(new MedicalRecordsIndexViewModel());
        }

        var members = await _context.Members
            .Where(m => m.FamilyGroupId == familyGroupId)
            .OrderBy(m => m.FullName)
            .ToListAsync();

        var memberIds = members.Select(m => m.Id).ToList();
        if (!memberIds.Any())
        {
            return View(new MedicalRecordsIndexViewModel { Members = members });
        }

        var records = await _context.MedicalRecords
            .Where(r => memberIds.Contains(r.MemberId))
            .Include(r => r.Member)
            .Include(r => r.MedicalAttachments)
            .Include(r => r.Prescriptions)
            .OrderByDescending(r => r.ExamDate)
            .ToListAsync();

        var latestMetric = await _context.HealthMetrics
            .Where(h => memberIds.Contains(h.MemberId))
            .Include(h => h.Member)
            .OrderByDescending(h => h.RecordedAt)
            .FirstOrDefaultAsync();

        var nextAppointment = await _context.Appointments
            .Where(a => memberIds.Contains(a.MemberId) && a.AppointmentDateTime >= DateTime.UtcNow.Date)
            .Include(a => a.Member)
            .OrderBy(a => a.AppointmentDateTime)
            .FirstOrDefaultAsync();

        var vm = new MedicalRecordsIndexViewModel
        {
            MedicalRecords = records,
            Members = members,
            LatestMetric = latestMetric,
            NextAppointment = nextAppointment
        };

        return View(vm);
    }

    private async Task<int> ResolveCurrentFamilyGroupIdAsync()
    {
        var fgClaim = User.FindFirst("FamilyGroupId")?.Value;
        if (int.TryParse(fgClaim, out int fgId) && fgId > 0)
        {
            return fgId;
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int uId) && uId > 0)
        {
            var fg = await _context.FamilyGroups.FirstOrDefaultAsync(f => f.CreatedByUserId == uId);
            if (fg != null) return fg.Id;

            var m = await _context.Members.FirstOrDefaultAsync(x => x.UserId == uId);
            if (m != null) return m.FamilyGroupId;
        }

        return 0;
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
