using IndustrialOS.Application.Common;
using IndustrialOS.Domain.Common;
using IndustrialOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IndustrialOS.Infrastructure.Persistence;

/// <summary>DbContext multi-tenant: Global Query Filter aplica tenant_id + soft delete.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Obra> Obras => Set<Obra>();
    public DbSet<ObraItem> ObraItens => Set<ObraItem>();
    public DbSet<UsuarioObra> UsuarioObras => Set<UsuarioObra>();
    public DbSet<FuncaoMaoObra> Funcoes => Set<FuncaoMaoObra>();
    public DbSet<Rdo> Rdos => Set<Rdo>();
    public DbSet<RdoMidia> RdoMidias => Set<RdoMidia>();
    public DbSet<CondicaoPagamento> CondicoesPagamento => Set<CondicaoPagamento>();
    public DbSet<FaturamentoPlano> FaturamentoPlanos => Set<FaturamentoPlano>();
    public DbSet<FaturamentoEvento> FaturamentoEventos => Set<FaturamentoEvento>();
    public DbSet<Medicao> Medicoes => Set<Medicao>();

    public Guid? CurrentTenant => tenant.TenantId;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Tenant>().ToTable("tenants");

        b.Entity<Empresa>(e =>
        {
            e.ToTable("empresas");
            e.Property(x => x.Config).HasColumnType("jsonb");
            e.HasIndex(x => x.TenantId);
        });

        b.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.Property(x => x.Funcao).HasConversion<string>();
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        });

        b.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.HasIndex(x => x.TenantId);
        });

        b.Entity<FuncaoMaoObra>(e =>
        {
            e.ToTable("funcoes");
            e.HasIndex(x => new { x.TenantId, x.Nome }).IsUnique();
        });

        b.Entity<Obra>(e =>
        {
            e.ToTable("obras");
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => x.TenantId);
        });

        b.Entity<ObraItem>(e =>
        {
            e.ToTable("obra_itens");
            e.HasIndex(x => x.ObraId);
        });

        b.Entity<UsuarioObra>(e =>
        {
            e.ToTable("usuario_obras");
            e.HasKey(x => new { x.UsuarioId, x.ObraId });
        });

        b.Entity<Rdo>(e =>
        {
            e.ToTable("rdos");
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Clima).HasColumnType("jsonb");
            e.Property(x => x.Jornada).HasColumnType("jsonb");
            e.Property(x => x.Dificuldades).HasColumnType("jsonb");
            e.Property(x => x.ProximoDia).HasColumnType("jsonb");
            e.Property(x => x.Planejamento).HasColumnType("jsonb");
            e.Property(x => x.Seguranca).HasColumnType("jsonb");
            e.Property(x => x.Assinaturas).HasColumnType("jsonb");
            e.HasIndex(x => new { x.ObraId, x.Numero }).IsUnique();
            e.HasIndex(x => x.TenantId);
            e.OwnsMany(x => x.Efetivo, o => { o.ToTable("rdo_efetivo"); o.HasKey(p => p.Id); o.WithOwner().HasForeignKey(p => p.RdoId); });
            e.OwnsMany(x => x.Paralisacoes, o => { o.ToTable("rdo_paralisacoes"); o.HasKey(p => p.Id); o.WithOwner().HasForeignKey(p => p.RdoId); });
            e.OwnsMany(x => x.Recursos, o => { o.ToTable("rdo_recursos"); o.HasKey(p => p.Id); o.WithOwner().HasForeignKey(p => p.RdoId); });
            e.OwnsMany(x => x.Servicos, o =>
            {
                o.ToTable("rdo_servicos");
                o.HasKey(p => p.Id);
                o.WithOwner().HasForeignKey(p => p.RdoId);
                o.Property(p => p.EtapasFeitas).HasColumnType("jsonb");
            });
            e.OwnsMany(x => x.Retrabalho, o => { o.ToTable("rdo_retrabalho"); o.HasKey(p => p.Id); o.WithOwner().HasForeignKey(p => p.RdoId); });
        });

        b.Entity<RdoMidia>(e =>
        {
            e.ToTable("rdo_midia");
            e.HasIndex(x => x.RdoId);
            e.HasOne<Rdo>().WithMany().HasForeignKey(x => x.RdoId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Faturamento / Medicao (Sprint 7) ----
        b.Entity<CondicaoPagamento>(e =>
        {
            e.ToTable("condicoes_pagamento");
            e.Property(x => x.Parcelas).HasColumnType("jsonb");
            e.HasIndex(x => x.TenantId);
        });

        b.Entity<FaturamentoPlano>(e =>
        {
            e.ToTable("faturamento_planos");
            e.HasIndex(x => x.ObraId);
            e.HasIndex(x => x.TenantId);
        });

        b.Entity<FaturamentoEvento>(e =>
        {
            e.ToTable("faturamento_eventos");
            e.HasIndex(x => x.FaturamentoPlanoId);
            e.HasIndex(x => x.TenantId);
        });

        b.Entity<Medicao>(e =>
        {
            e.ToTable("medicoes");
            e.HasIndex(x => new { x.ObraId, x.Numero }).IsUnique();
            e.HasIndex(x => x.TenantId);
            e.OwnsMany(x => x.Itens, o => { o.ToTable("medicao_itens"); o.HasKey(p => p.Id); o.WithOwner().HasForeignKey(p => p.MedicaoId); });
            e.OwnsMany(x => x.Parcelas, o => { o.ToTable("medicao_parcelas"); o.HasKey(p => p.Id); o.WithOwner().HasForeignKey(p => p.MedicaoId); });
        });

        // Filtro global de tenant + soft delete para toda BaseEntity.
        foreach (var et in b.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            var p = Expression.Parameter(et.ClrType, "e");
            var tenantId = Expression.Property(p, nameof(BaseEntity.TenantId));
            var current = Expression.Property(Expression.Constant(this), nameof(CurrentTenant));
            var byTenant = Expression.Equal(Expression.Convert(tenantId, typeof(Guid?)), current);
            var notDeleted = Expression.Equal(
                Expression.Property(p, nameof(BaseEntity.DeletadoEm)),
                Expression.Constant(null, typeof(DateTime?)));
            et.SetQueryFilter(Expression.Lambda(Expression.AndAlso(byTenant, notDeleted), p));
        }
    }

    public override int SaveChanges()
    {
        Stamp();
        return base.SaveChanges();
    }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        Stamp();
        return base.SaveChangesAsync(ct);
    }

    private void Stamp()
    {
        foreach (var e in ChangeTracker.Entries<BaseEntity>())
        {
            if (e.State == EntityState.Added && e.Entity.TenantId == Guid.Empty && tenant.TenantId is Guid t)
                e.Entity.TenantId = t;
            if (e.State == EntityState.Modified) e.Entity.AtualizadoEm = DateTime.UtcNow;
        }
    }
}
