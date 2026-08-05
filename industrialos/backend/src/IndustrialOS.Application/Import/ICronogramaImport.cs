namespace IndustrialOS.Application.Import;

public record ItemCronograma(
    string Descricao,
    string? Unidade,
    decimal? QtdPrevista,
    decimal? HhPrevisto,
    decimal? Valor,
    string? Disciplina,
    DateOnly? DataInicio,
    DateOnly? DataFim);

/// <summary>Le um cronograma/EAP de Excel (.xlsx) ou CSV e devolve os itens.</summary>
public interface ICronogramaImport
{
    IReadOnlyList<ItemCronograma> Parse(Stream conteudo, string nomeArquivo);
}
