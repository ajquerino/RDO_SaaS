namespace IndustrialOS.Domain.Entities;

/// <summary>Plano de assinatura do SaaS (catálogo global — não é BaseEntity, não tem TenantId).
/// Apenas o MODELO: nenhuma cobrança/gateway está implementada (ver TODO em PlanosController).</summary>
public class Plano
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = "";
    public int? LimiteObras { get; set; }
    public int? LimiteUsuarios { get; set; }
    public decimal? PrecoMensal { get; set; }
}
