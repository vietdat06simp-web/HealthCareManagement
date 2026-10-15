using HealthCareManagement.Web.Models.Entities;

namespace HealthCareManagement.Web.Models.ViewModels;

public class PrescriptionsIndexViewModel
{
    public List<MedicationSchedule> TodaySchedules { get; set; } = new();
    public List<MedicationSchedule> MorningSchedules { get; set; } = new();
    public List<MedicationSchedule> NoonSchedules { get; set; } = new();
    public List<MedicationSchedule> AfternoonSchedules { get; set; } = new();
    public List<MedicationSchedule> EveningSchedules { get; set; } = new();

    public List<Prescription> ActivePrescriptions { get; set; } = new();
    public List<Appointment> UpcomingAppointments { get; set; } = new();
    public List<Member> Members { get; set; } = new();
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public string ActiveTab { get; set; } = "medications";

    public PrescriptionCreateViewModel NewPrescription { get; set; } = new();
}
