namespace Expediteur.Domain.Models;

public sealed record EmailMessage
{
    public required string Destinataire { get; init; }
    public required string Sujet { get; init; }
    public required string CorpsHtml { get; init; }
    public byte[]? PieceJointe { get; init; }
    public string? NomPieceJointe { get; init; }
}
