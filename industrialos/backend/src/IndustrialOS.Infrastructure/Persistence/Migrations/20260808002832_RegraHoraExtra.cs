using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegraHoraExtra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "regras_hora_extra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LimiteSemanalHoras = table.Column<decimal>(type: "numeric", nullable: false),
                    PercentUtilFaixa1 = table.Column<decimal>(type: "numeric", nullable: false),
                    PercentUtilFaixa2 = table.Column<decimal>(type: "numeric", nullable: false),
                    SabadoPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    SabadoLimiteHoras = table.Column<decimal>(type: "numeric", nullable: false),
                    SabadoPercentAcima = table.Column<decimal>(type: "numeric", nullable: false),
                    SabadoUsaCorteHorario = table.Column<bool>(type: "boolean", nullable: false),
                    SabadoHoraCorte = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    SabadoPercentAposCorte = table.Column<decimal>(type: "numeric", nullable: true),
                    DomingoPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    DomingoLimiteHoras = table.Column<decimal>(type: "numeric", nullable: false),
                    DomingoPercentAcima = table.Column<decimal>(type: "numeric", nullable: false),
                    FeriadoPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    FeriadoLimiteHoras = table.Column<decimal>(type: "numeric", nullable: false),
                    FeriadoPercentAcima = table.Column<decimal>(type: "numeric", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regras_hora_extra", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_regras_hora_extra_TenantId",
                table: "regras_hora_extra",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "regras_hora_extra");
        }
    }
}
