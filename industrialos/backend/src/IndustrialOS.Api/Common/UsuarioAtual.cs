using System.Security.Claims;
using IndustrialOS.Application.Common;

namespace IndustrialOS.Api.Common;

/// <summary>Lê o id do usuário logado do claim do JWT (via HttpContext). Nulo fora de um request.</summary>
public class UsuarioAtual(IHttpContextAccessor http) : IUsuarioAtual
{
    public Guid? UsuarioId
    {
        get
        {
            var u = http.HttpContext?.User;
            var raw = u?.FindFirstValue(ClaimTypes.NameIdentifier) ?? u?.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
