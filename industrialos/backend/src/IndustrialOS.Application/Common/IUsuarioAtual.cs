namespace IndustrialOS.Application.Common;

/// <summary>Usuário autenticado do request corrente (lido do JWT). Usado pela auditoria.</summary>
public interface IUsuarioAtual
{
    Guid? UsuarioId { get; }
}
