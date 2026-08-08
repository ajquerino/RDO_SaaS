using IndustrialOS.Domain.Entities;

namespace IndustrialOS.Application.Auth;

public record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiraEm);

public interface IJwtService
{
    TokenPair Gerar(Usuario usuario);
    // Retorna o usuarioId (sub) e a sessão (claim "sessao") do refresh token; (null, null) se inválido.
    (Guid? Id, Guid? Sessao) ValidarRefresh(string refreshToken);
}
