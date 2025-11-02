using ClassSchedulingSys.Services;

namespace ClassSchedulingSys.Interfaces
{
    public interface INotificationService
    {
        Task NotifyScheduleAssignedAsync(string facultyEmail, string facultyName, ScheduleNotificationInfo info);
        Task NotifyScheduleChangedAsync(string facultyEmail, string facultyName, ScheduleChangeInfo info);
        Task NotifyScheduleDeletedAsync(string facultyEmail, string facultyName, ScheduleNotificationInfo info);

        // NEW: single method to be called when a schedule is updated.
        // The service itself will decide whether to notify old/new faculty or send an update.
        Task NotifyScheduleUpdatedAsync(ScheduleNotificationInfo oldInfo, ScheduleNotificationInfo newInfo, string? oldFacultyEmail, string? oldFacultyName, string? newFacultyEmail, string? newFacultyName);
    }
}
