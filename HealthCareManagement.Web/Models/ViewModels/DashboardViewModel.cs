namespace HealthCareManagement.Web.Models.ViewModels;

public class MemberVitalSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public int? LatestSystolic { get; set; }
    public int? LatestDiastolic { get; set; }
    public int? LatestHeartRate { get; set; }
    public DateTime? LastMeasuredAt { get; set; }
    public string VitalStatus { get; set; } = "Chưa có dữ liệu";
}

public class MedicationScheduleDto
{
    public int Id { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string MealTime { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
    public bool IsTaken { get; set; }
    public DateTime? TakenAt { get; set; }
}

public class UpcomingAppointmentDto
{
    public int Id { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public int DaysLeft { get; set; }
    public string? AppointmentType { get; set; }
    public string? Status { get; set; }
}

public class DashboardViewModel
{
    public string FamilyGroupName { get; set; } = "Tổ ấm gia đình";
    public string? InviteCode { get; set; }
    public string? Address { get; set; }
    public string? EmergencyPhone { get; set; }

    public int TotalMembers { get; set; } = 0;
    public int PendingMedicationsCount { get; set; } = 0;
    public int TotalRecordsCount { get; set; } = 0;
    public int TotalTodayMedications { get; set; } = 0;
    public int TakenMedicationsCount => Math.Max(0, TotalTodayMedications - PendingMedicationsCount);
    public int ProgressPercent => TotalTodayMedications > 0 ? (int)Math.Round((double)TakenMedicationsCount / TotalTodayMedications * 100) : 0;

    public List<MemberVitalSummaryDto> MembersSummary { get; set; } = new();
    public List<MedicationScheduleDto> TodayMedicationSchedules { get; set; } = new();
    public List<UpcomingAppointmentDto> UpcomingAppointments { get; set; } = new();
}
