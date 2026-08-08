using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Usuario interno autenticado por senha/JWT (PIN e atalho de campo, ver Arquitetura).</summary>
public class Usuario : BaseEntity
{
    public Guid? EmpresaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }        // unico por tenant
    public string SenhaHash { get; set; } = string.Empty;
    public Funcao Funcao { get; set; } = Funcao.Encarregado;
    public bool Ativo { get; set; } = true;
    public DateTime? UltimoLogin { get; set; }

    /// <summary>Sessão ativa atual. Cada login gera um novo Guid; tokens com sessão diferente são
    /// rejeitados (1 dispositivo por usuário — login novo derruba o anterior).</summary>
    public Guid? SessaoAtual { get; set; }

    // LGPD (Sprint 9)
    public bool ConsentimentoLgpd { get; set; }
    public DateTime? ConsentimentoEm { get; set; }
}
