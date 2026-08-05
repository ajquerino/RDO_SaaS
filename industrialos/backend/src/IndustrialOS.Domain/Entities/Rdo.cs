using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

public enum RdoStatus { Rascunho, Enviado, RevisaoSolicitada, EmRevisao, Aprovado }

/// <summary>Relatorio Diario de Obra — cabecalho. Secoes flexiveis em jsonb; listas como filhos.</summary>
public class Rdo : BaseEntity
{
    public Guid ObraId { get; set; }
    public int Numero { get; set; }              // sequencial por obra
    public int Revisao { get; set; }             // 0 = original
    public DateOnly Data { get; set; }
    public string? DiaSemana { get; set; }
    public string? Turno { get; set; }
    public Guid? ResponsavelUsuarioId { get; set; }

    // Secoes de conteudo livre/estruturado (jsonb) — preservam a flexibilidade do MVP.
    public string Clima { get; set; } = "{}";
    public string Jornada { get; set; } = "{}";
    public string? Ocorrencias { get; set; }
    public string Dificuldades { get; set; } = "[]";
    public string ProximoDia { get; set; } = "{}";
    public string Planejamento { get; set; } = "{}";
    public string Seguranca { get; set; } = "{}";
    public string Assinaturas { get; set; } = "{}";   // {encarregado:{nome,img}, fiscal:{nome,img}...}

    public RdoStatus Status { get; set; } = RdoStatus.Rascunho;
    public string? TokenAprovacao { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTime? AprovadoEm { get; set; }
    public string? PdfR2Key { get; set; }
    public Guid? EnviadoPor { get; set; }
    public DateTime? EnviadoEm { get; set; }

    // filhos (listas)
    public List<RdoEfetivo> Efetivo { get; set; } = [];
    public List<RdoParalisacao> Paralisacoes { get; set; } = [];
    public List<RdoRecurso> Recursos { get; set; } = [];
    public List<RdoServico> Servicos { get; set; } = [];
    public List<RdoRetrabalho> Retrabalho { get; set; } = [];
}
