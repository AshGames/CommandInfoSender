using System.IO;
using System.Net.Mail;
using System.Text;
using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Expediteur.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnvoyerAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var host = _configuration.GetValue<string>("Email:Smtp:Host") ?? "saintelucie1885-fr.mail.protection.outlook.com";
        var port = _configuration.GetValue<int?>("Email:Smtp:Port") ?? 25;
        var from = _configuration.GetValue<string>("Email:Expediteur") ?? "no-reply@saintelucie1885.fr";

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Email:Smtp:EnableSsl", true)
        };

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(from),
            Subject = message.Sujet,
            BodyEncoding = Encoding.UTF8,
            Body = message.CorpsHtml,
            IsBodyHtml = true
        };

        mailMessage.To.Add(new MailAddress(message.Destinataire));

        if (message.PieceJointe is not null && !string.IsNullOrWhiteSpace(message.NomPieceJointe))
        {
            mailMessage.Attachments.Add(new Attachment(new MemoryStream(message.PieceJointe), message.NomPieceJointe));
        }

        _logger.LogInformation("Envoi d'un email à {Recipient} via {Host}:{Port}", message.Destinataire, host, port);
        await smtp.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);
    }
}
