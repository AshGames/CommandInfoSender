namespace Expediteur.Domain.Contracts;

public interface IClock
{
    DateTimeOffset Maintenant();
}
