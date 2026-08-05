using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClientesObrasEap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Cnpj = table.Column<string>(type: "text", nullable: true),
                    Contato = table.Column<string>(type: "text", nullable: true),
                    Endereco = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "obra_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Unidade = table.Column<string>(type: "text", nullable: true),
                    QtdPrevista = table.Column<decimal>(type: "numeric", nullable: true),
                    HhPrevisto = table.Column<decimal>(type: "numeric", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric", nullable: true),
                    Disciplina = table.Column<string>(type: "text", nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: true),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obra_itens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "obras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Contrato = table.Column<string>(type: "text", nullable: true),
                    OrdemServico = table.Column<string>(type: "text", nullable: true),
                    Local = table.Column<string>(type: "text", nullable: true),
                    FrenteServico = table.Column<string>(type: "text", nullable: true),
                    ResponsavelPadrao = table.Column<string>(type: "text", nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: true),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    PrazoPagamento = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuario_obras",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_obras", x => new { x.UsuarioId, x.ObraId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_TenantId",
                table: "clientes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_obra_itens_ObraId",
                table: "obra_itens",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_obras_TenantId",
                table: "obras",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "obra_itens");

            migrationBuilder.DropTable(
                name: "obras");

            migrationBuilder.DropTable(
                name: "usuario_obras");
        }
    }
}
