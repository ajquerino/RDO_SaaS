using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RdoMidia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rdo_midia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RdoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    R2Key = table.Column<string>(type: "text", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Lat = table.Column<double>(type: "double precision", nullable: true),
                    Lon = table.Column<double>(type: "double precision", nullable: true),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rdo_midia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rdo_midia_rdos_RdoId",
                        column: x => x.RdoId,
                        principalTable: "rdos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rdo_midia_RdoId",
                table: "rdo_midia",
                column: "RdoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rdo_midia");
        }
    }
}
