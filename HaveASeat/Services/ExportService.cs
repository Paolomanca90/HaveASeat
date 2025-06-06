using ClosedXML.Excel;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Text;
using iText.Layout;

namespace HaveASeat.Services
{
    public class ExportService : IExportService
    {
        public byte[] ExportToCsv(DashboardExportData data)
        {
            var csv = new StringBuilder();

            // Header
            csv.AppendLine($"Report Dashboard - {data.NomeSalone}");
            csv.AppendLine($"Periodo: {data.Periodo}");
            csv.AppendLine($"Data export: {data.DataExport:dd/MM/yyyy HH:mm}");
            csv.AppendLine();

            // Statistiche generali
            csv.AppendLine("STATISTICHE GENERALI");
            csv.AppendLine("Metrica,Valore,Variazione %");
            csv.AppendLine($"Prenotazioni Oggi,{data.Stats.PrenotazioniOggi},{data.Stats.PercentualePrenotazioni}%");
            csv.AppendLine($"Nuovi Clienti,{data.Stats.NuoviClienti},{data.Stats.PercentualeNuoviClienti}%");
            csv.AppendLine($"Incasso Giornaliero,€{data.Stats.IncassoGiornaliero},{data.Stats.PercentualeIncasso}%");
            csv.AppendLine($"Servizi Completati,{data.Stats.ServiziCompletati},{data.Stats.PercentualeServizi}%");
            csv.AppendLine($"Numero Dipendenti,{data.NumeroDipendenti},-");
            csv.AppendLine();

            // Top servizi
            csv.AppendLine("TOP SERVIZI");
            csv.AppendLine("Servizio,Prenotazioni,Incasso");
            foreach (var servizio in data.TopServizi)
            {
                csv.AppendLine($"{servizio.Nome},{servizio.NumeroPrenotazioni},€{servizio.IncassoTotale}");
            }
            csv.AppendLine();

            // Dettaglio appuntamenti
            csv.AppendLine("DETTAGLIO APPUNTAMENTI");
            csv.AppendLine("Data,Ora Inizio,Ora Fine,Cliente,Servizio,Dipendente,Stato,Prezzo");
            foreach (var app in data.Appuntamenti)
            {
                csv.AppendLine($"{app.Data:dd/MM/yyyy},{app.OraInizio},{app.OraFine},{app.NomeCliente},{app.Servizio},{app.Dipendente},{app.Stato},€{app.Prezzo}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public byte[] ExportToExcel(DashboardExportData data)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Dashboard Report");

                // Styling
                var headerStyle = worksheet.Style;
                var titleRow = 1;

                // Header
                worksheet.Cell(titleRow, 1).Value = $"Report Dashboard - {data.NomeSalone}";
                worksheet.Cell(titleRow, 1).Style.Font.Bold = true;
                worksheet.Cell(titleRow, 1).Style.Font.FontSize = 16;
                worksheet.Range(titleRow, 1, titleRow, 8).Merge();

                worksheet.Cell(titleRow + 1, 1).Value = $"Periodo: {data.Periodo}";
                worksheet.Cell(titleRow + 2, 1).Value = $"Data export: {data.DataExport:dd/MM/yyyy HH:mm}";

                var currentRow = titleRow + 4;

                // Statistiche generali
                worksheet.Cell(currentRow, 1).Value = "STATISTICHE GENERALI";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                worksheet.Range(currentRow, 1, currentRow, 3).Merge();
                currentRow++;

                // Headers statistiche
                worksheet.Cell(currentRow, 1).Value = "Metrica";
                worksheet.Cell(currentRow, 2).Value = "Valore";
                worksheet.Cell(currentRow, 3).Value = "Variazione %";
                worksheet.Range(currentRow, 1, currentRow, 3).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Dati statistiche
                var statsData = new[]
                {
                    new { Metrica = "Prenotazioni Oggi", Valore = data.Stats.PrenotazioniOggi.ToString(), Variazione = $"{data.Stats.PercentualePrenotazioni}%" },
                    new { Metrica = "Nuovi Clienti", Valore = data.Stats.NuoviClienti.ToString(), Variazione = $"{data.Stats.PercentualeNuoviClienti}%" },
                    new { Metrica = "Incasso Giornaliero", Valore = $"€{data.Stats.IncassoGiornaliero}", Variazione = $"{data.Stats.PercentualeIncasso}%" },
                    new { Metrica = "Servizi Completati", Valore = data.Stats.ServiziCompletati.ToString(), Variazione = $"{data.Stats.PercentualeServizi}%" },
                    new { Metrica = "Numero Dipendenti", Valore = data.NumeroDipendenti.ToString(), Variazione = "-" }
                };

                foreach (var stat in statsData)
                {
                    worksheet.Cell(currentRow, 1).Value = stat.Metrica;
                    worksheet.Cell(currentRow, 2).Value = stat.Valore;
                    worksheet.Cell(currentRow, 3).Value = stat.Variazione;
                    currentRow++;
                }

                currentRow += 2;

                // Top servizi
                worksheet.Cell(currentRow, 1).Value = "TOP SERVIZI";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                worksheet.Range(currentRow, 1, currentRow, 3).Merge();
                currentRow++;

                worksheet.Cell(currentRow, 1).Value = "Servizio";
                worksheet.Cell(currentRow, 2).Value = "Prenotazioni";
                worksheet.Cell(currentRow, 3).Value = "Incasso";
                worksheet.Range(currentRow, 1, currentRow, 3).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                foreach (var servizio in data.TopServizi)
                {
                    worksheet.Cell(currentRow, 1).Value = servizio.Nome;
                    worksheet.Cell(currentRow, 2).Value = servizio.NumeroPrenotazioni;
                    worksheet.Cell(currentRow, 3).Value = $"€{servizio.IncassoTotale}";
                    currentRow++;
                }

                currentRow += 2;

                // Dettaglio appuntamenti
                worksheet.Cell(currentRow, 1).Value = "DETTAGLIO APPUNTAMENTI";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                worksheet.Range(currentRow, 1, currentRow, 8).Merge();
                currentRow++;

                var appHeaders = new[] { "Data", "Ora Inizio", "Ora Fine", "Cliente", "Servizio", "Dipendente", "Stato", "Prezzo" };
                for (int i = 0; i < appHeaders.Length; i++)
                {
                    worksheet.Cell(currentRow, i + 1).Value = appHeaders[i];
                }
                worksheet.Range(currentRow, 1, currentRow, appHeaders.Length).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, appHeaders.Length).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                foreach (var app in data.Appuntamenti)
                {
                    worksheet.Cell(currentRow, 1).Value = app.Data.ToString("dd/MM/yyyy");
                    worksheet.Cell(currentRow, 2).Value = app.OraInizio;
                    worksheet.Cell(currentRow, 3).Value = app.OraFine;
                    worksheet.Cell(currentRow, 4).Value = app.NomeCliente;
                    worksheet.Cell(currentRow, 5).Value = app.Servizio;
                    worksheet.Cell(currentRow, 6).Value = app.Dipendente;
                    worksheet.Cell(currentRow, 7).Value = app.Stato;
                    worksheet.Cell(currentRow, 8).Value = $"€{app.Prezzo}";
                    currentRow++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportToPdf(DashboardExportData data)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Font
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Header
                document.Add(new Paragraph($"Report Dashboard - {data.NomeSalone}")
                    .SetFont(boldFont)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Periodo: {data.Periodo}")
                    .SetFont(font)
                    .SetFontSize(12));

                document.Add(new Paragraph($"Data export: {data.DataExport:dd/MM/yyyy HH:mm}")
                    .SetFont(font)
                    .SetFontSize(12)
                    .SetMarginBottom(20));

                // Statistiche generali
                document.Add(new Paragraph("STATISTICHE GENERALI")
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetMarginTop(10));

                var statsTable = new Table(3);
                statsTable.SetWidth(UnitValue.CreatePercentValue(100));

                // Headers
                statsTable.AddHeaderCell(new Cell().Add(new Paragraph("Metrica").SetFont(boldFont)));
                statsTable.AddHeaderCell(new Cell().Add(new Paragraph("Valore").SetFont(boldFont)));
                statsTable.AddHeaderCell(new Cell().Add(new Paragraph("Variazione %").SetFont(boldFont)));

                // Dati
                statsTable.AddCell(new Cell().Add(new Paragraph("Prenotazioni Oggi")));
                statsTable.AddCell(new Cell().Add(new Paragraph(data.Stats.PrenotazioniOggi.ToString())));
                statsTable.AddCell(new Cell().Add(new Paragraph($"{data.Stats.PercentualePrenotazioni}%")));

                statsTable.AddCell(new Cell().Add(new Paragraph("Nuovi Clienti")));
                statsTable.AddCell(new Cell().Add(new Paragraph(data.Stats.NuoviClienti.ToString())));
                statsTable.AddCell(new Cell().Add(new Paragraph($"{data.Stats.PercentualeNuoviClienti}%")));

                statsTable.AddCell(new Cell().Add(new Paragraph("Incasso Giornaliero")));
                statsTable.AddCell(new Cell().Add(new Paragraph($"€{data.Stats.IncassoGiornaliero}")));
                statsTable.AddCell(new Cell().Add(new Paragraph($"{data.Stats.PercentualeIncasso}%")));

                statsTable.AddCell(new Cell().Add(new Paragraph("Servizi Completati")));
                statsTable.AddCell(new Cell().Add(new Paragraph(data.Stats.ServiziCompletati.ToString())));
                statsTable.AddCell(new Cell().Add(new Paragraph($"{data.Stats.PercentualeServizi}%")));

                statsTable.AddCell(new Cell().Add(new Paragraph("Numero Dipendenti")));
                statsTable.AddCell(new Cell().Add(new Paragraph(data.NumeroDipendenti.ToString())));
                statsTable.AddCell(new Cell().Add(new Paragraph("-")));

                document.Add(statsTable.SetMarginBottom(20));

                // Top servizi
                document.Add(new Paragraph("TOP SERVIZI")
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetMarginTop(20));

                var serviziTable = new Table(3);
                serviziTable.SetWidth(UnitValue.CreatePercentValue(100));

                serviziTable.AddHeaderCell(new Cell().Add(new Paragraph("Servizio").SetFont(boldFont)));
                serviziTable.AddHeaderCell(new Cell().Add(new Paragraph("Prenotazioni").SetFont(boldFont)));
                serviziTable.AddHeaderCell(new Cell().Add(new Paragraph("Incasso").SetFont(boldFont)));

                foreach (var servizio in data.TopServizi)
                {
                    serviziTable.AddCell(new Cell().Add(new Paragraph(servizio.Nome)));
                    serviziTable.AddCell(new Cell().Add(new Paragraph(servizio.NumeroPrenotazioni.ToString())));
                    serviziTable.AddCell(new Cell().Add(new Paragraph($"€{servizio.IncassoTotale}")));
                }

                document.Add(serviziTable.SetMarginBottom(20));

                // Appuntamenti (solo se ci sono)
                if (data.Appuntamenti.Any())
                {
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

                    document.Add(new Paragraph("DETTAGLIO APPUNTAMENTI")
                        .SetFont(boldFont)
                        .SetFontSize(14)
                        .SetMarginTop(20));

                    var appTable = new Table(8);
                    appTable.SetWidth(UnitValue.CreatePercentValue(100));
                    appTable.SetFontSize(10);

                    var headers = new[] { "Data", "Ora Inizio", "Ora Fine", "Cliente", "Servizio", "Dipendente", "Stato", "Prezzo" };
                    foreach (var header in headers)
                    {
                        appTable.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(boldFont).SetFontSize(9)));
                    }

                    foreach (var app in data.Appuntamenti)
                    {
                        appTable.AddCell(new Cell().Add(new Paragraph(app.Data.ToString("dd/MM/yyyy")).SetFontSize(9)));
                        appTable.AddCell(new Cell().Add(new Paragraph(app.OraInizio).SetFontSize(9)));
                        appTable.AddCell(new Cell().Add(new Paragraph(app.OraFine).SetFontSize(9)));
                        appTable.AddCell(new Cell().Add(new Paragraph(app.NomeCliente).SetFontSize(9)));
                        appTable.AddCell(new Cell().Add(new Paragraph(app.Servizio).SetFontSize(9)));
                        appTable.AddCell(new Cell().Add(new Paragraph(app.Dipendente).SetFontSize(9)));
                        appTable.AddCell(new Cell().Add(new Paragraph(app.Stato).SetFontSize(9)));
                        appTable.AddCell(new Cell().Add(new Paragraph($"€{app.Prezzo}").SetFontSize(9)));
                    }

                    document.Add(appTable);
                }

                document.Close();
                return stream.ToArray();
            }
        }
    }
}
