using System.Collections.Concurrent;
using ClassSchedulingSys.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassSchedulingSys.Services
{
    /// <summary>
    /// Thread-safe in-memory queue for email messages.
    /// Not persistent — if the app restarts queued messages are lost.
    /// For production, consider a persistent queue (DB, Redis, Azure Queue).
    /// </summary>
    public class BackgroundEmailQueue : IBackgroundEmailQueue
    {
        private readonly ConcurrentQueue<EmailMessage> _items = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void Enqueue(EmailMessage message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            _items.Enqueue(message);
            _signal.Release();
        }

        public async Task<EmailMessage?> DequeueAsync(CancellationToken cancellationToken)
        {
            // Wait until a message is available or cancellation requested
            await _signal.WaitAsync(cancellationToken);
            if (_items.TryDequeue(out var item))
                return item;
            return null;
        }
    }

}
