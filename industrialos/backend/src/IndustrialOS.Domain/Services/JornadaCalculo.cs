using System.Text.RegularExpressions;

namespace IndustrialOS.Domain.Services;

/// <summary>
/// Cálculo de horas/HE do RDO (regras validadas): seg–sex expediente até 16:48; extra = após 16:48
/// (50% até 10h/semana, 70% acima — aplicado no fechamento semanal). Sáb/Dom/Feriado = 100% até 8h, 150% acima.
/// </summary>
public static partial class JornadaCalculo
{
    private const int FimExpediente = 16 * 60 + 48; // 16:48

    public readonly record struct Resultado(string DiaTipo, int TrabalhadoMin, int ExtraUtilMin, int Extra100Min, int Extra150Min);

    public static Resultado? Calcular(string? inicio, string? almoco, string? retorno, string? termino, bool feriado, DateOnly data)
    {
        var ini = Hm(inicio); var alm = Hm(almoco); var ret = Hm(retorno); var ter = Hm(termino);
        if (ini is null || ter is null || ter <= ini) return null;

        int lunch = alm is not null && ret is not null && ret > alm ? ret.Value - alm.Value : 0;
        int trab = Math.Max(0, ter.Value - ini.Value - lunch);

        string dia = feriado ? "Feriado"
            : data.DayOfWeek == DayOfWeek.Sunday ? "Domingo"
            : data.DayOfWeek == DayOfWeek.Saturday ? "Sábado" : "Útil";

        int extraUtil = 0, extra100 = 0, extra150 = 0;
        if (dia == "Útil")
            extraUtil = Math.Max(0, ter.Value - FimExpediente);
        else
        {
            extra100 = Math.Min(trab, 480);        // até 8h
            extra150 = Math.Max(0, trab - 480);    // acima de 8h
        }
        return new Resultado(dia, trab, extraUtil, extra100, extra150);
    }

    public static string Fmt(int min) => $"{min / 60}h{min % 60:D2}";

    private static int? Hm(string? t)
    {
        if (string.IsNullOrWhiteSpace(t) || !HoraRegex().IsMatch(t)) return null;
        var p = t.Split(':');
        return int.Parse(p[0]) * 60 + int.Parse(p[1]);
    }

    [GeneratedRegex(@"^\d{1,2}:\d{2}$")]
    private static partial Regex HoraRegex();
}
