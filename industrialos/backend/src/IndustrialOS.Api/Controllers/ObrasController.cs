using System.Security.Claims;
using IndustrialOS.Application.Import;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record ObraRequest(Guid? ClienteId, Guid? EmpresaId, string Nome, string? Contrato,
    string? OrdemServico, string? Local, string? FrenteServico, string? ResponsavelPadrao,
    DateOnly? DataInicio, DateOnly? DataFim, string? PrazoPagamento, string? Status,
    double? Latitude, double? Longitude);

public record ItemRequest(string Descricao, string? Unidade, decimal? QtdPrevista,
    decimal? HhPrevisto, decimal? Valor, string? Disciplina, DateOnly? DataInicio, DateOnly? DataFim);

public record VincularRequest(Guid[] UsuarioIds);

[ApiController]
[Route("api/v1/obras")]
[Authorize]
public class ObrasController(AppDbContext db, ICronogramaImport import) : ControllerBase
{
    private bool EhGestor => User.IsInRole("Gestor") || User.IsInRole("Admin");
    // Planejamento/gestão veem todas as obras, os valores em R$ e todos os RDOs.
    private bool VeTodasObras => EhGestor || User.IsInRole("Planejador");
    private bool PodeVerValores => VeTodasObras;
    private Guid UsuarioId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id : Guid.Empty;

    // ENC/LID/SUP/PLAN veem so as obras vinculadas; GES/ADM veem todas.
    private IQueryable<Obra> ObrasVisiveis()
    {
        if (VeTodasObras) return db.Obras;
        var ids = db.UsuarioObras.Where(x => x.UsuarioId == UsuarioId).Select(x => x.ObraId);
        return db.Obras.Where(o => ids.Contains(o.Id));
    }

    [HttpGet]
    public async Task<IActionResult> Listar() =>
        Ok(await ObrasVisiveis().OrderBy(o => o.Nome).Select(o => new
        {
            o.Id, o.Nome, o.Contrato, o.Status, o.DataInicio, o.DataFim,
            Itens = db.ObraItens.Count(i => i.ObraId == o.Id)
        }).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id)
    {
        var obra = await ObrasVisiveis().FirstOrDefaultAsync(o => o.Id == id);
        if (obra is null) return NotFound();
        var itens = await db.ObraItens.Where(i => i.ObraId == id).OrderBy(i => i.Ordem).ToListAsync();
        if (!PodeVerValores) itens.ForEach(i => i.Valor = null); // não vaza R$ p/ campo
        return Ok(new { obra, itens });
    }

    [HttpPost]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Criar([FromBody] ObraRequest r)
    {
        var obra = Map(new Obra(), r);
        db.Obras.Add(obra);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = obra.Id }, obra);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] ObraRequest r)
    {
        var obra = await db.Obras.FindAsync(id);
        if (obra is null) return NotFound();
        Map(obra, r);
        await db.SaveChangesAsync();
        return Ok(obra);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Apagar(Guid id)
    {
        var obra = await db.Obras.FindAsync(id);
        if (obra is null) return NotFound();
        obra.DeletadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- EAP / itens ----
    [HttpGet("{id:guid}/itens")]
    public async Task<IActionResult> Itens(Guid id)
    {
        var itens = await db.ObraItens.Where(i => i.ObraId == id).OrderBy(i => i.Ordem).ToListAsync();
        if (!PodeVerValores) itens.ForEach(i => i.Valor = null);
        return Ok(itens);
    }

    [HttpPost("{id:guid}/itens")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> AdicionarItem(Guid id, [FromBody] ItemRequest r)
    {
        if (await db.Obras.FindAsync(id) is null) return NotFound();
        var ordem = (await db.ObraItens.Where(i => i.ObraId == id).MaxAsync(i => (int?)i.Ordem) ?? 0) + 1;
        var item = new ObraItem
        {
            ObraId = id, Descricao = r.Descricao, Unidade = r.Unidade, QtdPrevista = r.QtdPrevista,
            HhPrevisto = r.HhPrevisto, Valor = r.Valor, Disciplina = r.Disciplina,
            DataInicio = r.DataInicio, DataFim = r.DataFim, Ordem = ordem
        };
        db.ObraItens.Add(item);
        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("{id:guid}/itens/importar")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Importar(Guid id, IFormFile file)
    {
        if (await db.Obras.FindAsync(id) is null) return NotFound();
        if (file is null || file.Length == 0) return BadRequest(new { erro = "Arquivo vazio." });

        await using var stream = file.OpenReadStream();
        var itens = import.Parse(stream, file.FileName);
        if (itens.Count == 0) return BadRequest(new { erro = "Nenhum item reconhecido no arquivo." });

        var ordem = await db.ObraItens.Where(i => i.ObraId == id).MaxAsync(i => (int?)i.Ordem) ?? 0;
        foreach (var it in itens)
            db.ObraItens.Add(new ObraItem
            {
                ObraId = id, Descricao = it.Descricao, Unidade = it.Unidade, QtdPrevista = it.QtdPrevista,
                HhPrevisto = it.HhPrevisto, Valor = it.Valor, Disciplina = it.Disciplina,
                DataInicio = it.DataInicio, DataFim = it.DataFim, Ordem = ++ordem
            });
        await db.SaveChangesAsync();
        return Ok(new { importados = itens.Count });
    }

    // ---- vinculo usuario-obra ----
    [HttpPost("{id:guid}/usuarios")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Vincular(Guid id, [FromBody] VincularRequest r)
    {
        if (await db.Obras.FindAsync(id) is null) return NotFound();
        var existentes = await db.UsuarioObras.Where(x => x.ObraId == id).Select(x => x.UsuarioId).ToListAsync();
        foreach (var uid in r.UsuarioIds.Distinct().Where(u => !existentes.Contains(u)))
            db.UsuarioObras.Add(new UsuarioObra { ObraId = id, UsuarioId = uid });
        await db.SaveChangesAsync();
        return Ok(new { vinculados = r.UsuarioIds.Distinct().Count() });
    }

    private static Obra Map(Obra o, ObraRequest r)
    {
        o.ClienteId = r.ClienteId; o.EmpresaId = r.EmpresaId; o.Nome = r.Nome; o.Contrato = r.Contrato;
        o.OrdemServico = r.OrdemServico; o.Local = r.Local; o.FrenteServico = r.FrenteServico;
        o.ResponsavelPadrao = r.ResponsavelPadrao; o.DataInicio = r.DataInicio; o.DataFim = r.DataFim;
        o.PrazoPagamento = r.PrazoPagamento; o.Latitude = r.Latitude; o.Longitude = r.Longitude;
        if (Enum.TryParse<ObraStatus>(r.Status, true, out var st)) o.Status = st;
        return o;
    }
}
