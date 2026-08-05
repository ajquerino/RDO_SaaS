using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Item da EAP/escopo (linha do cronograma). Base do avanço fisico e da medicao.</summary>
public class ObraItem : BaseEntity
{
    public Guid ObraId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal? QtdPrevista { get; set; }
    public decimal? HhPrevisto { get; set; }
    public decimal? Valor { get; set; }
    public string? Disciplina { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public int Ordem { get; set; }
}
