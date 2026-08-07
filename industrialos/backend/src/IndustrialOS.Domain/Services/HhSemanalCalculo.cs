namespace IndustrialOS.Domain.Services;

/// <summary>Fechamento SEMANAL de HH por obra, aplicando a taxa de dia útil 50%/70% — que é SEMANAL
/// (o JornadaCalculo só resolve o dia). Só QUANTIDADE de HH (homem-hora); não há salário/R$ no sistema.
///
/// SUPOSIÇÃO (confirmar com o dono): a taxa 50/70 acumula POR PESSOA ao longo da semana — as
/// primeiras 10h extras de dia útil (após 8h48) de cada pessoa vão ao balde 50% e o excedente ao 70%.
/// Como a jornada é ÚNICA por RDO e só temos a CONTAGEM do efetivo (não a identidade de cada pessoa),
/// modela-se uma pessoa "representante": acumulam-se as horas extras diárias em sequência (seg→dom) e,
/// a cada dia, a fração que cai em cada balde é multiplicada pelo nº de pessoas do efetivo daquele dia.
/// Sáb/Dom/Feriado usam o split 100/150 do próprio dia (JornadaCalculo), x nº de pessoas.</summary>
public static class HhSemanalCalculo
{
    private const double LimiteSemanal50 = 10.0; // primeiras 10h extras (dia útil), por pessoa, a 50%

    public readonly record struct DiaEntrada(
        DateOnly Data, string? Inicio, string? Almoco, string? Retorno, string? Termino, bool Feriado, int NPessoas);

    public readonly record struct DiaHh(
        DateOnly Data, string DiaTipo, int NPessoas,
        double NormalHH, double Extra50HH, double Extra70HH, double Fds100HH, double Fds150HH);

    public readonly record struct Resultado(
        double NormalHH, double Extra50HH, double Extra70HH, double Fds100HH, double Fds150HH,
        double TotalHH, IReadOnlyList<DiaHh> PorDia);

    public static Resultado Calcular(IEnumerable<DiaEntrada> dias)
    {
        double normal = 0, e50 = 0, e70 = 0, f100 = 0, f150 = 0;
        double acumExtraUtilPessoa = 0; // horas extras de dia útil acumuladas na semana, por 1 pessoa
        var porDia = new List<DiaHh>();

        foreach (var d in dias.OrderBy(x => x.Data))
        {
            var r = JornadaCalculo.Calcular(d.Inicio, d.Almoco, d.Retorno, d.Termino, d.Feriado, d.Data);
            if (r is not { } j)
            {
                porDia.Add(new DiaHh(d.Data, "—", d.NPessoas, 0, 0, 0, 0, 0));
                continue;
            }

            double n = d.NPessoas;
            double normalH = 0, h50 = 0, h70 = 0, h100 = 0, h150 = 0;

            if (j.DiaTipo == "Útil")
            {
                double extraUtilH = j.ExtraUtilMin / 60.0;
                normalH = Math.Max(0, j.TrabalhadoMin - j.ExtraUtilMin) / 60.0;

                // Split 50/70 pelo acúmulo semanal (por pessoa): as primeiras 10h a 50%, resto a 70%.
                double antes = acumExtraUtilPessoa;
                double depois = antes + extraUtilH;
                h50 = Math.Max(0, Math.Min(depois, LimiteSemanal50) - Math.Min(antes, LimiteSemanal50));
                h70 = extraUtilH - h50;
                acumExtraUtilPessoa = depois;
            }
            else // Sábado / Domingo / Feriado — tudo é adicional (100% até 8h, 150% acima); sem "normal".
            {
                h100 = j.Extra100Min / 60.0;
                h150 = j.Extra150Min / 60.0;
            }

            double normalDia = normalH * n, e50Dia = h50 * n, e70Dia = h70 * n, f100Dia = h100 * n, f150Dia = h150 * n;
            normal += normalDia; e50 += e50Dia; e70 += e70Dia; f100 += f100Dia; f150 += f150Dia;

            porDia.Add(new DiaHh(d.Data, j.DiaTipo, d.NPessoas,
                Math.Round(normalDia, 2), Math.Round(e50Dia, 2), Math.Round(e70Dia, 2),
                Math.Round(f100Dia, 2), Math.Round(f150Dia, 2)));
        }

        double total = normal + e50 + e70 + f100 + f150;
        return new Resultado(
            Math.Round(normal, 2), Math.Round(e50, 2), Math.Round(e70, 2),
            Math.Round(f100, 2), Math.Round(f150, 2), Math.Round(total, 2), porDia);
    }
}
