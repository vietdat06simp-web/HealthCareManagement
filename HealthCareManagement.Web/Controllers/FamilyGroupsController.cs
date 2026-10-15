using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.Entities;
using HealthCareManagement.Web.Models.ViewModels;

namespace HealthCareManagement.Web.Controllers;

[Authorize]
public class FamilyGroupsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FamilyGroupsController> _logger;

    public FamilyGroupsController(ApplicationDbContext context, ILogger<FamilyGroupsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUserId = GetCurrentUserId();
        var familyGroupId = GetCurrentFamilyGroupId();

        // Nếu claim chưa có hoặc = 0, tìm qua Member hoặc CreatedByUser
        if (!familyGroupId.HasValue || familyGroupId.Value == 0)
        {
            if (currentUserId.HasValue)
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUserId.Value);
                if (member != null)
                {
                    familyGroupId = member.FamilyGroupId;
                }
                else
                {
                    var fg = await _context.FamilyGroups.FirstOrDefaultAsync(f => f.CreatedByUserId == currentUserId.Value);
                    if (fg != null)
                    {
                        familyGroupId = fg.Id;
                    }
                }
            }
        }

        FamilyGroup? familyGroup = null;
        if (familyGroupId.HasValue && familyGroupId.Value > 0)
        {
            familyGroup = await _context.FamilyGroups
                .Include(fg => fg.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(fg => fg.Id == familyGroupId.Value);
        }

        // Nếu người dùng chưa có tổ ấm trong database, tạo một tổ ấm sạch gắn với tài khoản người dùng
        if (familyGroup == null && currentUserId.HasValue)
        {
            var user = await _context.Users.FindAsync(currentUserId.Value);
            if (user != null)
            {
                familyGroup = new FamilyGroup
                {
                    GroupName = $"Tổ ấm {user.FullName}",
                    Address = string.Empty,
                    EmergencyPhone = user.PhoneNumber ?? string.Empty,
                    InviteCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    CreatedByUserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FamilyGroups.Add(familyGroup);
                await _context.SaveChangesAsync();

                await _context.Entry(familyGroup).Collection(f => f.Members).LoadAsync();
            }
        }

        // Map sang FamilyGroupViewModel để View hiển thị
        var model = new FamilyGroupViewModel
        {
            Id = familyGroup?.Id ?? 0,
            GroupName = familyGroup?.GroupName ?? string.Empty,
            Address = familyGroup?.Address ?? string.Empty,
            EmergencyPhone = familyGroup?.EmergencyPhone ?? string.Empty,
            GroupCode = familyGroup?.InviteCode ?? string.Empty,
            InviteCode = familyGroup?.InviteCode ?? string.Empty,
            Members = familyGroup?.Members != null && familyGroup.Members.Any()
                ? familyGroup.Members.Select(m => MapToMemberItemViewModel(m, currentUserId)).OrderByDescending(m => m.RoleBadgeType == "owner").ThenBy(m => m.Id).ToList()
                : new List<FamilyMemberItemViewModel>()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(FamilyGroupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var familyGroup = await _context.FamilyGroups.FindAsync(model.Id);
        if (familyGroup == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy dữ liệu tổ ấm cần cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        familyGroup.GroupName = model.GroupName?.Trim() ?? string.Empty;
        familyGroup.Address = model.Address?.Trim();
        familyGroup.EmergencyPhone = model.EmergencyPhone?.Trim();

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cập nhật thông tin tổ ấm thành công!";
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

    private FamilyMemberItemViewModel MapToMemberItemViewModel(Member m, int? currentUserId)
    {
        var isCurrentUser = currentUserId.HasValue && (m.UserId == currentUserId.Value || m.FullName == User.Identity?.Name);

        var age = DateTime.Today.Year - m.DateOfBirth.Year;
        if (m.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
        if (age < 0) age = 0;

        string medicalRole = m.Relationship;
        string roleBadgeType = "dependent";
        if (m.Relationship.Contains("Chủ hộ"))
        {
            medicalRole = "Chủ hộ (Toàn quyền)";
            roleBadgeType = "owner";
        }
        else if (m.Relationship.Contains("Giám hộ") || m.Relationship == "Vợ" || m.Relationship == "Chồng")
        {
            medicalRole = "Giám hộ y tế";
            roleBadgeType = "guardian";
        }
        else
        {
            medicalRole = "Thành viên phụ thuộc";
            roleBadgeType = "dependent";
        }

        var identityOrInsurance = !string.IsNullOrEmpty(m.InsuranceNumber)
            ? $"BHYT: {m.InsuranceNumber}"
            : (!string.IsNullOrEmpty(m.EmergencyPhone) ? $"Hotline: {m.EmergencyPhone}" : null);

        var notes = !string.IsNullOrEmpty(m.MedicalHistory)
            ? $"Bệnh nền: {m.MedicalHistory}"
            : (!string.IsNullOrEmpty(m.Allergies) ? $"Dị ứng: {m.Allergies}" : null);

        return new FamilyMemberItemViewModel
        {
            Id = m.Id,
            FullName = m.FullName,
            Relationship = m.Relationship,
            Age = age,
            Gender = m.Gender,
            BloodGroup = m.BloodGroup,
            Email = m.User?.Email,
            PhoneNumber = m.User?.PhoneNumber ?? m.EmergencyPhone,
            IdentityOrInsurance = identityOrInsurance,
            MedicalRole = medicalRole,
            RoleBadgeType = roleBadgeType,
            JoinedDate = m.CreatedAt,
            Notes = notes,
            IsCurrentUser = isCurrentUser
        };
    }
}
