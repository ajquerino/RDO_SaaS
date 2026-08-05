using System.Text.Json;

namespace IndustrialOS.Domain.Services;

/// <summary>Regras de medicao/faturamento (fonte unica). Cf. MODELO_DADOS_POSTGRES.md e CASOS_DE_TESTE T19/T20.</summary>
public static class MedicaoCalculo
{
    public record ParcelaDef(int Dias, decimal? Pct);
    public record ParcelaCalc(int Dias, DateOnly Vencimento, decimal Valor, decimal Pct);

    // medido no periodo do item = (pctFim - pctIni) * valor.  0..1 nos percentuais.
    public static decimal MedidoPeriodo(decimal pctIni, decimal pctFim, decimal valor) => (pctFim - pctIni) * valor;
    // medido acumulado do item = pctFim * valor.
    public static decimal MedidoAcum(decimal pctFim, decimal valor) => pctFim * valor;
    // pct financeiro da obra = medido acumulado / valor de contrato.
    public static decimal PctFinanceiro(decimal medidoAcumulado, decimal valorContrato) =>
        valorContrato > 0 ? medidoAcumulado / valorContrato : 0m;

    /// <summary>Le a lista [{dias,pct}] guardada em jsonb na condicao de pagamento.</summary>
    public static List<ParcelaDef> LerCondicao(string? parcelasJson)
    {
        var defs = new List<ParcelaDef>();
        if (string.IsNullOrWhiteSpace(parcelasJson)) return defs;
        try
        {
            using var doc = JsonDocument.Parse(parcelasJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return defs;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                int dias = e.TryGetProperty("dias", out var d) && d.ValueKind == JsonValueKind.Number ? d.GetInt32() : 0;
                decimal? pct = e.TryGetProperty("pct", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : null;
                defs.Add(new ParcelaDef(dias, pct));
            }
        }
        catch (JsonException) { /* condicao malformada -> tratado como sem parcelas */ }
        return defs;
    }

    /// <summary>Gera as parcelas de uma medicao a partir da condicao de pagamento.
    /// Vencimento = dataBase (fim do periodo) + dias. Se pct omitido, divide o restante igualmente.
    /// Sem condicao definida => 1 parcela a vista (100%) na dataBase.</summary>
    public static List<ParcelaCalc> Parcelas(IReadOnlyList<ParcelaDef> defs, decimal valorTotal, DateOnly dataBase)
    {
        if (defs.Count == 0)
            return [new ParcelaCalc(0, dataBase, valorTotal, 100m)];

        // resolve os percentuais: os que vierem nulos dividem igualmente o que sobra de 100%.
        decimal somaInformada = defs.Where(x => x.Pct is not null).Sum(x => x.Pct!.Value);
        int semPct = defs.Count(x => x.Pct is null);
        decimal pctPadrao = semPct > 0 ? Math.Max(0m, 100m - somaInformada) / semPct : 0m;

        var pcts = defs.Select(x => x.Pct ?? pctPadrao).ToList();

        var res = new List<ParcelaCalc>();
        decimal acumValor = 0m;
        for (int i = 0; i < defs.Count; i++)
        {
            // ultima parcela absorve o arredondamento para fechar exatamente no valorTotal.
            decimal valor = i == defs.Count - 1
                ? Math.Round(valorTotal - acumValor, 2)
                : Math.Round(valorTotal * pcts[i] / 100m, 2);
            acumValor += valor;
            res.Add(new ParcelaCalc(defs[i].Dias, dataBase.AddDays(defs[i].Dias), valor, Math.Round(pcts[i], 4)));
        }
        return res;
    }
}
