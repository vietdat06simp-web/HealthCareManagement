using System.Security.Claims;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.Entities;
using HealthCareManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareManagement.Web.Controllers;

[Authorize]
public class MedicalRecordsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MedicalRecordsController> _logger;

    public MedicalRecordsController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<MedicalRecordsController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? memberId, string? timeRange, string? search)
    {
        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (familyGroupId <= 0)
        {
            return View(new MedicalRecordsIndexViewModel());
        }

        // Lấy danh sách thành viên gia đình
        var members = await _context.Members
            .Where(m => m.FamilyGroupId == familyGroupId)
            .OrderBy(m => m.FullName)
            .ToListAsync();

        var memberIds = members.Select(m => m.Id).ToList();
        if (!memberIds.Any())
        {
            return View(new MedicalRecordsIndexViewModel { Members = members });
        }

        // Query hồ sơ bệnh án kèm đính kèm & đơn thuốc
        var query = _context.MedicalRecords
            .Where(r => memberIds.Contains(r.MemberId))
            .Include(r => r.Member)
            .Include(r => r.MedicalAttachments)
            .Include(r => r.Prescriptions)
            .AsQueryable();

        // 1. Lọc theo thành viên
        if (memberId.HasValue && memberId.Value > 0)
        {
            query = query.Where(r => r.MemberId == memberId.Value);
        }

        // 2. Lọc theo khoảng thời gian khám
        if (!string.IsNullOrWhiteSpace(timeRange) && timeRange != "all")
        {
            var today = DateTime.Today;
            if (timeRange == "30d")
            {
                var threshold = today.AddDays(-30);
                query = query.Where(r => r.ExamDate >= threshold);
            }
            else if (timeRange == "6m")
            {
                var threshold = today.AddMonths(-6);
                query = query.Where(r => r.ExamDate >= threshold);
            }
            else if (int.TryParse(timeRange, out int year))
            {
                query = query.Where(r => r.ExamDate.Year == year);
            }
        }

        // 3. Tìm kiếm theo tên bệnh viện, bác sĩ, chẩn đoán, hoặc mã ICD-10
        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(r =>
                r.HospitalName.Contains(kw) ||
                (r.DoctorName != null && r.DoctorName.Contains(kw)) ||
                r.Diagnosis.Contains(kw) ||
                (r.DiagnosisCode != null && r.DiagnosisCode.Contains(kw))
            );
        }

        var records = await query.OrderByDescending(r => r.ExamDate).ToListAsync();

        var latestMetric = await _context.HealthMetrics
            .Where(h => memberIds.Contains(h.MemberId))
            .Include(h => h.Member)
            .OrderByDescending(h => h.RecordedAt)
            .FirstOrDefaultAsync();

        var nextAppointment = await _context.Appointments
            .Where(a => memberIds.Contains(a.MemberId) && a.AppointmentDateTime >= DateTime.Today)
            .Include(a => a.Member)
            .OrderBy(a => a.AppointmentDateTime)
            .FirstOrDefaultAsync();

        var vm = new MedicalRecordsIndexViewModel
        {
            MedicalRecords = records,
            Members = members,
            LatestMetric = latestMetric,
            NextAppointment = nextAppointment,
            SelectedMemberId = memberId,
            SelectedTimeRange = timeRange ?? "all",
            SearchKeyword = search,
            NewRecord = new MedicalRecordCreateViewModel
            {
                MemberId = members.FirstOrDefault()?.Id ?? 0,
                VisitDate = DateTime.Today
            }
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicalRecordCreateViewModel model)
    {
        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (familyGroupId <= 0)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin tổ ấm gia đình.";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra thành viên có thuộc tổ ấm gia đình đang đăng nhập không
        var member = await _context.Members
            .FirstOrDefaultAsync(m => m.Id == model.MemberId && m.FamilyGroupId == familyGroupId);

        if (member == null)
        {
            ModelState.AddModelError(nameof(model.MemberId), "Thành viên đã chọn không hợp lệ hoặc không thuộc gia đình bạn.");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại thông tin.";
            return RedirectToAction(nameof(Index));
        }

        // Tạo bản ghi MedicalRecord
        var record = new MedicalRecord
        {
            MemberId = model.MemberId,
            HospitalName = model.FacilityName.Trim(),
            DoctorName = !string.IsNullOrWhiteSpace(model.DoctorName) ? model.DoctorName.Trim() : null,
            ExamDate = model.VisitDate,
            Diagnosis = model.Diagnosis.Trim(),
            DiagnosisCode = !string.IsNullOrWhiteSpace(model.DiagnosisCode) ? model.DiagnosisCode.Trim().ToUpper() : null,
            Conclusion = !string.IsNullOrWhiteSpace(model.TreatmentPlan) ? model.TreatmentPlan.Trim() : null,
            Notes = !string.IsNullOrWhiteSpace(model.Notes) ? model.Notes.Trim() : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.MedicalRecords.Add(record);
        await _context.SaveChangesAsync();

        // Xử lý tệp đính kèm (nếu có)
        if (model.AttachmentFile != null && model.AttachmentFile.Length > 0)
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "medical-records");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var originalFileName = Path.GetFileName(model.AttachmentFile.FileName);
                var extension = Path.GetExtension(originalFileName);
                var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AttachmentFile.CopyToAsync(stream);
                }

                var attachment = new MedicalAttachment
                {
                    MedicalRecordId = record.Id,
                    FilePath = $"/uploads/medical-records/{uniqueFileName}",
                    OriginalFileName = originalFileName,
                    FileType = !string.IsNullOrWhiteSpace(model.AttachmentFile.ContentType)
                        ? model.AttachmentFile.ContentType
                        : "application/octet-stream",
                    UploadedAt = DateTime.UtcNow
                };

                _context.MedicalAttachments.Add(attachment);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu tệp đính kèm hồ sơ y tế");
            }
        }

        // Tự động tạo Lịch hẹn tái khám nếu người dùng chọn ngày
        if (model.NextAppointmentDate.HasValue && model.NextAppointmentDate.Value >= DateTime.Today)
        {
            var appointment = new Appointment
            {
                MemberId = record.MemberId,
                MedicalRecordId = record.Id,
                AppointmentType = "Tái khám",
                Title = $"Tái khám: {record.Diagnosis}",
                AppointmentDateTime = model.NextAppointmentDate.Value.Date.AddHours(9), // Mặc định 09:00 sáng
                Location = record.HospitalName,
                DoctorName = record.DoctorName,
                PreparationNotes = record.Conclusion,
                Status = "Chờ khám",
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = $"Đã thêm hồ sơ bệnh án khám tại {record.HospitalName} thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue || id.Value <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (familyGroupId <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var record = await _context.MedicalRecords
            .Include(r => r.Member)
            .Include(r => r.MedicalAttachments)
            .Include(r => r.Prescriptions)
                .ThenInclude(p => p.PrescriptionItems)
            .FirstOrDefaultAsync(r => r.Id == id.Value);

        if (record == null || record.Member.FamilyGroupId != familyGroupId)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ bệnh án hoặc bạn không có quyền xem hồ sơ này.";
            return RedirectToAction(nameof(Index));
        }

        var nextAppointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.MedicalRecordId == record.Id);

        var vm = new MedicalRecordDetailsViewModel
        {
            Record = record,
            Attachments = record.MedicalAttachments.OrderByDescending(a => a.UploadedAt).ToList(),
            Prescriptions = record.Prescriptions.OrderByDescending(p => p.StartDate).ToList(),
            NextAppointment = nextAppointment
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        int familyGroupId = await ResolveCurrentFamilyGroupIdAsync();
        if (familyGroupId <= 0)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin tổ ấm gia đình.";
            return RedirectToAction(nameof(Index));
        }

        var record = await _context.MedicalRecords
            .Include(r => r.Member)
            .Include(r => r.MedicalAttachments)
            .Include(r => r.Appointments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null || record.Member.FamilyGroupId != familyGroupId)
        {
            TempData["ErrorMessage"] = "Hồ sơ không tồn tại hoặc bạn không có quyền thao tác.";
            return RedirectToAction(nameof(Index));
        }

        // Xóa file vật lý trong wwwroot/uploads/medical-records/
        foreach (var attachment in record.MedicalAttachments)
        {
            if (!string.IsNullOrWhiteSpace(attachment.FilePath))
            {
                var relativePath = attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPhysicalPath = Path.Combine(_environment.WebRootPath, relativePath);
                if (System.IO.File.Exists(fullPhysicalPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPhysicalPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể xóa file vật lý tại {Path}", fullPhysicalPath);
                    }
                }
            }
        }

        _context.MedicalRecords.Remove(record);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã xóa hồ sơ bệnh án và các tệp đính kèm liên quan thành công.";
        return RedirectToAction(nameof(Index));
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
}
