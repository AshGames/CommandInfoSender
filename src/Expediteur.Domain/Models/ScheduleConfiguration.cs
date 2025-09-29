namespace Expediteur.Domain.Models;

public sealed record ScheduleConfiguration
{
    public required int IntervalleHeures { get; init; }
    public DateTimeOffset ProchaineExecution { get; init; }
    public bool EstActif { get; init; } = true;
}
