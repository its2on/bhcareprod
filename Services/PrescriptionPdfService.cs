using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Barangay.Models;
using Microsoft.Extensions.Logging;

namespace Barangay.Services
{
    public interface IPrescriptionPdfService
    {
        Task<byte[]> GeneratePrescriptionPdfAsync(Prescription prescription, string patientName, string doctorName);
    }

    public class PrescriptionPdfService : IPrescriptionPdfService
    {
        private readonly ILogger<PrescriptionPdfService> _logger;

        public PrescriptionPdfService(ILogger<PrescriptionPdfService> logger)
        {
            _logger = logger;
        }

        public Task<byte[]> GeneratePrescriptionPdfAsync(Prescription prescription, string patientName, string doctorName)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Header
                document.Add(new Paragraph("CITY GOVERNMENT OF CALOOCAN")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12));
                document.Add(new Paragraph("CITY HEALTH DEPARTMENT")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12));
                document.Add(new Paragraph("BAESA HEALTH CENTER")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(14));
                document.Add(new Paragraph("350 ROSO")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10));

                document.Add(new Paragraph("\n")); // Spacing

                // Prescription Title
                document.Add(new Paragraph("PRESCRIPTION")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(16));

                document.Add(new Paragraph("\n")); // Spacing

                // Create prescription box with border
                var prescriptionBox = new Table(1).UseAllAvailableWidth();
                prescriptionBox.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                
                // Inner content table
                var innerTable = new Table(2).UseAllAvailableWidth();
                innerTable.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                innerTable.SetFontSize(11);

                // Left side - Patient and Doctor Info
                var leftCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                leftCell.Add(new Paragraph($"Patient: {patientName}").SetFontSize(11));
                leftCell.Add(new Paragraph($"Date: {prescription.PrescriptionDate:MM/dd/yyyy}").SetFontSize(11));
                leftCell.Add(new Paragraph($"Dr. {doctorName}").SetFontSize(11));
                innerTable.AddCell(leftCell);

                // Right side - Rx symbol and prescription
                var rightCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                rightCell.SetTextAlignment(TextAlignment.LEFT);
                
                // Rx symbol
                rightCell.Add(new Paragraph("Rx").SetFontSize(20));
                rightCell.Add(new Paragraph("\n"));
                
                // Prescription text
                if (!string.IsNullOrEmpty(prescription.Notes))
                {
                    var prescriptionLines = prescription.Notes.Split('\n');
                    foreach (var line in prescriptionLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            rightCell.Add(new Paragraph(line.Trim()).SetFontSize(11));
                        }
                    }
                }
                
                innerTable.AddCell(rightCell);
                
                // Add inner table to prescription box
                var boxCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                boxCell.Add(innerTable);
                prescriptionBox.AddCell(boxCell);
                
                document.Add(prescriptionBox);

                document.Add(new Paragraph("\n\n\n")); // Spacing for signature

                // Signature
                document.Add(new Paragraph("_________________________")
                    .SetTextAlignment(TextAlignment.RIGHT));
                document.Add(new Paragraph($"Dr. {doctorName}")
                    .SetTextAlignment(TextAlignment.RIGHT));
                document.Add(new Paragraph("Physician's Signature")
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontSize(10));

                document.Close();
                return Task.FromResult(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate prescription PDF for patient {PatientName}", patientName);
                throw;
            }
        }
    }
}
