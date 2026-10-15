using System.Diagnostics;
using System.Security.Claims;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models;
using HealthCareManagement.Web.Models.Entities;
using HealthCareManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareManagement.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();

        var familyGroup = familyGroupId > 0 
            ? await _context.FamilyGroups.FirstOrDefaultAsync(f => f.Id == familyGroupId) 
            : null;

        var members = familyGroupId > 0 
            ? await _context.Members.Where(m => m.FamilyGroupId == familyGroupId).ToListAsync() 
            : new List<Member>();

        var memberIds = members.Select(m => m.Id).ToList();

        var today = DateTime.Today;

        // Today doses
        var todaySchedules = familyGroupId > 0 && memberIds.Any()
            ? await _context.MedicationSchedules
                .Where(s => s.ScheduleDate.Date == today && memberIds.Contains(s.PrescriptionDose.PrescriptionItem.Prescription.MemberId))
                .ToListAsync()
            : new List<MedicationSchedule>();

        int dosesTotal = todaySchedules.Count;
        int dosesTaken = todaySchedules.Count(s => s.IsTaken);

        // Upcoming appointments
        var upcomingAppointments = familyGroupId > 0 && memberIds.Any()
            ? await _context.Appointments
                .Where(a => memberIds.Contains(a.MemberId) && a.AppointmentDateTime >= DateTime.UtcNow.Date)
                .Include(a => a.Member)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync()
            : new List<Appointment>();

        var nextApt = upcomingAppointments.FirstOrDefault();

        // Vitals
        var latestMetric = familyGroupId > 0 && memberIds.Any()
            ? await _context.HealthMetrics
                .Where(h => memberIds.Contains(h.MemberId))
                .Include(h => h.Member)
                .OrderByDescending(h => h.RecordedAt)
                .FirstOrDefaultAsync()
            : null;

        // Medical Records
        var records = familyGroupId > 0 && memberIds.Any()
            ? await _context.MedicalRecords
                .Where(r => memberIds.Contains(r.MemberId))
                .Include(r => r.MedicalAttachments)
                .OrderByDescending(r => r.ExamDate)
                .ToListAsync()
            : new List<MedicalRecord>();

        int totalRecords = records.Count;
        int totalAttachments = records.Sum(r => r.MedicalAttachments?.Count ?? 0);
        var latestRecord = records.FirstOrDefault();

        var vm = new HomeDashboardViewModel
        {
            FamilyGroupName = familyGroup?.GroupName ?? (User.Identity?.Name != null ? $"Gia đình {User.Identity.Name}" : "Tổ ấm gia đình"),
            InviteCode = familyGroup?.InviteCode,
            Address = familyGroup?.Address,
            EmergencyPhone = familyGroup?.EmergencyPhone,
            TotalMembers = members.Count,
            SafeMembersCount = members.Count,
            Members = members,
            TodayDosesTotal = dosesTotal,
            TodayDosesTaken = dosesTaken,
            UpcomingAppointmentsCount = upcomingAppointments.Count,
            NextAppointmentSummary = nextApt != null ? $"{nextApt.Location} • {(nextApt.DoctorName ?? nextApt.Title ?? "Khám định kỳ")}" : null,
            NextAppointmentMember = nextApt?.Member?.FullName,
            DaysUntilNextAppointment = nextApt != null ? Math.Max(0, (nextApt.AppointmentDateTime.Date - today).Days) : null,
            LatestBloodPressure = (latestMetric?.SystolicBP != null && latestMetric?.DiastolicBP != null) ? $"{latestMetric.SystolicBP}/{latestMetric.DiastolicBP}" : null,
            LatestHeartRate = latestMetric?.HeartRate,
            LatestSpO2 = latestMetric?.SpO2,
            LatestVitalsMember = latestMetric?.Member?.FullName,
            HasAbnormalVitals = latestMetric?.IsAbnormal ?? false,
            TotalMedicalRecords = totalRecords,
            TotalAttachments = totalAttachments,
            LatestHospital = latestRecord?.HospitalName
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
