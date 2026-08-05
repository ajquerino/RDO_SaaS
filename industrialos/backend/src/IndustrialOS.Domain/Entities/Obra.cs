using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

public enum ObraStatus { Planejada, Andamento, Paralisada, Concluida }

public class Obra : BaseEntity
{
    public Guid? ClienteId { get; set; }
    public Guid? EmpresaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Contrato { get; set; }
    public string? OrdemServico { get; set; }
    public string? Local { get; set; }
    public string? FrenteServico { get; set; }
    public string? ResponsavelPadrao { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public string? PrazoPagamento { get; set; } // ex.: "21/42", "30/45/60/180", "a vista"
    public ObraStatus Status { get; set; } = ObraStatus.Andamento;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
