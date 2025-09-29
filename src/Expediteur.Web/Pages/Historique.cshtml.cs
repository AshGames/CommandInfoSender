using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Expediteur.Web.Pages;

public class HistoriqueModel : PageModel
{
    private readonly IJobHistoryRepository _historyRepository;

    public IReadOnlyCollection<JobHistoryEntry> Historique { get; private set; } = Array.Empty<JobHistoryEntry>();

    public HistoriqueModel(IJobHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task OnGetAsync()
    {
        Historique = await _historyRepository.ObtenirHistoriqueAsync(100).ConfigureAwait(false);
    }
}
