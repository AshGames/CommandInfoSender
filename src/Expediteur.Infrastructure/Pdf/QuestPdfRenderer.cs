using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Expediteur.Infrastructure.Pdf;

public sealed class QuestPdfRenderer : IPdfRenderer
{
    public Task<byte[]> CreerAccuseAsync(OrderAcknowledgement acknowledgement, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Text(text =>
                {
                    text.Span("Expéditeur d'accusé de commande").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                    text.Span("\n");
                    text.Span($"Commande {acknowledgement.NumeroCommande} – {acknowledgement.Client}").FontSize(14);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text($"Date de commande : {acknowledgement.DateCommande:dd MMMM yyyy}");

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Référence");
                            header.Cell().Element(CellHeader).Text("Description");
                            header.Cell().Element(CellHeader).AlignCenter().Text("Qté");
                            header.Cell().Element(CellHeader).AlignRight().Text("PU");
                            header.Cell().Element(CellHeader).AlignRight().Text("Montant");
                        });

                        foreach (var ligne in acknowledgement.Lignes)
                        {
                            table.Cell().Element(CellBody).Text(ligne.ReferenceProduit);
                            table.Cell().Element(CellBody).Text(ligne.Description);
                            table.Cell().Element(CellBody).AlignCenter().Text(ligne.Quantite.ToString("N2"));
                            table.Cell().Element(CellBody).AlignRight().Text(ligne.PrixUnitaire.ToString("C2"));
                            table.Cell().Element(CellBody).AlignRight().Text(ligne.MontantTtc.ToString("C2"));
                        }

                        table.Footer(footer =>
                        {
                            footer.Cell().ColumnSpan(4).Element(CellFooter).AlignRight().Text("Total TTC");
                            footer.Cell().Element(CellFooter).AlignRight().Text(acknowledgement.TotalTtc.ToString("C2"));
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken2));
                    x.Span("Document généré automatiquement ");
                    x.Span(DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:mm"));
                });
            });
        });

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);

        IContainer CellHeader(IContainer container) => container
            .Background(Colors.Grey.Lighten3)
            .Padding(5)
            .DefaultTextStyle(x => x.SemiBold());

        IContainer CellBody(IContainer container) => container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3)
            .PaddingHorizontal(5);

        IContainer CellFooter(IContainer container) => container
            .Background(Colors.Grey.Lighten4)
            .Padding(5)
            .DefaultTextStyle(x => x.SemiBold());
    }
}
