using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Empresa assinante do SaaS (o inquilino). Nao carrega TenantId proprio.</summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string Plano { get; set; } = "trial";
    public Guid? PlanoId { get; set; }                 // vínculo opcional ao catálogo de Planos (Sprint 9)
    public string Status { get; set; } = "ativo";      // ativo | suspenso (| sistema)
    public bool EhSistema { get; set; }                // tenant reservado da plataforma (dono do SaaS)
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
