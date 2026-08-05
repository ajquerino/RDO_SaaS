namespace IndustrialOS.Domain.Entities;

/// <summary>Foto ou vídeo anexado a um RDO. Guardado no R2; aqui só a referência (r2_key).</summary>
public class RdoMidia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RdoId { get; set; }
    public string Tipo { get; set; } = "foto";     // foto | video
    public string R2Key { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string? Descricao { get; set; }
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public long TamanhoBytes { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
