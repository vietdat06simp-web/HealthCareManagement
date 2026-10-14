using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("CareReminders")]
public class CareReminder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    public virtual Member Member { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string SourceType { get; set; } = string.Empty; // 'Uống thuốc', 'Lịch hẹn', 'Đo chỉ số', 'Tập luyện'

    public int? SourceReferenceId { get; set; } // Liên kết Id của PrescriptionDose hoặc Appointment

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public DateTime ScheduledDateTime { get; set; }

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletedAt { get; set; }

    public int? CompletedByUserId { get; set; }

    [ForeignKey(nameof(CompletedByUserId))]
    public virtual User? CompletedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
