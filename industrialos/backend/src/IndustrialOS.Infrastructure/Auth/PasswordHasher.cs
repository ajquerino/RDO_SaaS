using IndustrialOS.Application.Auth;

namespace IndustrialOS.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 12);
    public bool Verify(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
