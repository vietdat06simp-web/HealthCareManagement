namespace HealthCareManagement.Web.Models.ViewModels;

public class FamilyGroupViewModel
{
    public int Id { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? EmergencyPhone { get; set; }

    public string? GroupCode { get; set; }

    public string? InviteCode { get; set; }

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
