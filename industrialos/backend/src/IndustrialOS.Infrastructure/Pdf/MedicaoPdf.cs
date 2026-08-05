using System.Globalization;
using IndustrialOS.Application.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IndustrialOS.Infrastructure.Pdf;

/// <summary>Boletim de Medicao em PDF (QuestPDF) — segue o padrao visual do RdoPdf.</summary>
public class MedicaoPdf : IMedicaoPdf
{
    static MedicaoPdf() => QuestPDF.Settings.License = LicenseType.Community;

    private static readonly string Azul = Colors.Blue.Darken2;
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private static string Brl(decimal v) => v.ToString("C2", Br);
    private static string Pct(decimal frac) => (frac * 100m).ToString("0.#", Br) + "%";

    public byte[] Gerar(MedicaoPdfModel m)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Element(h => Cabecalho(h, m));
                page.Content().PaddingVertical(8).Element(c => Corpo(c, m));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("IndustrialOS · gerado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "  ·  ").FontSize(7).FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                    t.Span("/").FontSize(7).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static void Cabecalho(IContainer c, MedicaoPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Column(x =>
                {
                    x.Item().Text("BOLETIM DE MEDIÇÃO").FontSize(15).Bold().FontColor(Azul);
                    x.Item().Text(m.ObraNome).FontSize(11).SemiBold();
                    if (m.Cliente is { } cli) x.Item().Text("Cliente: " + cli);
                    if (m.Contrato is { } ct) x.Item().Text("Contrato: " + ct);
                    if (m.Local is { } lo) x.Item().Text("Local: " + lo);
                });
                r.ConstantItem(160).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(x =>
                {
                    x.Item().AlignCenter().Text($"BM Nº {m.Numero}").Bold().FontColor(Azul);
                    x.Item().AlignCenter().Text($"Período {m.De} a {m.Ate}");
                    x.Item().AlignCenter().Text(m.Status).FontColor(Colors.Green.Darken2).SemiBold();
                    if (m.CriadoPor is { } cp) x.Item().AlignCenter().Text("Emitido por: " + cp).FontSize(8);
                });
            });
            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void Corpo(IContainer c, MedicaoPdfModel m)
    {
        c.Column(col =>
        {
            col.Spacing(10);

            // Resumo financeiro
            col.Item().Row(r =>
            {
                r.RelativeItem().Element(x => Bloco(x, "Valor de contrato", Brl(m.ValorContrato)));
                r.ConstantItem(8);
                r.RelativeItem().Element(x => Bloco(x, "Medido no período", Brl(m.ValorPeriodo)));
                r.ConstantItem(8);
                r.RelativeItem().Element(x => Bloco(x, "Medido acumulado", Brl(m.MedidoAcumulado)));
            });
            col.Item().Row(r =>
            {
                r.RelativeItem().Element(x => Bloco(x, "Avanço físico", Pct(m.PctFisico)));
                r.ConstantItem(8);
                r.RelativeItem().Element(x => Bloco(x, "Avanço financeiro", Pct(m.PctFinanceiro)));
            });

            // Itens medidos
            Tabela(col, "Itens medidos",
                new[] { ("Item (EAP)", 4f), ("% ini", 1f), ("% fim", 1f), ("Valor", 2f), ("Medido período", 2f), ("Medido acum.", 2f) },
                m.Itens.Count, i =>
                {
                    var it = m.Itens[i];
                    return new[] { it.Item, Pct(it.PctIni), Pct(it.PctFim), Brl(it.Valor), Brl(it.MedidoPeriodo), Brl(it.MedidoAcum) };
                });

            // Parcelas / vencimentos
            Tabela(col, "Faturamento — parcelas",
                new[] { ("Dias", 1f), ("Vencimento", 2f), ("%", 1f), ("Valor", 2f) },
                m.Parcelas.Count, i =>
                {
                    var p = m.Parcelas[i];
                    return new[] { p.Dias.ToString(), p.Vencimento, p.Pct.ToString("0.#", Br) + "%", Brl(p.Valor) };
                });
        });
    }

    private static void Bloco(IContainer c, string titulo, string texto) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(col =>
        {
            col.Item().Text(titulo).FontSize(8).FontColor(Colors.Grey.Darken1);
            col.Item().Text(texto).SemiBold().FontColor(Azul);
        });

    private static void Tabela(ColumnDescriptor col, string titulo, (string h, float w)[] cols, int linhas, Func<int, string[]> cel)
    {
        col.Item().Column(sec =>
        {
            sec.Item().Text(titulo).SemiBold().FontColor(Azul);
            if (linhas == 0) { sec.Item().Text("—").FontColor(Colors.Grey.Medium); return; }
            sec.Item().Table(table =>
            {
                table.ColumnsDefinition(cd => { foreach (var (_, w) in cols) cd.RelativeColumn(w); });
                table.Header(hd =>
                {
                    foreach (var (h, _) in cols)
                        hd.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(8);
                });
                for (int i = 0; i < linhas; i++)
                {
                    var vals = cel(i);
                    for (int j = 0; j < vals.Length; j++)
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(vals[j]).FontSize(8);
                }
            });
        });
    }
}
