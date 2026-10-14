using System.ComponentModel.DataAnnotations;

namespace HealthCareManagement.Web.Models.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại")]
    [Display(Name = "Email hoặc Số điện thoại")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ phiên đăng nhập")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}
