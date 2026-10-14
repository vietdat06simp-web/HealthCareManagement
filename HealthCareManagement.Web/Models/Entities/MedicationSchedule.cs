using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("MedicationSchedules")]
public class MedicationSchedule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int PrescriptionDoseId { get; set; }

    [ForeignKey(nameof(PrescriptionDoseId))]
    public virtual PrescriptionDose PrescriptionDose { get; set; } = null!;

    public DateTime ScheduleDate { get; set; } // Ngày theo dõi lịch uống

    public bool IsTaken { get; set; } = false;

    public DateTime? TakenAt { get; set; } // Thời điểm thực tế bấm xác nhận uống

    [MaxLength(255)]
    public string? TakenNotes { get; set; } // Ghi chú khi uống (HA: 122/80, uống sau ăn...)
}
