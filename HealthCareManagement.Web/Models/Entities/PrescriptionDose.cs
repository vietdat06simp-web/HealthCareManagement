using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("PrescriptionDoses")]
public class PrescriptionDose
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int PrescriptionItemId { get; set; }

    [ForeignKey(nameof(PrescriptionItemId))]
    public virtual PrescriptionItem PrescriptionItem { get; set; } = null!;

    public TimeSpan DoseTime { get; set; } // 08:00:00, 12:30:00, 20:00:00

    [MaxLength(20)]
    public string? TimeOfDay { get; set; } // 'Sáng', 'Trưa', 'Tối', 'Đêm'

    [Column(TypeName = "decimal(4,1)")]
    public decimal Quantity { get; set; } = 1.0m;

    // Navigation properties
    public virtual ICollection<MedicationSchedule> MedicationSchedules { get; set; } = new List<MedicationSchedule>();
}
