namespace IndustrialOS.Application.Pdf;

public record EfetivoRow(string? Funcao, int Quantidade, string? HoraExtra);
public record ParalisacaoRow(string? Inicio, string? Fim, string? Motivo, string? Descricao);
public record RecursoRow(string? Equipamento, int Quantidade, string? Horas);
public record ServicoRow(string? Atividade, string? Item, string? Status, decimal? QtdExec, string? Unidade, int PctItem);
public record RetrabalhoRow(string? Atividade, int Pessoas, decimal? Horas, string? Causa, string? AcaoCorretiva);
public record PendenciaRow(string? Descricao, string? Responsavel, string? Prazo, string? Status);
public record SegurancaModel(bool Dds, bool Apr, bool Pt, bool AreaIsolada, bool Epis, bool Ferramentas, string? Observacoes);
public record ProximoDiaModel(string? MaoObra, string? Equipamentos, string? Materiais, string? Ferramentas);
public record PlanejamentoModel(string? Servicos, string? Prioridades, string? Areas);
public record HorasModel(string DiaTipo, string Trabalhado, string? ExtraUtil, string? Extra100, string? Extra150);

public record RdoPdfModel(
    string ObraNome, string? Contrato, string? Cliente, string? Local,
    int Numero, int Revisao, string Data, string? DiaSemana, string? Turno, string? Responsavel, string Status,
    string[] ClimaCondicoes, int? Temperatura,
    string? JornInicio, string? JornAlmoco, string? JornRetorno, string? JornTermino, HorasModel? Horas,
    IReadOnlyList<EfetivoRow> Efetivo, IReadOnlyList<ParalisacaoRow> Paralisacoes,
    IReadOnlyList<RecursoRow> Recursos, IReadOnlyList<ServicoRow> Servicos,
    IReadOnlyList<RetrabalhoRow> Retrabalho, IReadOnlyList<PendenciaRow> Pendencias,
    SegurancaModel? Seguranca, ProximoDiaModel? ProximoDia, PlanejamentoModel? Planejamento,
    string? Dificuldades, string? Ocorrencias, int Fotos);

public interface IRdoPdf
{
    byte[] Gerar(RdoPdfModel m);
}
