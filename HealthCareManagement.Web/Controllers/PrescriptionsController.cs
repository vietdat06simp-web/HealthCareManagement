using System.Security.Claims;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.Entities;
using HealthCareManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareManagement.Web.Controllers;

[Authorize]
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

        // Lấy danh sách lịch uống thuốc trong ngày hôm nay của các thành viên gia đình
        var todaySchedules = await _context.MedicationSchedules
            .Where(s => s.ScheduleDate.Date == today && memberIds.Contains(s.PrescriptionDose.PrescriptionItem.Prescription.MemberId))
            .Include(s => s.PrescriptionDose)
                .ThenInclude(d => d.PrescriptionItem)
                    .ThenInclude(i => i.Prescription)
                        .ThenInclude(p => p.Member)
            .OrderBy(s => s.PrescriptionDose.DoseTime)
            .ToListAsync();

        // Phân nhóm theo 4 cữ: Sáng, Trưa, Chiều, Tối kèm trạng thái IsTaken
        var morningSchedules = todaySchedules.Where(s =>
            s.PrescriptionDose.TimeOfDay == "Sáng" ||
            (string.IsNullOrEmpty(s.PrescriptionDose.TimeOfDay) && s.PrescriptionDose.DoseTime < new TimeSpan(11, 0, 0))
        ).ToList();

        var noonSchedules = todaySchedules.Where(s =>
            s.PrescriptionDose.TimeOfDay == "Trưa" ||
            (string.IsNullOrEmpty(s.PrescriptionDose.TimeOfDay) && s.PrescriptionDose.DoseTime >= new TimeSpan(11, 0, 0) && s.PrescriptionDose.DoseTime < new TimeSpan(14, 0, 0))
        ).ToList();

        var afternoonSchedules = todaySchedules.Where(s =>
            s.PrescriptionDose.TimeOfDay == "Chiều" ||
            (string.IsNullOrEmpty(s.PrescriptionDose.TimeOfDay) && s.PrescriptionDose.DoseTime >= new TimeSpan(14, 0, 0) && s.PrescriptionDose.DoseTime < new TimeSpan(18, 30, 0))
        ).ToList();

        var eveningSchedules = todaySchedules.Where(s =>
            s.PrescriptionDose.TimeOfDay == "Tối" ||
            s.PrescriptionDose.TimeOfDay == "Đêm" ||
            (string.IsNullOrEmpty(s.PrescriptionDose.TimeOfDay) && s.PrescriptionDose.DoseTime >= new TimeSpan(18, 30, 0))
        ).ToList();

        var activePrescriptions = await _context.Prescriptions
            .Where(p => memberIds.Contains(p.MemberId) && p.IsActive)
            .Include(p => p.Member)
            .Include(p => p.MedicalRecord)
            .Include(p => p.PrescriptionItems)
                .ThenInclude(i => i.PrescriptionDoses)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        var upcomingAppointments = await _context.Appointments
            .Where(a => memberIds.Contains(a.MemberId) && a.AppointmentDateTime >= DateTime.UtcNow.Date)
            .Include(a => a.Member)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();

        var medicalRecords = await _context.MedicalRecords
            .Where(r => memberIds.Contains(r.MemberId))
            .OrderByDescending(r => r.ExamDate)
            .ToListAsync();

        var vm = new PrescriptionsIndexViewModel
        {
            TodaySchedules = todaySchedules,
            MorningSchedules = morningSchedules,
            NoonSchedules = noonSchedules,
            AfternoonSchedules = afternoonSchedules,
            EveningSchedules = eveningSchedules,
            ActivePrescriptions = activePrescriptions,
            UpcomingAppointments = upcomingAppointments,
            Members = members,
            MedicalRecords = medicalRecords,
            ActiveTab = tab
        };

        ViewBag.ActiveTab = tab;
        return View(vm);
    }

    [HttpPost]
    [Route("Prescriptions/MarkAsTaken/{id}")]
    [Route("Prescriptions/MarkAsTaken")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkAsTaken(int id, [FromQuery] bool? toggle = null)
    {
        var schedule = await _context.MedicationSchedules
            .Include(s => s.PrescriptionDose)
                .ThenInclude(d => d.PrescriptionItem)
                    .ThenInclude(i => i.Prescription)
                        .ThenInclude(p => p.Member)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return Json(new { success = false, message = "Không tìm thấy lịch uống thuốc." });
        }

        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (schedule.PrescriptionDose?.PrescriptionItem?.Prescription?.Member?.FamilyGroupId != familyGroupId)
        {
            return Json(new { success = false, message = "Bạn không có quyền cập nhật lịch uống thuốc này." });
        }

        if (toggle == true && schedule.IsTaken)
        {
            schedule.IsTaken = false;
            schedule.TakenAt = null;
        }
        else
        {
            schedule.IsTaken = true;
            schedule.TakenAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        var takenAtFormatted = schedule.TakenAt?.ToString("HH:mm") ?? "";
        return Json(new
        {
            success = true,
            isTaken = schedule.IsTaken,
            takenAt = takenAtFormatted,
            message = schedule.IsTaken ? $"Đã xác nhận uống thuốc lúc {takenAtFormatted}" : "Đã hủy trạng thái uống thuốc."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrescriptionCreateViewModel model)
    {
        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (familyGroupId <= 0)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verify member belongs to this family group
        var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == model.MemberId && m.FamilyGroupId == familyGroupId);
        if (member == null)
        {
            ModelState.AddModelError(nameof(model.MemberId), "Thành viên không hợp lệ hoặc không thuộc tổ ấm.");
        }

        // Normalize drugs list
        var drugsToProcess = new List<PrescriptionItemInputModel>();
        if (model.Drugs != null && model.Drugs.Any())
        {
            drugsToProcess.AddRange(model.Drugs.Where(d => !string.IsNullOrWhiteSpace(d.DrugName)));
        }

        if (!drugsToProcess.Any() && !string.IsNullOrWhiteSpace(model.DrugName))
        {
            drugsToProcess.Add(new PrescriptionItemInputModel
            {
                DrugName = model.DrugName.Trim(),
                Dosage = !string.IsNullOrWhiteSpace(model.Dosage) ? model.Dosage.Trim() : "1 viên",
                Frequency = model.Frequency?.Trim(),
                MealTiming = !string.IsNullOrWhiteSpace(model.MealTiming) ? model.MealTiming.Trim() : "Sau ăn",
                SpecialInstruction = model.SpecialInstruction?.Trim(),
                Morning = model.Morning,
                Noon = model.Noon,
                Afternoon = model.Afternoon,
                Evening = model.Evening
            });
        }

        if (!drugsToProcess.Any())
        {
            ModelState.AddModelError("Drugs", "Vui lòng nhập thông tin ít nhất một loại thuốc cho đơn thuốc.");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index), new { tab = "medications" });
        }

        var startDate = model.StartDate.Date;
        var endDate = model.EndDate.Date;
        if (endDate < startDate)
        {
            endDate = startDate.AddDays(7);
        }

        // Bound to maximum 90 days to prevent excessive memory/database consumption
        if ((endDate - startDate).TotalDays > 90)
        {
            endDate = startDate.AddDays(90);
        }

        var prescription = new Prescription
        {
            MemberId = model.MemberId,
            PrescriptionName = model.PrescriptionName.Trim(),
            DoctorName = !string.IsNullOrWhiteSpace(model.DoctorName) ? model.DoctorName.Trim() : null,
            MedicalRecordId = model.MedicalRecordId > 0 ? model.MedicalRecordId : null,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        var allSchedules = new List<MedicationSchedule>();

        foreach (var drug in drugsToProcess)
        {
            var item = new PrescriptionItem
            {
                PrescriptionId = prescription.Id,
                MedicineName = drug.DrugName.Trim(),
                DosagePerTime = !string.IsNullOrWhiteSpace(drug.Dosage) ? drug.Dosage.Trim() : "1 viên",
                MealTiming = !string.IsNullOrWhiteSpace(drug.MealTiming) ? drug.MealTiming.Trim() : "Sau ăn",
                SpecialInstruction = drug.SpecialInstruction?.Trim()
            };
            _context.PrescriptionItems.Add(item);
            await _context.SaveChangesAsync();

            bool morning = drug.Morning;
            bool noon = drug.Noon;
            bool afternoon = drug.Afternoon;
            bool evening = drug.Evening;

            // If user did not check any slot, default to Morning and Evening
            if (!morning && !noon && !afternoon && !evening)
            {
                morning = true;
                evening = true;
            }

            var createdDoses = new List<PrescriptionDose>();

            if (morning)
            {
                var dose = new PrescriptionDose
                {
                    PrescriptionItemId = item.Id,
                    DoseTime = new TimeSpan(8, 0, 0),
                    TimeOfDay = "Sáng",
                    Quantity = 1.0m
                };
                _context.PrescriptionDoses.Add(dose);
                createdDoses.Add(dose);
            }

            if (noon)
            {
                var dose = new PrescriptionDose
                {
                    PrescriptionItemId = item.Id,
                    DoseTime = new TimeSpan(12, 0, 0),
                    TimeOfDay = "Trưa",
                    Quantity = 1.0m
                };
                _context.PrescriptionDoses.Add(dose);
                createdDoses.Add(dose);
            }

            if (afternoon)
            {
                var dose = new PrescriptionDose
                {
                    PrescriptionItemId = item.Id,
                    DoseTime = new TimeSpan(17, 30, 0),
                    TimeOfDay = "Chiều",
                    Quantity = 1.0m
                };
                _context.PrescriptionDoses.Add(dose);
                createdDoses.Add(dose);
            }

            if (evening)
            {
                var dose = new PrescriptionDose
                {
                    PrescriptionItemId = item.Id,
                    DoseTime = new TimeSpan(20, 30, 0),
                    TimeOfDay = "Tối",
                    Quantity = 1.0m
                };
                _context.PrescriptionDoses.Add(dose);
                createdDoses.Add(dose);
            }

            await _context.SaveChangesAsync();

            // Tự động sinh các bản ghi MedicationSchedules cho từng ngày trong khoảng thời gian điều trị
            for (var curDate = startDate; curDate <= endDate; curDate = curDate.AddDays(1))
            {
                foreach (var dose in createdDoses)
                {
                    allSchedules.Add(new MedicationSchedule
                    {
                        PrescriptionDoseId = dose.Id,
                        ScheduleDate = curDate,
                        IsTaken = false,
                        TakenAt = null
                    });
                }
            }
        }

        if (allSchedules.Any())
        {
            _context.MedicationSchedules.AddRange(allSchedules);
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = $"Đã tạo đơn thuốc '{prescription.PrescriptionName}' và lập {allSchedules.Count} lịch uống nhắc nhở.";
        return RedirectToAction(nameof(Index), new { tab = "medications" });
    }

    [HttpPost]
    [Route("Prescriptions/Delete/{id}")]
    [Route("Prescriptions/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (familyGroupId <= 0)
        {
            return RedirectToAction("Login", "Account");
        }

        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
                .ThenInclude(i => i.PrescriptionDoses)
                    .ThenInclude(d => d.MedicationSchedules)
            .Include(p => p.Member)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Không tìm thấy đơn thuốc." });
            }
            TempData["ErrorMessage"] = "Không tìm thấy đơn thuốc.";
            return RedirectToAction(nameof(Index), new { tab = "medications" });
        }

        if (prescription.Member?.FamilyGroupId != familyGroupId)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa đơn thuốc này." });
            }
            return Forbid();
        }

        // Xóa đơn thuốc và các lịch uống liên quan
        var doses = prescription.PrescriptionItems.SelectMany(i => i.PrescriptionDoses).ToList();
        var schedules = doses.SelectMany(d => d.MedicationSchedules).ToList();

        if (schedules.Any()) _context.MedicationSchedules.RemoveRange(schedules);
        if (doses.Any()) _context.PrescriptionDoses.RemoveRange(doses);
        if (prescription.PrescriptionItems.Any()) _context.PrescriptionItems.RemoveRange(prescription.PrescriptionItems);
        _context.Prescriptions.Remove(prescription);

        await _context.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, message = "Đã xóa đơn thuốc thành công." });
        }

        TempData["SuccessMessage"] = $"Đã xóa đơn thuốc '{prescription.PrescriptionName}' và các lịch uống liên quan thành công.";
        return RedirectToAction(nameof(Index), new { tab = "medications" });
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
