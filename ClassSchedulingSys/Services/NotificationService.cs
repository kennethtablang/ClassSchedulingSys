using ClassSchedulingSys.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ClassSchedulingSys.Services
{
    // Reuse these record types (or keep your previous ones)
    public record ScheduleNotificationInfo(int ScheduleId, string SubjectCode, string SubjectTitle, string SectionLabel, string Day, string StartTime, string EndTime, string Room, string CourseCode, string SemesterLabel);
    public record ScheduleChangeInfo(ScheduleNotificationInfo OldInfo, ScheduleNotificationInfo NewInfo);

    public class NotificationService : INotificationService
    {
        private readonly IBackgroundEmailQueue _queue;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IBackgroundEmailQueue queue, ILogger<NotificationService> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public Task NotifyScheduleAssignedAsync(string facultyEmail, string facultyName, ScheduleNotificationInfo info)
        {
            if (string.IsNullOrWhiteSpace(facultyEmail))
            {
                _logger.LogWarning("NotifyScheduleAssignedAsync called but facultyEmail is empty for ScheduleId {ScheduleId}", info.ScheduleId);
                return Task.CompletedTask;
            }

            var subject = $"New Schedule Assigned: {info.SubjectCode} — {info.SubjectTitle}";
            var body = BuildAssignmentBody(facultyName ?? "Faculty", info);
            EnqueueEmail(facultyEmail, subject, body);
            return Task.CompletedTask;
        }

        public Task NotifyScheduleChangedAsync(string facultyEmail, string facultyName, ScheduleChangeInfo info)
        {
            if (string.IsNullOrWhiteSpace(facultyEmail))
            {
                _logger.LogWarning("NotifyScheduleChangedAsync called but facultyEmail is empty for ScheduleId {ScheduleId}", info.NewInfo.ScheduleId);
                return Task.CompletedTask;
            }

            var subject = $"Schedule Updated: {info.NewInfo.SubjectCode} — {info.NewInfo.SubjectTitle}";
            var body = BuildChangeBody(facultyName ?? "Faculty", info);
            EnqueueEmail(facultyEmail, subject, body);
            return Task.CompletedTask;
        }

        public Task NotifyScheduleDeletedAsync(string facultyEmail, string facultyName, ScheduleNotificationInfo info)
        {
            if (string.IsNullOrWhiteSpace(facultyEmail))
            {
                _logger.LogWarning("NotifyScheduleDeletedAsync called but facultyEmail is empty for ScheduleId {ScheduleId}", info.ScheduleId);
                return Task.CompletedTask;
            }

            var subject = $"Schedule Removed: {info.SubjectCode} — {info.SubjectTitle}";
            var body = BuildDeletionBody(facultyName ?? "Faculty", info);
            EnqueueEmail(facultyEmail, subject, body);
            return Task.CompletedTask;
        }

        // NEW: centralized schedule update notifier
        public async Task NotifyScheduleUpdatedAsync(
            ScheduleNotificationInfo oldInfo,
            ScheduleNotificationInfo newInfo,
            string? oldFacultyEmail,
            string? oldFacultyName,
            string? newFacultyEmail,
            string? newFacultyName)
        {
            try
            {
                // Case 1: same faculty (both emails non-null and equal) -> send a "changed" email
                if (!string.IsNullOrWhiteSpace(oldFacultyEmail) &&
                    !string.IsNullOrWhiteSpace(newFacultyEmail) &&
                    string.Equals(oldFacultyEmail.Trim(), newFacultyEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var change = new ScheduleChangeInfo(oldInfo, newInfo);
                    await NotifyScheduleChangedAsync(newFacultyEmail!, newFacultyName ?? "Faculty", change);
                    return;
                }

                // Case 2: faculty changed -> notify old faculty about removal, new faculty about assignment
                if (!string.IsNullOrWhiteSpace(oldFacultyEmail))
                {
                    // send deleted message to old faculty (using oldInfo)
                    await NotifyScheduleDeletedAsync(oldFacultyEmail!, oldFacultyName ?? "Faculty", oldInfo);
                }

                if (!string.IsNullOrWhiteSpace(newFacultyEmail))
                {
                    // send assignment message to new faculty (using newInfo)
                    await NotifyScheduleAssignedAsync(newFacultyEmail!, newFacultyName ?? "Faculty", newInfo);
                }
            }
            catch (Exception ex)
            {
                // never let exceptions bubble up to controller — log them for debugging
                _logger.LogError(ex, "Failed to process NotifyScheduleUpdatedAsync for schedule {ScheduleId}", newInfo?.ScheduleId ?? -1);
            }
        }

        #region template builders (same as before with minimal HTML)
        private void EnqueueEmail(string to, string subject, string htmlBody)
        {
            try
            {
                _queue.Enqueue(new EmailMessage(to, subject, htmlBody));
                _logger.LogInformation("Enqueued email to {To} subject {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue email to {To}", to);
            }
        }

        private string BuildAssignmentBody(string facultyName, ScheduleNotificationInfo info)
        {
            var sb = new StringBuilder();
            sb.Append($"<p>Dear {HtmlEncode(facultyName)},</p>");
            sb.Append("<p>A new schedule has been <strong>assigned</strong> to you:</p>");
            sb.Append("<ul>");
            sb.AppendFormat("<li><strong>Subject:</strong> {0} - {1}</li>", HtmlEncode(info.SubjectCode), HtmlEncode(info.SubjectTitle));
            sb.AppendFormat("<li><strong>Section:</strong> {0}</li>", HtmlEncode(info.SectionLabel));
            sb.AppendFormat("<li><strong>Day:</strong> {0}</li>", HtmlEncode(info.Day));
            sb.AppendFormat("<li><strong>Time:</strong> {0} - {1}</li>", HtmlEncode(info.StartTime), HtmlEncode(info.EndTime));
            sb.AppendFormat("<li><strong>Room:</strong> {0}</li>", HtmlEncode(info.Room));
            sb.AppendFormat("<li><strong>Course:</strong> {0}</li>", HtmlEncode(info.CourseCode));
            sb.AppendFormat("<li><strong>Semester:</strong> {0}</li>", HtmlEncode(info.SemesterLabel));
            sb.Append("</ul>");
            sb.Append("<p>Please check your schedule in the system and contact your department if there are issues.</p>");
            sb.Append("<p>— PCNL ClassSchedulingSys</p>");
            return sb.ToString();
        }

        private string BuildChangeBody(string facultyName, ScheduleChangeInfo change)
        {
            var sb = new StringBuilder();
            sb.Append($"<p>Dear {HtmlEncode(facultyName)},</p>");
            sb.Append("<p>Your schedule has been <strong>updated</strong>. Below are the changes:</p>");
            sb.Append("<h4>Previous</h4><ul>");
            sb.AppendFormat("<li>{0} — {1} ({2} {3}-{4}) in {5}</li>",
                HtmlEncode(change.OldInfo.SubjectCode),
                HtmlEncode(change.OldInfo.SubjectTitle),
                HtmlEncode(change.OldInfo.Day),
                HtmlEncode(change.OldInfo.StartTime),
                HtmlEncode(change.OldInfo.EndTime),
                HtmlEncode(change.OldInfo.Room));
            sb.Append("</ul>");
            sb.Append("<h4>Updated</h4><ul>");
            sb.AppendFormat("<li>{0} — {1} ({2} {3}-{4}) in {5}</li>",
                HtmlEncode(change.NewInfo.SubjectCode),
                HtmlEncode(change.NewInfo.SubjectTitle),
                HtmlEncode(change.NewInfo.Day),
                HtmlEncode(change.NewInfo.StartTime),
                HtmlEncode(change.NewInfo.EndTime),
                HtmlEncode(change.NewInfo.Room));
            sb.Append("</ul>");
            sb.Append("<p>If this change is incorrect, please contact your department or the scheduling administrator.</p>");
            sb.Append("<p>— PCNL ClassSchedulingSys</p>");
            return sb.ToString();
        }

        private string BuildDeletionBody(string facultyName, ScheduleNotificationInfo info)
        {
            var sb = new StringBuilder();
            sb.Append($"<p>Dear {HtmlEncode(facultyName)},</p>");
            sb.Append("<p>The following schedule has been <strong>removed</strong> from your assignments:</p>");
            sb.Append("<ul>");
            sb.AppendFormat("<li>{0} — {1} ({2} {3}-{4}) in {5} — Section: {6}</li>",
                HtmlEncode(info.SubjectCode),
                HtmlEncode(info.SubjectTitle),
                HtmlEncode(info.Day),
                HtmlEncode(info.StartTime),
                HtmlEncode(info.EndTime),
                HtmlEncode(info.Room),
                HtmlEncode(info.SectionLabel));
            sb.Append("</ul>");
            sb.Append("<p>If you have questions, please contact your department.</p>");
            sb.Append("<p>— PCNL ClassSchedulingSys</p>");
            return sb.ToString();
        }

        private static string HtmlEncode(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
        #endregion
    }
}
