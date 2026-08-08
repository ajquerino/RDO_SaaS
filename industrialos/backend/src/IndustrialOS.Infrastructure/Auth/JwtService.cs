using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IndustrialOS.Application.Auth;
using IndustrialOS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IndustrialOS.Infrastructure.Auth;

public class JwtService(IConfiguration config) : IJwtService
{
    private string Issuer => config["Jwt:Issuer"]!;
    private string Audience => config["Jwt:Audience"]!;
    private byte[] Key => Encoding.UTF8.GetBytes(config["Jwt:Key"]!);

    public TokenPair Gerar(Usuario u)
    {
        var expira = DateTime.UtcNow.AddHours(8);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha256);
        var sessao = u.SessaoAtual?.ToString() ?? "";
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim("tenant_id", u.TenantId.ToString()),
            new Claim("funcao", u.Funcao.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, u.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Name, u.Nome),
            new Claim("sessao", sessao)   // 1 sessão por usuário: token com sessão diferente é rejeitado
        };
        var jwt = new JwtSecurityToken(Issuer, Audience, claims,
            expires: expira, signingCredentials: creds);
        var access = new JwtSecurityTokenHandler().WriteToken(jwt);

        // Refresh: JWT longo assinado, com o usuarioId no sub, escopo "refresh" e a sessão.
        var refreshJwt = new JwtSecurityToken(Issuer, Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()), new Claim("scope", "refresh"), new Claim("sessao", sessao)],
            expires: DateTime.UtcNow.AddDays(30), signingCredentials: creds);
        var refresh = new JwtSecurityTokenHandler().WriteToken(refreshJwt);

        return new TokenPair(access, refresh, expira);
    }

    public (Guid? Id, Guid? Sessao) ValidarRefresh(string refreshToken)
    {
        try
        {
            var p = new JwtSecurityTokenHandler().ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Key),
                ValidateLifetime = true
            }, out _);
            if (p.FindFirst("scope")?.Value != "refresh") return (null, null);
            // "sub" pode vir remapeado para NameIdentifier dependendo do inbound claim map.
            var subStr = p.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? p.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? id = Guid.TryParse(subStr, out var i) ? i : null;
            Guid? sessao = Guid.TryParse(p.FindFirst("sessao")?.Value, out var s) ? s : null;
            return (id, sessao);
        }
        catch { return (null, null); }
    }
}
