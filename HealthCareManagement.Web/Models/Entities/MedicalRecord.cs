using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("MedicalRecords")]
public class MedicalRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    public virtual Member Member { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string HospitalName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DoctorName { get; set; }

    public DateTime ExamDate { get; set; }

    [Required]
    [MaxLength(255)]
    public string Diagnosis { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DiagnosisCode { get; set; } // Mã ICD-10 (ví dụ: I10, E11, J02)

    public string? Conclusion { get; set; } // Kết luận & dặn dò

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<MedicalAttachment> MedicalAttachments { get; set; } = new List<MedicalAttachment>();
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
