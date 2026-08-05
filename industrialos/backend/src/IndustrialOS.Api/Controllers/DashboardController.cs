using System.Security.Claims;
using System.Text.Json;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    private bool VeTodasObras => User.IsInRole("Gestor") || User.IsInRole("Admin") || User.IsInRole("Planejador");
    private Guid UsuarioId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private async Task<bool> PodeVerObra(Guid obraId)
    {
        if (await db.Obras.FindAsync(obraId) is null) return false;
        if (VeTodasObras) return true;
        return await db.UsuarioObras.AnyAsync(x => x.ObraId == obraId && x.UsuarioId == UsuarioId);
    }

    [HttpGet("obras/{obraId:guid}/dashboard")]
    public async Task<IActionResult> Dashboard(Guid obraId)
    {
        var obra = await db.Obras.FindAsync(obraId);
        if (obra is null || !await PodeVerObra(obraId)) return NotFound();

        var itens = await db.ObraItens.Where(i => i.ObraId == obraId).ToListAsync();
        var rdos = await db.Rdos.Where(r => r.ObraId == obraId).ToListAsync();

        // % atual de cada item = maior avanço reportado em qualquer RDO
        decimal PctItem(Guid itemId)
        {
            var item = itens.First(i => i.Id == itemId);
            decimal pct = 0m;
            foreach (var r in rdos)
                foreach (var s in r.Servicos.Where(s => s.ObraItemId == itemId))
                    pct = Math.Max(pct, AvancoCalculo.PctItem(s, item));
            return pct;
        }

        var itensAvanco = itens.Select(i => new { i.Descricao, i.HhPrevisto, i.QtdPrevista, Pct = Math.Round(PctItem(i.Id) * 100, 1) })
            .OrderByDescending(x => x.HhPrevisto ?? 0).ToList();

        // avanço da obra: HH ponderado -> fallback qtd -> média
        decimal somaHh = itens.Sum(i => i.HhPrevisto ?? 0);
        decimal somaQtd = itens.Sum(i => i.QtdPrevista ?? 0);
        decimal avancoPct;
        string baseAvanco;
        if (somaHh > 0) { avancoPct = itens.Sum(i => (i.HhPrevisto ?? 0) * PctItem(i.Id)) / somaHh; baseAvanco = "HH"; }
        else if (somaQtd > 0) { avancoPct = itens.Sum(i => (i.QtdPrevista ?? 0) * PctItem(i.Id)) / somaQtd; baseAvanco = "Quantidade"; }
        else { avancoPct = itens.Count > 0 ? itens.Average(i => PctItem(i.Id)) : 0; baseAvanco = "Média"; }

        // HH previsto x realizado (por RDO, para agregado e Curva S)
        decimal hhRealizado = 0; int somaEfetivo = 0; int diasComEfetivo = 0;
        var hhPorData = new List<(DateOnly Data, decimal Hh)>();
        foreach (var r in rdos)
        {
            var h = HorasTrabalhadas(r.Jornada, r.Data);
            var efetivo = r.Efetivo.Sum(e => e.Quantidade);
            var hh = efetivo * h;
            hhRealizado += hh;
            hhPorData.Add((r.Data, hh));
            if (efetivo > 0) { somaEfetivo += efetivo; diasComEfetivo++; }
        }
        var efetivoMedio = diasComEfetivo > 0 ? Math.Round((decimal)somaEfetivo / diasComEfetivo, 1) : 0;

        // Curva S: acumulado realizado (HH) x previsto (rampa linear no prazo da obra),
        // ambos em % do HH previsto total. Um ponto por dia com RDO.
        var curvaS = new List<object>();
        if (somaHh > 0 && hhPorData.Count > 0)
        {
            decimal acum = 0;
            foreach (var g in hhPorData.GroupBy(x => x.Data).OrderBy(x => x.Key))
            {
                acum += g.Sum(x => x.Hh);
                decimal previstoPct = 0;
                if (obra.DataInicio is { } di2 && obra.DataFim is { } df2 && df2 > di2)
                    previstoPct = Math.Clamp((decimal)(g.Key.DayNumber - di2.DayNumber) / (df2.DayNumber - di2.DayNumber), 0, 1) * 100;
                curvaS.Add(new
                {
                    data = g.Key,
                    realizadoPct = Math.Round(acum / somaHh * 100, 1),
                    previstoPct = Math.Round(previstoPct, 1)
                });
            }
        }

        // Pareto paralisações (por motivo)
        var paralisacoes = rdos.SelectMany(r => r.Paralisacoes)
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Motivo) ? "Sem motivo" : p.Motivo!)
            .Select(g => new { Motivo = g.Key, Ocorrencias = g.Count(), Minutos = g.Sum(p => DiffMin(p.Inicio, p.Fim)) })
            .OrderByDescending(x => x.Minutos).ToList();

        // Pareto retrabalho (por causa, HH perdido)
        var retrabalho = rdos.SelectMany(r => r.Retrabalho)
            .GroupBy(rt => string.IsNullOrWhiteSpace(rt.Causa) ? "Sem causa" : rt.Causa!)
            .Select(g => new { Causa = g.Key, Hh = g.Sum(rt => rt.Pessoas * (rt.Horas ?? 0)) })
            .OrderByDescending(x => x.Hh).ToList();

        // Farol (tempo x avanço)
        string farol = "cinza"; decimal? desvio = null;
        if (obra.DataInicio is { } di && obra.DataFim is { } df && df > di)
        {
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            decimal pctTempo = Math.Clamp((decimal)(hoje.DayNumber - di.DayNumber) / (df.DayNumber - di.DayNumber), 0, 1);
            desvio = Math.Round((pctTempo - avancoPct) * 100, 1);
            bool prazoEstourou = hoje > df && avancoPct < 1;
            farol = avancoPct >= 1 || desvio <= 10 ? "verde" : desvio <= 25 && !prazoEstourou ? "amarelo" : "vermelho";
        }

        return Ok(new
        {
            obra = new { obra.Nome, obra.Contrato, obra.DataInicio, obra.DataFim, obra.Status },
            avanco = new { pct = Math.Round(avancoPct * 100, 1), baseAvanco, itens = itensAvanco },
            hh = new { previsto = somaHh, realizado = Math.Round(hhRealizado, 1), efetivoMedio },
            farol = new { cor = farol, desvio },
            paralisacoes,
            retrabalho,
            curvaS,
            rdos = rdos.Count
        });
    }

    // ---- helpers ----
    private static decimal HorasTrabalhadas(string jornadaJson, DateOnly data)
    {
        var j = string.IsNullOrWhiteSpace(jornadaJson) ? "{}" : jornadaJson;
        using var doc = JsonDocument.Parse(j);
        var e = doc.RootElement;
        string? S(string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        bool feriado = e.ValueKind == JsonValueKind.Object && e.TryGetProperty("feriado", out var f) && f.ValueKind == JsonValueKind.True;
        var r = JornadaCalculo.Calcular(S("inicio"), S("almoco"), S("retorno"), S("termino"), feriado, data);
        return r is { } res ? Math.Round(res.TrabalhadoMin / 60m, 2) : 0m;
    }

    private static int DiffMin(string? ini, string? fim)
    {
        int? a = Hm(ini), b = Hm(fim);
        return a is not null && b is not null && b > a ? b.Value - a.Value : 0;
    }
    private static int? Hm(string? t)
    {
        if (string.IsNullOrWhiteSpace(t) || !System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d{1,2}:\d{2}$")) return null;
        var p = t.Split(':'); return int.Parse(p[0]) * 60 + int.Parse(p[1]);
    }
}
