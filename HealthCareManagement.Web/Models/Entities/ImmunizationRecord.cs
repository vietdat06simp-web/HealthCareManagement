using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("ImmunizationRecords")]
public class ImmunizationRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    public virtual Member Member { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string VaccineName { get; set; } = string.Empty;

    public int DoseNumber { get; set; } // Mũi 1, Mũi 2, Mũi nhắc

    public DateTime InjectedDate { get; set; }

    [MaxLength(150)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? BatchNumber { get; set; }

    [MaxLength(255)]
    public string? ReactionNotes { get; set; } // Phản ứng sau tiêm: sốt, sưng đau nhẹ...

    public DateTime? NextDoseDate { get; set; }
}
