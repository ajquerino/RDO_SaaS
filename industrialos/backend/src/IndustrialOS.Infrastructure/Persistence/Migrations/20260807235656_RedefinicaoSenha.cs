using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedefinicaoSenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "redefinicoes_senha",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redefinicoes_senha", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_redefinicoes_senha_TokenHash",
                table: "redefinicoes_senha",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_redefinicoes_senha_UsuarioId",
                table: "redefinicoes_senha",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redefinicoes_senha");
        }
    }
}
