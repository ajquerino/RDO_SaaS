namespace IndustrialOS.Domain.Entities;

/// <summary>Documento da obra (projeto, desenho, procedimento, arquivo). Guardado no R2; aqui só a referência
/// (r2_key). Segue o padrão de RdoMidia: não é BaseEntity — o isolamento vem da obra (FK).</summary>
public class Documento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ObraId { get; set; }
    public string Tipo { get; set; } = "arquivo"; // projeto | desenho | procedimento | arquivo
    public string Nome { get; set; } = string.Empty;
    public string R2Key { get; set; } = string.Empty;
    public string? Versao { get; set; }
    public long TamanhoBytes { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
