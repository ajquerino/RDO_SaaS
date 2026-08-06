using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint9Hardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentimentoEm",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConsentimentoLgpd",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanoId",
                table: "tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Acao = table.Column<string>(type: "text", nullable: false),
                    Entidade = table.Column<string>(type: "text", nullable: false),
                    EntidadeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Detalhe = table.Column<string>(type: "jsonb", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "planos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    LimiteObras = table.Column<int>(type: "integer", nullable: true),
                    LimiteUsuarios = table.Column<int>(type: "integer", nullable: true),
                    PrecoMensal = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_CriadoEm",
                table: "auditoria",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_Entidade",
                table: "auditoria",
                column: "Entidade");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_TenantId",
                table: "auditoria",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropTable(
                name: "planos");

            migrationBuilder.DropColumn(
                name: "ConsentimentoEm",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "ConsentimentoLgpd",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "PlanoId",
                table: "tenants");
        }
    }
}
