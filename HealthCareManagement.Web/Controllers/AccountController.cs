using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareManagement.Web.Data;
using HealthCareManagement.Web.Models.Entities;
using HealthCareManagement.Web.Models.ViewModels.Account;

namespace HealthCareManagement.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var input = model.Email.Trim();
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.Email == input || u.PhoneNumber == input || u.Username == input);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn hiện đang bị khóa hoặc chưa kích hoạt.");
            return View(model);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
            return View(model);
        }

        // Lấy FamilyGroupId liên kết với User
        int familyGroupId = user.Member?.FamilyGroupId ?? 0;
        if (familyGroupId == 0)
        {
            var fg = await _context.FamilyGroups.FirstOrDefaultAsync(f => f.CreatedByUserId == user.Id);
            if (fg != null)
            {
                familyGroupId = fg.Id;
            }
        }

        var roleName = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Chủ hộ";

        // Thiết lập danh sách Claims: NameIdentifier, Name, Email, Role, FamilyGroupId
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("FamilyGroupId", familyGroupId.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, "HealthCareCookie");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        await HttpContext.SignInAsync("HealthCareCookie", claimsPrincipal, authProperties);

        TempData["SuccessMessage"] = $"Chào mừng bạn trở lại, {user.FullName}!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new RegisterViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLower();
        var normalizedPhone = model.PhoneNumber.Trim();

        // 1. Kiểm tra trùng Email hoặc SĐT trong bảng Users
        var emailExists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng trên hệ thống.");
            return View(model);
        }

        var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone);
        if (phoneExists)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại này đã được sử dụng trên hệ thống.");
            return View(model);
        }

        // 2. Tạo User mới và băm mật khẩu
        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = normalizedPhone,
            Username = !string.IsNullOrWhiteSpace(normalizedEmail) ? normalizedEmail : normalizedPhone,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 3. Đảm bảo Role 'Chủ hộ' tồn tại trong bảng Roles và gán cho User
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Chủ hộ");
        if (role == null)
        {
            role = new Role
            {
                RoleName = "Chủ hộ",
                Description = "Chủ hộ gia đình (Quản trị tổ ấm)"
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAt = DateTime.UtcNow
        });

        // 4. Tạo mới một bản ghi FamilyGroups (GroupName theo tên người dùng nhập, InviteCode ngẫu nhiên 'FHH-XXXX-VN')
        var groupName = !string.IsNullOrWhiteSpace(model.FamilyGroupName)
            ? model.FamilyGroupName.Trim()
            : $"Tổ ấm {user.FullName}";

        var familyGroup = new FamilyGroup
        {
            GroupName = groupName,
            InviteCode = GenerateInviteCode(),
            CreatedByUserId = user.Id,
            EmergencyPhone = user.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };
        _context.FamilyGroups.Add(familyGroup);
        await _context.SaveChangesAsync();

        // 5. Tự động đăng nhập (SignInAsync với Claims: NameIdentifier, Name, Email, Role, FamilyGroupId)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, "Chủ hộ"),
            new Claim("FamilyGroupId", familyGroup.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, "HealthCareCookie");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync("HealthCareCookie", claimsPrincipal, authProperties);

        TempData["SuccessMessage"] = "Đăng ký tài khoản và khởi tạo tổ ấm thành công!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("HealthCareCookie");
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
        return $"FHH-{code}-VN";
    }
}
