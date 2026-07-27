using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HybridDecisionIntelligence.Infrastructure.Reports
{
    /// <summary>
    /// Renders a decision as a formal, single-page bank document using QuestPDF —
    /// bordered form-style layout, terse factual language, and color used only
    /// to mark the decision status (not as decoration).
    /// </summary>
    public class QuestPdfDecisionReportGenerator : IDecisionReportGenerator
    {
        private const string BorderColor = "#cbd5e1";
        private const string MutedText = "#64748b";
        private const string Ink = "#0f172a";

        private static readonly Dictionary<string, string> Jobs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["admin."] = "Nëpunës/e (Administratë)",
            ["blue-collar"] = "Punëtor/e",
            ["entrepreneur"] = "Sipërmarrës/e",
            ["housemaid"] = "Shtëpiake",
            ["management"] = "Menaxhim",
            ["retired"] = "I/e Pensionuar",
            ["self-employed"] = "I/e Vetëpunësuar",
            ["services"] = "Shërbime",
            ["student"] = "Student/e",
            ["technician"] = "Teknik/e",
            ["unemployed"] = "I/e Papunë",
            ["unknown"] = "Tjetër / Panjohur",
        };

        private static readonly Dictionary<string, string> Marital = new(StringComparer.OrdinalIgnoreCase)
        {
            ["single"] = "Beqar/e",
            ["married"] = "I/e Martuar",
            ["divorced"] = "I/e Divorcuar",
        };

        private static readonly Dictionary<string, string> Education = new(StringComparer.OrdinalIgnoreCase)
        {
            ["primary"] = "Fillore",
            ["secondary"] = "E Mesme",
            ["tertiary"] = "Universitare",
            ["unknown"] = "Panjohur",
        };

        private static readonly Dictionary<string, string> YesNo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["yes"] = "Po",
            ["no"] = "Jo",
        };

        private static readonly Dictionary<string, string> Contact = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cellular"] = "Celular",
            ["telephone"] = "Telefon Fiks",
            ["unknown"] = "Panjohur",
        };

        private static readonly Dictionary<string, string> Months = new(StringComparer.OrdinalIgnoreCase)
        {
            ["jan"] = "Janar", ["feb"] = "Shkurt", ["mar"] = "Mars", ["apr"] = "Prill",
            ["may"] = "Maj", ["jun"] = "Qershor", ["jul"] = "Korrik", ["aug"] = "Gusht",
            ["sep"] = "Shtator", ["oct"] = "Tetor", ["nov"] = "Nëntor", ["dec"] = "Dhjetor",
        };

        private static readonly Dictionary<string, string> POutcome = new(StringComparer.OrdinalIgnoreCase)
        {
            ["unknown"] = "Pa Kontakt të Mëparshëm",
            ["failure"] = "Dështim",
            ["other"] = "Tjetër",
            ["success"] = "Sukses",
        };

        private static string Translate(Dictionary<string, string> map, string value) =>
            map.TryGetValue(value ?? string.Empty, out var label) ? label : value ?? string.Empty;

        static QuestPdfDecisionReportGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateDecisionReportPdf(HybridDecision decision, BankCustomer? customer)
        {
            var approved = decision.FinalDecision;
            var statusColor = approved ? "#15803d" : "#b91c1c";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    // Explicit font: the default system font resolved on some machines has
                    // a "ti" ligature whose glyph doesn't map back to Unicode in the PDF's
                    // text layer, silently dropping "ti" from copy-pasted/extracted text
                    // (e.g. "Artificiale" -> "Arficiale") even though it renders correctly.
                    // Arial doesn't apply that ligature, so extraction stays correct.
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Ink).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("RAPORT VENDIMI FINANCIAR").FontSize(15).Bold().FontColor(Ink);
                            row.ConstantItem(100).AlignRight().Text("KONFIDENCIAL").FontSize(8).FontColor(MutedText);
                        });
                        col.Item().PaddingTop(2).Text("Sistemi Hibrid i Vendimmarrjes me Inteligjencë Artificiale të Shpjegueshme").FontSize(8).FontColor(MutedText);
                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Ink);

                        // Formal reference row
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            void RefCell(string label, string value)
                            {
                                table.Cell().Border(1).BorderColor(BorderColor).Padding(6).Column(fc =>
                                {
                                    fc.Item().Text(label).FontSize(7).FontColor(MutedText);
                                    fc.Item().Text(value).FontSize(9).Bold();
                                });
                            }

                            RefCell("NR. VENDIMI", $"#{decision.Id}");
                            RefCell("NR. KLIENTIT", $"#{decision.CustomerId}");
                            RefCell("DATA", decision.CreatedAt.ToString("dd.MM.yyyy"));
                            RefCell("ORA", decision.CreatedAt.ToString("HH:mm"));
                        });
                    });

                    page.Content().PaddingTop(16).Column(col =>
                    {
                        col.Spacing(12);

                        // Vendimi Final — bordered, not filled, status color used only on the word itself
                        col.Item().Border(1.5f).BorderColor(statusColor).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("VENDIMI FINAL").FontSize(7).FontColor(MutedText);
                                c.Item().Text(approved ? "MIRATUAR" : "REFUZUAR").FontSize(15).Bold().FontColor(statusColor);
                            });
                            if (approved)
                            {
                                row.ConstantItem(140).AlignRight().Column(c =>
                                {
                                    c.Item().AlignRight().Text("NORMA E INTERESIT").FontSize(7).FontColor(MutedText);
                                    c.Item().AlignRight().Text($"{decision.ApprovedInterestRate:P2}").FontSize(13).Bold();
                                });
                            }
                            else if (decision.WasOverridden)
                            {
                                row.ConstantItem(180).AlignRight().AlignMiddle()
                                    .Text("ANULUAR NGA RREGULLAT E BIZNESIT").FontSize(8).Bold().FontColor("#b45309");
                            }
                        });

                        // Baza e Vendimit — terse, factual, bullet-style (not a prose explanation)
                        col.Item().Column(c =>
                        {
                            c.Item().Text("BAZA E VENDIMIT").FontSize(9).Bold().FontColor(Ink);
                            c.Item().PaddingTop(4).Border(1).BorderColor(BorderColor).Padding(8).Column(bc =>
                            {
                                bc.Spacing(3);
                                foreach (var line in BuildDecisionBasis(decision))
                                {
                                    bc.Item().Text(line).FontSize(9);
                                }
                            });
                        });

                        // Profili i Klientit — formal bordered form table
                        col.Item().Column(c =>
                        {
                            c.Item().Text("PROFILI I KLIENTIT").FontSize(9).Bold().FontColor(Ink);
                            c.Item().PaddingTop(4);

                            if (customer == null)
                            {
                                c.Item().Border(1).BorderColor(BorderColor).Padding(8)
                                    .Text("Profili i klientit nuk është i disponueshëm për këtë vendim.")
                                    .FontSize(8).Italic().FontColor(MutedText);
                            }
                            else
                            {
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn();
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn();
                                    });

                                    void Field(string label, string value)
                                    {
                                        table.Cell().Border(1).BorderColor(BorderColor).Background("#f8fafc")
                                            .Padding(5).Text(label).FontSize(7.5f).FontColor(MutedText);
                                        table.Cell().Border(1).BorderColor(BorderColor)
                                            .Padding(5).Text(value).FontSize(9).SemiBold();
                                    }

                                    Field("Mosha", $"{customer.Age} vjeç");
                                    Field("Profesioni", Translate(Jobs, customer.Job));
                                    Field("Gjendja Civile", Translate(Marital, customer.Marital));
                                    Field("Arsimimi", Translate(Education, customer.Education));
                                    Field("Bilanci Bankar", $"€{customer.Balance:N0}");
                                    Field("Histori Mospagimi", Translate(YesNo, customer.Default));
                                    Field("Kredi Banesore", Translate(YesNo, customer.Housing));
                                    Field("Kredi Personale", Translate(YesNo, customer.Loan));
                                    Field("Mënyra e Kontaktit", Translate(Contact, customer.Contact));
                                    Field("Data e Kontaktit", $"{customer.Day} {Translate(Months, customer.Month)}");
                                    Field("Kontakte në Fushatë", customer.Campaign.ToString());
                                    Field("Fushata e Mëparshme", Translate(POutcome, customer.POutcome));
                                });
                            }
                        });

                        // Vlerësimi Teknik
                        col.Item().Column(c =>
                        {
                            c.Item().Text("VLERËSIMI TEKNIK (AI + RREGULLAT E BIZNESIT)").FontSize(9).Bold().FontColor(Ink);
                            c.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.1f);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(1.1f);
                                    columns.RelativeColumn();
                                });

                                void Field(string label, string value)
                                {
                                    table.Cell().Border(1).BorderColor(BorderColor).Background("#f8fafc")
                                        .Padding(5).Text(label).FontSize(7.5f).FontColor(MutedText);
                                    table.Cell().Border(1).BorderColor(BorderColor)
                                        .Padding(5).Text(value).FontSize(9).SemiBold();
                                }

                                Field("Parashikimi i AI-së", decision.MLPredicted ? "MIRATO" : "REFUZO");
                                Field("Besueshmëria e AI-së", $"{decision.MLConfidence:P2}");
                                Field("Rregullat e Aplikuara", string.IsNullOrWhiteSpace(decision.RulesApplied) ? "—" : decision.RulesApplied);
                                Field("Norma e Interesit", decision.ApprovedInterestRate > 0 ? $"{decision.ApprovedInterestRate:P2}" : "N/A");
                            });

                            if (decision.WasOverridden && !string.IsNullOrWhiteSpace(decision.OverrideReason))
                            {
                                c.Item().PaddingTop(6).Border(1).BorderColor("#b91c1c").Padding(7).Column(oc =>
                                {
                                    oc.Item().Text("ARSYEJA E ANULIMIT").FontSize(7.5f).Bold().FontColor("#b91c1c");
                                    oc.Item().Text(decision.OverrideReason).FontSize(8.5f);
                                });
                            }
                        });

                        // Gjurma e auditimit
                        col.Item().Column(c =>
                        {
                            c.Item().Text("GJURMA E AUDITIMIT (REGJISTRIM I SISTEMIT)").FontSize(9).Bold().FontColor(Ink);
                            c.Item().PaddingTop(4).Border(1).BorderColor(BorderColor).Background("#f8fafc").Padding(8)
                                .Text(decision.AuditTrail).FontSize(7.5f).FontFamily(Fonts.Consolas).FontColor("#334155");
                        });
                    });

                    page.Footer().PaddingTop(8).Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(BorderColor);
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Dokument i gjeneruar automatikisht — nuk kërkon nënshkrim.")
                                .FontSize(7).FontColor(MutedText);
                            row.ConstantItem(80).AlignRight().Text(text =>
                            {
                                text.Span("Faqja ").FontSize(7).FontColor(MutedText);
                                text.CurrentPageNumber().FontSize(7).FontColor(MutedText);
                                text.Span(" / ").FontSize(7).FontColor(MutedText);
                                text.TotalPages().FontSize(7).FontColor(MutedText);
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Short, factual statements for the report — not a conversational
        /// explanation. Each line is a discrete fact a reviewer can verify.
        /// </summary>
        private static List<string> BuildDecisionBasis(HybridDecision decision)
        {
            var rulesPassed = decision.AuditTrail.Contains("Business Rules Evaluation: PASS");

            var lines = new List<string>
            {
                $"Probabiliteti i vlerësuar nga AI: {decision.MLConfidence:P2} — parashikimi: {(decision.MLPredicted ? "MIRATIM" : "REFUZIM")}.",
                $"Vlerësimi i rregullave të biznesit: {(rulesPassed ? "KALOI" : "DËSHTOI")}."
            };

            if (decision.WasOverridden)
            {
                var reason = string.IsNullOrWhiteSpace(decision.OverrideReason) ? "shkelje e një rregulli të bankës" : decision.OverrideReason;
                lines.Add($"Rregullat e biznesit anuluan parashikimin e AI-së. Arsyeja: {reason}.");
            }

            lines.Add($"Vendimi final: {(decision.FinalDecision ? "MIRATUAR" : "REFUZUAR")}.");
            return lines;
        }
    }
}
