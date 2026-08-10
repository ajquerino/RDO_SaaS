using System.Security.Claims;
using System.Text.Json;
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

// Import mapeável (2 passos): o front devolve as linhas + o mapeamento coluna→campo escolhido na tela.
public record MapeamentoDto(int Descricao, int? Unidade, int? Qtd, int? Hh, int? Valor, int? Disciplina, int? Inicio, int? Fim);
public record AplicarImportRequest(List<List<string>> Linhas, MapeamentoDto Mapeamento);

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
        db.EventosDominio.Add(new EventoDominio
        {
            Tipo = "obra_criada", AgregadoTipo = "Obra", AgregadoId = obra.Id,
            Payload = JsonSerializer.Serialize(new { obra.Nome, obra.Contrato })
        });
        await db.SaveChangesAsync();

        // Aviso (NÃO bloqueia): já contando a obra recém-criada vs o limite do plano.
        var uso = await Common.UsoPlanoCalc.CalcularAsync(db);
        var aviso = Common.UsoPlanoCalc.AvisoObras(uso.NObras, uso.LimiteObras, uso.Plano);
        return CreatedAtAction(nameof(Obter), new { id = obra.Id }, new { obra, aviso });
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

    [HttpPut("{id:guid}/itens/{itemId:guid}")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> EditarItem(Guid id, Guid itemId, [FromBody] ItemRequest r)
    {
        var item = await db.ObraItens.FirstOrDefaultAsync(i => i.Id == itemId && i.ObraId == id);
        if (item is null) return NotFound();

        item.Descricao = r.Descricao;
        item.Unidade = r.Unidade;
        item.QtdPrevista = r.QtdPrevista;
        item.HhPrevisto = r.HhPrevisto;
        item.Disciplina = r.Disciplina;
        item.DataInicio = r.DataInicio;
        item.DataFim = r.DataFim;
        // Valor só é alterado por quem pode ver R$ — evita zerar o valor sem querer para os demais papéis.
        if (PodeVerValores) item.Valor = r.Valor;

        await db.SaveChangesAsync();
        if (!PodeVerValores) item.Valor = null; // não vaza R$ na resposta
        return Ok(item);
    }

    [HttpDelete("{id:guid}/itens/{itemId:guid}")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> ExcluirItem(Guid id, Guid itemId)
    {
        var item = await db.ObraItens.FirstOrDefaultAsync(i => i.Id == itemId && i.ObraId == id);
        if (item is null) return NotFound();

        // ObraItem é BaseEntity: soft delete (DeletadoEm) — some da EAP pelo filtro global, sem quebrar
        // referências de RDO/Medição (ObraItemId) e reversível pelo banco.
        item.DeletadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
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

    // ---- Import mapeável (2 passos): analisar → apontar colunas na tela → aplicar ----

    // Passo 1: sobe QUALQUER planilha e devolve colunas + amostra + TODAS as linhas (o front reenvia no aplicar,
    // evitando reupload) + uma sugestão de mapeamento pelos aliases conhecidos.
    [HttpPost("{id:guid}/itens/importar/analisar")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> ImportarAnalisar(Guid id, IFormFile file)
    {
        if (await db.Obras.FindAsync(id) is null) return NotFound();
        if (file is null || file.Length == 0) return BadRequest(new { erro = "Arquivo vazio." });

        await using var stream = file.OpenReadStream();
        var a = import.Analisar(stream, file.FileName);
        if (a.Colunas.Count == 0) return BadRequest(new { erro = "Não foi possível ler as colunas do arquivo." });

        var s = import.Sugerir(a.Colunas);
        return Ok(new
        {
            colunas = a.Colunas,
            amostra = a.Linhas.Take(8),        // primeiras linhas p/ conferência
            totalLinhas = a.Linhas.Count,
            linhas = a.Linhas,                 // TODAS: o front devolve no aplicar (sem reupload/estado no server)
            sugestao = new
            {
                descricao = s.Descricao >= 0 ? s.Descricao : (int?)null,
                unidade = s.Unidade, qtd = s.Qtd, hh = s.Hh, valor = s.Valor,
                disciplina = s.Disciplina, inicio = s.Inicio, fim = s.Fim
            }
        });
    }

    // Passo 2: aplica o mapeamento escolhido e insere os itens (Ordem seguindo o MAX atual, igual ao importar).
    [HttpPost("{id:guid}/itens/importar/aplicar")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> ImportarAplicar(Guid id, [FromBody] AplicarImportRequest req)
    {
        if (await db.Obras.FindAsync(id) is null) return NotFound();
        if (req?.Mapeamento is null || req.Mapeamento.Descricao < 0)
            return BadRequest(new { erro = "Escolha a coluna de Descrição." });

        var linhas = (req.Linhas ?? []).Select(r => (IReadOnlyList<string>)r).ToList();
        var map = new MapeamentoColunas(
            req.Mapeamento.Descricao, req.Mapeamento.Unidade, req.Mapeamento.Qtd, req.Mapeamento.Hh,
            req.Mapeamento.Valor, req.Mapeamento.Disciplina, req.Mapeamento.Inicio, req.Mapeamento.Fim);

        var itens = import.Mapear(linhas, map);
        if (itens.Count == 0) return BadRequest(new { erro = "Nenhum item reconhecido." });

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
