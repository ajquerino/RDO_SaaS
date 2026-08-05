using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Regra de vencimento reutilizavel (ex.: "21/42", "21/35/42/60", "100% a vista").
/// Parcelas em jsonb: lista de {dias, pct}. Se pct omitido, divide igualmente.</summary>
public class CondicaoPagamento : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Parcelas { get; set; } = "[]"; // jsonb: [{ "dias": 21, "pct": 50 }, ...]
}

/// <summary>Plano de faturamento POR OBRA/CONTRATO — uma lista de eventos (marcos/parcelas).</summary>
public class FaturamentoPlano : BaseEntity
{
    public Guid ObraId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal ValorContrato { get; set; }
    public Guid? CondicaoPagamentoId { get; set; }   // condicao default do plano (sobrescrivivel por evento)
}

/// <summary>Marco/parcela do plano de faturamento. Tipos e bases em string (paridade com o MVP flexivel).</summary>
public class FaturamentoEvento : BaseEntity
{
    public Guid FaturamentoPlanoId { get; set; }
    // assinatura_contrato | entrada | mobilizacao | canteiro_mensal | medicao_periodica |
    // comissionamento | entrega_tecnica | entrega_final | outro
    public string Tipo { get; set; } = "outro";
    public string Base { get; set; } = "percentual";   // percentual | valor_fixo
    public decimal? Percentual { get; set; }
    public decimal? Valor { get; set; }
    public string? Gatilho { get; set; }               // data | evento | por_medicao
    public DateOnly? DataPrevista { get; set; }
    public string? Recorrencia { get; set; }           // quinzenal | mensal
    public Guid? CondicaoPagamentoId { get; set; }      // sobrescreve a condicao do plano
    public int Ordem { get; set; }
    public string? Descricao { get; set; }
}
