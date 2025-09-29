using Dapper;
using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Expediteur.Infrastructure.Data;

namespace Expediteur.Infrastructure.Scheduling;

public sealed class SqlScheduleConfigurationRepository : IScheduleConfigurationRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlScheduleConfigurationRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ScheduleConfiguration> ObtenirConfigurationAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    const string sql = "SELECT TOP 1 IntervalHours, NextExecutionUtc, IsActive FROM Expediteur.ScheduleConfiguration";
        var row = await connection.QueryFirstOrDefaultAsync<ScheduleRow>(sql).ConfigureAwait(false);
        if (row is null)
        {
            return new ScheduleConfiguration
            {
                IntervalleHeures = 4,
                ProchaineExecution = DateTimeOffset.UtcNow.AddHours(4),
                EstActif = true
            };
        }

        var nextExecutionUtc = DateTime.SpecifyKind(row.NextExecutionUtc, DateTimeKind.Utc);
        return new ScheduleConfiguration
        {
            IntervalleHeures = row.IntervalHours,
            ProchaineExecution = new DateTimeOffset(nextExecutionUtc, TimeSpan.Zero),
            EstActif = row.IsActive
        };
    }

    public async Task MettreAJourAsync(int intervalleHeures, bool estActif, DateTimeOffset prochaineExecution, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    const string upsertSql = "MERGE Expediteur.ScheduleConfiguration AS cible USING (SELECT @Id AS Id) AS source ON cible.Id = source.Id WHEN MATCHED THEN UPDATE SET IntervalHours = @Intervalle, IsActive = @Actif, NextExecutionUtc = @Next WHEN NOT MATCHED THEN INSERT (Id, IntervalHours, NextExecutionUtc, IsActive) VALUES (@Id, @Intervalle, @Next, @Actif);";
        await connection.ExecuteAsync(upsertSql, new
        {
            Id = 1,
            Intervalle = intervalleHeures,
            Actif = estActif,
            Next = prochaineExecution.UtcDateTime
        }).ConfigureAwait(false);
    }

    private sealed record ScheduleRow
    {
        public int IntervalHours { get; init; }
        public DateTime NextExecutionUtc { get; init; }
        public bool IsActive { get; init; }
    }
}
