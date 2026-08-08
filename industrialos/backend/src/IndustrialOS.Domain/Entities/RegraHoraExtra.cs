using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Regras de hora-extra POR EMPRESA (uma por tenant). Os defaults reproduzem exatamente o
/// comportamento anterior (fixo no código): dia útil 50% até 10h/semana e 70% acima; sáb/dom/feriado
/// 100% até 8h e 150% acima. Uma instância "nova" (sem persistir) já é o default seguro.</summary>
public class RegraHoraExtra : BaseEntity
{
    // ---- Dia útil (acúmulo SEMANAL por pessoa) ----
    public decimal LimiteSemanalHoras { get; set; } = 10;   // 1ª faixa até X h extras na semana
    public decimal PercentUtilFaixa1 { get; set; } = 50;    // % até o limite semanal
    public decimal PercentUtilFaixa2 { get; set; } = 70;    // % acima do limite semanal

    // ---- Sábado ----
    public decimal SabadoPercent { get; set; } = 100;       // % base (até o limite diário)
    public decimal SabadoLimiteHoras { get; set; } = 8;
    public decimal SabadoPercentAcima { get; set; } = 150;  // % acima do limite diário
    public bool SabadoUsaCorteHorario { get; set; } = false; // se true, ignora o limite por horas e usa o corte por horário
    public TimeOnly? SabadoHoraCorte { get; set; }           // ex.: 12:00
    public decimal? SabadoPercentAposCorte { get; set; }     // ex.: após meio-dia, 70%

    // ---- Domingo ----
    public decimal DomingoPercent { get; set; } = 100;
    public decimal DomingoLimiteHoras { get; set; } = 8;
    public decimal DomingoPercentAcima { get; set; } = 150;

    // ---- Feriado ----
    public decimal FeriadoPercent { get; set; } = 100;
    public decimal FeriadoLimiteHoras { get; set; } = 8;
    public decimal FeriadoPercentAcima { get; set; } = 150;
}
