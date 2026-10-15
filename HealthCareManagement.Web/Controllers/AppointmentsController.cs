using System.Security.Claims;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareManagement.Web.Controllers;

public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AppointmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "appointments")
    {
        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();

        if (familyGroupId <= 0)
        {
            return View(new PrescriptionsIndexViewModel { ActiveTab = tab });
        }

        var members = await _context.Members
            .Where(m => m.FamilyGroupId == familyGroupId)
            .OrderBy(m => m.FullName)
            .ToListAsync();

        var memberIds = members.Select(m => m.Id).ToList();

        var today = DateTime.Today;

        // Appointments
        var upcomingAppointments = await _context.Appointments
            .Where(a => memberIds.Contains(a.MemberId) && a.AppointmentDateTime >= DateTime.UtcNow.Date)
            .Include(a => a.Member)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();

        // Schedules today
        var todaySchedules = await _context.MedicationSchedules
            .Where(s => s.ScheduleDate.Date == today && memberIds.Contains(s.PrescriptionDose.PrescriptionItem.Prescription.MemberId))
            .Include(s => s.PrescriptionDose)
                .ThenInclude(pd => pd.PrescriptionItem)
                    .ThenInclude(pi => pi.Prescription)
                        .ThenInclude(p => p.Member)
            .OrderBy(s => s.PrescriptionDose.DoseTime)
            .ToListAsync();

        // Active Prescriptions
        var activePrescriptions = await _context.Prescriptions
            .Where(p => memberIds.Contains(p.MemberId) && p.IsActive)
            .Include(p => p.Member)
            .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.PrescriptionDoses)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var vm = new PrescriptionsIndexViewModel
        {
            TodaySchedules = todaySchedules,
            ActivePrescriptions = activePrescriptions,
            UpcomingAppointments = upcomingAppointments,
            Members = members,
            ActiveTab = tab
        };

        return View(vm);
    }

    private async Task<int> ResolveCurrentFamilyGroupIdAsync()
    {
        var claimVal = User.FindFirst("FamilyGroupId")?.Value;
        if (int.TryParse(claimVal, out int fgId) && fgId > 0)
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
        return RedirectToAction(nameof(Index), new { tab = "appointments" });
    }

    [HttpGet]
    public IActionResult Calendar()
    {
        return RedirectToAction(nameof(Index), new { tab = "appointments" });
    }
}
