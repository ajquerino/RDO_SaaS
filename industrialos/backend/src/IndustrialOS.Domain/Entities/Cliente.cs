using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

public class Cliente : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? Contato { get; set; }
    public string? Endereco { get; set; }
}
