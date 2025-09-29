using System.ComponentModel.DataAnnotations;
using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Expediteur.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Expediteur.Web.Pages;

public class ConfigurationModel : PageModel
{
    private readonly IScheduleConfigurationRepository _scheduleRepository;
    private readonly ScheduleInitializer _scheduleInitializer;

    public ScheduleConfiguration? ConfigurationActuelle { get; private set; }

    [BindProperty]
    public ConfigurationInput Input { get; set; } = new();

    [TempData]
    public string? Notification { get; set; }

    public ConfigurationModel(IScheduleConfigurationRepository scheduleRepository, ScheduleInitializer scheduleInitializer)
    {
        _scheduleRepository = scheduleRepository;
        _scheduleInitializer = scheduleInitializer;
    }

    public async Task OnGetAsync()
    {
        ConfigurationActuelle = await _scheduleRepository.ObtenirConfigurationAsync().ConfigureAwait(false);
        Input.IntervalleHeures = ConfigurationActuelle.IntervalleHeures;
        Input.EstActif = ConfigurationActuelle.EstActif;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ConfigurationActuelle = await _scheduleRepository.ObtenirConfigurationAsync().ConfigureAwait(false);
            return Page();
        }

        var prochaineExecution = DateTimeOffset.UtcNow.AddHours(Math.Max(1, Input.IntervalleHeures));
        await _scheduleRepository.MettreAJourAsync(Input.IntervalleHeures, Input.EstActif, prochaineExecution).ConfigureAwait(false);
        await _scheduleInitializer.EnsureRecurringJobAsync().ConfigureAwait(false);

        Notification = "La configuration a été mise à jour.";
        return RedirectToPage();
    }

    public sealed class ConfigurationInput
    {
    [Display(Name = "Intervalle en heures"), Range(1, 24, ErrorMessage = "L'intervalle doit être compris entre 1 et 24 heures.")]
        public int IntervalleHeures { get; set; } = 4;

        [Display(Name = "Planification active")]
        public bool EstActif { get; set; } = true;
    }
}
