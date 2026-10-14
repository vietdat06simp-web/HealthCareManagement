using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareManagement.Web.Models.Entities;

[Table("HealthMetrics")]
public class HealthMetric
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    public virtual Member Member { get; set; } = null!;

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? WeightKg { get; set; } // Cân nặng (kg)

    [Column(TypeName = "decimal(5,2)")]
    public decimal? HeightCm { get; set; } // Chiều cao (cm)

    public int? SystolicBP { get; set; } // Huyết áp tâm thu (mmHg)

    public int? DiastolicBP { get; set; } // Huyết áp tâm trương (mmHg)

    public int? HeartRate { get; set; } // Nhịp tim (bpm)

    [Column(TypeName = "decimal(5,2)")]
    public decimal? BloodSugar { get; set; } // Đường huyết (mmol/L hoặc mg/dL)

    public int? SpO2 { get; set; } // Nồng độ oxy trong máu (%)

    [Column(TypeName = "decimal(4,1)")]
    public decimal? BodyTemperature { get; set; } // Thân nhiệt (°C)

    public bool IsAbnormal { get; set; } = false; // Cảnh báo vượt ngưỡng an toàn

    [MaxLength(255)]
    public string? Notes { get; set; }
}
