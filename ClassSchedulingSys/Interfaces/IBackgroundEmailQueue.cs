using System.Net.Mail;


namespace ClassSchedulingSys.Interfaces
{
    // Simple DTO for queued emails; you can move to Services if you prefer
    public record EmailMessage(string To, string Subject, string HtmlBody);

    public interface IBackgroundEmailQueue
    {
        /// <summary>
        /// Enqueue an email message for background sending.
        /// Should be fire-and-forget from controllers/services.
        /// </summary>
        void Enqueue(EmailMessage message);

        /// <summary>
        /// Dequeue the next message. Blocks until an item is available or cancellation requested.
        /// </summary>
        Task<EmailMessage?> DequeueAsync(CancellationToken cancellationToken);
    }
}
