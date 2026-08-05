namespace IndustrialOS.Application.Auth;

public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verify(string senha, string hash);
}
