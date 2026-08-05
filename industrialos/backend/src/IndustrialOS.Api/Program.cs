using System.Text;
using System.Text.Json.Serialization;
using IndustrialOS.Api.Middleware;
using IndustrialOS.Application.Auth;
using IndustrialOS.Application.Common;
using IndustrialOS.Infrastructure;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Config local (fora do Git) com segredos do R2 etc.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// Infra: DbContext multi-tenant, ITenantContext, hasher, JWT.
builder.Services.AddInfrastructure(builder.Configuration);

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

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration["ConnectionStrings:Postgres"]!, name: "postgres");

var app = builder.Build();

// Migrations + seed de dev automaticos.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db,
        scope.ServiceProvider.GetRequiredService<ITenantContext>(),
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>());

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>(); // apos autenticar: le o claim tenant_id
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { app = "IndustrialOS API", status = "ok" }));

app.Run();
