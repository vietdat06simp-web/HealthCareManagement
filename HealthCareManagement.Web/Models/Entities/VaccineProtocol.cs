using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("VaccineProtocols")]
public class VaccineProtocol
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string VaccineName { get; set; } = string.Empty; // Lao, Viêm gan B, 6 trong 1...

    [Required]
    [MaxLength(200)]
    public string TargetDisease { get; set; } = string.Empty;

    public int RecommendedAgeMonths { get; set; } // Tuổi chỉ định (tháng tuổi)

    public int DoseIndex { get; set; } // Mũi 1, Mũi 2, Mũi 3...

    [MaxLength(255)]
    public string? Notes { get; set; }
}
