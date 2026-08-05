using System.Security.Claims;
using System.Text.Json;
using IndustrialOS.Application.Pdf;
using IndustrialOS.Application.Storage;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

// ---- DTOs de entrada ----
public record EfetivoDto(string? Funcao, int Quantidade, string? Entrada, string? Saida, string? HoraExtra, string? Obs);
public record ParalisacaoDto(string? Inicio, string? Fim, string? Motivo, string? Descricao);
public record RecursoDto(string? Equipamento, int Quantidade, string? Horas, string? Obs);
public record ServicoDto(Guid? ObraItemId, string? Atividade, string? Local, decimal? QtdExec, string? Unidade,
    string? Status, decimal? PctInformado, JsonElement? EtapasFeitas, string? MotivoHold, string? Obs);
public record RetrabalhoDto(string? Atividade, string? Local, decimal? Quantidade, string? Unidade, int Pessoas,
    decimal? Horas, string? Causa, string? Origem, string? Descricao, string? AcaoCorretiva);
public record RdoUpsert(DateOnly Data, string? DiaSemana, string? Turno, string? Ocorrencias,
    JsonElement? Clima, JsonElement? Jornada, JsonElement? Dificuldades, JsonElement? ProximoDia,
    JsonElement? Planejamento, JsonElement? Seguranca, JsonElement? Assinaturas,
    EfetivoDto[]? Efetivo, ParalisacaoDto[]? Paralisacoes, RecursoDto[]? Recursos, ServicoDto[]? Servicos,
    RetrabalhoDto[]? Retrabalho);

[ApiController]
[Route("api/v1")]
[Authorize]
public class RdosController(AppDbContext db, IRdoPdf pdf, IStorage storage) : ControllerBase
{
    // Planejador/Gestor/Admin acompanham RDOs de todas as obras; demais só das vinculadas.
    private bool VeTodasObras => User.IsInRole("Gestor") || User.IsInRole("Admin") || User.IsInRole("Planejador");
    private Guid UsuarioId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private async Task<bool> PodeVerObra(Guid obraId)
    {
        if (await db.Obras.FindAsync(obraId) is null) return false;
        if (VeTodasObras) return true;
        return await db.UsuarioObras.AnyAsync(x => x.ObraId == obraId && x.UsuarioId == UsuarioId);
    }

    [HttpGet("obras/{obraId:guid}/rdos")]
    public async Task<IActionResult> Listar(Guid obraId)
    {
        if (!await PodeVerObra(obraId)) return Forbid();
        var rdos = await db.Rdos.Where(r => r.ObraId == obraId)
            .OrderByDescending(r => r.Numero)
            .Select(r => new { r.Id, r.Numero, r.Revisao, r.Data, r.Turno, Status = r.Status.ToString() })
            .ToListAsync();
        return Ok(rdos);
    }

