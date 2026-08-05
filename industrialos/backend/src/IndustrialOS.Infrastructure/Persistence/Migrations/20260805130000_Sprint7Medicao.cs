using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7Medicao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "condicoes_pagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Parcelas = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_condicoes_pagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "faturamento_eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FaturamentoPlanoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Base = table.Column<string>(type: "text", nullable: false),
                    Percentual = table.Column<decimal>(type: "numeric", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric", nullable: true),
                    Gatilho = table.Column<string>(type: "text", nullable: true),
                    DataPrevista = table.Column<DateOnly>(type: "date", nullable: true),
                    Recorrencia = table.Column<string>(type: "text", nullable: true),
                    CondicaoPagamentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faturamento_eventos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "faturamento_planos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    ValorContrato = table.Column<decimal>(type: "numeric", nullable: false),
                    CondicaoPagamentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faturamento_planos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medicoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObraId = table.Column<Guid>(type: "uuid", nullable: false),
                    FaturamentoEventoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    De = table.Column<DateOnly>(type: "date", nullable: false),
                    Ate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorContrato = table.Column<decimal>(type: "numeric", nullable: false),
                    MedidoAcumulado = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorPeriodo = table.Column<decimal>(type: "numeric", nullable: false),
                    PctFisico = table.Column<decimal>(type: "numeric", nullable: false),
                    PctFinanceiro = table.Column<decimal>(type: "numeric", nullable: false),
                    PdfR2Key = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TokenAprovacao = table.Column<string>(type: "text", nullable: true),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AprovadoPor = table.Column<string>(type: "text", nullable: true),
                    AprovadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medicao_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObraItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PctIni = table.Column<decimal>(type: "numeric", nullable: false),
                    PctFim = table.Column<decimal>(type: "numeric", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    MedidoPeriodo = table.Column<decimal>(type: "numeric", nullable: false),
                    MedidoAcum = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicao_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medicao_itens_medicoes_MedicaoId",
                        column: x => x.MedicaoId,
                        principalTable: "medicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medicao_parcelas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dias = table.Column<int>(type: "integer", nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    Pct = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicao_parcelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_medicao_parcelas_medicoes_MedicaoId",
                        column: x => x.MedicaoId,
                        principalTable: "medicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_condicoes_pagamento_TenantId",
                table: "condicoes_pagamento",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_faturamento_eventos_FaturamentoPlanoId",
                table: "faturamento_eventos",
                column: "FaturamentoPlanoId");

            migrationBuilder.CreateIndex(
                name: "IX_faturamento_eventos_TenantId",
                table: "faturamento_eventos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_faturamento_planos_ObraId",
                table: "faturamento_planos",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_faturamento_planos_TenantId",
                table: "faturamento_planos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_medicao_itens_MedicaoId",
                table: "medicao_itens",
                column: "MedicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_medicao_parcelas_MedicaoId",
                table: "medicao_parcelas",
                column: "MedicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_medicoes_ObraId_Numero",
                table: "medicoes",
                columns: new[] { "ObraId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medicoes_TenantId",
                table: "medicoes",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "condicoes_pagamento");

            migrationBuilder.DropTable(
                name: "faturamento_eventos");

            migrationBuilder.DropTable(
                name: "faturamento_planos");

            migrationBuilder.DropTable(
                name: "medicao_itens");

            migrationBuilder.DropTable(
                name: "medicao_parcelas");

            migrationBuilder.DropTable(
                name: "medicoes");
        }
    }
}
