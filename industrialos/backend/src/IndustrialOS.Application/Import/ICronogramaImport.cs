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

/// <summary>Resultado da análise de uma planilha: cabeçalho (na ordem) + só as linhas de dados (como texto).</summary>
public record AnaliseCronograma(IReadOnlyList<string> Colunas, IReadOnlyList<IReadOnlyList<string>> Linhas);

/// <summary>Mapeamento coluna→campo escolhido pelo usuário. Índices 0-based; -1/null = não mapeado.</summary>
public record MapeamentoColunas(
    int Descricao, int? Unidade, int? Qtd, int? Hh, int? Valor, int? Disciplina, int? Inicio, int? Fim);

/// <summary>Le um cronograma/EAP de Excel (.xlsx) ou CSV e devolve os itens.</summary>
public interface ICronogramaImport
{
    /// <summary>Fluxo antigo (cabeçalho por alias): lê e já mapeia. Preservado.</summary>
    IReadOnlyList<ItemCronograma> Parse(Stream conteudo, string nomeArquivo);

    /// <summary>Passo 1 do fluxo mapeável: lê o arquivo e devolve colunas + linhas de dados (texto).</summary>
    AnaliseCronograma Analisar(Stream conteudo, string nomeArquivo);

    /// <summary>Sugestão de mapeamento a partir dos aliases conhecidos do cabeçalho (mesma lógica do Parse).</summary>
    MapeamentoColunas Sugerir(IReadOnlyList<string> colunas);

    /// <summary>Passo 2: aplica o mapeamento escolhido às linhas de dados e devolve os itens.</summary>
    IReadOnlyList<ItemCronograma> Mapear(IReadOnlyList<IReadOnlyList<string>> linhas, MapeamentoColunas map);
}
