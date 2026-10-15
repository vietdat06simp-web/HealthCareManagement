using HealthCareManagement.Web.Models.Entities;

namespace HealthCareManagement.Web.Models.ViewModels;

public class HomeDashboardViewModel
{
    public string FamilyGroupName { get; set; } = "Gia đình";
    public string? InviteCode { get; set; }
    public string? Address { get; set; }
    public string? EmergencyPhone { get; set; }
    public int TotalMembers { get; set; } = 0;
    public int SafeMembersCount { get; set; } = 0;
    public List<Member> Members { get; set; } = new();

    // Medications
    public int TodayDosesTaken { get; set; } = 0;
    public int TodayDosesTotal { get; set; } = 0;
    public int TodayDosesPending => Math.Max(0, TodayDosesTotal - TodayDosesTaken);
    public int DoseProgressPercent => TodayDosesTotal > 0 ? (int)Math.Round((double)TodayDosesTaken / TodayDosesTotal * 100) : 0;

    // Appointments
    public int UpcomingAppointmentsCount { get; set; } = 0;
    public string? NextAppointmentSummary { get; set; }
    public string? NextAppointmentMember { get; set; }
    public int? DaysUntilNextAppointment { get; set; }

    // Vitals
    public string? LatestBloodPressure { get; set; }
    public int? LatestHeartRate { get; set; }
    public int? LatestSpO2 { get; set; }
    public string? LatestVitalsMember { get; set; }
    public bool HasAbnormalVitals { get; set; } = false;

    // Medical Records
    public int TotalMedicalRecords { get; set; } = 0;
    public int TotalAttachments { get; set; } = 0;
    public string? LatestHospital { get; set; }
}
