using IndustrialOS.Application.Auth;
using IndustrialOS.Application.Common;
using IndustrialOS.Infrastructure.Auth;
using IndustrialOS.Infrastructure.Multitenancy;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration c)
    {
        s.AddScoped<ITenantContext, TenantContext>();
        s.AddDbContext<AppDbContext>(o => o.UseNpgsql(c["ConnectionStrings:Postgres"]));
        s.AddSingleton<IPasswordHasher, PasswordHasher>();
        s.AddScoped<IJwtService, JwtService>();
        s.AddScoped<Application.Import.ICronogramaImport, Import.CronogramaImport>();
        s.AddSingleton<Application.Pdf.IRdoPdf, Pdf.RdoPdf>();
        s.AddSingleton<Application.Pdf.IMedicaoPdf, Pdf.MedicaoPdf>();
        s.AddSingleton<Application.Storage.IStorage, Storage.R2Storage>();

        // IA: por ora só o stub por regras (sem rede/sem provedor). Para ligar IA de verdade,
        // criar ResumoIaLlm : IResumoIa (chamando Anthropic/OpenAI) e trocar este registro —
        // a interface (IResumoIa) e o contexto (RdoContexto) já estão prontos.
        s.AddScoped<Application.Ia.IResumoIa, Ia.ResumoIaRegras>();
        return s;
    }
}
