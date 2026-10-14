using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("PrescriptionItems")]
public class PrescriptionItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int PrescriptionId { get; set; }

    [ForeignKey(nameof(PrescriptionId))]
    public virtual Prescription Prescription { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string MedicineName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DosagePerTime { get; set; } = string.Empty; // '1 viên', '5ml', '1 giọt'

    [Required]
    [MaxLength(50)]
    public string MealTiming { get; set; } = string.Empty; // 'Trước ăn 30p', 'Sau ăn no', 'Trong bữa ăn'

    [MaxLength(255)]
    public string? SpecialInstruction { get; set; }

    // Navigation properties
    public virtual ICollection<PrescriptionDose> PrescriptionDoses { get; set; } = new List<PrescriptionDose>();
}
