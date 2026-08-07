namespace IndustrialOS.Domain.Entities;

/// <summary>Assinatura de um tenant (UMA por empresa). Estado de cobrança — não é BaseEntity, não
/// entra no Global Query Filter (igual Tenant/Plano). Hoje a data de vencimento é gerenciada
/// manualmente pelo super-admin; no futuro virá de um gateway BR (Asaas/Iugu/etc.), por isso os
/// campos de provedor são nullable.</summary>
public class Assinatura
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? PlanoId { get; set; }
    public DateOnly? VencimentoEm { get; set; }
    public DateOnly? TrialAte { get; set; }
    public bool Cancelada { get; set; } = false;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Futuro (gateway de pagamento) — preenchidos ao integrar o provedor.
    public string? ProvedorNome { get; set; }
    public string? ProvedorCustomerId { get; set; }
    public string? ProvedorAssinaturaId { get; set; }
}
