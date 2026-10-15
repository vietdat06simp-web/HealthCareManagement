using HealthCareManagement.Web.Models.Entities;

namespace HealthCareManagement.Web.Models.ViewModels;

public class PrescriptionsIndexViewModel
{
    public List<MedicationSchedule> TodaySchedules { get; set; } = new();
    public List<Prescription> ActivePrescriptions { get; set; } = new();
    public List<Appointment> UpcomingAppointments { get; set; } = new();
    public List<Member> Members { get; set; } = new();
    public string ActiveTab { get; set; } = "medications";
}
