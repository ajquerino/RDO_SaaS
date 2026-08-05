using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetrabalhoAssinaturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Assinaturas",
                table: "rdos",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "rdo_retrabalho",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RdoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Atividade = table.Column<string>(type: "text", nullable: true),
                    Local = table.Column<string>(type: "text", nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: true),
                    Unidade = table.Column<string>(type: "text", nullable: true),
                    Pessoas = table.Column<int>(type: "integer", nullable: false),
                    Horas = table.Column<decimal>(type: "numeric", nullable: true),
                    Causa = table.Column<string>(type: "text", nullable: true),
                    Origem = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    AcaoCorretiva = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rdo_retrabalho", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rdo_retrabalho_rdos_RdoId",
                        column: x => x.RdoId,
                        principalTable: "rdos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rdo_retrabalho_RdoId",
                table: "rdo_retrabalho",
                column: "RdoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rdo_retrabalho");

            migrationBuilder.DropColumn(
                name: "Assinaturas",
                table: "rdos");
        }
    }
}
