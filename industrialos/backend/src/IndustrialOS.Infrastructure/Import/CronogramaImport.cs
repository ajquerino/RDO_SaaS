using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using IndustrialOS.Application.Import;

namespace IndustrialOS.Infrastructure.Import;

/// <summary>Importa EAP de .xlsx (ClosedXML) ou CSV. Cabecalho flexivel (sem acento/caixa).</summary>
public class CronogramaImport : ICronogramaImport
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // Fluxo antigo (compatível): lê, sugere o mapeamento por alias e aplica — tudo de uma vez.
    public IReadOnlyList<ItemCronograma> Parse(Stream conteudo, string nomeArquivo)
    {
        var a = Analisar(conteudo, nomeArquivo);
        if (a.Colunas.Count == 0) return [];
        return Mapear(a.Linhas, Sugerir(a.Colunas));
    }

    // Passo 1: lê o arquivo → cabeçalho (bruto, p/ exibir) + linhas de dados (texto).
    public AnaliseCronograma Analisar(Stream conteudo, string nomeArquivo)
    {
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        var linhas = ext is ".xlsx" or ".xlsm" ? LerXlsx(conteudo) : LerCsv(conteudo);
        if (linhas.Count == 0) return new AnaliseCronograma([], []);

        var colunas = linhas[0].Select(h => (h ?? "").Trim()).ToList();
        var dados = linhas.Skip(1)
            .Select(r => (IReadOnlyList<string>)r.Select(c => c?.Trim() ?? "").ToList())
            .ToList();
        return new AnaliseCronograma(colunas, dados);
    }

    // Sugestão de mapeamento pelos MESMOS aliases do fluxo antigo (cabeçalho normalizado).
    public MapeamentoColunas Sugerir(IReadOnlyList<string> colunas)
    {
        var header = colunas.Select(Normalizar).ToList();
        int Col(params string[] nomes) => header.FindIndex(h => nomes.Contains(h));
        static int? Nz(int i) => i >= 0 ? i : null;

        return new MapeamentoColunas(
            Col("descricao", "item", "atividade", "servico"),          // Descricao pode vir -1 (não achado)
            Nz(Col("unidade", "un", "und")),
            Nz(Col("qtdprevista", "qtd", "quantidade", "qtdprev")),
            Nz(Col("hhprevisto", "hh", "hhprev", "homemhora")),
            Nz(Col("valor", "preco", "custo")),
            Nz(Col("disciplina")),
            Nz(Col("datainicio", "inicio", "dtinicio")),
            Nz(Col("datafim", "fim", "termino", "dtfim")));
    }

    // Passo 2: aplica o mapeamento escolhido às linhas de dados. Pula linha com Descrição vazia.
    public IReadOnlyList<ItemCronograma> Mapear(IReadOnlyList<IReadOnlyList<string>> linhas, MapeamentoColunas map)
    {
        static string Get(IReadOnlyList<string> r, int? c) =>
            c is int i && i >= 0 && i < r.Count ? r[i]?.Trim() ?? "" : "";

        var itens = new List<ItemCronograma>();
        foreach (var r in linhas)
        {
            var desc = Get(r, map.Descricao);
            if (string.IsNullOrWhiteSpace(desc)) continue;

            itens.Add(new ItemCronograma(
                desc,
                NuloSeVazio(Get(r, map.Unidade)),
                Decimal(Get(r, map.Qtd)),
                Decimal(Get(r, map.Hh)),
                Decimal(Get(r, map.Valor)),
                NuloSeVazio(Get(r, map.Disciplina)),
                Data(Get(r, map.Inicio)),
                Data(Get(r, map.Fim))));
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
