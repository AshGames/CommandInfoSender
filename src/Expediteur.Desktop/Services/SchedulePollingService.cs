using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Expediteur.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Expediteur.Desktop.Services;

public sealed class SchedulePollingService : BackgroundService
{
    private readonly IScheduleConfigurationRepository _scheduleRepository;
    private readonly ICommandAcknowledger _acknowledger;
    private readonly IClock _clock;
    private readonly ILogger<SchedulePollingService> _logger;

    public SchedulePollingService(
        IScheduleConfigurationRepository scheduleRepository,
        ICommandAcknowledger acknowledger,
        IClock clock,
        ILogger<SchedulePollingService> logger)
    {
        _scheduleRepository = scheduleRepository;
        _acknowledger = acknowledger;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service de planification démarré.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var configuration = await _scheduleRepository.ObtenirConfigurationAsync(stoppingToken).ConfigureAwait(false);
                var now = _clock.Maintenant();

                if (configuration.EstActif && configuration.ProchaineExecution <= now.AddSeconds(5))
                {
                    _logger.LogInformation("Déclenchement automatique des accusés de commande.");
                    try
                    {
                        await _acknowledger.TraiterAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Échec du traitement automatique des accusés.");
                    }
                }

                var attente = CalculateDelay(configuration, now);
                await Task.Delay(attente, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Interruption normale lors de l'arrêt de l'application.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue dans le service de planification.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Service de planification arrêté.");
    }

    private static TimeSpan CalculateDelay(ScheduleConfiguration configuration, DateTimeOffset now)
    {
        if (!configuration.EstActif)
        {
            return TimeSpan.FromMinutes(5);
        }

        var delta = configuration.ProchaineExecution - now;
        if (delta < TimeSpan.FromSeconds(30))
        {
            return TimeSpan.FromSeconds(30);
        }

        if (delta > TimeSpan.FromMinutes(10))
        {
            return TimeSpan.FromMinutes(10);
        }

        return delta;
    }
}
