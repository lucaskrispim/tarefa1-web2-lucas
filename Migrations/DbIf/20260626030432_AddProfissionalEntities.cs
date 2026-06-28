using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projeto1_Web2_IF_Lucas.Migrations.DbIf
{
    /// <inheritdoc />
    public partial class AddProfissionalEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbPlano",
                columns: table => new
                {
                    IdPlano = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: false),
                    Validade = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbPlano", x => x.IdPlano);
                });

            migrationBuilder.CreateTable(
                name: "tbTipoProfissional",
                columns: table => new
                {
                    IdTipoProfissional = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbTipoProfissional", x => x.IdTipoProfissional);
                });

            migrationBuilder.CreateTable(
                name: "tbContrato",
                columns: table => new
                {
                    IdContrato = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdPlano = table.Column<int>(type: "INTEGER", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataFim = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbContrato", x => x.IdContrato);
                    table.ForeignKey(
                        name: "FK_tbContrato_tbPlano",
                        column: x => x.IdPlano,
                        principalTable: "tbPlano",
                        principalColumn: "IdPlano",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbProfissional",
                columns: table => new
                {
                    IdProfissional = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdTipoProfissional = table.Column<int>(type: "INTEGER", nullable: true),
                    IdContrato = table.Column<int>(type: "INTEGER", nullable: false),
                    IdTipoAcesso = table.Column<int>(type: "INTEGER", nullable: true),
                    IdCidade = table.Column<int>(type: "INTEGER", nullable: false),
                    IdUser = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: false),
                    CPF = table.Column<string>(type: "TEXT", unicode: false, maxLength: 15, nullable: false),
                    CRM_CRN = table.Column<string>(type: "TEXT", unicode: false, maxLength: 20, nullable: true),
                    Especialidade = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true),
                    Logradouro = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true),
                    Numero = table.Column<string>(type: "TEXT", unicode: false, maxLength: 10, nullable: false),
                    Bairro = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: false),
                    CEP = table.Column<string>(type: "TEXT", unicode: false, maxLength: 10, nullable: false),
                    Cidade = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "TEXT", unicode: false, maxLength: 2, nullable: true),
                    DDD1 = table.Column<string>(type: "TEXT", unicode: false, maxLength: 2, nullable: true),
                    DDD2 = table.Column<string>(type: "TEXT", unicode: false, maxLength: 2, nullable: true),
                    Telefone1 = table.Column<string>(type: "TEXT", unicode: false, maxLength: 25, nullable: true),
                    Telefone2 = table.Column<string>(type: "TEXT", unicode: false, maxLength: 25, nullable: true),
                    Salario = table.Column<decimal>(type: "decimal(10, 2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbProfissional", x => x.IdProfissional);
                    table.ForeignKey(
                        name: "FK_tbProfissional_tbCidade",
                        column: x => x.IdCidade,
                        principalTable: "tbCidade",
                        principalColumn: "IdCidade",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbProfissional_tbContrato",
                        column: x => x.IdContrato,
                        principalTable: "tbContrato",
                        principalColumn: "IdContrato",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbProfissional_tbTipoProfissional",
                        column: x => x.IdTipoProfissional,
                        principalTable: "tbTipoProfissional",
                        principalColumn: "IdTipoProfissional");
                });

            migrationBuilder.CreateTable(
                name: "tbMedico_Paciente",
                columns: table => new
                {
                    IdMedico_Paciente = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdPaciente = table.Column<int>(type: "INTEGER", nullable: false),
                    IdProfissional = table.Column<int>(type: "INTEGER", nullable: false),
                    InformacaoResumida = table.Column<string>(type: "TEXT", unicode: false, maxLength: 5000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbMedico_Paciente", x => x.IdMedico_Paciente);
                    table.ForeignKey(
                        name: "FK_tbMedico_Paciente_tbPaciente",
                        column: x => x.IdPaciente,
                        principalTable: "tbPaciente",
                        principalColumn: "IdPaciente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbMedico_Paciente_tbProfissional",
                        column: x => x.IdProfissional,
                        principalTable: "tbProfissional",
                        principalColumn: "IdProfissional",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbContrato_IdPlano",
                table: "tbContrato",
                column: "IdPlano");

            migrationBuilder.CreateIndex(
                name: "IX_tbMedico_Paciente_IdPaciente",
                table: "tbMedico_Paciente",
                column: "IdPaciente");

            migrationBuilder.CreateIndex(
                name: "IX_tbMedico_Paciente_IdProfissional",
                table: "tbMedico_Paciente",
                column: "IdProfissional");

            migrationBuilder.CreateIndex(
                name: "IX_tbProfissional_IdCidade",
                table: "tbProfissional",
                column: "IdCidade");

            migrationBuilder.CreateIndex(
                name: "IX_tbProfissional_IdContrato",
                table: "tbProfissional",
                column: "IdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_tbProfissional_IdTipoAcesso",
                table: "tbProfissional",
                column: "IdTipoAcesso");

            migrationBuilder.CreateIndex(
                name: "IX_tbProfissional_IdTipoProfissional",
                table: "tbProfissional",
                column: "IdTipoProfissional");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbMedico_Paciente");

            migrationBuilder.DropTable(
                name: "tbProfissional");

            migrationBuilder.DropTable(
                name: "tbContrato");

            migrationBuilder.DropTable(
                name: "tbTipoProfissional");

            migrationBuilder.DropTable(
                name: "tbPlano");
        }
    }
}
