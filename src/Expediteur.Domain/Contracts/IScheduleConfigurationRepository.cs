using Expediteur.Domain.Models;

namespace Expediteur.Domain.Contracts;

public interface IScheduleConfigurationRepository
{
    Task<ScheduleConfiguration> ObtenirConfigurationAsync(CancellationToken cancellationToken = default);
    Task MettreAJourAsync(int intervalleHeures, bool estActif, DateTimeOffset prochaineExecution, CancellationToken cancellationToken = default);
}
