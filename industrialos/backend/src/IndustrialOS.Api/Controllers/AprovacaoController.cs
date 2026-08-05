using IndustrialOS.Application.Pdf;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

// DTOs de entrada do fiscal
public record AprovarDto(string? Nome, string? Assinatura);
public record RevisaoDto(string? Nome, string? Motivo);

/// <summary>Rota PUBLICA (sem login) para o fiscal do cliente aprovar/rejeitar um RDO
/// via link com token opaco gerado no "Finalizar". O token e a credencial de acesso —
/// por isso ignoramos o filtro global de tenant (nao ha usuario autenticado aqui).</summary>
[ApiController]
[Route("api/v1/aprovacao")]
[AllowAnonymous]
public class AprovacaoController(AppDbContext db, IRdoPdf pdf) : ControllerBase
{
    private Task<Rdo?> PorToken(string token) =>
        db.Rdos.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.TokenAprovacao == token && r.DeletadoEm == null);

    [HttpGet("{token}")]
    public async Task<IActionResult> Obter(string token)
    {
        var rdo = await PorToken(token);
        if (rdo is null) return NotFound(new { erro = "Link invalido ou expirado." });

        var obra = await db.Obras.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == rdo.ObraId);
        var cliente = obra?.ClienteId is { } cid ? await db.Clientes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cid) : null;
        var resp = rdo.ResponsavelUsuarioId is { } uid ? (await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == uid))?.Nome : null;
        var itens = await db.ObraItens.IgnoreQueryFilters().Where(i => i.ObraId == rdo.ObraId).ToDictionaryAsync(i => i.Id);

        var servicos = rdo.Servicos.Select(s => new
        {
            s.Atividade,
            Item = s.ObraItemId is { } oid && itens.TryGetValue(oid, out var it) ? it.Descricao : null,
            s.Status, s.QtdExec, s.Unidade,
            Pct = (int)Math.Round(AvancoCalculo.PctItem(s, s.ObraItemId is { } o && itens.TryGetValue(o, out var it2) ? it2 : null) * 100)
        }).ToList();

        return Ok(new
        {
            rdo.Numero, rdo.Revisao, rdo.Data, rdo.DiaSemana, rdo.Turno,
            Status = rdo.Status.ToString(), rdo.Ocorrencias, Responsavel = resp,
            Obra = new { obra?.Nome, obra?.Contrato, obra?.Local, Cliente = cliente?.Nome },
            Efetivo = rdo.Efetivo.Select(e => new { e.Funcao, e.Quantidade, e.HoraExtra }),
            Paralisacoes = rdo.Paralisacoes.Select(p => new { p.Inicio, p.Fim, p.Motivo, p.Descricao }),
            Servicos = servicos,
            Retrabalho = rdo.Retrabalho.Select(rt => new { rt.Atividade, rt.Pessoas, rt.Horas, rt.Causa }),
            rdo.MotivoRevisao, rdo.RevisadoPor, rdo.AprovadoPor, rdo.AprovadoEm
        });
    }

    [HttpGet("{token}/pdf")]
    public async Task<IActionResult> Pdf(string token)
    {
        var rdo = await PorToken(token);
        if (rdo is null) return NotFound(new { erro = "Link invalido ou expirado." });

        var model = await RdoPdfFactory.BuildAsync(db, rdo);
        return File(pdf.Gerar(model), "application/pdf", $"RDO-{rdo.Numero}.pdf");
    }

    [HttpPost("{token}/aprovar")]
    public async Task<IActionResult> Aprovar(string token, [FromBody] AprovarDto dto)
    {
        var rdo = await PorToken(token);
        if (rdo is null) return NotFound(new { erro = "Link invalido ou expirado." });
        if (rdo.Status == RdoStatus.Aprovado) return Conflict(new { erro = "Este RDO ja foi aprovado." });
        if (rdo.Status != RdoStatus.Enviado) return Conflict(new { erro = "Este RDO nao esta aguardando aprovacao." });
        if (string.IsNullOrWhiteSpace(dto.Nome)) return BadRequest(new { erro = "Informe seu nome." });

        rdo.Status = RdoStatus.Aprovado;
        rdo.AprovadoPor = dto.Nome!.Trim();
        rdo.AprovadoEm = DateTime.UtcNow;

        // registra a assinatura do fiscal (se enviada) na secao de assinaturas
        if (!string.IsNullOrWhiteSpace(dto.Assinatura))
            rdo.Assinaturas = GravarAssinaturaFiscal(rdo.Assinaturas, dto.Nome!.Trim(), dto.Assinatura!);

        // token consumido: nao pode mais ser reutilizado
        rdo.TokenAprovacao = null;
        await db.SaveChangesAsync();
        return Ok(new { Status = rdo.Status.ToString(), rdo.AprovadoPor, rdo.AprovadoEm });
    }

    [HttpPost("{token}/revisao")]
    public async Task<IActionResult> SolicitarRevisao(string token, [FromBody] RevisaoDto dto)
    {
        var rdo = await PorToken(token);
        if (rdo is null) return NotFound(new { erro = "Link invalido ou expirado." });
        if (rdo.Status == RdoStatus.Aprovado) return Conflict(new { erro = "Este RDO ja foi aprovado." });
        if (rdo.Status != RdoStatus.Enviado) return Conflict(new { erro = "Este RDO nao esta aguardando aprovacao." });
        if (string.IsNullOrWhiteSpace(dto.Motivo)) return BadRequest(new { erro = "Descreva o que precisa ser revisado." });

        rdo.Status = RdoStatus.RevisaoSolicitada;
        rdo.MotivoRevisao = dto.Motivo!.Trim();
        rdo.RevisadoPor = string.IsNullOrWhiteSpace(dto.Nome) ? null : dto.Nome!.Trim();
        rdo.RevisadoEm = DateTime.UtcNow;
        // token invalidado: a equipe corrige e reenvia (gera novo token no Finalizar)
        rdo.TokenAprovacao = null;
        await db.SaveChangesAsync();
        return Ok(new { Status = rdo.Status.ToString(), rdo.MotivoRevisao });
    }

    private static string GravarAssinaturaFiscal(string assinaturasJson, string nome, string img)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(string.IsNullOrWhiteSpace(assinaturasJson) ? "{}" : assinaturasJson)
                   as System.Text.Json.Nodes.JsonObject ?? new System.Text.Json.Nodes.JsonObject();
        node["fiscal"] = new System.Text.Json.Nodes.JsonObject { ["nome"] = nome, ["img"] = img };
        return node.ToJsonString();
    }
}
