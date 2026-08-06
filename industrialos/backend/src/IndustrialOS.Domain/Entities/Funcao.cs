namespace IndustrialOS.Domain.Entities;

/// <summary>
/// Papeis. Encarregado..Admin sao internos da EMPRESA-CLIENTE (tenant). Fiscal/Cliente e externo (so token).
/// SuperAdmin e o DONO DA PLATAFORMA (SaaS) — acima do Admin do tenant: gerencia planos e cria novos tenants.
/// </summary>
public enum Funcao
{
    Encarregado,
    Lider,
    Supervisor,
    Planejador,
    Gestor,
    Admin,
    SuperAdmin
}
