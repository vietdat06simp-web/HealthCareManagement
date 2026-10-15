using HealthCareManagement.Web.Models.Entities;

namespace HealthCareManagement.Web.Models.ViewModels;

public class MedicalRecordsIndexViewModel
{
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Member> Members { get; set; } = new();
    public HealthMetric? LatestMetric { get; set; }
    public Appointment? NextAppointment { get; set; }
    public int TotalAttachments => MedicalRecords.Sum(r => r.MedicalAttachments?.Count ?? 0);
    public int TotalPrescriptions => MedicalRecords.Sum(r => r.Prescriptions?.Count ?? 0);
    public MedicalRecord? LatestRecord => MedicalRecords.FirstOrDefault();
}
