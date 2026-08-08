using IndustrialOS.Domain.Entities;

namespace IndustrialOS.Domain.Services;

public enum EstadoAssinatura { SemAssinatura, Trial, EmDia, PrestesAVencer, Vencido, Bloqueada }

/// <summary>Motor de ciclo de vida da assinatura (função pura e testável — estilo JornadaCalculo).
/// DEFAULT SEGURO: sem assinatura ou cancelada NUNCA bloqueia (empresas existentes seguem livres).
/// Tolerância padrão de 7 dias após o vencimento antes de bloquear (acesso vira somente-leitura).</summary>
public static class AssinaturaCalculo
{
    public readonly record struct Resultado(
        EstadoAssinatura Estado,
        int? DiasParaVencer,
        int? DiasAtraso,
        int? AvisoNivel,     // 7 | 3 | 1 quando PrestesAVencer
        bool Bloqueada,
        DateOnly? VencimentoEm);

    public static Resultado Avaliar(Assinatura? a, DateOnly hoje, int toleranciaDias = 7)
    {
        // Sem assinatura ou cancelada => nunca bloqueia (segurança: não trava quem nunca teve cobrança).
        if (a is null || a.Cancelada)
            return new Resultado(EstadoAssinatura.SemAssinatura, null, null, null, false, null);

        // Em período de trial => livre até o fim do trial.
        if (a.TrialAte is { } trial && hoje <= trial)
            return new Resultado(EstadoAssinatura.Trial, null, null, null, false, a.VencimentoEm);

        // Trial acabou (não caiu no check acima) ou nunca houve trial. O vencimento efetivo é
        // VencimentoEm; na sua ausência, cai pro fim do trial (TrialAte) — assim o TRIAL FECHA:
        // passa a Vencido e, depois da tolerância, Bloqueada. Sem NENHUM dos dois (empresa legada
        // sem cobrança nem trial) => EmDia (default seguro, não trava quem nunca teve cobrança).
        var vencOpt = a.VencimentoEm ?? a.TrialAte;
        if (vencOpt is not { } venc)
            return new Resultado(EstadoAssinatura.EmDia, null, null, null, false, null);

        int diasParaVencer = venc.DayNumber - hoje.DayNumber;

        // hoje <= Venc-7 => Em dia.
        if (hoje <= venc.AddDays(-7))
            return new Resultado(EstadoAssinatura.EmDia, null, null, null, false, venc);

        // Venc-7 < hoje <= Venc => prestes a vencer (aviso escalonado 7/3/1).
        if (hoje <= venc)
        {
            int aviso = diasParaVencer <= 1 ? 1 : diasParaVencer <= 3 ? 3 : 7;
            return new Resultado(EstadoAssinatura.PrestesAVencer, diasParaVencer, null, aviso, false, venc);
        }

        int diasAtraso = hoje.DayNumber - venc.DayNumber;

        // Venc < hoje <= Venc+tolerância => vencido, mas ainda NÃO bloqueia (janela de tolerância).
        if (hoje <= venc.AddDays(toleranciaDias))
            return new Resultado(EstadoAssinatura.Vencido, null, diasAtraso, null, false, venc);

        // hoje > Venc+tolerância => BLOQUEADA (somente-leitura).
        return new Resultado(EstadoAssinatura.Bloqueada, null, diasAtraso, null, true, venc);
    }
}
