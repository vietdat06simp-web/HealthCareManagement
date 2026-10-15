using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("Prescriptions")]
public class Prescription
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? MedicalRecordId { get; set; }

    [ForeignKey(nameof(MedicalRecordId))]
    public virtual MedicalRecord? MedicalRecord { get; set; }

    public int MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    public virtual Member Member { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string PrescriptionName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [MaxLength(100)]
    public string? DoctorName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
