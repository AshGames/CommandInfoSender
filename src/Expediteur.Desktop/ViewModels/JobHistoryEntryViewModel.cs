using Expediteur.Domain.Models;
using System;
using System.Globalization;

namespace Expediteur.Desktop.ViewModels;

public sealed record JobHistoryEntryViewModel
{
    public string DateExecutionLocale { get; init; } = string.Empty;
    public string NumeroCommande { get; init; } = string.Empty;
    public string Destinataire { get; init; } = string.Empty;
    public string Resultat { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public static JobHistoryEntryViewModel FromModel(JobHistoryEntry entry)
    {
        var localeDate = entry.DateExecution.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        return new JobHistoryEntryViewModel
        {
            DateExecutionLocale = localeDate,
            NumeroCommande = entry.NumeroCommande ?? "-",
            Destinataire = entry.Destinataire ?? "-",
            Resultat = entry.EstReussie ? "Réussi" : "Échec",
            Message = entry.Message
        };
    }
}
