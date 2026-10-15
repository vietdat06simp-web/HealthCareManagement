using System.ComponentModel.DataAnnotations;
using HealthCareManagement.Web.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace HealthCareManagement.Web.Models.ViewModels;

public class MedicalRecordCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn thành viên khám bệnh")]
    [Display(Name = "Thành viên gia đình")]
    public int MemberId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày khám")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày khám")]
    public DateTime VisitDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng nhập tên cơ sở y tế / Bệnh viện")]
    [MaxLength(150, ErrorMessage = "Tên bệnh viện tối đa 150 ký tự")]
    [Display(Name = "Cơ sở y tế / Bệnh viện")]
    public string FacilityName { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Tên bác sĩ tối đa 100 ký tự")]
    [Display(Name = "Bác sĩ điều trị")]
    public string? DoctorName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập chẩn đoán bệnh")]
    [MaxLength(255, ErrorMessage = "Chẩn đoán tối đa 255 ký tự")]
    [Display(Name = "Chẩn đoán y khoa")]
    public string Diagnosis { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Mã ICD-10 tối đa 20 ký tự")]
    [Display(Name = "Mã chẩn đoán (ICD-10)")]
    public string? DiagnosisCode { get; set; }

    [Display(Name = "Phác đồ điều trị / Lời dặn")]
    public string? TreatmentPlan { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày hẹn tái khám (nếu có)")]
    public DateTime? NextAppointmentDate { get; set; }

    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
    [Display(Name = "Ghi chú thêm")]
    public string? Notes { get; set; }

    [Display(Name = "Tệp đính kèm (Ảnh X-quang, Kết quả xét nghiệm, PDF)")]
    public IFormFile? AttachmentFile { get; set; }
}

public class MedicalRecordDetailsViewModel
{
    public MedicalRecord Record { get; set; } = null!;
    public List<MedicalAttachment> Attachments { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public Appointment? NextAppointment { get; set; }
}
