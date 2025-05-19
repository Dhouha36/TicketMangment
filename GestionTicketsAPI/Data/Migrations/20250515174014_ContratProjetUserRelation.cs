using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTicketsAPI.Migrations
{
    /// <inheritdoc />
    public partial class ContratProjetUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contrats_Users_ClientId",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "TypeContrat",
                table: "Contrats");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "Contrats",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Contrats_ClientId",
                table: "Contrats",
                newName: "IX_Contrats_UserId");

            migrationBuilder.AddColumn<int>(
                name: "ProjetId",
                table: "Contrats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Contrats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Contrats_ProjetId",
                table: "Contrats",
                column: "ProjetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contrats_Projets_ProjetId",
                table: "Contrats",
                column: "ProjetId",
                principalTable: "Projets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contrats_Users_UserId",
                table: "Contrats",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contrats_Projets_ProjetId",
                table: "Contrats");

            migrationBuilder.DropForeignKey(
                name: "FK_Contrats_Users_UserId",
                table: "Contrats");

            migrationBuilder.DropIndex(
                name: "IX_Contrats_ProjetId",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "ProjetId",
                table: "Contrats");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Contrats");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Contrats",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_Contrats_UserId",
                table: "Contrats",
                newName: "IX_Contrats_ClientId");

            migrationBuilder.AddColumn<string>(
                name: "TypeContrat",
                table: "Contrats",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Contrats_Users_ClientId",
                table: "Contrats",
                column: "ClientId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
