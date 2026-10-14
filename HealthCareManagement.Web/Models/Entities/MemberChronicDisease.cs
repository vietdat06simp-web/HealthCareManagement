using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("MemberChronicDiseases")]
public class MemberChronicDisease
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    public virtual Member Member { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string DiseaseName { get; set; } = string.Empty; // 'Cao huyết áp', 'Tiểu đường type 2'

    public int? DiagnosedYear { get; set; }

    [MaxLength(100)]
    public string? CurrentStatus { get; set; }
}
