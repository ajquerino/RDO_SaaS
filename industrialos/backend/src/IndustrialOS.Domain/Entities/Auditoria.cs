namespace IndustrialOS.Domain.Entities;

/// <summary>Registro de auditoria (quem/o quê/quando). NÃO é BaseEntity: pode ter TenantId nulo
/// (operações globais/sistema) e nunca é auditada (evita loop). Gerada no SaveChanges do AppDbContext.</summary>
public class Auditoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Acao { get; set; } = "";          // create | update | delete
    public string Entidade { get; set; } = "";       // nome do tipo (Obra, Rdo, ...)
    public Guid? EntidadeId { get; set; }
    public string Detalhe { get; set; } = "{}";      // jsonb: campos alterados (sem dados sensíveis)
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
