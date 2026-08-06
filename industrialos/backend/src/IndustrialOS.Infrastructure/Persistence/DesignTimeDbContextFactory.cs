using IndustrialOS.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IndustrialOS.Infrastructure.Persistence;

/// <summary>Usado pelo `dotnet ef migrations` (design-time), sem DI nem tenant real.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                 ?? "Host=localhost;Port=5432;Database=industrialos;Username=industrialos;Password=industrialos";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options;
        return new AppDbContext(options, new NoTenant(), new NoUsuario());
    }

    private class NoTenant : ITenantContext
    {
        public Guid? TenantId => null;
        public void Set(Guid tenantId) { }
    }

    private class NoUsuario : IUsuarioAtual
    {
        public Guid? UsuarioId => null;
    }
}
