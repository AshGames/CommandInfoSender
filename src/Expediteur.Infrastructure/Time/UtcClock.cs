using Expediteur.Domain.Contracts;

namespace Expediteur.Infrastructure.Time;

public sealed class UtcClock : IClock
{
    public DateTimeOffset Maintenant() => DateTimeOffset.UtcNow;
}
