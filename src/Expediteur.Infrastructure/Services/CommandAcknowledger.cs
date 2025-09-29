using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Expediteur.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Expediteur.Infrastructure.Services;

public sealed class CommandAcknowledger : ICommandAcknowledger
{
    private readonly IOrderAcknowledgementRepository _orderRepository;
    private readonly IPdfRenderer _pdfRenderer;
    private readonly IEmailSender _emailSender;
    private readonly IJobHistoryRepository _historyRepository;
    private readonly IScheduleConfigurationRepository _scheduleRepository;
    private readonly IClock _clock;
    private readonly ILogger<CommandAcknowledger> _logger;
    private readonly IConfiguration _configuration;

    public CommandAcknowledger(
        IOrderAcknowledgementRepository orderRepository,
        IPdfRenderer pdfRenderer,
        IEmailSender emailSender,
        IJobHistoryRepository historyRepository,
        IScheduleConfigurationRepository scheduleRepository,
        IClock clock,
        ILogger<CommandAcknowledger> logger,
        IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _pdfRenderer = pdfRenderer;
        _emailSender = emailSender;
        _historyRepository = historyRepository;
        _scheduleRepository = scheduleRepository;
        _clock = clock;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<CommandAcknowledgerResult> TraiterAsync(CancellationToken cancellationToken = default)
    {
        var executionTime = _clock.Maintenant();
        _logger.LogInformation("Démarrage du traitement d'accusés de commande à {ExecutionTime}", executionTime);

        var commandes = await _orderRepository.ObtenirAccusesAsync(executionTime, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("{Count} commandes à traiter", commandes.Count);

        var historique = new List<JobHistoryEntry>();
        var nombreSucces = 0;

        foreach (var commande in commandes)
        {
            var entry = new JobHistoryEntry
            {
                Id = Guid.NewGuid(),
                DateExecution = executionTime,
                EstReussie = false,
                Message = string.Empty,
                NumeroCommande = commande.NumeroCommande,
                Destinataire = commande.EmailDestinataire
            };

            try
            {
                var pdfBytes = await _pdfRenderer.CreerAccuseAsync(commande, cancellationToken).ConfigureAwait(false);

                var sujet = BuildSubject(commande);
                var corps = BuildBodyHtml(commande);
                var message = new EmailMessage
                {
                    Destinataire = commande.EmailDestinataire,
                    Sujet = sujet,
                    CorpsHtml = corps,
                    PieceJointe = pdfBytes,
                    NomPieceJointe = $"Accuse_{commande.NumeroCommande}.pdf"
                };

                await _emailSender.EnvoyerAsync(message, cancellationToken).ConfigureAwait(false);

                entry = entry with
                {
                    EstReussie = true,
                    Message = "Envoi réussi"
                };
                nombreSucces++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'accusé pour la commande {NumeroCommande}", commande.NumeroCommande);
                entry = entry with
                {
                    EstReussie = false,
                    Message = ex.Message
                };
            }

            await _historyRepository.EnregistrerAsync(entry, cancellationToken).ConfigureAwait(false);
            historique.Add(entry);
        }

        await UpdateNextExecutionAsync(nombreSucces, executionTime, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Traitement terminé : {Succes}/{Total} envoyés", nombreSucces, commandes.Count);
        return new CommandAcknowledgerResult
        {
            NombreDocumentsEnvoyes = nombreSucces,
            HistoriqueCree = historique
        };
    }

    private string BuildSubject(OrderAcknowledgement commande)
    {
        var template = _configuration.GetValue<string>("Email:Sujet") ?? "Accusé de commande {0}";
        return string.Format(CultureInfo.GetCultureInfo("fr-FR"), template, commande.NumeroCommande);
    }

    private string BuildBodyHtml(OrderAcknowledgement commande)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html lang=\"fr\"><body style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2933;\">");
        sb.AppendLine("<h2 style=\"color:#0f766e;\">Expéditeur d'accusé de commande</h2>");
        sb.AppendLine($"<p>Bonjour,</p>");
        sb.AppendLine($"<p>Veuillez trouver ci-joint l'accusé de réception de la commande <strong>{commande.NumeroCommande}</strong> datée du {commande.DateCommande:dd MMMM yyyy}.</p>");
        sb.AppendLine("<p>Résumé des lignes :</p>");
        sb.AppendLine("<table style=\"border-collapse:collapse;width:100%;\"><thead><tr><th style=\"border-bottom:1px solid #d9e0e6;text-align:left;padding:4px;\">Article</th><th style=\"border-bottom:1px solid #d9e0e6;text-align:right;padding:4px;\">Quantité</th><th style=\"border-bottom:1px solid #d9e0e6;text-align:right;padding:4px;\">Montant</th></tr></thead><tbody>");
        foreach (var ligne in commande.Lignes)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td style=\"padding:4px;border-bottom:1px solid #f1f5f9;\">{ligne.Description}</td>");
            sb.AppendLine($"<td style=\"padding:4px;border-bottom:1px solid #f1f5f9;text-align:right;\">{ligne.Quantite:N2}</td>");
            sb.AppendLine($"<td style=\"padding:4px;border-bottom:1px solid #f1f5f9;text-align:right;\">{ligne.MontantTtc:C2}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");
        sb.AppendLine($"<p style=\"margin-top:12px;\"><strong>Total TTC :</strong> {commande.TotalTtc:C2}</p>");
        sb.AppendLine("<p style=\"margin-top:20px;color:#64748b;\">Ce message a été envoyé automatiquement, merci de ne pas y répondre.</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private async Task UpdateNextExecutionAsync(int successCount, DateTimeOffset executionTime, CancellationToken cancellationToken)
    {
        var config = await _scheduleRepository.ObtenirConfigurationAsync(cancellationToken).ConfigureAwait(false);
        if (!config.EstActif)
        {
            _logger.LogInformation("Planification inactive, prochaine exécution non recalculée.");
            return;
        }

        var interval = Math.Max(1, config.IntervalleHeures);
        var prochaineExecution = executionTime.AddHours(interval);
        await _scheduleRepository.MettreAJourAsync(interval, config.EstActif, prochaineExecution, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Prochaine exécution recalculée pour {Execution}", prochaineExecution);
    }
}
