using IndustrialOS.Application.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IndustrialOS.Infrastructure.Pdf;

public class RdoPdf : IRdoPdf
{
    static RdoPdf() => QuestPDF.Settings.License = LicenseType.Community;

    private static readonly string Azul = Colors.Blue.Darken2;

    public byte[] Gerar(RdoPdfModel m)
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

    private static void Cabecalho(IContainer c, RdoPdfModel m)
    {
        c.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Column(x =>
                {
                    x.Item().Text("RELATÓRIO DIÁRIO DE OBRA").FontSize(15).Bold().FontColor(Azul);
                    x.Item().Text(m.ObraNome).FontSize(11).SemiBold();
                    if (m.Cliente is { } cli) x.Item().Text("Cliente: " + cli);
                    if (m.Contrato is { } ct) x.Item().Text("Contrato: " + ct);
                    if (m.Local is { } lo) x.Item().Text("Local: " + lo);
                });
                r.ConstantItem(150).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(x =>
                {
                    x.Item().AlignCenter().Text($"RDO Nº {m.Numero}" + (m.Revisao > 0 ? $" · rev.{m.Revisao}" : "")).Bold().FontColor(Azul);
                    x.Item().AlignCenter().Text(m.Data + (m.DiaSemana is { } d ? $" · {d}" : ""));
                    x.Item().AlignCenter().Text("Turno: " + (m.Turno ?? "—"));
                    x.Item().AlignCenter().Text(m.Status).FontColor(Colors.Green.Darken2).SemiBold();
                });
            });
            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void Corpo(IContainer c, RdoPdfModel m)
    {
        c.Column(col =>
        {
            col.Spacing(10);

            col.Item().Row(r =>
            {
                r.RelativeItem().Element(x => Bloco(x, "Clima",
                    (m.ClimaCondicoes.Length > 0 ? string.Join(", ", m.ClimaCondicoes) : "—") +
                    (m.Temperatura is { } t ? $"  ·  {t}°C" : "")));
                r.ConstantItem(10);
                r.RelativeItem().Element(x => Bloco(x, "Jornada",
                    $"Início {m.JornInicio ?? "—"}  ·  Almoço {m.JornAlmoco ?? "—"}–{m.JornRetorno ?? "—"}  ·  Término {m.JornTermino ?? "—"}"));
            });

            if (m.Horas is { } hm)
            {
                var extras = new List<string>();
                if (hm.ExtraUtil is { } eu) extras.Add($"HE {eu} (após 16:48)");
                if (hm.Extra100 is { } e1) extras.Add($"100%: {e1}");
                if (hm.Extra150 is { } e2) extras.Add($"150%: {e2}");
                col.Item().Text(t =>
                {
                    t.Span($"{hm.DiaTipo} · Trabalhado {hm.Trabalhado}").SemiBold();
                    if (extras.Count > 0) t.Span("  ·  " + string.Join(" · ", extras)).FontColor(Colors.Orange.Darken2);
                });
            }

            if (m.Responsavel is { } resp)
                col.Item().Text(t => { t.Span("Responsável: ").SemiBold(); t.Span(resp); });

            // Efetivo
            Tabela(col, "Efetivo", new[] { ("Função", 3f), ("Qtd", 1f), ("Hora extra", 2f) }, m.Efetivo.Count, i =>
            {
                var e = m.Efetivo[i];
                return new[] { e.Funcao ?? "—", e.Quantidade.ToString(), e.HoraExtra ?? "—" };
            });

            // Serviços (com avanço)
            Tabela(col, "Serviços / Avanço", new[] { ("Atividade", 3f), ("Item (EAP)", 3f), ("Status", 2f), ("Qtd exec", 1.4f), ("Avanço", 1.2f) }, m.Servicos.Count, i =>
            {
                var s = m.Servicos[i];
                return new[] { s.Atividade ?? "—", s.Item ?? "(extra)", s.Status ?? "—",
                    (s.QtdExec?.ToString() ?? "—") + (s.Unidade is { } u ? " " + u : ""), s.PctItem + "%" };
            });

            // Paralisações
            Tabela(col, "Paralisações", new[] { ("Início", 1.2f), ("Fim", 1.2f), ("Motivo", 3f), ("Descrição", 4f) }, m.Paralisacoes.Count, i =>
            {
                var p = m.Paralisacoes[i];
                return new[] { p.Inicio ?? "—", p.Fim ?? "—", p.Motivo ?? "—", p.Descricao ?? "—" };
            });

            // Recursos
            Tabela(col, "Recursos / Equipamentos", new[] { ("Equipamento", 4f), ("Qtd", 1f), ("Horas", 2f) }, m.Recursos.Count, i =>
            {
                var re = m.Recursos[i];
                return new[] { re.Equipamento ?? "—", re.Quantidade.ToString(), re.Horas ?? "—" };
            });

            // Retrabalho
            Tabela(col, "Retrabalho", new[] { ("Atividade", 3f), ("Pessoas", 1f), ("Horas", 1f), ("HH", 1f), ("Causa", 2f), ("Ação corretiva", 3f) }, m.Retrabalho.Count, i =>
            {
                var rt = m.Retrabalho[i];
                var hh = rt.Horas is { } hr ? (rt.Pessoas * hr).ToString("0.##") : "—";
                return new[] { rt.Atividade ?? "—", rt.Pessoas.ToString(), rt.Horas?.ToString("0.##") ?? "—", hh, rt.Causa ?? "—", rt.AcaoCorretiva ?? "—" };
            });

            // Segurança
            if (m.Seguranca is { } sg)
            {
                string Chk(bool b) => b ? "☑" : "☐";
                col.Item().Element(x => Bloco(x, "Segurança",
                    $"{Chk(sg.Dds)} DDS   {Chk(sg.Apr)} APR   {Chk(sg.Pt)} PT   {Chk(sg.AreaIsolada)} Área isolada   {Chk(sg.Epis)} EPIs   {Chk(sg.Ferramentas)} Ferramentas" +
                    (string.IsNullOrWhiteSpace(sg.Observacoes) ? "" : $"\n{sg.Observacoes}")));
            }

            // Pendências
            Tabela(col, "Pendências", new[] { ("Pendência", 4f), ("Responsável", 2f), ("Prazo", 1.5f), ("Status", 1.5f) }, m.Pendencias.Count, i =>
            {
                var p = m.Pendencias[i];
                return new[] { p.Descricao ?? "—", p.Responsavel ?? "—", p.Prazo ?? "—", p.Status ?? "—" };
            });

            if (!string.IsNullOrWhiteSpace(m.Dificuldades))
                col.Item().Element(x => Bloco(x, "Dificuldades", m.Dificuldades!));
            if (!string.IsNullOrWhiteSpace(m.Ocorrencias))
                col.Item().Element(x => Bloco(x, "Ocorrências", m.Ocorrencias!));

            // Próximo dia
            if (m.ProximoDia is { } pd && (pd.MaoObra ?? pd.Equipamentos ?? pd.Materiais ?? pd.Ferramentas) is not null)
                col.Item().Element(x => Bloco(x, "Recursos para o próximo dia",
                    $"Mão de obra: {pd.MaoObra ?? "—"}\nEquipamentos: {pd.Equipamentos ?? "—"}\nMateriais: {pd.Materiais ?? "—"}\nFerramentas: {pd.Ferramentas ?? "—"}"));

            // Planejamento
            if (m.Planejamento is { } pl && (pl.Servicos ?? pl.Prioridades ?? pl.Areas) is not null)
                col.Item().Element(x => Bloco(x, "Planejamento do próximo dia",
                    $"Serviços: {pl.Servicos ?? "—"}\nPrioridades: {pl.Prioridades ?? "—"}\nÁreas: {pl.Areas ?? "—"}"));

            if (m.Fotos > 0)
                col.Item().Text($"📎 {m.Fotos} foto(s)/vídeo(s) anexado(s) ao RDO.").FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void Bloco(IContainer c, string titulo, string texto) =>
        c.Column(col =>
        {
            col.Item().Text(titulo).SemiBold().FontColor(Azul);
            col.Item().Text(texto);
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
