using System.ComponentModel.DataAnnotations;

namespace Expediteur.Web.Models;

public sealed class ScheduleUpdateRequest
{
    [Range(1, 24)]
    public int IntervalleHeures { get; init; }

    public bool EstActif { get; init; }
}
