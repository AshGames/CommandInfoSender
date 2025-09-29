namespace Expediteur.Domain.Models;

public sealed record JobHistoryEntry
{
    public required Guid Id { get; init; }
    public required DateTimeOffset DateExecution { get; init; }
    public required bool EstReussie { get; init; }
    public required string Message { get; init; }
    public string? NumeroCommande { get; init; }
    public string? Destinataire { get; init; }
}
