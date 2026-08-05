namespace IndustrialOS.Domain.Common;

/// <summary>Base de toda entidade de negocio: PK uuid + multi-tenant + timestamps.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? DeletadoEm { get; set; } // soft delete opcional
}
