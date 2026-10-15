using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.Entities;
using HealthCareManagement.Web.Models.ViewModels;

namespace HealthCareManagement.Web.Controllers;

[Authorize]
public class MembersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MembersController> _logger;

    public MembersController(ApplicationDbContext context, ILogger<MembersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var familyGroupId = await ResolveFamilyGroupIdAsync();
        if (!familyGroupId.HasValue || familyGroupId.Value <= 0)
        {
            return View(new MembersIndexViewModel());
        }

        var familyGroup = await _context.FamilyGroups.FindAsync(familyGroupId.Value);

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId.Value);
            if (currentUser != null)
            {
                bool ownerExists = await _context.Members.AnyAsync(m =>
                    m.FamilyGroupId == familyGroupId.Value &&
                    ((m.UserId.HasValue && m.UserId.Value == currentUser.Id) ||
                     (!string.IsNullOrWhiteSpace(m.FullName) && m.FullName.Trim() == currentUser.FullName.Trim()))
                );

                if (!ownerExists)
                {
                    var ownerMember = new Member
                    {
                        FamilyGroupId = familyGroupId.Value,
                        UserId = currentUser.Id,
                        FullName = currentUser.FullName,
                        Relationship = "Chủ hộ",
                        DateOfBirth = DateTime.Today.AddYears(-30),
                        Gender = "Nam",
                        EmergencyPhone = currentUser.PhoneNumber,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Members.Add(ownerMember);
                    await _context.SaveChangesAsync();
                }
            }
        }

        var members = await _context.Members
            .Where(m => m.FamilyGroupId == familyGroupId.Value)
            .Include(m => m.HealthMetrics)
            .Include(m => m.MemberAllergies)
            .Include(m => m.MemberChronicDiseases)
            .OrderBy(m => m.Id)
            .ToListAsync();

        var viewModel = new MembersIndexViewModel
        {
            FamilyGroupId = familyGroupId.Value,
            FamilyGroupName = familyGroup?.GroupName ?? "Tổ ấm gia đình",
            Members = members
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberCreateViewModel model)
    {
        var familyGroupId = await ResolveFamilyGroupIdAsync();
        if (!familyGroupId.HasValue || familyGroupId.Value <= 0)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin tổ ấm của bạn. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Dữ liệu thêm thành viên chưa hợp lệ: {errors}";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var member = new Member
            {
                FamilyGroupId = familyGroupId.Value,
                FullName = model.FullName.Trim(),
                Relationship = model.Relationship.Trim(),
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                BloodGroup = string.IsNullOrWhiteSpace(model.BloodGroup) ? null : model.BloodGroup.Trim(),
                InsuranceNumber = string.IsNullOrWhiteSpace(model.InsuranceNumber) ? null : model.InsuranceNumber.Trim(),
                EmergencyPhone = string.IsNullOrWhiteSpace(model.EmergencyPhone) ? null : model.EmergencyPhone.Trim(),
                MedicalHistory = string.IsNullOrWhiteSpace(model.MedicalHistory) ? null : model.MedicalHistory.Trim(),
                Allergies = string.IsNullOrWhiteSpace(model.Allergies) ? null : model.Allergies.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(member.MedicalHistory))
            {
                member.MemberChronicDiseases.Add(new MemberChronicDisease
                {
                    DiseaseName = member.MedicalHistory,
                    CurrentStatus = "Đang theo dõi"
                });
            }

            if (!string.IsNullOrWhiteSpace(member.Allergies))
            {
                member.MemberAllergies.Add(new MemberAllergy
                {
                    AllergenType = "Dị ứng",
                    AllergenName = member.Allergies,
                    SeverityLevel = "Cần lưu ý"
                });
            }

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm thành viên {member.FullName} vào tổ ấm thành công!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm thành viên mới vào tổ ấm {FamilyGroupId}", familyGroupId);
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi thêm thành viên. Vui lòng thử lại.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MemberEditViewModel model)
    {
        var familyGroupId = await ResolveFamilyGroupIdAsync();
        if (!familyGroupId.HasValue || familyGroupId.Value <= 0)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin tổ ấm.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Dữ liệu chỉnh sửa chưa hợp lệ: {errors}";
            return RedirectToAction(nameof(Index));
        }

        var member = await _context.Members
            .Include(m => m.MemberChronicDiseases)
            .Include(m => m.MemberAllergies)
            .FirstOrDefaultAsync(m => m.Id == model.Id && m.FamilyGroupId == familyGroupId.Value);

        if (member == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thành viên hoặc bạn không có quyền cập nhật thành viên này.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            member.FullName = model.FullName.Trim();
            member.Relationship = model.Relationship.Trim();
            member.DateOfBirth = model.DateOfBirth;
            member.Gender = model.Gender;
            member.BloodGroup = string.IsNullOrWhiteSpace(model.BloodGroup) ? null : model.BloodGroup.Trim();
            member.InsuranceNumber = string.IsNullOrWhiteSpace(model.InsuranceNumber) ? null : model.InsuranceNumber.Trim();
            member.EmergencyPhone = string.IsNullOrWhiteSpace(model.EmergencyPhone) ? null : model.EmergencyPhone.Trim();
            member.MedicalHistory = string.IsNullOrWhiteSpace(model.MedicalHistory) ? null : model.MedicalHistory.Trim();
            member.Allergies = string.IsNullOrWhiteSpace(model.Allergies) ? null : model.Allergies.Trim();

            // Cập nhật hoặc đồng bộ bệnh nền
            if (!string.IsNullOrWhiteSpace(member.MedicalHistory))
            {
                var existingCd = member.MemberChronicDiseases.FirstOrDefault();
                if (existingCd != null)
                {
                    existingCd.DiseaseName = member.MedicalHistory;
                }
                else
                {
                    member.MemberChronicDiseases.Add(new MemberChronicDisease
                    {
                        DiseaseName = member.MedicalHistory,
                        CurrentStatus = "Đang theo dõi"
                    });
                }
            }

            // Cập nhật hoặc đồng bộ dị ứng
            if (!string.IsNullOrWhiteSpace(member.Allergies))
            {
                var existingAl = member.MemberAllergies.FirstOrDefault();
                if (existingAl != null)
                {
                    existingAl.AllergenName = member.Allergies;
                }
                else
                {
                    member.MemberAllergies.Add(new MemberAllergy
                    {
                        AllergenType = "Dị ứng",
                        AllergenName = member.Allergies,
                        SeverityLevel = "Cần lưu ý"
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Cập nhật thông tin thành viên {member.FullName} thành công!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật thành viên {MemberId}", model.Id);
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật thông tin thành viên.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var familyGroupId = await ResolveFamilyGroupIdAsync();
        if (!familyGroupId.HasValue || familyGroupId.Value <= 0)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin tổ ấm.";
            return RedirectToAction(nameof(Index));
        }

        var member = await _context.Members
            .FirstOrDefaultAsync(m => m.Id == id && m.FamilyGroupId == familyGroupId.Value);

        if (member == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thành viên cần xóa hoặc bạn không có quyền.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var memberName = member.FullName;
            _context.Members.Remove(member);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa thành viên {memberName} khỏi danh sách gia đình thành công!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa thành viên {MemberId}", id);
            TempData["ErrorMessage"] = "Không thể xóa thành viên vì đang có dữ liệu y tế liên kết.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordVitals(RecordVitalsViewModel model)
    {
        var familyGroupId = await ResolveFamilyGroupIdAsync();
        if (!familyGroupId.HasValue || familyGroupId.Value <= 0)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = "Không tìm thấy thông tin tổ ấm của bạn." });

            TempData["ErrorMessage"] = "Không tìm thấy thông tin tổ ấm của bạn.";
            return RedirectToAction(nameof(Index));
        }

        var member = await _context.Members
            .FirstOrDefaultAsync(m => m.Id == model.MemberId && m.FamilyGroupId == familyGroupId.Value);

        if (member == null)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = "Thành viên không hợp lệ hoặc không thuộc tổ ấm." });

            TempData["ErrorMessage"] = "Thành viên không hợp lệ hoặc không thuộc tổ ấm của bạn.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            if (IsAjaxRequest())
                return Json(new { success = false, message = errors });

            TempData["ErrorMessage"] = $"Dữ liệu sinh hiệu chưa hợp lệ: {errors}";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            bool isAbnormal = CheckIsAbnormal(
                model.SystolicBP,
                model.DiastolicBP,
                model.HeartRate,
                model.BloodSugar,
                model.SpO2,
                model.BodyTemperature
            );

            string? noteCombined = model.Notes?.Trim();
            if (!string.IsNullOrWhiteSpace(model.TimeContext))
            {
                noteCombined = string.IsNullOrWhiteSpace(noteCombined)
                    ? $"[{model.TimeContext.Trim()}]"
                    : $"[{model.TimeContext.Trim()}] {noteCombined}";
            }

            var metric = new HealthMetric
            {
                MemberId = member.Id,
                SystolicBP = model.SystolicBP,
                DiastolicBP = model.DiastolicBP,
                HeartRate = model.HeartRate,
                BloodSugar = model.BloodSugar.HasValue ? Convert.ToDecimal(model.BloodSugar.Value) : null,
                SpO2 = model.SpO2,
                BodyTemperature = model.BodyTemperature.HasValue ? Convert.ToDecimal(model.BodyTemperature.Value) : null,
                HeightCm = model.HeightCm.HasValue ? Convert.ToDecimal(model.HeightCm.Value) : null,
                WeightKg = model.WeightKg.HasValue ? Convert.ToDecimal(model.WeightKg.Value) : null,
                RecordedAt = DateTime.UtcNow,
                IsAbnormal = isAbnormal,
                Notes = noteCombined
            };

            _context.HealthMetrics.Add(metric);
            await _context.SaveChangesAsync();

            var bpText = metric.SystolicBP.HasValue && metric.DiastolicBP.HasValue
                ? $"{metric.SystolicBP}/{metric.DiastolicBP} mmHg"
                : "--/--";
            var hrText = metric.HeartRate.HasValue ? $"{metric.HeartRate} bpm" : "--";

            var successMsg = $"Ghi nhận sinh hiệu cho {member.FullName} thành công! (Huyết áp: {bpText}, Nhịp tim: {hrText})";
            if (isAbnormal)
            {
                successMsg += " - Lưu ý: Chỉ số có dấu hiệu bất thường, gia đình nên chú ý theo dõi.";
            }

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = successMsg,
                    isAbnormal = isAbnormal,
                    metricId = metric.Id,
                    bp = bpText,
                    hr = hrText
                });
            }

            TempData["SuccessMessage"] = successMsg;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ghi nhận sinh hiệu cho thành viên {MemberId}", model.MemberId);
            if (IsAjaxRequest())
                return Json(new { success = false, message = "Đã xảy ra lỗi khi lưu chỉ số sinh hiệu." });

            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lưu chỉ số sinh hiệu.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id.HasValue && id.Value > 0)
        {
            return RedirectToAction("Index", "MedicalRecords", new { memberId = id.Value });
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> ResolveFamilyGroupIdAsync()
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return null;

        var fgId = GetCurrentFamilyGroupId();
        if (fgId.HasValue && fgId.Value > 0)
        {
            var exists = await _context.FamilyGroups.AnyAsync(f => f.Id == fgId.Value);
            if (exists) return fgId.Value;
        }

        var fg = await _context.FamilyGroups.FirstOrDefaultAsync(f => f.CreatedByUserId == currentUserId.Value);
        if (fg != null)
        {
            return fg.Id;
        }

        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUserId.Value);
        if (member != null)
        {
            return member.FamilyGroupId;
        }

        // Tự động khởi tạo tổ ấm nếu người dùng mới chưa có
        var user = await _context.Users.FindAsync(currentUserId.Value);
        if (user != null)
        {
            var newFg = new FamilyGroup
            {
                GroupName = $"Tổ ấm {user.FullName}",
                Address = string.Empty,
                EmergencyPhone = user.PhoneNumber ?? string.Empty,
                InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.FamilyGroups.Add(newFg);
            await _context.SaveChangesAsync();
            return newFg.Id;
        }

        return null;
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

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               Request.Headers.Accept.ToString().Contains("application/json");
    }

    private static bool CheckIsAbnormal(int? systolic, int? diastolic, int? heartRate, double? bloodSugar, int? spO2, double? temp)
    {
        if (systolic.HasValue && (systolic.Value >= 140 || systolic.Value < 90)) return true;
        if (diastolic.HasValue && (diastolic.Value >= 90 || diastolic.Value < 60)) return true;
        if (heartRate.HasValue && (heartRate.Value > 100 || heartRate.Value < 50)) return true;
        if (bloodSugar.HasValue && (bloodSugar.Value > 7.8 || bloodSugar.Value < 3.9)) return true;
        if (spO2.HasValue && spO2.Value < 95) return true;
        if (temp.HasValue && (temp.Value >= 38.0 || temp.Value < 35.5)) return true;
        return false;
    }
}
