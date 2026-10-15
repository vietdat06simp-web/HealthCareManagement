using System.ComponentModel.DataAnnotations;

namespace HealthCareManagement.Web.Models.ViewModels;

public class PrescriptionItemInputModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên thuốc")]
    [MaxLength(150)]
    public string DrugName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Dosage { get; set; } = "1 viên";

    [MaxLength(50)]
    public string? Frequency { get; set; }

    [MaxLength(50)]
    public string? MealTiming { get; set; } = "Sau ăn";

    [MaxLength(255)]
    public string? SpecialInstruction { get; set; }

    public bool Morning { get; set; }
    public bool Noon { get; set; }
    public bool Afternoon { get; set; }
    public bool Evening { get; set; }
}

public class PrescriptionCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn thành viên")]
    public int MemberId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đơn thuốc")]
    [MaxLength(150)]
    public string PrescriptionName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DoctorName { get; set; }

    public int? MedicalRecordId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    // List of drugs added dynamically
    public List<PrescriptionItemInputModel> Drugs { get; set; } = new();

    // Single drug fallback fields for simple form submission
    [MaxLength(150)]
    public string? DrugName { get; set; }

    [MaxLength(50)]
    public string? Dosage { get; set; }

    [MaxLength(50)]
    public string? Frequency { get; set; }

    [MaxLength(50)]
    public string? MealTiming { get; set; }

    [MaxLength(255)]
    public string? SpecialInstruction { get; set; }

    public bool Morning { get; set; }
    public bool Noon { get; set; }
    public bool Afternoon { get; set; }
    public bool Evening { get; set; }
}
