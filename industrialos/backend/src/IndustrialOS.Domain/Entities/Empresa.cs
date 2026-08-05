using IndustrialOS.Domain.Common;

namespace IndustrialOS.Domain.Entities;

/// <summary>Empresa/filial do cliente dentro de um tenant (1 tenant tem 1..n empresas).</summary>
public class Empresa : BaseEntity
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? LogoUrl { get; set; }
    public Guid? MatrizId { get; set; }        // self-fk: filial aponta para a matriz
    public string Config { get; set; } = "{}"; // jsonb
}
