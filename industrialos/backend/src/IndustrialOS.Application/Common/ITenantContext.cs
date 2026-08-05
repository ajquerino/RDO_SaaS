namespace IndustrialOS.Application.Common;

/// <summary>Tenant corrente resolvido do JWT; usado pelo Global Query Filter do EF.</summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    void Set(Guid tenantId);
}
