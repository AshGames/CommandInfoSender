using System.Globalization;
using System.IO;
using System.Threading;
using Expediteur.Domain.Contracts;
using Expediteur.Domain.Models;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using MigraDocCore.Rendering;

namespace Expediteur.Infrastructure.Pdf;

public sealed class MigraDocPdfRenderer : IPdfRenderer
{
    private static readonly Color PrimaryColor = Colors.DarkBlue;
    private static readonly Color AccentColor = Colors.Azure;
    private static readonly Color BorderColor = Colors.Gainsboro;
    private static readonly Color FooterColor = Colors.Gray;
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("fr-FR");

    static MigraDocPdfRenderer()
    {
        WindowsFontResolver.EnsureRegistered();
    }

    public Task<byte[]> CreerAccuseAsync(OrderAcknowledgement acknowledgement, CancellationToken cancellationToken = default)
    {
        var document = BuildDocument(acknowledgement);

        using var stream = new MemoryStream();
        var renderer = new PdfDocumentRenderer(unicode: true)
        {
            Document = document
        };

        renderer.RenderDocument();
        renderer.PdfDocument.Save(stream, closeStream: false);

        return Task.FromResult(stream.ToArray());
    }

    private static Document BuildDocument(OrderAcknowledgement acknowledgement)
    {
        var document = new Document();
        document.Info.Title = $"Accusé de commande {acknowledgement.NumeroCommande}";
        document.Info.Subject = "Confirmation d'envoi de commande";
        document.Info.Author = "Service Expédition";

        ConfigureStyles(document);

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(2.5);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(2);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2.5);
        section.PageSetup.RightMargin = Unit.FromCentimeter(2);

        BuildHeader(section, acknowledgement);
        BuildSummary(section, acknowledgement);
        BuildLinesTable(section, acknowledgement);
        BuildTotals(section, acknowledgement);
        BuildFooter(section);

