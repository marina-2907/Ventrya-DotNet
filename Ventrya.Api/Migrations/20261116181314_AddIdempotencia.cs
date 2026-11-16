using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ventrya.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosIdempotencia",
                columns: table => new
                {
                    Chave = table.Column<string>(type: "TEXT", nullable: false),
                    HashRequisicao = table.Column<string>(type: "TEXT", nullable: false),
                    RespostaJson = table.Column<string>(type: "TEXT", nullable: false),
                    Localizacao = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosIdempotencia", x => x.Chave);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosIdempotencia");
        }
    }
}
