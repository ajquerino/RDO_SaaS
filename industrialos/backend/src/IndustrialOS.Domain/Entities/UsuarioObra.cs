namespace IndustrialOS.Domain.Entities;

/// <summary>Vinculo N:N usuario-obra: define quais obras cada usuario responde.</summary>
public class UsuarioObra
{
    public Guid UsuarioId { get; set; }
    public Guid ObraId { get; set; }
}
