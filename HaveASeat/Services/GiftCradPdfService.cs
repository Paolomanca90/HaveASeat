// Services/GiftCardPdfService.cs - Versione corretta per iText7
using iText.Html2pdf;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using HaveASeat.Models;

namespace HaveASeat.Services
{
	public interface IGiftCardPdfService
	{
		Task<byte[]> GenerateGiftCardPdfAsync(GiftCard giftCard);
		Task<byte[]> GenerateGiftCardFromHtmlAsync(GiftCard giftCard);
	}

	public class GiftCardPdfService : IGiftCardPdfService
	{
		private readonly IWebHostEnvironment _environment;
		private readonly ILogger<GiftCardPdfService> _logger;

		public GiftCardPdfService(IWebHostEnvironment environment, ILogger<GiftCardPdfService> logger)
		{
			_environment = environment;
			_logger = logger;
		}

		public async Task<byte[]> GenerateGiftCardPdfAsync(GiftCard giftCard)
		{
			try
			{
				using var stream = new MemoryStream();
				var writer = new PdfWriter(stream);
				var pdf = new PdfDocument(writer);
				var document = new Document(pdf);

				// Imposta margini
				document.SetMargins(50, 50, 50, 50);

				// Font
				var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
				var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
				var italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

				// Colori
				var primaryColor = new DeviceRgb(124, 58, 237); // Purple
				var secondaryColor = new DeviceRgb(236, 72, 153); // Pink
				var grayColor = new DeviceRgb(107, 114, 128);

				// Header
				var headerTable = new Table(2);
				headerTable.SetWidth(UnitValue.CreatePercentValue(100));

				var logoCell = new Cell();
				logoCell.Add(new Paragraph("HAVEASEAT")
					.SetFont(boldFont)
					.SetFontSize(24)
					.SetFontColor(primaryColor));
				logoCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

				var dateCell = new Cell();
				dateCell.Add(new Paragraph($"Creato il: {giftCard.CreatedAt:dd/MM/yyyy}")
					.SetFont(regularFont)
					.SetFontSize(10)
					.SetTextAlignment(TextAlignment.RIGHT)
					.SetFontColor(grayColor));
				dateCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

				headerTable.AddCell(logoCell);
				headerTable.AddCell(dateCell);
				document.Add(headerTable);

				// Spazio
				document.Add(new Paragraph("\n"));

				// Titolo principale
				document.Add(new Paragraph("BUONO REGALO")
					.SetFont(boldFont)
					.SetFontSize(32)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetFontColor(primaryColor)
					.SetMarginBottom(30));

				// Card principale del buono regalo
				var cardTable = new Table(1);
				cardTable.SetWidth(UnitValue.CreatePercentValue(80));
				cardTable.SetHorizontalAlignment(HorizontalAlignment.CENTER);
				cardTable.SetMarginBottom(30);

				var cardCell = new Cell();
				cardCell.SetBackgroundColor(new DeviceRgb(248, 250, 252));
				cardCell.SetPadding(30);
				cardCell.SetBorder(new iText.Layout.Borders.SolidBorder(primaryColor, 2));

				// Contenuto della card
				var cardContent = new Div();

				// Importo
				cardContent.Add(new Paragraph($"€{giftCard.Amount}")
					.SetFont(boldFont)
					.SetFontSize(48)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetFontColor(primaryColor)
					.SetMarginBottom(20));

				// Codice
				cardContent.Add(new Paragraph($"Codice: {giftCard.FormattedCode}")
					.SetFont(boldFont)
					.SetFontSize(16)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetMarginBottom(15));

				// Messaggio personalizzato
				if (!string.IsNullOrEmpty(giftCard.Message))
				{
					cardContent.Add(new Paragraph($"\"{giftCard.Message}\"")
						.SetFont(italicFont) // Usa font italic separato
						.SetFontSize(12)
						.SetTextAlignment(TextAlignment.CENTER)
						.SetFontColor(grayColor)
						.SetMarginBottom(15));
				}

				// Destinatario
				cardContent.Add(new Paragraph($"Per: {giftCard.RecipientName}")
					.SetFont(regularFont)
					.SetFontSize(14)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetMarginBottom(10));

				// Salone o universale
				var salonText = giftCard.Salone?.Nome ?? "Valido in tutti i saloni partner HaveASeat";
				cardContent.Add(new Paragraph(salonText)
					.SetFont(regularFont)
					.SetFontSize(12)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetFontColor(grayColor));

				cardCell.Add(cardContent);
				cardTable.AddCell(cardCell);
				document.Add(cardTable);

				// Informazioni aggiuntive
				var infoTable = new Table(2);
				infoTable.SetWidth(UnitValue.CreatePercentValue(100));
				infoTable.SetMarginTop(30);

				// Colonna sinistra
				var leftCell = new Cell();
				leftCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
				leftCell.Add(new Paragraph("DETTAGLI BUONO")
					.SetFont(boldFont)
					.SetFontSize(12)
					.SetMarginBottom(10));

				leftCell.Add(new Paragraph($"Mittente: {giftCard.SenderName}")
					.SetFont(regularFont)
					.SetFontSize(10)
					.SetMarginBottom(5));

				leftCell.Add(new Paragraph($"Email: {giftCard.SenderEmail}")
					.SetFont(regularFont)
					.SetFontSize(10)
					.SetMarginBottom(5));

				leftCell.Add(new Paragraph($"Data creazione: {giftCard.CreatedAt:dd/MM/yyyy}")
					.SetFont(regularFont)
					.SetFontSize(10)
					.SetMarginBottom(5));

				leftCell.Add(new Paragraph($"Scadenza: {giftCard.ExpiryDate:dd/MM/yyyy}")
					.SetFont(boldFont) // Usa bold invece di colore rosso
					.SetFontSize(10));

				// Colonna destra
				var rightCell = new Cell();
				rightCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
				rightCell.Add(new Paragraph("COME UTILIZZARE")
					.SetFont(boldFont)
					.SetFontSize(12)
					.SetMarginBottom(10));

				// Usa paragrafi invece di List per maggiore compatibilità
				rightCell.Add(new Paragraph("• Vai su www.haveaseat.com")
					.SetFont(regularFont)
					.SetFontSize(10)
					.SetMarginBottom(3));

				rightCell.Add(new Paragraph("• Cerca e seleziona un salone")
					.SetFont(regularFont)
					.SetFontSize(10)
					.SetMarginBottom(3));

				rightCell.Add(new Paragraph("• Al momento del pagamento inserisci il codice")
					.SetFont(regularFont)
					.SetFontSize(10)
					.SetMarginBottom(3));

				rightCell.Add(new Paragraph("• Il valore del buono verrà detratto dal totale")
					.SetFont(regularFont)
					.SetFontSize(10)
					.SetMarginBottom(3));

				rightCell.Add(new Paragraph("• Valido per qualsiasi servizio")
					.SetFont(regularFont)
					.SetFontSize(10));

				infoTable.AddCell(leftCell);
				infoTable.AddCell(rightCell);
				document.Add(infoTable);

				// Footer
				document.Add(new Paragraph("\n\n"));
				document.Add(new Paragraph("Questo buono regalo è stato generato digitalmente ed è valido fino alla data di scadenza indicata.")
					.SetFont(regularFont)
					.SetFontSize(8)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetFontColor(grayColor));

				document.Add(new Paragraph("Per assistenza: support@haveaseat.com | Tel: +39 123 456 789")
					.SetFont(regularFont)
					.SetFontSize(8)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetFontColor(grayColor));

				document.Close();
				return stream.ToArray();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore durante la generazione del PDF per gift card {GiftCardId}", giftCard.GiftCardId);
				throw;
			}
		}

