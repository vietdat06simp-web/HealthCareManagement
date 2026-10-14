using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("MedicineDictionary")]
public class MedicineDictionary
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string BrandName { get; set; } = string.Empty; // Panadol Extra, Augmentin, Amlodipine...

    [Required]
    [MaxLength(150)]
    public string ActiveIngredient { get; set; } = string.Empty; // Hoạt chất chính

    [Required]
    [MaxLength(50)]
    public string DefaultDosageForm { get; set; } = string.Empty; // Viên nén, Gói bột, Siro...

    [MaxLength(255)]
    public string? StandardUsage { get; set; } // Hướng dẫn sử dụng chuẩn

    public bool IsActive { get; set; } = true;
}
