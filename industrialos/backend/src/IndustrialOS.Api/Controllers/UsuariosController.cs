using IndustrialOS.Application.Auth;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record CriarUsuarioRequest(string Nome, string? Email, string Senha, string Funcao);

[ApiController]
[Route("api/v1/usuarios")]
[Authorize]
public class UsuariosController(AppDbContext db, IPasswordHasher hasher) : ControllerBase
{
    // Leitura: gestão + planejador (para o vínculo de obra). Cadastro: só Gestor/Admin.
    [HttpGet]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Listar() =>
        Ok(await db.Usuarios
            .OrderBy(u => u.Nome)
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Email, u.Funcao.ToString()))
            .ToListAsync());

    [HttpPost]
    [Authorize(Roles = "Gestor,Admin")]
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest req)
    {
        if (!Enum.TryParse<Funcao>(req.Funcao, ignoreCase: true, out var funcao))
            return BadRequest(new { erro = "Funcao invalida." });

        var user = new Usuario
        {
            Nome = req.Nome,
            Email = req.Email,
            SenhaHash = hasher.Hash(req.Senha),
            Funcao = funcao
        }; // TenantId e carimbado no SaveChanges a partir do token

        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), new { id = user.Id },
            new UsuarioDto(user.Id, user.Nome, user.Email, user.Funcao.ToString()));
    }
}
