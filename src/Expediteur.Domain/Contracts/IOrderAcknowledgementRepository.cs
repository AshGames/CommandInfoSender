using Expediteur.Domain.Models;

namespace Expediteur.Domain.Contracts;

public interface IOrderAcknowledgementRepository
{
    Task<IReadOnlyCollection<OrderAcknowledgement>> ObtenirAccusesAsync(DateTimeOffset executionDate, CancellationToken cancellationToken = default);
}
