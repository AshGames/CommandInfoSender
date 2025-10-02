using Expediteur.Desktop.Commands;
using Expediteur.Domain.Contracts;
using Expediteur.Domain.Services;
using Expediteur.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Expediteur.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IJobHistoryRepository _historyRepository;
    private readonly IScheduleConfigurationRepository _scheduleRepository;
    private readonly ICommandAcknowledger _acknowledger;
    private readonly IClock _clock;
    private readonly ILogger<MainViewModel> _logger;

    public ObservableCollection<JobHistoryEntryViewModel> Historique { get; } = new();

    private int _intervalleHeures = 4;
    public int IntervalleHeures
    {
        get => _intervalleHeures;
        set => SetProperty(ref _intervalleHeures, value);
    }

    private bool _estActif = true;
    public bool EstActif
    {
        get => _estActif;
        set
        {
            if (SetProperty(ref _estActif, value))
            {
                OnPropertyChanged(nameof(EtatTexte));
            }
        }
    }

    private DateTime _prochaineExecutionLocale;
    public string ProchaineExecutionLocale => _prochaineExecutionLocale == default
        ? "Non planifiée"
        : _prochaineExecutionLocale.ToString("f", CultureInfo.CurrentCulture);

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(EtatTexte));
            }
        }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string EtatTexte => IsBusy ? "Traitement en cours..." : (EstActif ? "Prêt" : "Planification arrêtée");

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand SaveScheduleCommand { get; }
    public AsyncRelayCommand TriggerCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(
        IJobHistoryRepository historyRepository,
        IScheduleConfigurationRepository scheduleRepository,
        ICommandAcknowledger acknowledger,
        IClock clock,
        ILogger<MainViewModel> logger)
    {
        _historyRepository = historyRepository;
        _scheduleRepository = scheduleRepository;
        _acknowledger = acknowledger;
        _clock = clock;
        _logger = logger;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        SaveScheduleCommand = new AsyncRelayCommand(SaveScheduleAsync, CanModify);
        TriggerCommand = new AsyncRelayCommand(TriggerAsync, () => !IsBusy);
    }

    private bool CanModify() => !IsBusy;

    public async Task InitializeAsync()
    {
        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            SetBusy(true);
            StatusMessage = "Chargement en cours...";

            await LoadDataCoreAsync().ConfigureAwait(false);

            StatusMessage = $"Historique mis à jour ({Historique.Count} entrées).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du rafraîchissement des données");
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SaveScheduleAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var interval = Math.Clamp(IntervalleHeures, 1, 24);
            IntervalleHeures = interval;

            var prochaineExecution = _clock.Maintenant().AddHours(interval);
            await _scheduleRepository.MettreAJourAsync(interval, EstActif, prochaineExecution).ConfigureAwait(false);
            await LoadDataCoreAsync().ConfigureAwait(false);

            StatusMessage = "Planification enregistrée.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement de la planification");
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task TriggerAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            SetBusy(true);
            StatusMessage = "Déclenchement en cours...";

            var result = await _acknowledger.TraiterAsync().ConfigureAwait(false);
            await LoadDataCoreAsync().ConfigureAwait(false);

            StatusMessage = $"Traitement terminé : {result.NombreDocumentsEnvoyes} envoi(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du déclenchement manuel");
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ApplyConfiguration(ScheduleConfiguration configuration)
    {
        IntervalleHeures = configuration.IntervalleHeures;
        EstActif = configuration.EstActif;
        _prochaineExecutionLocale = configuration.ProchaineExecution.ToLocalTime().DateTime;
        OnPropertyChanged(nameof(ProchaineExecutionLocale));
        OnPropertyChanged(nameof(EtatTexte));
    }

    private void SetBusy(bool isBusy)
    {
        RunOnUiThread(() =>
        {
            IsBusy = isBusy;
            RefreshCommand.RaiseCanExecuteChanged();
            SaveScheduleCommand.RaiseCanExecuteChanged();
            TriggerCommand.RaiseCanExecuteChanged();
        });
    }

    private async Task LoadDataCoreAsync()
    {
        var configuration = await _scheduleRepository.ObtenirConfigurationAsync().ConfigureAwait(false);
        var historique = await _historyRepository.ObtenirHistoriqueAsync(50).ConfigureAwait(false);

        await RunOnUiThreadAsync(() =>
        {
            ApplyConfiguration(configuration);

            Historique.Clear();
            foreach (var entry in historique.OrderByDescending(x => x.DateExecution))
            {
                Historique.Add(JobHistoryEntryViewModel.FromModel(entry));
            }
        }).ConfigureAwait(false);
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        RunOnUiThread(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, DispatcherPriority.Normal);
    }

    private static Task RunOnUiThreadAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action, DispatcherPriority.Normal).Task;
    }
}
