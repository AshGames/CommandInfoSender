using Expediteur.Domain.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Expediteur.Web.Background;

public sealed class OrderAcknowledgementJob
{
    private readonly ICommandAcknowledger _acknowledger;
    private readonly ILogger<OrderAcknowledgementJob> _logger;

    public OrderAcknowledgementJob(ICommandAcknowledger acknowledger, ILogger<OrderAcknowledgementJob> logger)
    {
        _acknowledger = acknowledger;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Exécution du job d'accusés de commande déclenchée par Hangfire.");
        await _acknowledger.TraiterAsync().ConfigureAwait(false);
    }
}
