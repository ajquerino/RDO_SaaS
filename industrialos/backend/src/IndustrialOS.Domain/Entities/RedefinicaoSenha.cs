namespace IndustrialOS.Domain.Entities;

/// <summary>Token de redefinição de senha — uso único, expira em 1h. Guarda só o HASH (SHA-256) do
/// token; o token em claro só existe no link do e-mail. Top-level (estilo Plano), sem filtro de tenant.</summary>
public class RedefinicaoSenha
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public DateTime? UsadoEm { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
