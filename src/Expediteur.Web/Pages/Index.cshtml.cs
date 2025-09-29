using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Expediteur.Web.Background;
using Expediteur.Web.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Expediteur.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IJobHistoryRepository _historyRepository;
    private readonly IScheduleConfigurationRepository _scheduleRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ScheduleInitializer _scheduleInitializer;
    private readonly ILogger<IndexModel> _logger;

    public IReadOnlyCollection<JobHistoryEntry> DernieresExecutions { get; private set; } = Array.Empty<JobHistoryEntry>();
    public ScheduleConfiguration? Configuration { get; private set; }

    [TempData]
    public string? Notification { get; set; }

    public IndexModel(
        IJobHistoryRepository historyRepository,
        IScheduleConfigurationRepository scheduleRepository,
        IBackgroundJobClient backgroundJobClient,
        ScheduleInitializer scheduleInitializer,
        ILogger<IndexModel> logger)
    {
        _historyRepository = historyRepository;
        _scheduleRepository = scheduleRepository;
        _backgroundJobClient = backgroundJobClient;
        _scheduleInitializer = scheduleInitializer;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        Configuration = await _scheduleRepository.ObtenirConfigurationAsync().ConfigureAwait(false);
        DernieresExecutions = await _historyRepository.ObtenirHistoriqueAsync(10).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostDeclencherAsync()
    {
        _backgroundJobClient.Enqueue<OrderAcknowledgementJob>(job => job.ExecuteAsync());
        Notification = "Le traitement a été déclenché manuellement.";
        _logger.LogInformation("Déclenchement manuel du job enregistré.");
        await _scheduleInitializer.EnsureRecurringJobAsync().ConfigureAwait(false);
        return RedirectToPage();
    }
}
