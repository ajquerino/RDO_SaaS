namespace IndustrialOS.Application.Ia;

/// <summary>Serviço de um item da EAP dentro do contexto do RDO (avanço em %, 0..100).</summary>
public record RdoServicoContexto(string? Atividade, string? Item, string? Status, int PctAvanco);

/// <summary>Dados NORMALIZADOS de um RDO — o "input" que uma IA (ou o stub por regras) consome.
/// Independente das entidades do domínio; montado pelo <c>RdoContextoBuilder</c>.</summary>
public record RdoContexto(
    string ObraNome,
    string? Contrato,
    string Data,                 // dd/MM/yyyy
    string DiaTipo,              // Útil | Sábado | Domingo | Feriado
    int EfetivoTotal,
    int EfetivoDireto,
    int EfetivoIndireto,
    IReadOnlyList<KeyValuePair<string, int>> EfetivoPorFuncao,
    decimal HorasDia,            // horas trabalhadas na jornada
    decimal HhDia,               // efetivo total × horas
    IReadOnlyList<RdoServicoContexto> Servicos,
    int Paralisacoes,
    int ParalisacoesMin,
    decimal RetrabalhoHh,
    IReadOnlyList<string> Clima,
    int? Temperatura,
    string? Ocorrencias);

/// <summary>Resultado do resumo. <c>Origem</c> = "regras" (stub determinístico) | "ia" (LLM, futuro).</summary>
public record ResumoResultado(string Texto, string Origem);

/// <summary>Abstração para gerar o resumo de um RDO. Hoje há só o stub por regras (sem rede/sem IA).
/// Para ligar IA de verdade, criar uma implementação LLM e trocar o registro no DI — o contrato já está pronto.</summary>
public interface IResumoIa
{
    Task<ResumoResultado> ResumirRdoAsync(RdoContexto ctx, CancellationToken ct = default);
}
