using Dapper;
using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;

namespace Expediteur.Infrastructure.Data;

public sealed class SqlOrderAcknowledgementRepository : IOrderAcknowledgementRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlOrderAcknowledgementRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<OrderAcknowledgement>> ObtenirAccusesAsync(DateTimeOffset executionDate, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var lignes = await connection.QueryAsync<OrderRow>(
            sql: "dbo.ObtenirAccusesCommande",
            param: new { DateExecution = executionDate.UtcDateTime },
            commandType: System.Data.CommandType.StoredProcedure
        ).ConfigureAwait(false);

        var commandes = lignes
            .GroupBy(ligne => new { ligne.NumeroCommande, ligne.Client, ligne.DateCommande, ligne.EmailDestinataire })
            .Select(groupe => new OrderAcknowledgement
            {
                NumeroCommande = groupe.Key.NumeroCommande,
                Client = groupe.Key.Client,
                EmailDestinataire = groupe.Key.EmailDestinataire,
                DateCommande = groupe.Key.DateCommande,
                Lignes = groupe.Select(ligne => new OrderAcknowledgementLigne
                {
                    ReferenceProduit = ligne.ReferenceProduit,
                    Description = ligne.Description,
                    Quantite = ligne.Quantite,
                    PrixUnitaire = ligne.PrixUnitaire
                }).ToList()
            })
            .ToList();

        return commandes;
    }

    private sealed record OrderRow
    {
        public required string NumeroCommande { get; init; }
        public required string Client { get; init; }
        public required DateTime DateCommande { get; init; }
    public required string EmailDestinataire { get; init; }
        public required string ReferenceProduit { get; init; }
        public required string Description { get; init; }
        public required decimal Quantite { get; init; }
        public required decimal PrixUnitaire { get; init; }
    }
}
