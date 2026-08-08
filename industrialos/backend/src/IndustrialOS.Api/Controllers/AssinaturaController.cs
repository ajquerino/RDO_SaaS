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
public class AssinaturaController(AppDbContext db, ITenantContext tenant, IAbacatePay abacate,
    Microsoft.Extensions.Configuration.IConfiguration cfg) : ControllerBase
{
    /// <summary>Gera um CHECKOUT hospedado do AbacatePay (PIX/cartão/boleto) do plano da empresa e
    /// devolve a URL de pagamento. Fica em /assinatura (rota livre), então empresa BLOQUEADA consegue
    /// pagar. Ao pagar, o webhook regulariza o vencimento e o bloqueio some. Cria o "produto" do plano
    /// no AbacatePay sob demanda (uma vez por plano; id guardado em Plano.ProvedorProdutoId).</summary>
    [HttpPost("cobrar")]
    public async Task<IActionResult> Cobrar()
    {
        if (!abacate.Configurado)
            return StatusCode(501, new { erro = "Pagamento online ainda não configurado. Contate o suporte." });
        if (tenant.TenantId is not Guid tid)
            return BadRequest(new { erro = "Sem empresa no contexto." });

        var a = await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tid);
        var plano = a?.PlanoId is Guid pid
            ? await db.Planos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == pid) : null;
        if (plano?.PrecoMensal is not > 0)
            return BadRequest(new { erro = "Escolha um plano com preço definido antes de pagar." });

        try
        {
            // Garante o produto do plano no AbacatePay (cria uma vez, guarda o id).
            if (string.IsNullOrEmpty(plano.ProvedorProdutoId))
            {
                var centavos = (long)Math.Round(plano.PrecoMensal.Value * 100m);
                plano.ProvedorProdutoId = await abacate.CriarProdutoAsync(
                    $"IndustrialOS — {plano.Nome}", "Assinatura mensal IndustrialOS", centavos, $"plano-{plano.Id}");
                await db.SaveChangesAsync();
            }

            var retorno = $"{(cfg["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/')}/assinatura";
            var checkout = await abacate.CriarCheckoutAsync(plano.ProvedorProdutoId!, tid.ToString(), retorno);
            return Ok(new { checkout.Id, checkout.Url, checkout.Status, valor = plano.PrecoMensal, plano = plano.Nome });
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
