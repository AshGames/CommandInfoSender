using Expediteur.Domain.Models;

namespace Expediteur.Domain.Contracts;

public interface IJobHistoryRepository
{
    Task EnregistrerAsync(JobHistoryEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<JobHistoryEntry>> ObtenirHistoriqueAsync(int derniereOccurrences, CancellationToken cancellationToken = default);
}
