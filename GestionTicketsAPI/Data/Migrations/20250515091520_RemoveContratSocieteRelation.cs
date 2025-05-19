using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTicketsAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveContratSocieteRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contrats_Societes_SocietePartenaireId",
                table: "Contrats");

            migrationBuilder.DropIndex(
                name: "IX_Contrats_SocietePartenaireId",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "SocietePartenaireId",
                table: "Contrats");

            migrationBuilder.AddColumn<string>(
                name: "CodePostal",
                table: "Societes",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Ville",
                table: "Societes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodePostal",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "Ville",
                table: "Societes");

            migrationBuilder.AddColumn<int>(
                name: "SocietePartenaireId",
                table: "Contrats",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_SocietePartenaireId",
                table: "Contrats",
                column: "SocietePartenaireId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contrats_Societes_SocietePartenaireId",
                table: "Contrats",
                column: "SocietePartenaireId",
                principalTable: "Societes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
