namespace Expediteur.Domain.Models;

public sealed record OrderAcknowledgement
{
    public required string NumeroCommande { get; init; }
    public required string Client { get; init; }
    public required string EmailDestinataire { get; init; }
    public required DateTime DateCommande { get; init; }
    public required IReadOnlyCollection<OrderAcknowledgementLigne> Lignes { get; init; }
    public decimal TotalTtc => Lignes.Sum(l => l.MontantTtc);
}

public sealed record OrderAcknowledgementLigne
{
    public required string ReferenceProduit { get; init; }
    public required string Description { get; init; }
    public required decimal Quantite { get; init; }
    public required decimal PrixUnitaire { get; init; }
    public decimal MontantTtc => Quantite * PrixUnitaire;
}