        return document;
    }

    private static void ConfigureStyles(Document document)
    {
    var normal = document.Styles[StyleNames.Normal];
    normal.Font.Name = WindowsFontResolver.PrimaryFontFamily;
        normal.Font.Size = 10;

        var heading1 = document.Styles[StyleNames.Heading1];
    heading1.Font.Name = WindowsFontResolver.PrimaryFontFamily;
        heading1.Font.Size = 20;
        heading1.Font.Bold = true;
        heading1.Font.Color = PrimaryColor;
        heading1.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0.3);

        var heading2 = document.Styles[StyleNames.Heading2];
    heading2.Font.Name = WindowsFontResolver.PrimaryFontFamily;
        heading2.Font.Size = 11;
        heading2.Font.Bold = true;
        heading2.Font.Color = PrimaryColor;
        heading2.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.4);
        heading2.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0.1);
    }

    private static void BuildHeader(Section section, OrderAcknowledgement acknowledgement)
    {
        var title = section.AddParagraph("Accusé de commande");
        title.Style = StyleNames.Heading1;

        var infoTable = section.AddTable();
        infoTable.Format.SpaceAfter = Unit.FromMillimeter(4);
        infoTable.Borders.Width = 0.5;
        infoTable.Borders.Color = PrimaryColor;
        infoTable.AddColumn(Unit.FromCentimeter(16));

        var infoRow = infoTable.AddRow();
        infoRow.Shading.Color = AccentColor;
        infoRow.TopPadding = Unit.FromMillimeter(3);
        infoRow.BottomPadding = Unit.FromMillimeter(3);

        var heading = infoRow.Cells[0].AddParagraph($"Commande {acknowledgement.NumeroCommande}");
        heading.Format.Font.Bold = true;
        heading.Format.SpaceAfter = Unit.FromMillimeter(2);

        var details = infoRow.Cells[0].AddParagraph();
        details.AddText($"Client : {acknowledgement.Client}");
        details.AddLineBreak();
        details.AddText($"Email : {acknowledgement.EmailDestinataire}");
        details.AddLineBreak();
        details.AddText($"Date de commande : {acknowledgement.DateCommande.ToString("dddd d MMMM yyyy", Culture)}");
    }

    private static void BuildSummary(Section section, OrderAcknowledgement acknowledgement)
    {
        var message = section.AddParagraph();
        message.Format.SpaceBefore = Unit.FromMillimeter(4);
        message.Format.SpaceAfter = Unit.FromMillimeter(6);
        message.AddText("Nous confirmons l'enregistrement et la prise en charge de votre commande. Les éléments ci-dessous récapitulent son contenu détaillé. Pour toute question, notre équipe reste à votre disposition.");
    }

    private static void BuildLinesTable(Section section, OrderAcknowledgement acknowledgement)
    {
        var table = section.AddTable();
        table.Format.SpaceBefore = Unit.FromMillimeter(2);
        table.Format.SpaceAfter = Unit.FromMillimeter(4);
        table.Borders.Width = 0.25;
    table.Borders.Color = BorderColor;
        table.Rows.LeftIndent = 0;

        table.AddColumn(Unit.FromCentimeter(2.8));
        table.AddColumn(Unit.FromCentimeter(7));
        table.AddColumn(Unit.FromCentimeter(2.2));
        table.AddColumn(Unit.FromCentimeter(2.4));
        table.AddColumn(Unit.FromCentimeter(2.4));

        var headerRow = table.AddRow();
        headerRow.Format.Font.Bold = true;
        headerRow.Shading.Color = PrimaryColor;
        headerRow.Format.Font.Color = Colors.White;
        headerRow.HeadingFormat = true;
        headerRow.VerticalAlignment = VerticalAlignment.Center;
        headerRow.Cells[0].AddParagraph("Référence");
        headerRow.Cells[1].AddParagraph("Description");
        headerRow.Cells[2].AddParagraph("Qté");
        headerRow.Cells[2].Format.Alignment = ParagraphAlignment.Center;
        headerRow.Cells[3].AddParagraph("PU");
        headerRow.Cells[3].Format.Alignment = ParagraphAlignment.Right;
        headerRow.Cells[4].AddParagraph("Montant");
        headerRow.Cells[4].Format.Alignment = ParagraphAlignment.Right;

        foreach (var ligne in acknowledgement.Lignes)
        {
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].AddParagraph(ligne.ReferenceProduit);
            row.Cells[1].AddParagraph(ligne.Description);

            var quantityParagraph = row.Cells[2].AddParagraph(ligne.Quantite.ToString("N2", Culture));
            quantityParagraph.Format.Alignment = ParagraphAlignment.Center;

            var unitPriceParagraph = row.Cells[3].AddParagraph(ligne.PrixUnitaire.ToString("C2", Culture));
            unitPriceParagraph.Format.Alignment = ParagraphAlignment.Right;

            var amountParagraph = row.Cells[4].AddParagraph(ligne.MontantTtc.ToString("C2", Culture));
            amountParagraph.Format.Alignment = ParagraphAlignment.Right;
        }

        table.SetEdge(0, 0, table.Columns.Count, table.Rows.Count, Edge.Box, BorderStyle.Single, 0.5, table.Borders.Color);
    }

    private static void BuildTotals(Section section, OrderAcknowledgement acknowledgement)
    {
        var summaryTable = section.AddTable();
        summaryTable.Borders.Width = 0;
        summaryTable.AddColumn(Unit.FromCentimeter(11.8));
        summaryTable.AddColumn(Unit.FromCentimeter(2.5));

        var totalRow = summaryTable.AddRow();
        totalRow.Cells[0].Borders.Width = 0;
        totalRow.Cells[0].AddParagraph("Total TTC").Format.Alignment = ParagraphAlignment.Right;
        totalRow.Cells[0].Format.Font.Bold = true;

        var totalValueParagraph = totalRow.Cells[1].AddParagraph(acknowledgement.TotalTtc.ToString("C2", Culture));
        totalRow.Cells[1].Format.Font.Bold = true;
        totalValueParagraph.Format.Alignment = ParagraphAlignment.Right;

        var thankYou = section.AddParagraph();
        thankYou.Format.SpaceBefore = Unit.FromMillimeter(10);
        thankYou.AddFormattedText("Merci pour votre confiance.", TextFormat.Bold);
        thankYou.AddLineBreak();
        thankYou.AddText("Nous vous informerons par email dès l'expédition de votre commande.");
    }

    private static void BuildFooter(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.Format.Font.Size = 8;
    footer.Format.Font.Color = FooterColor;
        footer.AddText($"Document généré automatiquement le {DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:mm", Culture)}");
    }
}
