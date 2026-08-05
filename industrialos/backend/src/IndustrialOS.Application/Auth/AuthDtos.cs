namespace IndustrialOS.Application.Auth;

public record LoginRequest(string EmailOuNome, string Senha);
public record RefreshRequest(string RefreshToken);
public record UsuarioDto(Guid Id, string Nome, string? Email, string Funcao);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiraEm, UsuarioDto Usuario);
