using Expediteur.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Expediteur.Desktop
{
    public partial class App : Application
    {
        private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "Expediteur.log");
        private static readonly object LogLock = new();

        private IHost? _host;

        public App()
        {
            Startup += OnStartupAsync;
            Exit += OnExitAsync;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void EnsureHost()
        {
            if (_host is not null)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);

            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.SetBasePath(AppContext.BaseDirectory);
                    configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
                    configuration.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddInfrastructure(context.Configuration);

                    services.AddLogging(builder =>
                    {
                        builder.AddDebug();
                        builder.AddConsole();
                    });

                    services.AddHostedService<Services.SchedulePollingService>();

                    services.AddSingleton<ViewModels.MainViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        private async void OnStartupAsync(object? sender, StartupEventArgs e)
        {
            LogInfo("Démarrage de l'application WPF");

            try
            {
                EnsureHost();

                if (_host is null)
                {
                    throw new InvalidOperationException("L'hôte de l'application n'a pas pu être initialisé.");
                }

                LogInfo("Hôte initialisé, démarrage en cours...");
                await _host.StartAsync();
                LogInfo("Hôte démarré.");

                var window = _host.Services.GetRequiredService<MainWindow>();
                MainWindow = window;
                window.Show();
                LogInfo("Fenêtre principale affichée.");
            }
            catch (Exception ex)
            {
                ReportCriticalError("Erreur de configuration", ex);
                Shutdown(-1);
            }
        }

        private async void OnExitAsync(object? sender, ExitEventArgs e)
        {
            if (_host is null)
            {
                return;
            }

            using (_host)
            {
                try
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                    LogInfo("Arrêt de l'hôte terminé.");
                }
                catch (Exception ex)
                {
                    ReportCriticalError("Erreur lors de l'arrêt", ex);
                }
            }
        }

        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ReportCriticalError("Erreur non gérée", e.Exception);
            e.Handled = true;
            Current?.Shutdown(-1);
        }

        private static void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                ReportCriticalError("Erreur critique", exception);
            }
            else
            {
                var message = "Une erreur critique non gérée est survenue.";
                Console.Error.WriteLine(message);
                LogInfo(message);
            }
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ReportCriticalError("Erreur (tâche asynchrone)", e.Exception);
            e.SetObserved();
        }

        private static void ReportCriticalError(string title, Exception exception)
        {
            var message = BuildDetailedMessage(exception);
            Console.Error.WriteLine(message);
            LogInternal($"[{DateTimeOffset.Now:u}] {title}: {message}{Environment.NewLine}{exception}");

            void ShowMessageBox()
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var dispatcher = Current?.Dispatcher;

            if (dispatcher is null)
            {
                ShowMessageBox();
                return;
            }

            if (dispatcher.CheckAccess())
            {
                ShowMessageBox();
            }
            else
            {
                try
                {
                    dispatcher.Invoke(ShowMessageBox);
                }
                catch (TaskCanceledException)
                {
                    // Dispatcher shutting down; best effort to display the error.
                    ShowMessageBox();
                }
            }
        }

        private static string BuildDetailedMessage(Exception exception)
        {
            var current = exception;
            var builder = new System.Text.StringBuilder();

            while (current is not null)
            {
                builder.AppendLine(current.Message);
                current = current.InnerException;

                if (current is not null)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static void LogInfo(string message)
        {
            LogInternal($"[{DateTimeOffset.Now:u}] INFO  {message}");
        }

        private static void LogInternal(string message)
        {
            try
            {
                lock (LogLock)
                {
                    File.AppendAllText(LogFilePath, message + Environment.NewLine);
                }
            }
            catch
            {
                // Ignorer les erreurs de journalisation pour ne pas masquer le problème initial.
            }
        }
    }
}