    [HttpPost("obras/{obraId:guid}/rdos")]
    public async Task<IActionResult> Criar(Guid obraId, [FromBody] RdoUpsert dto)
    {
        if (!await PodeVerObra(obraId)) return Forbid();
        var numero = (await db.Rdos.Where(r => r.ObraId == obraId).MaxAsync(r => (int?)r.Numero) ?? 0) + 1;

        var rdo = new Rdo { ObraId = obraId, Numero = numero, ResponsavelUsuarioId = UsuarioId };
        Aplicar(rdo, dto);
        db.Rdos.Add(rdo);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = rdo.Id }, new { rdo.Id, rdo.Numero });
    }

    [HttpGet("rdos/{id:guid}")]
    public async Task<IActionResult> Obter(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        // itens da EAP para calcular o avanco por qtd
        var itens = await db.ObraItens.Where(i => i.ObraId == rdo.ObraId).ToDictionaryAsync(i => i.Id);

        return Ok(new
        {
            rdo.Id, rdo.ObraId, rdo.Numero, rdo.Revisao, rdo.Data, rdo.DiaSemana, rdo.Turno,
            Status = rdo.Status.ToString(), rdo.Ocorrencias,
            Clima = Json(rdo.Clima), Jornada = Json(rdo.Jornada), Dificuldades = Json(rdo.Dificuldades),
            ProximoDia = Json(rdo.ProximoDia), Planejamento = Json(rdo.Planejamento), Seguranca = Json(rdo.Seguranca),
            Assinaturas = Json(rdo.Assinaturas),
            rdo.Efetivo, rdo.Paralisacoes, rdo.Recursos, rdo.Retrabalho,
            Servicos = rdo.Servicos.Select(s => new
            {
                s.Id, s.ObraItemId, s.Atividade, s.Local, s.QtdExec, s.Unidade, s.Status, s.PctInformado,
                s.MotivoHold, s.Obs,
                PctItem = AvancoCalculo.PctItem(s, s.ObraItemId is { } oid && itens.TryGetValue(oid, out var it) ? it : null)
            })
        });
    }

    [HttpPut("rdos/{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] RdoUpsert dto)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();
        if (rdo.Status == RdoStatus.Aprovado)
            return Conflict(new { erro = "RDO aprovado nao pode ser editado." });

        rdo.Efetivo.Clear(); rdo.Paralisacoes.Clear(); rdo.Recursos.Clear(); rdo.Servicos.Clear(); rdo.Retrabalho.Clear();
        Aplicar(rdo, dto);
        await db.SaveChangesAsync();
        return Ok(new { rdo.Id, Status = rdo.Status.ToString() });
    }

    [HttpPost("rdos/{id:guid}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();
        if (rdo.Status == RdoStatus.Aprovado) return Conflict(new { erro = "Ja aprovado." });

        rdo.Status = RdoStatus.Enviado;
        rdo.EnviadoPor = UsuarioId;
        rdo.EnviadoEm = DateTime.UtcNow;
        rdo.TokenAprovacao = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync();
        // PDF (QuestPDF) e snapshot de avanco entram nos Sprints seguintes.
        return Ok(new { rdo.Id, Status = rdo.Status.ToString(), rdo.TokenAprovacao });
    }

    [HttpGet("rdos/{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        var obra = await db.Obras.FindAsync(rdo.ObraId);
        var cliente = obra?.ClienteId is { } cid ? await db.Clientes.FindAsync(cid) : null;
        var resp = rdo.ResponsavelUsuarioId is { } uid ? (await db.Usuarios.FindAsync(uid))?.Nome : null;
        var itens = await db.ObraItens.Where(i => i.ObraId == rdo.ObraId).ToDictionaryAsync(i => i.Id);

        var fotos = await db.RdoMidias.CountAsync(m => m.RdoId == id);

        var clima = Doc(rdo.Clima); var jorn = Doc(rdo.Jornada); var seg = Doc(rdo.Seguranca);
        var prox = Doc(rdo.ProximoDia); var plan = Doc(rdo.Planejamento); var dif = Doc(rdo.Dificuldades);

        string? J(string k) => Str(jorn, k);
        string[] condicoes = clima.TryGetProperty("condicoes", out var cc) && cc.ValueKind == JsonValueKind.Array
            ? cc.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x != "").ToArray() : [];
        int? temp = clima.TryGetProperty("temperatura", out var tp) && tp.ValueKind == JsonValueKind.Number ? tp.GetInt32() : null;

        // Horas / hora extra (cálculo no backend)
        HorasModel? horas = null;
        if (JornadaCalculo.Calcular(J("inicio"), J("almoco"), J("retorno"), J("termino"), Bool(jorn, "feriado"), rdo.Data) is { } h)
            horas = new HorasModel(h.DiaTipo, JornadaCalculo.Fmt(h.TrabalhadoMin),
                h.ExtraUtilMin > 0 ? JornadaCalculo.Fmt(h.ExtraUtilMin) : null,
                h.Extra100Min > 0 ? JornadaCalculo.Fmt(h.Extra100Min) : null,
                h.Extra150Min > 0 ? JornadaCalculo.Fmt(h.Extra150Min) : null);

        var pendencias = prox.TryGetProperty("pendencias", out var pe) && pe.ValueKind == JsonValueKind.Array
            ? pe.EnumerateArray().Select(x => new PendenciaRow(Str(x, "descricao"), Str(x, "responsavel"), Str(x, "prazo"), Str(x, "status"))).ToList()
            : [];

        // assinaturas (base64 -> bytes)
        var assDoc = Doc(rdo.Assinaturas);
        AssinaturaModel? Ass(string papel, string label)
        {
            if (assDoc.ValueKind != JsonValueKind.Object || !assDoc.TryGetProperty(papel, out var a) || a.ValueKind != JsonValueKind.Object) return null;
            var nome = Str(a, "nome");
            byte[]? img = null;
            var imgStr = Str(a, "img");
            if (!string.IsNullOrEmpty(imgStr))
            {
                var b64 = imgStr.Contains(',') ? imgStr[(imgStr.IndexOf(',') + 1)..] : imgStr;
                try { img = Convert.FromBase64String(b64); } catch { /* ignora imagem inválida */ }
            }
            return nome is null && img is null ? null : new AssinaturaModel(label, nome, img);
        }
        var assinaturas = new[] { Ass("encarregado", "Encarregado"), Ass("fiscal", "Fiscal"), Ass("supervisor", "Supervisor") }
            .Where(x => x is not null).Select(x => x!).ToList();

        var model = new RdoPdfModel(
            obra?.Nome ?? "Obra", obra?.Contrato, cliente?.Nome, obra?.Local,
            rdo.Numero, rdo.Revisao, rdo.Data.ToString("dd/MM/yyyy"), rdo.DiaSemana, rdo.Turno, resp, rdo.Status.ToString(),
            condicoes, temp, J("inicio"), J("almoco"), J("retorno"), J("termino"), horas,
            rdo.Efetivo.Select(e => new EfetivoRow(e.Funcao, e.Quantidade, e.HoraExtra)).ToList(),
            rdo.Paralisacoes.Select(p => new ParalisacaoRow(p.Inicio, p.Fim, p.Motivo, p.Descricao)).ToList(),
            rdo.Recursos.Select(r => new RecursoRow(r.Equipamento, r.Quantidade, r.Horas)).ToList(),
            rdo.Servicos.Select(s => new ServicoRow(s.Atividade,
                s.ObraItemId is { } oid && itens.TryGetValue(oid, out var it) ? it.Descricao : null,
                s.Status, s.QtdExec, s.Unidade,
                (int)Math.Round(AvancoCalculo.PctItem(s, s.ObraItemId is { } o && itens.TryGetValue(o, out var it2) ? it2 : null) * 100))).ToList(),
            rdo.Retrabalho.Select(rt => new RetrabalhoRow(rt.Atividade, rt.Pessoas, rt.Horas, rt.Causa, rt.AcaoCorretiva)).ToList(),
            pendencias,
            new SegurancaModel(Bool(seg, "dds"), Bool(seg, "apr"), Bool(seg, "pt"), Bool(seg, "areaIsolada"), Bool(seg, "epis"), Bool(seg, "ferramentas"), Str(seg, "observacoes")),
            new ProximoDiaModel(Str(prox, "maoObra"), Str(prox, "equipamentos"), Str(prox, "materiais"), Str(prox, "ferramentas")),
            new PlanejamentoModel(Str(plan, "servicos"), Str(plan, "prioridades"), Str(plan, "areas")),
            Str(dif, "descricao"), rdo.Ocorrencias, fotos, assinaturas);

        return File(pdf.Gerar(model), "application/pdf", $"RDO-{rdo.Numero}.pdf");
    }

    // ---- midia (fotos/videos) — separada do autosave, guardada no R2 ----
    [HttpPost("rdos/{id:guid}/midia")]
    [RequestSizeLimit(50_000_000)] // 50 MB
    public async Task<IActionResult> SubirMidia(Guid id, IFormFile file, [FromForm] string? categoria, [FromForm] string? descricao)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();
        if (file is null || file.Length == 0) return BadRequest(new { erro = "Arquivo vazio." });

        var tipo = file.ContentType.StartsWith("video") ? "video" : "foto";
        var ext = Path.GetExtension(file.FileName);
        var key = $"rdos/{id}/{Guid.NewGuid():N}{ext}";

        await using (var s = file.OpenReadStream())
            await storage.UploadAsync(s, key, string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);

        var midia = new RdoMidia { RdoId = id, Tipo = tipo, R2Key = key, Categoria = categoria, Descricao = descricao, TamanhoBytes = file.Length };
        db.RdoMidias.Add(midia);
        await db.SaveChangesAsync();
        return Ok(new { midia.Id, midia.Tipo });
    }

    [HttpGet("rdos/{id:guid}/midia")]
    public async Task<IActionResult> ListarMidia(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        var itens = await db.RdoMidias.Where(m => m.RdoId == id).OrderBy(m => m.CriadoEm).ToListAsync();
        var res = new List<object>();
        foreach (var m in itens)
            res.Add(new { m.Id, m.Tipo, m.Categoria, m.Descricao, m.TamanhoBytes, Url = await storage.UrlAssinadaAsync(m.R2Key, TimeSpan.FromHours(1)) });
        return Ok(res);
    }

    [HttpDelete("midia/{midiaId:guid}")]
    public async Task<IActionResult> ApagarMidia(Guid midiaId)
    {
        var m = await db.RdoMidias.FindAsync(midiaId);
        if (m is null) return NotFound();
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == m.RdoId);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        await storage.DeleteAsync(m.R2Key);
        db.RdoMidias.Remove(m);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- helpers ----
    private static JsonElement Json(string raw) => JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
    private static JsonElement Doc(string raw) => JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
    private static string? Str(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static bool Bool(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.True;
    private static string Raw(JsonElement? e, string fallback) => e is { } el ? el.GetRawText() : fallback;

    private static void Aplicar(Rdo rdo, RdoUpsert d)
    {
        rdo.Data = d.Data; rdo.DiaSemana = d.DiaSemana; rdo.Turno = d.Turno; rdo.Ocorrencias = d.Ocorrencias;
        rdo.Clima = Raw(d.Clima, "{}"); rdo.Jornada = Raw(d.Jornada, "{}");
        rdo.Dificuldades = Raw(d.Dificuldades, "[]"); rdo.ProximoDia = Raw(d.ProximoDia, "{}");
        rdo.Planejamento = Raw(d.Planejamento, "{}"); rdo.Seguranca = Raw(d.Seguranca, "{}");
        rdo.Assinaturas = Raw(d.Assinaturas, "{}");

        foreach (var rt in d.Retrabalho ?? [])
            rdo.Retrabalho.Add(new RdoRetrabalho
            {
                Atividade = rt.Atividade, Local = rt.Local, Quantidade = rt.Quantidade, Unidade = rt.Unidade,
                Pessoas = rt.Pessoas, Horas = rt.Horas, Causa = rt.Causa, Origem = rt.Origem,
                Descricao = rt.Descricao, AcaoCorretiva = rt.AcaoCorretiva
            });

        foreach (var e in d.Efetivo ?? [])
            rdo.Efetivo.Add(new RdoEfetivo { Funcao = e.Funcao, Quantidade = e.Quantidade, Entrada = e.Entrada, Saida = e.Saida, HoraExtra = e.HoraExtra, Obs = e.Obs });
        foreach (var p in d.Paralisacoes ?? [])
            rdo.Paralisacoes.Add(new RdoParalisacao { Inicio = p.Inicio, Fim = p.Fim, Motivo = p.Motivo, Descricao = p.Descricao });
        foreach (var r in d.Recursos ?? [])
            rdo.Recursos.Add(new RdoRecurso { Equipamento = r.Equipamento, Quantidade = r.Quantidade, Horas = r.Horas, Obs = r.Obs });
        foreach (var s in d.Servicos ?? [])
            rdo.Servicos.Add(new RdoServico
            {
                ObraItemId = s.ObraItemId, Atividade = s.Atividade, Local = s.Local, QtdExec = s.QtdExec,
                Unidade = s.Unidade, Status = s.Status, PctInformado = s.PctInformado,
                EtapasFeitas = Raw(s.EtapasFeitas, "[]"), MotivoHold = s.MotivoHold, Obs = s.Obs
            });
    }
}
