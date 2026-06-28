using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projeto1_Web2_IF_Lucas.Migrations.DbIf
{
    /// <inheritdoc />
    public partial class InitialDbIf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbCidade",
                columns: table => new
                {
                    IdCidade = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdEstado = table.Column<int>(type: "INTEGER", nullable: true),
                    nome = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbCidade", x => x.IdCidade);
                });

            migrationBuilder.CreateTable(
                name: "tbPaciente",
                columns: table => new
                {
                    IdPaciente = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: false),
                    RG = table.Column<string>(type: "TEXT", unicode: false, maxLength: 15, nullable: false),
                    CPF = table.Column<string>(type: "TEXT", unicode: false, maxLength: 15, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NomeResponsavel = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true),
                    Sexo = table.Column<string>(type: "TEXT", unicode: false, maxLength: 1, nullable: false),
                    Etnia = table.Column<int>(type: "INTEGER", nullable: false),
                    Endereco = table.Column<string>(type: "TEXT", unicode: false, maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "TEXT", unicode: false, maxLength: 50, nullable: true),
                    IdCidade = table.Column<int>(type: "INTEGER", nullable: true),
                    TelResidencial = table.Column<string>(type: "TEXT", unicode: false, maxLength: 25, nullable: true),
                    TelComercial = table.Column<string>(type: "TEXT", unicode: false, maxLength: 25, nullable: true),
                    TelCelular = table.Column<string>(type: "TEXT", unicode: false, maxLength: 25, nullable: true),
                    Profissao = table.Column<string>(type: "TEXT", unicode: false, maxLength: 30, nullable: true),
                    FlgAtleta = table.Column<bool>(type: "INTEGER", nullable: true),
                    FlgGestante = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbPaciente", x => x.IdPaciente);
                    table.ForeignKey(
                        name: "FK_tbPaciente_tbCidade",
                        column: x => x.IdCidade,
                        principalTable: "tbCidade",
                        principalColumn: "IdCidade");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbPaciente_IdCidade",
                table: "tbPaciente",
                column: "IdCidade");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbPaciente");

            migrationBuilder.DropTable(
                name: "tbCidade");
        }
    }
}
