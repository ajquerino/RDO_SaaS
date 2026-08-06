using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Outbox de eventos de domínio (append-only). Gravado na MESMA transação dos momentos-chave
/// de negócio. Base para IA/integrações futuras: um worker consumirá os não-processados — aqui só persistimos.</summary>
public class EventoDominio : BaseEntity
{
    // rdo_finalizado | rdo_aprovado | revisao_solicitada | medicao_emitida | obra_criada
    public string Tipo { get; set; } = "";
    public string AgregadoTipo { get; set; } = "";   // Rdo | Medicao | Obra
    public Guid AgregadoId { get; set; }
    public string Payload { get; set; } = "{}";       // jsonb: resumo mínimo do que aconteceu
    public bool Processado { get; set; }
    public DateTime? ProcessadoEm { get; set; }
}
