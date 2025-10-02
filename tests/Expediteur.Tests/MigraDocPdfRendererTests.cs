using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Expediteur.Domain.Models;
using Expediteur.Infrastructure.Pdf;
using FluentAssertions;
using Xunit;

namespace Expediteur.Tests;

public sealed class MigraDocPdfRendererTests
{
    [Fact]
    public async Task CreerAccuseAsync_ShouldProduceNonEmptyPdf()
    {
        var renderer = new MigraDocPdfRenderer();
        var acknowledgement = new OrderAcknowledgement
        {
            NumeroCommande = "CMD-12345",
            Client = "Société Demo",
            EmailDestinataire = "demo@example.com",
            DateCommande = new DateTime(2025, 9, 30, 10, 30, 0, DateTimeKind.Utc),
            Lignes = new List<OrderAcknowledgementLigne>
            {
                new()
                {
                    ReferenceProduit = "PRD-001",
                    Description = "Produit de démonstration",
                    Quantite = 5,
                    PrixUnitaire = 19.99m
                },
                new()
                {
                    ReferenceProduit = "PRD-002",
                    Description = "Produit complémentaire",
                    Quantite = 2,
                    PrixUnitaire = 49.90m
                }
            }
        };

        var bytes = await renderer.CreerAccuseAsync(acknowledgement);

        bytes.Should().NotBeNull();
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(1024); // heuristique : PDF minimalement rempli
    }
}
