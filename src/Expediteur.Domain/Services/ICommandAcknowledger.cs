using Expediteur.Domain.Models;

namespace Expediteur.Domain.Services;

public interface ICommandAcknowledger
{
    Task<CommandAcknowledgerResult> TraiterAsync(CancellationToken cancellationToken = default);
}

public sealed record CommandAcknowledgerResult
{
    public required int NombreDocumentsEnvoyes { get; init; }
    public required IReadOnlyCollection<JobHistoryEntry> HistoriqueCree { get; init; }
}
