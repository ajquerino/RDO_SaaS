using System.Text.RegularExpressions;
using IndustrialOS.Domain.Entities;

namespace IndustrialOS.Domain.Services;

/// <summary>Fechamento SEMANAL de HH por obra. As taxas vêm de <see cref="RegraHoraExtra"/> (por empresa);
/// sem config, usa-se uma instância default (= comportamento anterior: útil 50/70 até 10h/semana, sáb/dom/
/// feriado 100/150 até 8h). Os adicionais saem agrupados POR PERCENTUAL (não há mais baldes fixos 50/70/
/// 100/150), para suportar empresas com taxas diferentes. Só QUANTIDADE de HH — não há R$ no sistema.
///
/// SUPOSIÇÃO (confirmar): a faixa 1→2 do dia útil acumula POR PESSOA ao longo da semana; a jornada é única
/// por RDO e só temos a CONTAGEM do efetivo, então usa-se uma pessoa "representante" e multiplica-se pelo
/// nº de pessoas do dia. Sáb/Dom/Feriado usam o limite/percentuais do próprio dia; o sábado pode usar corte
/// por horário (até a hora de corte a % base; após, a % pós-corte).</summary>
public static partial class HhSemanalCalculo
{
    public readonly record struct DiaEntrada(
        DateOnly Data, string? Inicio, string? Almoco, string? Retorno, string? Termino, bool Feriado, int NPessoas);

    public readonly record struct FaixaHh(decimal Percentual, double Horas);

    public readonly record struct DiaHh(
        DateOnly Data, string DiaTipo, int NPessoas, double NormalHH, IReadOnlyList<FaixaHh> Extras);

    public readonly record struct Resultado(
        double NormalHH, IReadOnlyList<FaixaHh> Extras, double TotalHH, IReadOnlyList<DiaHh> PorDia);

    public static Resultado Calcular(IEnumerable<DiaEntrada> dias, RegraHoraExtra? regra = null)
    {
        var r = regra ?? new RegraHoraExtra(); // sem config salva => defaults (comportamento anterior)
        double limiteSemana = (double)r.LimiteSemanalHoras;

        double normal = 0;
        double acumUtilPessoa = 0; // horas extras de dia útil acumuladas na semana, por 1 pessoa
        var acum = new Dictionary<decimal, double>(); // percentual -> HH da semana (já x pessoas)
        var porDia = new List<DiaHh>();

        foreach (var d in dias.OrderBy(x => x.Data))
        {
            var j = JornadaCalculo.Calcular(d.Inicio, d.Almoco, d.Retorno, d.Termino, d.Feriado, d.Data);
            var faixasDia = new Dictionary<decimal, double>();
            double normalDia = 0;
            double n = d.NPessoas;

            if (j is { } res)
            {
                if (res.DiaTipo == "Útil")
                {
                    double extraUtilH = res.ExtraUtilMin / 60.0;
                    double normalH = Math.Max(0, res.TrabalhadoMin - res.ExtraUtilMin) / 60.0;

                    double antes = acumUtilPessoa, depois = antes + extraUtilH;
                    double h1 = Math.Max(0, Math.Min(depois, limiteSemana) - Math.Min(antes, limiteSemana));
                    double h2 = extraUtilH - h1;
                    acumUtilPessoa = depois;

                    normalDia = normalH * n;
                    Add(faixasDia, r.PercentUtilFaixa1, h1 * n);
                    Add(faixasDia, r.PercentUtilFaixa2, h2 * n);
                }
                else if (res.DiaTipo == "Sábado")
                {
                    if (r.SabadoUsaCorteHorario && r.SabadoHoraCorte is { } corte && r.SabadoPercentAposCorte is { } pctApos)
                    {
                        var (antesH, depoisH) = TrabalhoAntesDepois(d, corte);
                        Add(faixasDia, r.SabadoPercent, antesH * n);
                        Add(faixasDia, pctApos, depoisH * n);
                    }
                    else
                        SplitLimite(faixasDia, n, res.TrabalhadoMin / 60.0, (double)r.SabadoLimiteHoras, r.SabadoPercent, r.SabadoPercentAcima);
                }
                else if (res.DiaTipo == "Domingo")
                    SplitLimite(faixasDia, n, res.TrabalhadoMin / 60.0, (double)r.DomingoLimiteHoras, r.DomingoPercent, r.DomingoPercentAcima);
                else // Feriado
                    SplitLimite(faixasDia, n, res.TrabalhadoMin / 60.0, (double)r.FeriadoLimiteHoras, r.FeriadoPercent, r.FeriadoPercentAcima);
            }

            foreach (var kv in faixasDia) Add(acum, kv.Key, kv.Value);
            normal += normalDia;
            porDia.Add(new DiaHh(d.Data, j?.DiaTipo ?? "—", d.NPessoas, Math.Round(normalDia, 2),
                faixasDia.OrderBy(k => k.Key).Select(k => new FaixaHh(k.Key, Math.Round(k.Value, 2))).ToList()));
        }

        double total = normal + acum.Values.Sum();
        var extras = acum.OrderBy(k => k.Key).Select(k => new FaixaHh(k.Key, Math.Round(k.Value, 2))).ToList();
        return new Resultado(Math.Round(normal, 2), extras, Math.Round(total, 2), porDia);
    }

    private static void SplitLimite(Dictionary<decimal, double> d, double n, double trabH, double limiteH, decimal pctBase, decimal pctAcima)
    {
        Add(d, pctBase, Math.Min(trabH, limiteH) * n);
        Add(d, pctAcima, Math.Max(0, trabH - limiteH) * n);
    }

    private static void Add(Dictionary<decimal, double> d, decimal pct, double horas)
    {
        if (horas > 0) d[pct] = d.GetValueOrDefault(pct) + horas;
    }

    /// <summary>Horas trabalhadas antes e depois de um horário de corte (desconta o almoço em cada trecho).</summary>
    private static (double antesH, double depoisH) TrabalhoAntesDepois(DiaEntrada d, TimeOnly corte)
    {
        int? ini = Hm(d.Inicio), alm = Hm(d.Almoco), ret = Hm(d.Retorno), ter = Hm(d.Termino);
        if (ini is null || ter is null || ter <= ini) return (0, 0);
        int c = corte.Hour * 60 + corte.Minute;

        var segs = alm is not null && ret is not null && ret > alm
            ? new[] { (a: ini.Value, b: alm.Value), (a: ret.Value, b: ter.Value) }
            : new[] { (a: ini.Value, b: ter.Value) };

        int antes = 0, depois = 0;
        foreach (var (a, b) in segs)
        {
            antes += Math.Max(0, Math.Min(b, c) - a);
            depois += Math.Max(0, b - Math.Max(a, c));
        }
        return (antes / 60.0, depois / 60.0);
    }

    private static int? Hm(string? t)
    {
        if (string.IsNullOrWhiteSpace(t) || !HoraRegex().IsMatch(t)) return null;
        var p = t.Split(':');
        return int.Parse(p[0]) * 60 + int.Parse(p[1]);
    }

    [GeneratedRegex(@"^\d{1,2}:\d{2}$")]
    private static partial Regex HoraRegex();
}
