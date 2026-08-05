using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Catálogo de equipamentos por tenant (guindaste, maçarico, etc.). Usado no RDO (Recursos).</summary>
public class Equipamento : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string ProprioLocado { get; set; } = "proprio"; // proprio | locado
    public decimal? Horimetro { get; set; }
    public decimal? CustoHora { get; set; }                 // custo/hora (opcional, p/ custos futuros)
    public string? StatusManutencao { get; set; }
    public bool Ativo { get; set; } = true;
}
