using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record RegraHoraExtraDto(
    decimal LimiteSemanalHoras, decimal PercentUtilFaixa1, decimal PercentUtilFaixa2,
    decimal SabadoPercent, decimal SabadoLimiteHoras, decimal SabadoPercentAcima,
    bool SabadoUsaCorteHorario, TimeOnly? SabadoHoraCorte, decimal? SabadoPercentAposCorte,
    decimal DomingoPercent, decimal DomingoLimiteHoras, decimal DomingoPercentAcima,
    decimal FeriadoPercent, decimal FeriadoLimiteHoras, decimal FeriadoPercentAcima);

/// <summary>Configurações da empresa (tenant). Por ora: regras de hora-extra (usadas no fechamento
/// semanal de HH). Só Gestor/Admin. Sem config salva, o GET devolve os defaults (comportamento atual).</summary>
[ApiController]
[Route("api/v1/configuracoes")]
[Authorize(Roles = "Gestor,Admin")]
public class ConfiguracoesController(AppDbContext db) : ControllerBase
{
    [HttpGet("hora-extra")]
    public async Task<IActionResult> GetHoraExtra()
    {
        var r = await db.RegrasHoraExtra.FirstOrDefaultAsync() ?? new RegraHoraExtra();
        return Ok(Map(r));
    }

    [HttpPut("hora-extra")]
    public async Task<IActionResult> PutHoraExtra([FromBody] RegraHoraExtraDto dto)
    {
        decimal[] naoNeg =
        [
            dto.LimiteSemanalHoras, dto.PercentUtilFaixa1, dto.PercentUtilFaixa2,
            dto.SabadoPercent, dto.SabadoLimiteHoras, dto.SabadoPercentAcima,
            dto.DomingoPercent, dto.DomingoLimiteHoras, dto.DomingoPercentAcima,
            dto.FeriadoPercent, dto.FeriadoLimiteHoras, dto.FeriadoPercentAcima,
        ];
        if (naoNeg.Any(v => v < 0) || dto.SabadoPercentAposCorte is < 0)
            return BadRequest(new { erro = "Percentuais e limites não podem ser negativos." });

        // Uma regra por tenant (upsert). O filtro global já escopa ao tenant atual.
        var r = await db.RegrasHoraExtra.FirstOrDefaultAsync();
        if (r is null) { r = new RegraHoraExtra(); db.RegrasHoraExtra.Add(r); }

        r.LimiteSemanalHoras = dto.LimiteSemanalHoras;
        r.PercentUtilFaixa1 = dto.PercentUtilFaixa1;
        r.PercentUtilFaixa2 = dto.PercentUtilFaixa2;
        r.SabadoPercent = dto.SabadoPercent;
        r.SabadoLimiteHoras = dto.SabadoLimiteHoras;
        r.SabadoPercentAcima = dto.SabadoPercentAcima;
        r.SabadoUsaCorteHorario = dto.SabadoUsaCorteHorario;
        r.SabadoHoraCorte = dto.SabadoHoraCorte;
        r.SabadoPercentAposCorte = dto.SabadoPercentAposCorte;
        r.DomingoPercent = dto.DomingoPercent;
        r.DomingoLimiteHoras = dto.DomingoLimiteHoras;
        r.DomingoPercentAcima = dto.DomingoPercentAcima;
        r.FeriadoPercent = dto.FeriadoPercent;
        r.FeriadoLimiteHoras = dto.FeriadoLimiteHoras;
        r.FeriadoPercentAcima = dto.FeriadoPercentAcima;

        await db.SaveChangesAsync(); // Stamp() carimba o TenantId no insert
        return Ok(Map(r));
    }

    private static object Map(RegraHoraExtra r) => new
    {
        r.LimiteSemanalHoras, r.PercentUtilFaixa1, r.PercentUtilFaixa2,
        r.SabadoPercent, r.SabadoLimiteHoras, r.SabadoPercentAcima,
        r.SabadoUsaCorteHorario, r.SabadoHoraCorte, r.SabadoPercentAposCorte,
        r.DomingoPercent, r.DomingoLimiteHoras, r.DomingoPercentAcima,
        r.FeriadoPercent, r.FeriadoLimiteHoras, r.FeriadoPercentAcima,
    };
}
