using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RdoNucleo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rdos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Revisao = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    DiaSemana = table.Column<string>(type: "text", nullable: true),
                    Turno = table.Column<string>(type: "text", nullable: true),
                    ResponsavelUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Clima = table.Column<string>(type: "jsonb", nullable: false),
                    Jornada = table.Column<string>(type: "jsonb", nullable: false),
                    Ocorrencias = table.Column<string>(type: "text", nullable: true),
                    Dificuldades = table.Column<string>(type: "jsonb", nullable: false),
                    ProximoDia = table.Column<string>(type: "jsonb", nullable: false),
                    Planejamento = table.Column<string>(type: "jsonb", nullable: false),
                    Seguranca = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TokenAprovacao = table.Column<string>(type: "text", nullable: true),
                    AprovadoPor = table.Column<string>(type: "text", nullable: true),
                    AprovadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PdfR2Key = table.Column<string>(type: "text", nullable: true),
                    EnviadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    EnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rdos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rdo_efetivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RdoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Funcao = table.Column<string>(type: "text", nullable: true),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    Entrada = table.Column<string>(type: "text", nullable: true),
                    Saida = table.Column<string>(type: "text", nullable: true),
                    HoraExtra = table.Column<string>(type: "text", nullable: true),
                    Obs = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rdo_efetivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rdo_efetivo_rdos_RdoId",
                        column: x => x.RdoId,
                        principalTable: "rdos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rdo_paralisacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RdoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Inicio = table.Column<string>(type: "text", nullable: true),
                    Fim = table.Column<string>(type: "text", nullable: true),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rdo_paralisacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rdo_paralisacoes_rdos_RdoId",
                        column: x => x.RdoId,
                        principalTable: "rdos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rdo_recursos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RdoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Equipamento = table.Column<string>(type: "text", nullable: true),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    Horas = table.Column<string>(type: "text", nullable: true),
                    Obs = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rdo_recursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rdo_recursos_rdos_RdoId",
                        column: x => x.RdoId,
                        principalTable: "rdos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rdo_servicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RdoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObraItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Atividade = table.Column<string>(type: "text", nullable: true),
                    Local = table.Column<string>(type: "text", nullable: true),
                    QtdExec = table.Column<decimal>(type: "numeric", nullable: true),
                    Unidade = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    PctInformado = table.Column<decimal>(type: "numeric", nullable: true),
                    EtapasFeitas = table.Column<string>(type: "jsonb", nullable: false),
                    MotivoHold = table.Column<string>(type: "text", nullable: true),
                    Obs = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rdo_servicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rdo_servicos_rdos_RdoId",
                        column: x => x.RdoId,
                        principalTable: "rdos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rdo_efetivo_RdoId",
                table: "rdo_efetivo",
                column: "RdoId");

            migrationBuilder.CreateIndex(
                name: "IX_rdo_paralisacoes_RdoId",
                table: "rdo_paralisacoes",
                column: "RdoId");

            migrationBuilder.CreateIndex(
                name: "IX_rdo_recursos_RdoId",
                table: "rdo_recursos",
                column: "RdoId");

            migrationBuilder.CreateIndex(
                name: "IX_rdo_servicos_RdoId",
                table: "rdo_servicos",
                column: "RdoId");

            migrationBuilder.CreateIndex(
                name: "IX_rdos_ObraId_Numero",
                table: "rdos",
                columns: new[] { "ObraId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rdos_TenantId",
                table: "rdos",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rdo_efetivo");

            migrationBuilder.DropTable(
                name: "rdo_paralisacoes");

            migrationBuilder.DropTable(
                name: "rdo_recursos");

            migrationBuilder.DropTable(
                name: "rdo_servicos");

            migrationBuilder.DropTable(
                name: "rdos");
        }
    }
}
