using Expediteur.Domain.Contracts;
using Expediteur.Web.Background;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Expediteur.Web.Services;

public sealed class ScheduleInitializer
{
    private readonly IScheduleConfigurationRepository _scheduleRepository;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<ScheduleInitializer> _logger;

    public ScheduleInitializer(
        IScheduleConfigurationRepository scheduleRepository,
        IRecurringJobManager recurringJobManager,
        ILogger<ScheduleInitializer> logger)
    {
        _scheduleRepository = scheduleRepository;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public async Task EnsureRecurringJobAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await _scheduleRepository.ObtenirConfigurationAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Configuration de planification chargée : intervalle {Interval}h, actif = {Active}", configuration.IntervalleHeures, configuration.EstActif);

        var interval = Math.Max(1, configuration.IntervalleHeures);
        if (configuration.EstActif)
        {
            var cron = Cron.HourInterval(interval);
            _recurringJobManager.AddOrUpdate<OrderAcknowledgementJob>(
                HangfireJobIds.CommandAcknowledgement,
                job => job.ExecuteAsync(),
                cron,
                TimeZoneInfo.Utc);
            _logger.LogInformation("Job récurrent configuré avec un intervalle de {Interval} heure(s).", interval);
        }
        else
        {
            _recurringJobManager.RemoveIfExists(HangfireJobIds.CommandAcknowledgement);
            _logger.LogWarning("Job récurrent désactivé.");
        }

        var prochaineExecution = configuration.ProchaineExecution > DateTimeOffset.UtcNow
            ? configuration.ProchaineExecution
            : DateTimeOffset.UtcNow.AddHours(interval);

        await _scheduleRepository.MettreAJourAsync(interval, configuration.EstActif, prochaineExecution, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Prochaine exécution enregistrée pour {NextExecution}", prochaineExecution);
    }
}
