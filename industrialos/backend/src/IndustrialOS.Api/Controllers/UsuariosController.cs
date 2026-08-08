using System.Security.Claims;
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

        // Aviso (NÃO bloqueia): já contando o usuário recém-criado vs o limite do plano.
        var uso = await Common.UsoPlanoCalc.CalcularAsync(db);
        var aviso = Common.UsoPlanoCalc.AvisoUsuarios(uso.NUsuarios, uso.LimiteUsuarios, uso.Plano);
        return CreatedAtAction(nameof(Listar), new { id = user.Id },
            new { usuario = new UsuarioDto(user.Id, user.Nome, user.Email, user.Funcao.ToString()), aviso });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Gestor,Admin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var meuId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var uid) ? uid : Guid.Empty;
        if (id == meuId)
            return BadRequest(new { erro = "Você não pode excluir seu próprio usuário." });

        // Filtro global já garante: só usuários da própria empresa e não deletados.
        var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (user.Funcao == Funcao.SuperAdmin)
            return BadRequest(new { erro = "Não é possível excluir esse usuário." });

        user.DeletadoEm = DateTime.UtcNow; // soft delete (Usuario é BaseEntity)
        await db.SaveChangesAsync();
        return NoContent();
    }
}
