using IndustrialOS.Domain.Entities;

namespace IndustrialOS.Application.Auth;

public record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiraEm);

public interface IJwtService
{
    TokenPair Gerar(Usuario usuario);
    // Token de MODO SUPORTE: emitido pelo super-admin para agir COMO o admin de uma empresa.
    // Curto e sem claim "sessao" (não entra no enforcement de sessão única); carrega "suporte"=superAdminId.
    string GerarSuporte(Usuario admin, Guid superAdminId, TimeSpan? duracao = null);
    // Retorna o usuarioId (sub) e a sessão (claim "sessao") do refresh token; (null, null) se inválido.
    (Guid? Id, Guid? Sessao) ValidarRefresh(string refreshToken);
}
