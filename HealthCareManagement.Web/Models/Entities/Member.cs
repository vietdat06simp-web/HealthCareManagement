using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("Members")]
public class Member
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FamilyGroupId { get; set; }

    [ForeignKey(nameof(FamilyGroupId))]
    public virtual FamilyGroup FamilyGroup { get; set; } = null!;

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Relationship { get; set; } = string.Empty; // 'Chủ hộ', 'Vợ', 'Chồng', 'Con', 'Bố mẹ'

    public DateTime DateOfBirth { get; set; }

    [Required]
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty; // 'Nam', 'Nữ', 'Khác'

    [MaxLength(5)]
    public string? BloodGroup { get; set; } // 'A+', 'B+', 'O+', 'AB-'

    [MaxLength(30)]
    public string? InsuranceNumber { get; set; } // Mã thẻ BHYT

    [MaxLength(15)]
    [Phone]
    public string? EmergencyPhone { get; set; }

    [MaxLength(255)]
    public string? AvatarUrl { get; set; }

    [MaxLength(500)]
    public string? MedicalHistory { get; set; } // Tiền sử bệnh nền tóm tắt

    [MaxLength(500)]
    public string? Allergies { get; set; } // Dị ứng tóm tắt

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<MemberAllergy> MemberAllergies { get; set; } = new List<MemberAllergy>();
    public virtual ICollection<MemberChronicDisease> MemberChronicDiseases { get; set; } = new List<MemberChronicDisease>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<ImmunizationRecord> ImmunizationRecords { get; set; } = new List<ImmunizationRecord>();
    public virtual ICollection<CareReminder> CareReminders { get; set; } = new List<CareReminder>();
    public virtual ICollection<HealthMetric> HealthMetrics { get; set; } = new List<HealthMetric>();
}
