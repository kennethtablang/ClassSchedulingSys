using ClassSchedulingSys.Interfaces;

namespace ClassSchedulingSys.Services
{
    /// <summary>
    /// Hosted background service that consumes the IBackgroundEmailQueue and sends emails via IEmailService.
    /// Keeps HTTP responses snappy by moving actual SMTP work to a background worker.
    /// </summary>
    public class BackgroundEmailSender : BackgroundService
    {
        private readonly IBackgroundEmailQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundEmailSender> _logger;

        public BackgroundEmailSender(
            IBackgroundEmailQueue queue,
            IServiceProvider serviceProvider,
            ILogger<BackgroundEmailSender> logger)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundEmailSender started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queue.DequeueAsync(stoppingToken);
                    if (message == null) continue;

                    // Create a scope for resolving scoped services like IEmailService and ILogger<T>
                    using var scope = _serviceProvider.CreateScope();
                    var scopedEmailSvc = scope.ServiceProvider.GetService<IEmailService>();
                    if (scopedEmailSvc == null)
                    {
                        _logger.LogError("IEmailService not registered. Cannot send email to {To}", message.To);
                        continue; // drop or persist, depending on your policy
                    }

                    try
                    {
                        await scopedEmailSvc.SendEmailAsync(message.To, message.Subject, message.HtmlBody, stoppingToken);
                        _logger.LogInformation("Email sent to {To}", message.To);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send email to {To}. Retrying once.", message.To);

                        // one retry
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

                            // resolve again in fresh scope to be safe (optional)
                            using var retryScope = _serviceProvider.CreateScope();
                            var retryEmailSvc = retryScope.ServiceProvider.GetService<IEmailService>();
                            if (retryEmailSvc != null)
                            {
                                await retryEmailSvc.SendEmailAsync(message.To, message.Subject, message.HtmlBody, stoppingToken);
                                _logger.LogInformation("Email sent to {To} on retry", message.To);
                            }
                            else
                            {
                                _logger.LogError("IEmailService not available on retry for {To}", message.To);
                            }
                        }
                        catch (Exception rex)
                        {
                            _logger.LogError(rex, "Retry failed for email to {To}. Consider persisting this message for later retry.", message.To);
                            // optionally persist failed message to DB for manual retry/inspection
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BackgroundEmailSender unexpected error; pausing briefly.");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogInformation("BackgroundEmailSender stopping.");
        }
    }
}
