using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using IndustrialOS.Api.Common;
using IndustrialOS.Api.Filtros;
using IndustrialOS.Api.Middleware;
using IndustrialOS.Application.Auth;
using IndustrialOS.Application.Common;
using IndustrialOS.Infrastructure;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Config local (fora do Git) com segredos do R2 etc.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext() // expõe o CorrelationId injetado pelo middleware
    .WriteTo.Console());

// Infra: DbContext multi-tenant, ITenantContext, hasher, JWT.
builder.Services.AddInfrastructure(builder.Configuration);

// Usuário atual (para auditoria) + cache em memória + ProblemDetails.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();

// Rate limiting: protege os endpoints de auth (10 req/min por IP).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// JWT: mapeia o claim "funcao" como Role para habilitar [Authorize(Roles=...)].
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
        RoleClaimType = "funcao"
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers(o => o.Filters.Add<SomenteLeituraInadimplenteFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration["ConnectionStrings:Postgres"]!, name: "postgres");

var app = builder.Build();

// Migrations + seed. Migração é idempotente e roda em TODOS os ambientes (cria/atualiza o
// schema no 1º boot em produção). O seed de SISTEMA (tenant Plataforma + super-admin) também
// roda sempre; o seed de DEMO (empresa/admin/funções fictícios) só em Development.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedSistemaAsync(db, hasher, builder.Configuration);

    if (app.Environment.IsDevelopment())
        await DbSeeder.SeedDemoAsync(db, scope.ServiceProvider.GetRequiredService<ITenantContext>(), hasher);
}

// Swagger só em Development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Correlação + tratamento global de erros envolvem todo o pipeline.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>(); // apos autenticar: le o claim tenant_id
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { app = "IndustrialOS API", status = "ok" }));

app.Run();
