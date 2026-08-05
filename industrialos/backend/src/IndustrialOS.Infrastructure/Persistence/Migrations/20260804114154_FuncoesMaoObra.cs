using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FuncoesMaoObra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "funcoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    CustoHh = table.Column<decimal>(type: "numeric", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funcoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_funcoes_TenantId_Nome",
                table: "funcoes",
                columns: new[] { "TenantId", "Nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "funcoes");
        }
    }
}
