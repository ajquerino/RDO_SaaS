using System.Security.Cryptography;
using System.Text;
using IndustrialOS.Api.Common;
using IndustrialOS.Application.Auth;
using IndustrialOS.Application.Email;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IndustrialOS.Api.Controllers;

public record EsqueciSenhaRequest(string Email);
public record RedefinirSenhaRequest(string Token, string NovaSenha);

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")] // 10 req/min por IP em login/refresh/esqueci-senha/redefinir-senha
public class AuthController(AppDbContext db, IPasswordHasher hasher, IJwtService jwt, IEmailSender email, IConfiguration cfg, SessaoService sessoes) : ControllerBase
{
    /// <summary>Login por e-mail OU nome + senha. Pre-tenant: ignora o filtro de tenant.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var chave = req.EmailOuNome.Trim();
        var user = await db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Ativo && u.DeletadoEm == null &&
                (u.Email == chave || u.Nome == chave));

        if (user is null || !hasher.Verify(req.Senha, user.SenhaHash))
            return Unauthorized(new { erro = "Credenciais invalidas." });

        // Empresa-cliente excluída/suspensa: bloqueia o login dos seus usuários (não afeta o super-admin).
        if (user.Funcao != Funcao.SuperAdmin)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);
            if (tenant is null || tenant.DeletadoEm != null)
                return StatusCode(403, new { erro = "Empresa não encontrada. Contate o suporte." });
            if (tenant.Status == "suspenso")
                return StatusCode(403, new { erro = "Empresa suspensa. Contate o suporte." });
        }

        // 1 sessão por usuário: novo login gera nova sessão (derruba o dispositivo anterior).
        user.SessaoAtual = Guid.NewGuid();
        user.UltimoLogin = DateTime.UtcNow;
        await db.SaveChangesAsync();
        sessoes.Atualizar(user.Id, user.SessaoAtual); // kick imediato do dispositivo antigo

        var tokens = jwt.Gerar(user); // lê user.SessaoAtual já atualizado
        return Ok(new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiraEm,
            new UsuarioDto(user.Id, user.Nome, user.Email, user.Funcao.ToString())));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var (id, sessaoToken) = jwt.ValidarRefresh(req.RefreshToken);
        if (id is null) return Unauthorized(new { erro = "Refresh token invalido." });

        var user = await db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.Ativo && u.DeletadoEm == null);
        if (user is null) return Unauthorized();

        // A sessão do token precisa bater com a sessão vigente; senão, foi derrubada por outro login.
        if (user.SessaoAtual is null || user.SessaoAtual != sessaoToken)
            return Unauthorized(new { erro = "Sessão encerrada em outro dispositivo.", code = "sessao_encerrada" });

        // Refresh NÃO cria sessão nova — regenera mantendo a mesma SessaoAtual.
        var tokens = jwt.Gerar(user);
        return Ok(new { tokens.AccessToken, tokens.RefreshToken, tokens.ExpiraEm });
    }

    /// <summary>Solicita redefinição de senha. Responde SEMPRE 200 neutro (não revela se o e-mail existe).</summary>
    [HttpPost("esqueci-senha")]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest req)
    {
        const string neutra = "Se o e-mail existir, enviamos as instruções de redefinição.";
        var alvo = req.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(alvo)) return Ok(new { mensagem = neutra });

        var user = await db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == alvo && u.Ativo && u.DeletadoEm == null);
        if (user is not null)
        {
            var token = GerarToken();
            db.RedefinicoesSenha.Add(new RedefinicaoSenha
            {
                UsuarioId = user.Id,
                TokenHash = Hash(token),
                ExpiraEm = DateTime.UtcNow.AddHours(1),
            });
            await db.SaveChangesAsync();

            var baseUrl = (cfg["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var link = $"{baseUrl}/redefinir-senha/{token}";
            var html = $"""
                <p>Recebemos um pedido para redefinir sua senha no IndustrialOS.</p>
                <p><a href="{link}">Clique aqui para criar uma nova senha</a> — o link expira em 1 hora.</p>
                <p>Se não foi você, ignore este e-mail; nada muda.</p>
                """;
            // Falha de envio nunca altera a resposta (não vaza existência do e-mail).
            try { await email.EnviarAsync(user.Email!, "Redefinição de senha — IndustrialOS", html); }
            catch { /* logado no EmailSender */ }
        }
        return Ok(new { mensagem = neutra });
    }

    /// <summary>Redefine a senha a partir do token (uso único, 1h). Invalida os demais tokens do usuário.</summary>
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NovaSenha) || req.NovaSenha.Length < 6)
            return BadRequest(new { erro = "Informe o token e uma senha com ao menos 6 caracteres." });

        var hash = Hash(req.Token.Trim());
        var pedido = await db.RedefinicoesSenha.FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (pedido is null || pedido.UsadoEm != null || pedido.ExpiraEm < DateTime.UtcNow)
            return BadRequest(new { erro = "Link inválido ou expirado." });

        var user = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == pedido.UsuarioId);
        if (user is null) return BadRequest(new { erro = "Link inválido ou expirado." });

        user.SenhaHash = hasher.Hash(req.NovaSenha);
        pedido.UsadoEm = DateTime.UtcNow;

        // Invalida quaisquer outros tokens ativos do mesmo usuário.
        await db.RedefinicoesSenha
            .Where(x => x.UsuarioId == user.Id && x.UsadoEm == null && x.Id != pedido.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsadoEm, DateTime.UtcNow));

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Senha redefinida com sucesso." });
    }

    // Token aleatório de 32 bytes em base64url (só o hash é persistido).
    private static string GerarToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
