namespace IndustrialOS.Domain.Entities;

public class RdoEfetivo
{
    public Guid Id { get; set; }
    public Guid RdoId { get; set; }
    public string? Funcao { get; set; }
    public int Quantidade { get; set; }
    public string? Entrada { get; set; }
    public string? Saida { get; set; }
    public string? HoraExtra { get; set; }
    public string? Obs { get; set; }
}

public class RdoParalisacao
{
    public Guid Id { get; set; }
    public Guid RdoId { get; set; }
    public string? Inicio { get; set; }
    public string? Fim { get; set; }
    public string? Motivo { get; set; }
    public string? Descricao { get; set; }
}

public class RdoRecurso
{
    public Guid Id { get; set; }
    public Guid RdoId { get; set; }
    public string? Equipamento { get; set; }
    public int Quantidade { get; set; }
    public string? Horas { get; set; }
    public string? Obs { get; set; }
}

/// <summary>Retrabalho do dia — HH perdido, causa e ação corretiva (alimenta produtividade).</summary>
public class RdoRetrabalho
{
    public Guid Id { get; set; }
    public Guid RdoId { get; set; }
    public string? Atividade { get; set; }
    public string? Local { get; set; }
    public decimal? Quantidade { get; set; }
    public string? Unidade { get; set; }
    public int Pessoas { get; set; }
    public decimal? Horas { get; set; }
    public string? Causa { get; set; }
    public string? Origem { get; set; }
    public string? Descricao { get; set; }
    public string? AcaoCorretiva { get; set; }
}

/// <summary>Servico do dia, opcionalmente vinculado a um item da EAP, com avanco flexivel.</summary>
public class RdoServico
{
    public Guid Id { get; set; }
    public Guid RdoId { get; set; }
    public Guid? ObraItemId { get; set; }        // null = servico extra (fora do escopo)
    public string? Atividade { get; set; }
    public string? Local { get; set; }
    public decimal? QtdExec { get; set; }
    public string? Unidade { get; set; }
    public string? Status { get; set; }          // Concluido / Em andamento / Em espera / Nao iniciado
    public decimal? PctInformado { get; set; }
    public string EtapasFeitas { get; set; } = "[]"; // jsonb: etapas marcadas
    public string? MotivoHold { get; set; }
    public string? Obs { get; set; }
}
