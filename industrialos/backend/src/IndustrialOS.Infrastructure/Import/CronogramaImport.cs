using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using IndustrialOS.Application.Import;

namespace IndustrialOS.Infrastructure.Import;

/// <summary>Importa EAP de .xlsx (ClosedXML) ou CSV. Cabecalho flexivel (sem acento/caixa).</summary>
public class CronogramaImport : ICronogramaImport
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public IReadOnlyList<ItemCronograma> Parse(Stream conteudo, string nomeArquivo)
    {
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        var linhas = ext is ".xlsx" or ".xlsm" ? LerXlsx(conteudo) : LerCsv(conteudo);
        if (linhas.Count == 0) return [];

        var header = linhas[0].Select(Normalizar).ToList();
        int Col(params string[] nomes) => header.FindIndex(h => nomes.Contains(h));

        int cDesc = Col("descricao", "item", "atividade", "servico");
        int cUn = Col("unidade", "un", "und");
        int cQtd = Col("qtdprevista", "qtd", "quantidade", "qtdprev");
        int cHh = Col("hhprevisto", "hh", "hhprev", "homemhora");
        int cValor = Col("valor", "preco", "custo");
        int cDisc = Col("disciplina");
        int cIni = Col("datainicio", "inicio", "dtinicio");
        int cFim = Col("datafim", "fim", "termino", "dtfim");

        var itens = new List<ItemCronograma>();
        for (int i = 1; i < linhas.Count; i++)
        {
            var r = linhas[i];
            string Get(int c) => c >= 0 && c < r.Count ? r[c]?.Trim() ?? "" : "";
            var desc = Get(cDesc);
            if (string.IsNullOrWhiteSpace(desc)) continue; // ignora linhas vazias

            itens.Add(new ItemCronograma(
                desc,
                NuloSeVazio(Get(cUn)),
                Decimal(Get(cQtd)),
                Decimal(Get(cHh)),
                Decimal(Get(cValor)),
                NuloSeVazio(Get(cDisc)),
                Data(Get(cIni)),
                Data(Get(cFim))));
        }
        return itens;
    }

    private static List<List<string>> LerXlsx(Stream s)
    {
        using var wb = new XLWorkbook(s);
        var ws = wb.Worksheets.First();
        var linhas = new List<List<string>>();
        foreach (var row in ws.RangeUsed()?.RowsUsed() ?? Enumerable.Empty<IXLRangeRow>())
            linhas.Add(row.Cells().Select(c => c.GetFormattedString()).ToList());
        return linhas;
    }

    private static List<List<string>> LerCsv(Stream s)
    {
        using var sr = new StreamReader(s, Encoding.UTF8);
        var texto = sr.ReadToEnd();
        var sep = texto.Contains(';') ? ';' : ','; // pt-BR costuma usar ';'
        return texto.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Split(sep).ToList()).ToList();
    }

    private static string Normalizar(string s)
    {
        var semAcento = new string(s.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return new string(semAcento.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }

    private static string? NuloSeVazio(string v) => string.IsNullOrWhiteSpace(v) ? null : v;

    private static decimal? Decimal(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        v = v.Replace("R$", "").Trim();
        if (decimal.TryParse(v, NumberStyles.Any, PtBr, out var d)) return d;
        if (decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
        return null;
    }

    private static DateOnly? Data(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        string[] fmts = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy"];
        if (DateOnly.TryParseExact(v.Trim(), fmts, PtBr, DateTimeStyles.None, out var d)) return d;
        if (DateOnly.TryParse(v.Trim(), PtBr, DateTimeStyles.None, out d)) return d;
        return null;
    }
}
