using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("MedicalAttachments")]
public class MedicalAttachment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    [ForeignKey(nameof(MedicalRecordId))]
    public virtual MedicalRecord MedicalRecord { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string FilePath { get; set; } = string.Empty; // Đường dẫn vật lý hoặc URL cloud

    [Required]
    [MaxLength(200)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty; // 'image/jpeg', 'application/pdf'

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
