using System.Security.Claims;
using System.Text.Json;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

/// <summary>Fechamento semanal de HH por obra (baldes Normal / 50 / 70 / 100 / 150), aplicando a taxa
/// SEMANAL de dia útil. Respeita tenant (filtro global) e permissão por obra (igual ao Dashboard).</summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class HhSemanalController(AppDbContext db) : ControllerBase
{
    private bool VeTodasObras => User.IsInRole("Gestor") || User.IsInRole("Admin") || User.IsInRole("Planejador");
    private Guid UsuarioId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    [HttpGet("obras/{obraId:guid}/hh-semanal")]
    public async Task<IActionResult> HhSemanal(Guid obraId, [FromQuery] DateOnly? de)
    {
        var obra = await db.Obras.FindAsync(obraId);
        if (obra is null) return NotFound();
        if (!VeTodasObras && !await db.UsuarioObras.AnyAsync(x => x.ObraId == obraId && x.UsuarioId == UsuarioId))
            return NotFound(); // Encarregado só vê as obras vinculadas.

        // Normaliza qualquer dia da semana para a segunda-feira (início da semana seg→dom).
        var baseDia = de ?? DateOnly.FromDateTime(DateTime.Today);
        var segunda = baseDia.AddDays(-(((int)baseDia.DayOfWeek + 6) % 7));
        var domingo = segunda.AddDays(6);

        var rdos = await db.Rdos
            .Where(r => r.ObraId == obraId && r.Data >= segunda && r.Data <= domingo)
            .ToListAsync();

        var dias = rdos.Select(r =>
        {
            var (ini, alm, ret, ter, fer) = ParseJornada(r.Jornada);
            return new HhSemanalCalculo.DiaEntrada(r.Data, ini, alm, ret, ter, fer, r.Efetivo.Sum(e => e.Quantidade));
        });

        var res = HhSemanalCalculo.Calcular(dias);

        return Ok(new
        {
            de = segunda,
            ate = domingo,
            normalHH = res.NormalHH,
            extra50HH = res.Extra50HH,
            extra70HH = res.Extra70HH,
            fds100HH = res.Fds100HH,
            fds150HH = res.Fds150HH,
            totalHH = res.TotalHH,
            porDia = res.PorDia.Select(d => new
            {
                data = d.Data,
                diaTipo = d.DiaTipo,
                nPessoas = d.NPessoas,
                normalHH = d.NormalHH,
                extra50HH = d.Extra50HH,
                extra70HH = d.Extra70HH,
                fds100HH = d.Fds100HH,
                fds150HH = d.Fds150HH,
            }),
        });
    }

    private static (string?, string?, string?, string?, bool) ParseJornada(string jornadaJson)
    {
        var j = string.IsNullOrWhiteSpace(jornadaJson) ? "{}" : jornadaJson;
        using var doc = JsonDocument.Parse(j);
        var e = doc.RootElement;
        string? S(string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        bool feriado = e.ValueKind == JsonValueKind.Object && e.TryGetProperty("feriado", out var f) && f.ValueKind == JsonValueKind.True;
        return (S("inicio"), S("almoco"), S("retorno"), S("termino"), feriado);
    }
}
