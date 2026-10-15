using System.ComponentModel.DataAnnotations;
using HealthCareManagement.Web.Models.Entities;

namespace HealthCareManagement.Web.Models.ViewModels;

public class MembersIndexViewModel
{
    public int FamilyGroupId { get; set; }
    public string FamilyGroupName { get; set; } = string.Empty;
    public List<Member> Members { get; set; } = new();
    public MemberCreateViewModel NewMember { get; set; } = new();
    public RecordVitalsViewModel NewVitals { get; set; } = new();
}

public class MemberCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn mối quan hệ.")]
    [StringLength(50, ErrorMessage = "Mối quan hệ không được vượt quá 50 ký tự.")]
    public string Relationship { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-25);

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    [StringLength(10, ErrorMessage = "Giới tính không hợp lệ.")]
    public string Gender { get; set; } = "Nam";

    [StringLength(5, ErrorMessage = "Nhóm máu tối đa 5 ký tự.")]
    public string? BloodGroup { get; set; }

    [StringLength(30, ErrorMessage = "Mã BHYT tối đa 30 ký tự.")]
    public string? InsuranceNumber { get; set; }

    [StringLength(15, ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    public string? EmergencyPhone { get; set; }

    [StringLength(500, ErrorMessage = "Tiền sử bệnh tối đa 500 ký tự.")]
    public string? MedicalHistory { get; set; }

    [StringLength(500, ErrorMessage = "Thông tin dị ứng tối đa 500 ký tự.")]
    public string? Allergies { get; set; }
}

public class MemberEditViewModel
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn mối quan hệ.")]
    [StringLength(50, ErrorMessage = "Mối quan hệ không được vượt quá 50 ký tự.")]
    public string Relationship { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    [StringLength(10, ErrorMessage = "Giới tính không hợp lệ.")]
    public string Gender { get; set; } = "Nam";

    [StringLength(5, ErrorMessage = "Nhóm máu tối đa 5 ký tự.")]
    public string? BloodGroup { get; set; }

    [StringLength(30, ErrorMessage = "Mã BHYT tối đa 30 ký tự.")]
    public string? InsuranceNumber { get; set; }

    [StringLength(15, ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    public string? EmergencyPhone { get; set; }

    [StringLength(500, ErrorMessage = "Tiền sử bệnh tối đa 500 ký tự.")]
    public string? MedicalHistory { get; set; }

    [StringLength(500, ErrorMessage = "Thông tin dị ứng tối đa 500 ký tự.")]
    public string? Allergies { get; set; }
}

public class RecordVitalsViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn thành viên.")]
    public int MemberId { get; set; }

    [Range(50, 260, ErrorMessage = "Huyết áp tâm thu từ 50 - 260 mmHg.")]
    public int? SystolicBP { get; set; }

    [Range(30, 160, ErrorMessage = "Huyết áp tâm trương từ 30 - 160 mmHg.")]
    public int? DiastolicBP { get; set; }

    [Range(30, 220, ErrorMessage = "Nhịp tim từ 30 - 220 bpm.")]
    public int? HeartRate { get; set; }

    [Range(1.0, 45.0, ErrorMessage = "Đường huyết từ 1.0 - 45.0 mmol/L.")]
    public double? BloodSugar { get; set; }

    [Range(50, 100, ErrorMessage = "SpO2 từ 50 - 100%.")]
    public int? SpO2 { get; set; }

    [Range(30.0, 45.0, ErrorMessage = "Thân nhiệt từ 30.0 - 45.0 °C.")]
    public double? BodyTemperature { get; set; }

    [Range(30.0, 250.0, ErrorMessage = "Chiều cao từ 30 - 250 cm.")]
    public double? HeightCm { get; set; }

    [Range(1.0, 300.0, ErrorMessage = "Cân nặng từ 1 - 300 kg.")]
    public double? WeightKg { get; set; }

    public string? TimeContext { get; set; }

    [StringLength(255, ErrorMessage = "Ghi chú không quá 255 ký tự.")]
    public string? Notes { get; set; }
}
