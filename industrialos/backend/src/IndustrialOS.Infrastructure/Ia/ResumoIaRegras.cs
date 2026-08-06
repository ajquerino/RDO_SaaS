using System.Globalization;
using System.Text;
using IndustrialOS.Application.Ia;

namespace IndustrialOS.Infrastructure.Ia;

/// <summary>Resumo do RDO por REGRAS (determinístico, sem IA e sem rede). Implementa a mesma interface
/// que uma futura implementação LLM usaria — basta trocar o registro no DI para ligar IA de verdade.</summary>
public class ResumoIaRegras : IResumoIa
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private static string N(decimal v) => v.ToString("0.#", Br);

    public Task<ResumoResultado> ResumirRdoAsync(RdoContexto c, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        // Cabeçalho: obra, data, tipo de dia
        sb.Append($"RDO de {c.Data}");
        if (c.DiaTipo != "Útil") sb.Append($" ({c.DiaTipo})");
        sb.Append($" — obra {c.ObraNome}");
        if (!string.IsNullOrWhiteSpace(c.Contrato)) sb.Append($" (contrato {c.Contrato})");
        sb.Append(". ");

        // Efetivo e HH
        if (c.EfetivoTotal > 0)
        {
            sb.Append($"Efetivo de {c.EfetivoTotal}");
            if (c.EfetivoDireto + c.EfetivoIndireto > 0)
                sb.Append($" ({c.EfetivoDireto} diretos, {c.EfetivoIndireto} indiretos)");
            if (c.HorasDia > 0) sb.Append($", jornada de {N(c.HorasDia)}h → {N(c.HhDia)} HH no dia");
            sb.Append(". ");
            var topFuncoes = c.EfetivoPorFuncao.Take(3).Select(kv => $"{kv.Value} {kv.Key}");
            if (c.EfetivoPorFuncao.Count > 0) sb.Append($"Funções: {string.Join(", ", topFuncoes)}");
            if (c.EfetivoPorFuncao.Count > 3) sb.Append($" e mais {c.EfetivoPorFuncao.Count - 3}");
            sb.Append(". ");
        }
        else sb.Append("Sem efetivo lançado. ");

        // Serviços / avanço
        var comAvanco = c.Servicos.Where(s => s.PctAvanco > 0).ToList();
        if (c.Servicos.Count > 0)
        {
            sb.Append($"Avanço reportado em {comAvanco.Count} de {c.Servicos.Count} serviço(s)");
            var destaque = comAvanco.OrderByDescending(s => s.PctAvanco).Take(2)
                .Select(s => $"{s.Item ?? s.Atividade ?? "serviço"} {s.PctAvanco}%");
            if (comAvanco.Count > 0) sb.Append($" — {string.Join(", ", destaque)}");
            sb.Append(". ");
        }

        // Paralisações
        if (c.Paralisacoes > 0)
            sb.Append($"{c.Paralisacoes} paralisação(ões) totalizando {c.ParalisacoesMin} min. ");

        // Retrabalho
        if (c.RetrabalhoHh > 0)
            sb.Append($"Retrabalho de {N(c.RetrabalhoHh)} HH. ");

        // Clima
        if (c.Clima.Count > 0 || c.Temperatura is not null)
        {
            sb.Append("Clima: ");
            sb.Append(c.Clima.Count > 0 ? string.Join(", ", c.Clima) : "—");
            if (c.Temperatura is { } t) sb.Append($" · {t}°C");
            sb.Append(". ");
        }

        // Ocorrências
        if (!string.IsNullOrWhiteSpace(c.Ocorrencias))
            sb.Append($"Ocorrências: {c.Ocorrencias.Trim()}. ");

        return Task.FromResult(new ResumoResultado(sb.ToString().Trim(), "regras"));
    }
}
