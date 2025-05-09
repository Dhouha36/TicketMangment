using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTicketsAPI.Migrations
{
    /// <inheritdoc />
    public partial class commentPhotot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Pays_PaysId",
                table: "Photos");

            migrationBuilder.AlterColumn<int>(
                name: "PaysId",
                table: "Photos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CommentaireId",
                table: "Photos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_CommentaireId",
                table: "Photos",
                column: "CommentaireId");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Commentaires_CommentaireId",
                table: "Photos",
                column: "CommentaireId",
                principalTable: "Commentaires",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Pays_PaysId",
                table: "Photos",
                column: "PaysId",
                principalTable: "Pays",
                principalColumn: "id_pays");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Commentaires_CommentaireId",
                table: "Photos");

            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Pays_PaysId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_CommentaireId",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "CommentaireId",
                table: "Photos");

            migrationBuilder.AlterColumn<int>(
                name: "PaysId",
                table: "Photos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Pays_PaysId",
                table: "Photos",
                column: "PaysId",
                principalTable: "Pays",
                principalColumn: "id_pays",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
