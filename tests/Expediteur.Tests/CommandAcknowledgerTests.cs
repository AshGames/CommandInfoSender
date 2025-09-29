using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using Expediteur.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Expediteur.Tests;

public sealed class CommandAcknowledgerTests
{
    private readonly IOrderAcknowledgementRepository _orderRepository = Substitute.For<IOrderAcknowledgementRepository>();
    private readonly IPdfRenderer _pdfRenderer = Substitute.For<IPdfRenderer>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IJobHistoryRepository _historyRepository = Substitute.For<IJobHistoryRepository>();
    private readonly IScheduleConfigurationRepository _scheduleRepository = Substitute.For<IScheduleConfigurationRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ILogger<CommandAcknowledger> _logger = Substitute.For<ILogger<CommandAcknowledger>>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Email:Sujet"] = "Accusé {0}"
    }).Build();

    [Fact]
    public async Task TraiterAsync_ShouldSendEmailAndPersistHistory_WhenAckIsSuccessful()
    {
        // Arrange
        var executionTime = new DateTimeOffset(2024, 06, 01, 8, 0, 0, TimeSpan.Zero);
        _clock.Maintenant().Returns(executionTime);

        var commande = new OrderAcknowledgement
        {
            NumeroCommande = "CMD-001",
            Client = "Boulangerie Dupont",
            EmailDestinataire = "contact@dupont.fr",
            DateCommande = new DateTime(2024, 05, 31),
            Lignes = new[]
            {
                new OrderAcknowledgementLigne
                {
                    ReferenceProduit = "FARINE-01",
                    Description = "Farine de blé T65",
                    Quantite = 10,
                    PrixUnitaire = 12.5m
                }
            }
        };

        _orderRepository.ObtenirAccusesAsync(executionTime, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<OrderAcknowledgement>>(new[] { commande }));

        _pdfRenderer.CreerAccuseAsync(commande, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new byte[] { 0x01, 0x02 }));

        _scheduleRepository.ObtenirConfigurationAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ScheduleConfiguration
        {
            IntervalleHeures = 4,
            ProchaineExecution = executionTime.AddHours(4),
            EstActif = true
        }));

        var sut = new CommandAcknowledger(
            _orderRepository,
            _pdfRenderer,
            _emailSender,
            _historyRepository,
            _scheduleRepository,
            _clock,
            _logger,
            _configuration);

        // Act
        var result = await sut.TraiterAsync();

        // Assert
        result.NombreDocumentsEnvoyes.Should().Be(1);
        await _emailSender.Received(1).EnvoyerAsync(Arg.Is<EmailMessage>(mail => mail.Destinataire == "contact@dupont.fr"));
        await _historyRepository.Received(1).EnregistrerAsync(Arg.Is<JobHistoryEntry>(entry => entry.EstReussie));
        await _scheduleRepository.Received(1).MettreAJourAsync(4, true, executionTime.AddHours(4), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TraiterAsync_ShouldRecordFailure_WhenEmailThrows()
    {
        // Arrange
        var executionTime = new DateTimeOffset(2024, 06, 01, 8, 0, 0, TimeSpan.Zero);
        _clock.Maintenant().Returns(executionTime);

        var commande = new OrderAcknowledgement
        {
            NumeroCommande = "CMD-002",
            Client = "Fromagerie Martin",
            EmailDestinataire = "ventes@martin.fr",
            DateCommande = new DateTime(2024, 05, 31),
            Lignes = Array.Empty<OrderAcknowledgementLigne>()
        };

        _orderRepository.ObtenirAccusesAsync(executionTime, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<OrderAcknowledgement>>(new[] { commande }));

        _pdfRenderer.CreerAccuseAsync(commande, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Array.Empty<byte>()));
        _emailSender.EnvoyerAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("SMTP indisponible"));

        _scheduleRepository.ObtenirConfigurationAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ScheduleConfiguration
        {
            IntervalleHeures = 2,
            ProchaineExecution = executionTime.AddHours(2),
            EstActif = true
        }));

        var sut = new CommandAcknowledger(
            _orderRepository,
            _pdfRenderer,
            _emailSender,
            _historyRepository,
            _scheduleRepository,
            _clock,
            _logger,
            _configuration);

        // Act
        var result = await sut.TraiterAsync();

        // Assert
        result.NombreDocumentsEnvoyes.Should().Be(0);
        await _historyRepository.Received(1).EnregistrerAsync(Arg.Is<JobHistoryEntry>(entry => !entry.EstReussie && entry.Message.Contains("SMTP")));
    }
}
