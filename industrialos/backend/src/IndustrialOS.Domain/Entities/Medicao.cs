using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Boletim de Medicao (BM) — cabecalho. Numero sequencial por obra, itens e parcelas como filhos owned.</summary>
public class Medicao : BaseEntity
{
    public Guid ObraId { get; set; }
    public Guid? FaturamentoEventoId { get; set; }
    public int Numero { get; set; }                 // sequencial por obra (UNIQUE ObraId,Numero)
    public DateOnly De { get; set; }
    public DateOnly Ate { get; set; }
    public decimal ValorContrato { get; set; }
    public decimal MedidoAcumulado { get; set; }
    public decimal ValorPeriodo { get; set; }
    public decimal PctFisico { get; set; }          // avanco fisico ponderado por valor (0..1)
    public decimal PctFinanceiro { get; set; }      // medido_acumulado / valor_contrato (0..1)
    public string? PdfR2Key { get; set; }
    public string Status { get; set; } = "emitido"; // emitido | aprovado
    public string? TokenAprovacao { get; set; }
    public Guid? CriadoPor { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTime? AprovadoEm { get; set; }

    // filhos owned (SEM Id = Guid.NewGuid() no construtor — EF gera o Id)
    public List<MedicaoItem> Itens { get; set; } = [];
    public List<MedicaoParcela> Parcelas { get; set; } = [];
}

/// <summary>Linha do boletim por item da EAP — %ini (ultima medicao) -> %fim (avanco atual).</summary>
public class MedicaoItem
{
    public Guid Id { get; set; }
    public Guid MedicaoId { get; set; }
    public Guid ObraItemId { get; set; }
    public decimal PctIni { get; set; }             // 0..1
    public decimal PctFim { get; set; }             // 0..1
    public decimal Valor { get; set; }              // valor do item no contrato
    public decimal MedidoPeriodo { get; set; }      // (pctFim - pctIni) * valor
    public decimal MedidoAcum { get; set; }         // pctFim * valor
}

/// <summary>Parcela do faturamento da medicao — derivada da condicao de pagamento.</summary>
public class MedicaoParcela
{
    public Guid Id { get; set; }
    public Guid MedicaoId { get; set; }
    public int Dias { get; set; }
    public DateOnly Vencimento { get; set; }
    public decimal Valor { get; set; }
    public decimal Pct { get; set; }
}
