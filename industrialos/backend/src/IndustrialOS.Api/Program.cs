using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
builder.Services.AddScoped<SessaoService>(); // 1 sessão por usuário (enforcement no JwtBearer)
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
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            RoleClaimType = "funcao"
        };
        // 1 sessão por usuário: rejeita tokens cuja sessão não bate com a vigente (kick imediato).
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                // MODO SUPORTE: token emitido pelo super-admin para agir COMO o admin de uma empresa.
                // É curto (1h), auditado na emissão e NÃO participa da sessão única — não derruba a sessão
                // real do admin nem é derrubado por ela. Por isso pula o enforcement de sessão aqui.
                if (ctx.Principal?.FindFirst("suporte") is not null) return;

                var sub = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!Guid.TryParse(sub, out var userId)) { ctx.Fail("sem sub"); return; }

                Guid.TryParse(ctx.Principal?.FindFirst("sessao")?.Value, out var sessaoToken);
                var sessoes = ctx.HttpContext.RequestServices.GetRequiredService<SessaoService>();
                var atual = await sessoes.SessaoAtualAsync(userId);
                if (atual is null || atual != sessaoToken)
                {
                    // Cache pode estar velho (ex.: logo após o login, uma leitura concorrente repovoou
                    // o cache com a sessão antiga). Confirma no banco (autoritativo) antes de derrubar,
                    // evitando falso 401. O kick real continua: se o banco também divergir, falha.
                    atual = await sessoes.RevalidarNoBancoAsync(userId);
                    if (atual is null || atual != sessaoToken)
                    {
                        ctx.HttpContext.Items["sessaoEncerrada"] = true;
                        ctx.Fail("sessao encerrada em outro dispositivo");
                    }
                }
            },
            OnChallenge = ctx =>
            {
                // Sinal p/ o front separar "sessão encerrada" de "token expirado".
                if (ctx.HttpContext.Items.ContainsKey("sessaoEncerrada"))
                    ctx.Response.Headers["X-Sessao"] = "encerrada";
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers(o => o.Filters.Add<SomenteLeituraInadimplenteFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()
        .WithExposedHeaders("X-Sessao"))); // front precisa ler esse header no 401 cross-origin
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

    // Conta de exemplo (obra + EAP + 6 RDOs): as contas JÁ EXISTENTES ganham o exemplo no próximo deploy.
    // Idempotente (marcador "[EXEMPLO]"), roda em TODOS os ambientes; cada tenant em try/catch (não quebra o boot).
    if (ContaExemploSeeder.Habilitado(builder.Configuration))
    {
        var logExemplo = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ContaExemploSeeder");
        await ContaExemploSeeder.BackfillAsync(db, logExemplo);
    }

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
