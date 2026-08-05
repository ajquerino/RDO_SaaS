using IndustrialOS.Domain.Entities;

namespace IndustrialOS.Domain.Services;

/// <summary>Regra de avanco validada no MVP: o % do item e o MAIOR valor informado.</summary>
public static class AvancoCalculo
{
    /// <summary>pct_item = max(concluido?1, pct_informado/100, qtd_exec/qtd_prev). 0..1, nunca &gt;1.</summary>
    public static decimal PctItem(RdoServico s, ObraItem? item)
    {
        decimal pct = 0m;

        if (string.Equals(s.Status, "Concluido", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.Status, "Concluida", StringComparison.OrdinalIgnoreCase))
            pct = 1m;

        if (s.PctInformado is { } p) pct = Math.Max(pct, p / 100m);

        if (s.QtdExec is { } qe && item?.QtdPrevista is { } qp && qp > 0)
            pct = Math.Max(pct, qe / qp);

        return Math.Clamp(pct, 0m, 1m);
    }
}
