namespace IndustrialOS.Application.Pdf;

public record MedicaoItemRow(string Item, decimal PctIni, decimal PctFim, decimal Valor, decimal MedidoPeriodo, decimal MedidoAcum);
public record MedicaoParcelaRow(int Dias, string Vencimento, decimal Valor, decimal Pct);

public record MedicaoPdfModel(
    string ObraNome, string? Contrato, string? Cliente, string? Local,
    int Numero, string De, string Ate, string Status,
    decimal ValorContrato, decimal MedidoAcumulado, decimal ValorPeriodo, decimal PctFisico, decimal PctFinanceiro,
    IReadOnlyList<MedicaoItemRow> Itens, IReadOnlyList<MedicaoParcelaRow> Parcelas,
    string? CriadoPor);

public interface IMedicaoPdf
{
    byte[] Gerar(MedicaoPdfModel m);
}
