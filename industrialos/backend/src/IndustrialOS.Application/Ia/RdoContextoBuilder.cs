using System.Text.Json;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Domain.Services;

namespace IndustrialOS.Application.Ia;

/// <summary>Monta o <see cref="RdoContexto"/> a partir das entidades — reutiliza AvancoCalculo/JornadaCalculo.
/// É o input que uma IA consumiria; por ora alimenta o resumo por regras.</summary>
public static class RdoContextoBuilder
{
    public static RdoContexto Build(Rdo rdo, Obra obra, IReadOnlyList<ObraItem> itens,
        IReadOnlyDictionary<string, string>? categoriasPorFuncao = null)
    {
        var itensPorId = itens.ToDictionary(i => i.Id);

        // Jornada / horas trabalhadas
        var jorn = Doc(rdo.Jornada);
        decimal horasDia = 0m;
        var diaTipo = FimDeSemanaOuFeriado(jorn, rdo.Data);
        if (JornadaCalculo.Calcular(Str(jorn, "inicio"), Str(jorn, "almoco"), Str(jorn, "retorno"),
                Str(jorn, "termino"), Bool(jorn, "feriado"), rdo.Data) is { } h)
        {
            horasDia = Math.Round(h.TrabalhadoMin / 60m, 2);
            diaTipo = h.DiaTipo;
        }

        // Efetivo total / por função / direto x indireto
        int efetivoTotal = rdo.Efetivo.Sum(e => e.Quantidade);
        var porFuncao = rdo.Efetivo
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Funcao) ? "(sem função)" : e.Funcao!.Trim())
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Sum(e => e.Quantidade)))
            .OrderByDescending(kv => kv.Value).ToList();

        int direto = 0, indireto = 0;
        if (categoriasPorFuncao is not null)
            foreach (var e in rdo.Efetivo)
            {
                var cat = e.Funcao is { } f && categoriasPorFuncao.TryGetValue(f.Trim().ToLowerInvariant(), out var c) ? c : null;
                if (cat == "Direta") direto += e.Quantidade;
                else if (cat == "Indireta") indireto += e.Quantidade;
            }

        // Serviços com avanço
        var servicos = rdo.Servicos.Select(s => new RdoServicoContexto(
            s.Atividade,
            s.ObraItemId is { } oid && itensPorId.TryGetValue(oid, out var it) ? it.Descricao : null,
            s.Status,
            (int)Math.Round(AvancoCalculo.PctItem(s, s.ObraItemId is { } o && itensPorId.TryGetValue(o, out var it2) ? it2 : null) * 100)))
            .ToList();

        // Paralisações
        int paralisacoes = rdo.Paralisacoes.Count;
        int paralisacoesMin = rdo.Paralisacoes.Sum(p => DiffMin(p.Inicio, p.Fim));

        // Retrabalho (HH perdido)
        decimal retrabalhoHh = rdo.Retrabalho.Sum(rt => rt.Pessoas * (rt.Horas ?? 0));

        // Clima
        var clima = Doc(rdo.Clima);
        string[] condicoes = clima.TryGetProperty("condicoes", out var cc) && cc.ValueKind == JsonValueKind.Array
            ? cc.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x != "").ToArray() : [];
        int? temp = clima.TryGetProperty("temperatura", out var tp) && tp.ValueKind == JsonValueKind.Number ? tp.GetInt32() : null;

        return new RdoContexto(
            obra.Nome, obra.Contrato, rdo.Data.ToString("dd/MM/yyyy"), diaTipo,
            efetivoTotal, direto, indireto, porFuncao,
            horasDia, Math.Round(efetivoTotal * horasDia, 1),
            servicos, paralisacoes, paralisacoesMin, retrabalhoHh,
            condicoes, temp, string.IsNullOrWhiteSpace(rdo.Ocorrencias) ? null : rdo.Ocorrencias);
    }

    // ---- helpers ----
    private static JsonElement Doc(string raw) => JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
    private static string? Str(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static bool Bool(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.True;

    private static string FimDeSemanaOuFeriado(JsonElement jorn, DateOnly data) =>
        Bool(jorn, "feriado") ? "Feriado"
        : data.DayOfWeek == DayOfWeek.Sunday ? "Domingo"
        : data.DayOfWeek == DayOfWeek.Saturday ? "Sábado" : "Útil";

    private static int DiffMin(string? ini, string? fim)
    {
        int? a = Hm(ini), b = Hm(fim);
        return a is not null && b is not null && b > a ? b.Value - a.Value : 0;
    }
    private static int? Hm(string? t)
    {
        if (string.IsNullOrWhiteSpace(t) || !System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d{1,2}:\d{2}$")) return null;
        var p = t.Split(':');
        return int.Parse(p[0]) * 60 + int.Parse(p[1]);
    }
}
