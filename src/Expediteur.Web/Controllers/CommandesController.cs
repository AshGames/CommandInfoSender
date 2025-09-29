using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Expediteur.Web.Background;
using Expediteur.Web.Models;
using Expediteur.Web.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Expediteur.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommandesController : ControllerBase
{
    private readonly IJobHistoryRepository _historyRepository;
    private readonly IScheduleConfigurationRepository _scheduleRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ScheduleInitializer _scheduleInitializer;

    public CommandesController(
        IJobHistoryRepository historyRepository,
        IScheduleConfigurationRepository scheduleRepository,
        IBackgroundJobClient backgroundJobClient,
        ScheduleInitializer scheduleInitializer)
    {
        _historyRepository = historyRepository;
        _scheduleRepository = scheduleRepository;
        _backgroundJobClient = backgroundJobClient;
        _scheduleInitializer = scheduleInitializer;
    }

    [HttpGet("historique")]
    public async Task<IReadOnlyCollection<JobHistoryEntry>> ObtenirHistorique([FromQuery] int limite = 20)
    {
        var take = Math.Clamp(limite, 1, 200);
        return await _historyRepository.ObtenirHistoriqueAsync(take).ConfigureAwait(false);
    }

    [HttpGet("configuration")]
    public async Task<ScheduleConfiguration> ObtenirConfiguration()
    {
        return await _scheduleRepository.ObtenirConfigurationAsync().ConfigureAwait(false);
    }

    [HttpPost("declencher")]
    public async Task<IActionResult> DeclencherManuellement()
    {
        _backgroundJobClient.Enqueue<OrderAcknowledgementJob>(job => job.ExecuteAsync());
        await _scheduleInitializer.EnsureRecurringJobAsync().ConfigureAwait(false);
        return Accepted(new { message = "Traitement déclenché" });
    }

    [HttpPut("configuration")]
    public async Task<IActionResult> MettreAJourConfiguration([FromBody] ScheduleUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var prochaineExecution = DateTimeOffset.UtcNow.AddHours(Math.Max(1, request.IntervalleHeures));
        await _scheduleRepository.MettreAJourAsync(request.IntervalleHeures, request.EstActif, prochaineExecution).ConfigureAwait(false);
        await _scheduleInitializer.EnsureRecurringJobAsync().ConfigureAwait(false);
        return Ok(new { message = "Configuration mise à jour" });
    }
}