		public async Task<byte[]> GenerateGiftCardFromHtmlAsync(GiftCard giftCard)
		{
			try
			{
				var htmlContent = GenerateGiftCardHtml(giftCard);

				using var stream = new MemoryStream();
				var properties = new ConverterProperties();

				// Configura font e risorse se necessario
				var fontProvider = new iText.Html2pdf.Resolver.Font.DefaultFontProvider(true, true, true);
				properties.SetFontProvider(fontProvider);

				HtmlConverter.ConvertToPdf(htmlContent, stream, properties);

				return stream.ToArray();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore durante la generazione del PDF HTML per gift card {GiftCardId}", giftCard.GiftCardId);
				throw;
			}
		}

		private string GenerateGiftCardHtml(GiftCard giftCard)
		{
			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .header {{ text-align: center; margin-bottom: 40px; }}
        .logo {{ color: #7c3aed; font-size: 24px; font-weight: bold; }}
        .title {{ color: #7c3aed; font-size: 32px; font-weight: bold; margin: 30px 0; text-align: center; }}
        .gift-card {{ 
            border: 3px solid #7c3aed; 
            background: #f8fafc; 
            padding: 40px; 
            margin: 30px auto; 
            width: 60%; 
            text-align: center; 
        }}
        .amount {{ color: #7c3aed; font-size: 48px; font-weight: bold; margin-bottom: 20px; }}
        .code {{ font-size: 16px; font-weight: bold; margin-bottom: 15px; }}
        .message {{ font-style: italic; color: #6b7280; margin-bottom: 15px; }}
        .recipient {{ font-size: 14px; margin-bottom: 10px; }}
        .salon {{ font-size: 12px; color: #6b7280; }}
        .details {{ display: table; width: 100%; margin-top: 40px; }}
        .left-column, .right-column {{ display: table-cell; width: 50%; vertical-align: top; }}
        .section-title {{ font-weight: bold; font-size: 12px; margin-bottom: 10px; }}
        .detail-item {{ font-size: 10px; margin-bottom: 5px; }}
        .instructions {{ font-size: 10px; }}
        .footer {{ text-align: center; margin-top: 40px; font-size: 8px; color: #6b7280; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='logo'>HAVEASEAT</div>
        <div style='font-size: 10px; color: #6b7280;'>Creato il: {giftCard.CreatedAt:dd/MM/yyyy}</div>
    </div>

    <div class='title'>BUONO REGALO</div>

    <div class='gift-card'>
        <div class='amount'>€{giftCard.Amount}</div>
        <div class='code'>Codice: {giftCard.FormattedCode}</div>
        {(!string.IsNullOrEmpty(giftCard.Message) ? $"<div class='message'>\"{giftCard.Message}\"</div>" : "")}
        <div class='recipient'>Per: {giftCard.RecipientName}</div>
        <div class='salon'>{(giftCard.Salone?.Nome ?? "Valido in tutti i saloni partner HaveASeat")}</div>
    </div>

    <div class='details'>
        <div class='left-column'>
            <div class='section-title'>DETTAGLI BUONO</div>
            <div class='detail-item'>Mittente: {giftCard.SenderName}</div>
            <div class='detail-item'>Email: {giftCard.SenderEmail}</div>
            <div class='detail-item'>Data creazione: {giftCard.CreatedAt:dd/MM/yyyy}</div>
            <div class='detail-item' style='font-weight: bold;'>Scadenza: {giftCard.ExpiryDate:dd/MM/yyyy}</div>
        </div>
        <div class='right-column'>
            <div class='section-title'>COME UTILIZZARE</div>
            <div class='instructions'>
                • Vai su www.haveaseat.com<br>
                • Cerca e seleziona un salone<br>
                • Al momento del pagamento inserisci il codice<br>
                • Il valore del buono verrà detratto dal totale<br>
                • Valido per qualsiasi servizio
            </div>
        </div>
    </div>

    <div class='footer'>
        <div>Questo buono regalo è stato generato digitalmente ed è valido fino alla data di scadenza indicata.</div>
        <div>Per assistenza: support@haveaseat.com | Tel: +39 123 456 789</div>
    </div>
</body>
</html>";
		}
	}
}