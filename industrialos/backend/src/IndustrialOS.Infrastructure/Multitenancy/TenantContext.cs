using IndustrialOS.Application.Common;

namespace IndustrialOS.Infrastructure.Multitenancy;

/// <summary>Escopo por request; preenchido pelo TenantMiddleware a partir do claim JWT.</summary>
public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public void Set(Guid tenantId) => TenantId = tenantId;
}
