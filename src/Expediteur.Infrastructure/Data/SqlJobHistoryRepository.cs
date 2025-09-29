using Dapper;
using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;

namespace Expediteur.Infrastructure.Data;

public sealed class SqlJobHistoryRepository : IJobHistoryRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlJobHistoryRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task EnregistrerAsync(JobHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    const string sql = "INSERT INTO Expediteur.JobHistory (Id, ExecutionDate, Succeeded, Message, OrderNumber, Recipient) VALUES (@Id, @DateExecution, @EstReussie, @Message, @NumeroCommande, @Destinataire);";
        await connection.ExecuteAsync(sql, new
        {
            entry.Id,
            DateExecution = entry.DateExecution.UtcDateTime,
            entry.EstReussie,
            entry.Message,
            entry.NumeroCommande,
            entry.Destinataire
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<JobHistoryEntry>> ObtenirHistoriqueAsync(int derniereOccurrences, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    const string sql = "SELECT TOP (@Take) Id, ExecutionDate, Succeeded, Message, OrderNumber, Recipient FROM Expediteur.JobHistory ORDER BY ExecutionDate DESC";
        var rows = await connection.QueryAsync<JobHistoryRow>(sql, new { Take = derniereOccurrences }).ConfigureAwait(false);

        return rows.Select(row => new JobHistoryEntry
        {
            Id = row.Id,
            DateExecution = DateTime.SpecifyKind(row.ExecutionDate, DateTimeKind.Utc),
            EstReussie = row.Succeeded,
            Message = row.Message,
            NumeroCommande = row.OrderNumber,
            Destinataire = row.Recipient
        }).ToList();
    }

    private sealed record JobHistoryRow
    {
        public Guid Id { get; init; }
        public DateTime ExecutionDate { get; init; }
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? OrderNumber { get; init; }
        public string? Recipient { get; init; }
    }
}
