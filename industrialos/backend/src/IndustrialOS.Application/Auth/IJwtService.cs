using IndustrialOS.Domain.Entities;

namespace IndustrialOS.Application.Auth;

public record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiraEm);

public interface IJwtService
{
    TokenPair Gerar(Usuario usuario);
    Guid? ValidarRefresh(string refreshToken); // retorna o usuarioId se valido
}
