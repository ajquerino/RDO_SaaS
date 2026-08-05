using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Catálogo de funções de mão de obra por empresa (Soldador, Caldeireiro, etc.).</summary>
public class FuncaoMaoObra : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Direta";   // Direta | Indireta (base de HH direto/indireto)
    public decimal? CustoHh { get; set; }                // custo hora-homem (opcional, p/ custos futuros)
    public bool Ativo { get; set; } = true;
}
