using System.Diagnostics;
using System.Security.Claims;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models;
using HealthCareManagement.Web.Models.Entities;
using HealthCareManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareManagement.Web.Controllers;

[Authorize]
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
            ? await _context.Members.Where(m => m.FamilyGroupId == familyGroupId).OrderBy(m => m.FullName).ToListAsync()
            : new List<Member>();

        var memberIds = members.Select(m => m.Id).ToList();
        var today = DateTime.Today;

        // 1. Đếm tổng số thành viên
        int totalMembers = members.Count;

        // 2. Lấy danh sách cữ thuốc hôm nay và đếm số cữ chưa uống
        var todaySchedules = (familyGroupId > 0 && memberIds.Any())
            ? await _context.MedicationSchedules
                .Where(s => s.ScheduleDate.Date == today && memberIds.Contains(s.PrescriptionDose.PrescriptionItem.Prescription.MemberId))
                .Include(s => s.PrescriptionDose)
                    .ThenInclude(d => d.PrescriptionItem)
                        .ThenInclude(i => i.Prescription)
                            .ThenInclude(p => p.Member)
                .OrderBy(s => s.PrescriptionDose.DoseTime)
                .ToListAsync()
            : new List<MedicationSchedule>();

        var medicationDtos = todaySchedules.Select(s => new MedicationScheduleDto
        {
            Id = s.Id,
            MemberName = s.PrescriptionDose?.PrescriptionItem?.Prescription?.Member?.FullName ?? "Thành viên",
            DrugName = s.PrescriptionDose?.PrescriptionItem?.MedicineName ?? "Thuốc",
            Dosage = s.PrescriptionDose?.PrescriptionItem?.DosagePerTime ?? "1 liều",
            MealTime = !string.IsNullOrWhiteSpace(s.PrescriptionDose?.PrescriptionItem?.MealTiming)
                ? s.PrescriptionDose.PrescriptionItem.MealTiming
                : "Sau ăn",
            TimeSlot = !string.IsNullOrWhiteSpace(s.PrescriptionDose?.TimeOfDay)
                ? s.PrescriptionDose.TimeOfDay
                : (s.PrescriptionDose?.DoseTime < new TimeSpan(11, 0, 0) ? "Sáng" :
                   s.PrescriptionDose?.DoseTime < new TimeSpan(14, 0, 0) ? "Trưa" :
                   s.PrescriptionDose?.DoseTime < new TimeSpan(18, 30, 0) ? "Chiều" : "Tối"),
            IsTaken = s.IsTaken,
            TakenAt = s.TakenAt
        }).ToList();

        int pendingMedicationsCount = todaySchedules.Count(s => !s.IsTaken);
        int totalTodayMedications = todaySchedules.Count;

        // 3. Đếm tổng số hồ sơ bệnh án (MedicalRecords) của gia đình
        int totalRecordsCount = (familyGroupId > 0 && memberIds.Any())
            ? await _context.MedicalRecords.CountAsync(r => memberIds.Contains(r.MemberId))
            : 0;

        // 4. Lấy chỉ số sinh hiệu mới nhất của từng thành viên
        var membersSummary = new List<MemberVitalSummaryDto>();
        foreach (var m in members)
        {
            var latestMetric = await _context.HealthMetrics
                .Where(h => h.MemberId == m.Id)
                .OrderByDescending(h => h.RecordedAt)
                .FirstOrDefaultAsync();

            string status = "Chưa có dữ liệu";
            if (latestMetric != null)
            {
                bool isHighBP = (latestMetric.SystolicBP.HasValue && latestMetric.SystolicBP.Value >= 140) ||
                                (latestMetric.DiastolicBP.HasValue && latestMetric.DiastolicBP.Value >= 90);
                bool isAbnormalHR = latestMetric.HeartRate.HasValue && (latestMetric.HeartRate.Value < 55 || latestMetric.HeartRate.Value > 100);

                if (latestMetric.IsAbnormal || isHighBP || isAbnormalHR)
                {
                    status = "Cần theo dõi";
                }
                else
                {
                    status = "Bình thường";
                }
            }

            membersSummary.Add(new MemberVitalSummaryDto
            {
                Id = m.Id,
                FullName = m.FullName,
                Relationship = m.Relationship,
                LatestSystolic = latestMetric?.SystolicBP,
                LatestDiastolic = latestMetric?.DiastolicBP,
                LatestHeartRate = latestMetric?.HeartRate,
                LastMeasuredAt = latestMetric?.RecordedAt,
                VitalStatus = status
            });
        }

        // 5. Lấy các lịch hẹn tái khám (NextAppointmentDate >= DateTime.Today) sắp xếp gần nhất lên đầu (top 3 - 5 lịch hẹn)
        var appointmentsList = (familyGroupId > 0 && memberIds.Any())
            ? await _context.Appointments
                .Where(a => memberIds.Contains(a.MemberId) && a.AppointmentDateTime.Date >= today)
                .Include(a => a.Member)
                .OrderBy(a => a.AppointmentDateTime)
                .Take(5)
                .ToListAsync()
            : new List<Appointment>();

        var upcomingDtos = appointmentsList.Select(a => new UpcomingAppointmentDto
        {
            Id = a.Id,
            MemberName = a.Member?.FullName ?? "Thành viên",
            FacilityName = !string.IsNullOrWhiteSpace(a.Location) ? a.Location : "Cơ sở y tế",
            DoctorName = !string.IsNullOrWhiteSpace(a.DoctorName) ? a.DoctorName : (!string.IsNullOrWhiteSpace(a.Title) ? a.Title : "Bác sĩ phụ trách"),
            AppointmentDate = a.AppointmentDateTime,
            DaysLeft = Math.Max(0, (a.AppointmentDateTime.Date - today).Days),
            AppointmentType = a.AppointmentType ?? "Khám chuyên khoa",
            Status = a.Status ?? "Đã đặt lịch"
        }).ToList();

        var vm = new DashboardViewModel
        {
            FamilyGroupName = familyGroup?.GroupName ?? (User.Identity?.Name != null ? $"Gia đình {User.Identity.Name}" : "Tổ ấm gia đình"),
            InviteCode = familyGroup?.InviteCode,
            Address = familyGroup?.Address,
            EmergencyPhone = familyGroup?.EmergencyPhone,
            TotalMembers = totalMembers,
            PendingMedicationsCount = pendingMedicationsCount,
            TotalTodayMedications = totalTodayMedications,
            TotalRecordsCount = totalRecordsCount,
            MembersSummary = membersSummary,
            TodayMedicationSchedules = medicationDtos,
            UpcomingAppointments = upcomingDtos
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
