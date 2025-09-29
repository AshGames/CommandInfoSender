using Expediteur.Domain.Models;

namespace Expediteur.Domain.Contracts;

public interface IPdfRenderer
{
    Task<byte[]> CreerAccuseAsync(OrderAcknowledgement acknowledgement, CancellationToken cancellationToken = default);
}
