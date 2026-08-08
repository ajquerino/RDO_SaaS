using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Assinatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assinaturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoId = table.Column<Guid>(type: "uuid", nullable: true),
                    VencimentoEm = table.Column<DateOnly>(type: "date", nullable: true),
                    TrialAte = table.Column<DateOnly>(type: "date", nullable: true),
                    Cancelada = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProvedorNome = table.Column<string>(type: "text", nullable: true),
                    ProvedorCustomerId = table.Column<string>(type: "text", nullable: true),
                    ProvedorAssinaturaId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assinaturas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assinaturas_TenantId",
                table: "assinaturas",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assinaturas");
        }
    }
}
