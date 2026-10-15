using System.Security.Claims;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareManagement.Web.Controllers;

public class PrescriptionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PrescriptionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "medications")
    {
        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (familyGroupId <= 0)
        {
            ViewBag.ActiveTab = tab;
            return View(new PrescriptionsIndexViewModel { ActiveTab = tab });
        }

        var members = await _context.Members
            .Where(m => m.FamilyGroupId == familyGroupId)
            .OrderBy(m => m.FullName)
            .ToListAsync();

        var memberIds = members.Select(m => m.Id).ToList();
        if (!memberIds.Any())
        {
            ViewBag.ActiveTab = tab;
            return View(new PrescriptionsIndexViewModel { Members = members, ActiveTab = tab });
        }

        var today = DateTime.Today;

        var todaySchedules = await _context.MedicationSchedules
            .Where(s => s.ScheduleDate.Date == today && memberIds.Contains(s.PrescriptionDose.PrescriptionItem.Prescription.MemberId))
            .Include(s => s.PrescriptionDose)
                .ThenInclude(d => d.PrescriptionItem)
                    .ThenInclude(i => i.Prescription)
                        .ThenInclude(p => p.Member)
            .OrderBy(s => s.PrescriptionDose.DoseTime)
            .ToListAsync();

        var activePrescriptions = await _context.Prescriptions
            .Where(p => memberIds.Contains(p.MemberId) && p.IsActive)
            .Include(p => p.Member)
            .Include(p => p.PrescriptionItems)
                .ThenInclude(i => i.PrescriptionDoses)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        var upcomingAppointments = await _context.Appointments
            .Where(a => memberIds.Contains(a.MemberId) && a.AppointmentDateTime >= DateTime.UtcNow.Date)
            .Include(a => a.Member)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();

        var vm = new PrescriptionsIndexViewModel
        {
            TodaySchedules = todaySchedules,
            ActivePrescriptions = activePrescriptions,
            UpcomingAppointments = upcomingAppointments,
            Members = members,
            ActiveTab = tab
        };

        ViewBag.ActiveTab = tab;
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
        return RedirectToAction(nameof(Index), new { tab = "medications" });
    }

    [HttpGet]
    public IActionResult Details(int? id)
    {
        return RedirectToAction(nameof(Index), new { tab = "medications" });
    }
}
