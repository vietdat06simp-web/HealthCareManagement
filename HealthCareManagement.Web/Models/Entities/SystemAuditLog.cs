using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("SystemAuditLogs")]
public class SystemAuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // 'LOGIN', 'CREATE_MEMBER', 'DELETE_RECORD'

    [MaxLength(100)]
    public string? EntityName { get; set; }

    public int? EntityId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public string? Details { get; set; }
}
