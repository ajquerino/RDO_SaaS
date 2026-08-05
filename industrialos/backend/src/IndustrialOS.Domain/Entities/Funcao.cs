namespace IndustrialOS.Domain.Entities;

/// <summary>Papeis internos (RBAC). Fiscal/Cliente e externo (sem login, so token).</summary>
public enum Funcao
{
    Encarregado,
    Lider,
    Supervisor,
    Planejador,
    Gestor,
    Admin
}
