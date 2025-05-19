using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTicketsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddContratsNavigationToProjet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contrats_Projets_ProjetId",
                table: "Contrats");

            migrationBuilder.AddForeignKey(
                name: "FK_Contrats_Projets_ProjetId",
                table: "Contrats",
                column: "ProjetId",
                principalTable: "Projets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contrats_Projets_ProjetId",
                table: "Contrats");

            migrationBuilder.AddForeignKey(
                name: "FK_Contrats_Projets_ProjetId",
                table: "Contrats",
                column: "ProjetId",
                principalTable: "Projets",
                principalColumn: "Id");
        }
    }
}
