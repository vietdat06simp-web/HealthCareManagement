namespace HealthCareManagement.Web.Models.ViewModels;

public class FamilyGroupViewModel
{
    public int Id { get; set; } = 1;

    public string GroupName { get; set; } = "Tổ ấm Đại gia đình Họ Trần";

    public string? Address { get; set; } = "Số 45, Đường Lê Duẩn, P. Bến Nghé, Quận 1, TP. HCM";

    public string? EmergencyPhone { get; set; } = "0912 345 678";

    public string? GroupCode { get; set; } = "#FHH-7729-HCM";

    public List<FamilyMemberItemViewModel> Members { get; set; } = new();
}

public class FamilyMemberItemViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string? BloodGroup { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? IdentityOrInsurance { get; set; }

    public string MedicalRole { get; set; } = string.Empty;

    public string? RoleBadgeType { get; set; } // "owner", "guardian", "dependent"

    public DateTime JoinedDate { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    public bool IsCurrentUser { get; set; }
}
