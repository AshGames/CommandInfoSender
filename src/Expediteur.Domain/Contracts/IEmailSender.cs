using Expediteur.Domain.Models;

namespace Expediteur.Domain.Contracts;

public interface IEmailSender
{
    Task EnvoyerAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
