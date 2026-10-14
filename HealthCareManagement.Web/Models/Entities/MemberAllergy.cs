using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("MemberAllergies")]
public class MemberAllergy
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    public virtual Member Member { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string AllergenType { get; set; } = string.Empty; // 'Thuốc', 'Thực phẩm', 'Thời tiết'

    [Required]
    [MaxLength(100)]
    public string AllergenName { get; set; } = string.Empty; // 'Penicillin', 'Hải sản'...

    [Required]
    [MaxLength(30)]
    public string SeverityLevel { get; set; } = string.Empty; // 'Nhẹ', 'Trung bình', 'Sốc phản vệ'

    [MaxLength(255)]
    public string? Notes { get; set; }
}
