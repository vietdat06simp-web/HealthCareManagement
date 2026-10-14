using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("Appointments")]
public class Appointment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    public virtual Member Member { get; set; } = null!;

    public int? MedicalRecordId { get; set; }

    [ForeignKey(nameof(MedicalRecordId))]
    public virtual MedicalRecord? MedicalRecord { get; set; }

    [Required]
    [MaxLength(50)]
    public string AppointmentType { get; set; } = string.Empty; // 'Tái khám', 'Tiêm phòng', 'Xét nghiệm định kỳ'

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public DateTime AppointmentDateTime { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty; // Tên bệnh viện, địa chỉ, phòng khám

    [MaxLength(100)]
    public string? DoctorName { get; set; }

    [MaxLength(300)]
    public string? PreparationNotes { get; set; } // 'Nhịn ăn sáng', 'Đem sổ tiêm cũ'...

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Chờ khám"; // 'Chờ khám', 'Đã xác nhận phòng khám', 'Đã hoàn thành', 'Đã hủy'

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
