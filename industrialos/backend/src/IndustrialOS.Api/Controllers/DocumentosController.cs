using System.Security.Claims;
using IndustrialOS.Application.Storage;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

/// <summary>Documentos da obra (projeto/desenho/procedimento/arquivo) no R2 — padrão de RdoMidia.
/// Ver = quem enxerga a obra; gerir (upload/apagar) = Planejador/Gestor/Admin.</summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class DocumentosController(AppDbContext db, IStorage storage) : ControllerBase
{
    private bool VeTodasObras => User.IsInRole("Gestor") || User.IsInRole("Admin") || User.IsInRole("Planejador");
    private Guid UsuarioId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private async Task<bool> PodeVerObra(Guid obraId)
    {
        if (await db.Obras.FindAsync(obraId) is null) return false;
        if (VeTodasObras) return true;
        return await db.UsuarioObras.AnyAsync(x => x.ObraId == obraId && x.UsuarioId == UsuarioId);
    }

    [HttpGet("obras/{obraId:guid}/documentos")]
    public async Task<IActionResult> Listar(Guid obraId)
    {
        if (!await PodeVerObra(obraId)) return NotFound();
        var docs = await db.Documentos.Where(d => d.ObraId == obraId).OrderByDescending(d => d.CriadoEm).ToListAsync();
        var res = new List<object>();
        foreach (var d in docs)
            res.Add(new { d.Id, d.Tipo, d.Nome, d.Versao, d.TamanhoBytes, d.CriadoEm, Url = await storage.UrlAssinadaAsync(d.R2Key, TimeSpan.FromHours(1)) });
        return Ok(res);
    }

    [HttpPost("obras/{obraId:guid}/documentos")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    [RequestSizeLimit(100_000_000)] // 100 MB
    public async Task<IActionResult> Subir(Guid obraId, IFormFile file, [FromForm] string? tipo, [FromForm] string? versao)
    {
        if (!await PodeVerObra(obraId)) return NotFound();
        if (file is null || file.Length == 0) return BadRequest(new { erro = "Arquivo vazio." });

        var ext = Path.GetExtension(file.FileName);
        var key = $"obras/{obraId}/docs/{Guid.NewGuid():N}{ext}";
        await using (var s = file.OpenReadStream())
            await storage.UploadAsync(s, key, string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);

        var doc = new Documento
        {
            ObraId = obraId,
            Tipo = string.IsNullOrWhiteSpace(tipo) ? "arquivo" : tipo,
            Nome = Path.GetFileName(file.FileName),
            R2Key = key, Versao = versao, TamanhoBytes = file.Length,
        };
        db.Documentos.Add(doc);
        await db.SaveChangesAsync();
        return Ok(new { doc.Id, doc.Tipo, doc.Nome });
    }

    [HttpDelete("documentos/{id:guid}")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Apagar(Guid id)
    {
        var doc = await db.Documentos.FindAsync(id);
        if (doc is null || !await PodeVerObra(doc.ObraId)) return NotFound();

        await storage.DeleteAsync(doc.R2Key);
        db.Documentos.Remove(doc);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
