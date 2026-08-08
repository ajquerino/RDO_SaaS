using IndustrialOS.Application.Common;
using IndustrialOS.Application.Pagamento;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record EscolherPlanoRequest(Guid PlanoId);

/// <summary>Status da assinatura do tenant atual — alimenta o banner e o modo somente-leitura do front.</summary>
[ApiController]
[Route("api/v1/assinatura")]
[Authorize]
public class AssinaturaController(AppDbContext db, ITenantContext tenant, IAbacatePay abacate) : ControllerBase
{
    /// <summary>Gera uma cobrança PIX do plano da empresa (via AbacatePay). Fica em /assinatura
    /// (rota livre no filtro), então uma empresa BLOQUEADA ainda consegue pagar. Ao pagar, o
    /// webhook regulariza o vencimento e o bloqueio some.</summary>
    [HttpPost("cobrar")]
    public async Task<IActionResult> Cobrar()
    {
        if (!abacate.Configurado)
            return StatusCode(501, new { erro = "Pagamento online ainda não configurado. Contate o suporte." });
        if (tenant.TenantId is not Guid tid)
            return BadRequest(new { erro = "Sem empresa no contexto." });

        var a = await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tid);
        var preco = a?.PlanoId is Guid pid
            ? await db.Planos.IgnoreQueryFilters().Where(p => p.Id == pid).Select(p => p.PrecoMensal).FirstOrDefaultAsync()
            : null;
        if (preco is not > 0)
            return BadRequest(new { erro = "Nenhum plano com preço definido para esta empresa." });

        var empresa = await db.Empresas.IgnoreQueryFilters().Where(e => e.TenantId == tid).Select(e => e.RazaoSocial).FirstOrDefaultAsync();
        var cnpj = await db.Tenants.IgnoreQueryFilters().Where(t => t.Id == tid).Select(t => t.Cnpj).FirstOrDefaultAsync();
        var centavos = (long)Math.Round(preco.Value * 100m);
        try
        {
            var cobranca = await abacate.CriarCobrancaPixAsync(centavos, tid.ToString(),
                $"IndustrialOS — assinatura mensal ({empresa})", empresa, null, cnpj);
            return Ok(new { cobranca.Id, cobranca.BrCode, cobranca.BrCodeBase64, cobranca.Status, valor = preco });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { erro = "Falha ao gerar a cobrança no gateway.", detalhe = ex.Message });
        }
    }

    [HttpGet("minha")]
    public async Task<IActionResult> Minha()
    {
        if (tenant.TenantId is not Guid tid)
            return Ok(new { estado = nameof(EstadoAssinatura.SemAssinatura), bloqueada = false });

        var a = await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tid);
        var e = AssinaturaCalculo.Avaliar(a, DateOnly.FromDateTime(DateTime.UtcNow));

        string? planoNome = null;
        if (a?.PlanoId is Guid pid)
            planoNome = await db.Planos.IgnoreQueryFilters().Where(p => p.Id == pid).Select(p => p.Nome).FirstOrDefaultAsync();

        return Ok(new
        {
            estado = e.Estado.ToString(),
            bloqueada = e.Bloqueada,
            diasParaVencer = e.DiasParaVencer,
            diasAtraso = e.DiasAtraso,
            avisoNivel = e.AvisoNivel,
            vencimentoEm = e.VencimentoEm,
            planoNome,
        });
    }

    /// <summary>Catálogo de planos para a empresa escolher ao assinar/pagar (só leitura).</summary>
    [HttpGet("planos")]
    public async Task<IActionResult> Planos() =>
        Ok(await db.Planos.OrderBy(p => p.PrecoMensal ?? 0)
            .Select(p => new { p.Id, p.Nome, p.LimiteObras, p.LimiteUsuarios, p.PrecoMensal })
            .ToListAsync());

    /// <summary>A empresa escolhe/troca o próprio plano (upsert do PlanoId na Assinatura do tenant).
    /// Rota livre no filtro, então uma empresa bloqueada consegue escolher antes de pagar.</summary>
    [HttpPost("escolher-plano")]
    public async Task<IActionResult> EscolherPlano([FromBody] EscolherPlanoRequest req)
    {
        if (tenant.TenantId is not Guid tid)
            return BadRequest(new { erro = "Sem empresa no contexto." });

        var plano = await db.Planos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == req.PlanoId);
        if (plano is null) return BadRequest(new { erro = "Plano não encontrado." });

        var a = await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tid);
        if (a is null) { a = new Assinatura { TenantId = tid }; db.Assinaturas.Add(a); }
        a.PlanoId = plano.Id;
        await db.SaveChangesAsync();

        return Ok(new { a.PlanoId, planoNome = plano.Nome });
    }
}
