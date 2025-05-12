using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionTicketsAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCommNotifClientEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Client_Pays_PaysNavigationIdPays",
                table: "Client",
                column: "PaysNavigationIdPays",
                principalTable: "Pays",
                principalColumn: "id_pays",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Client_Societes_SocieteId",
                table: "Client",
                column: "SocieteId",
                principalTable: "Societes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Commentaires_Client_ClientId",
                table: "Commentaires",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Commentaires_Users_UserId",
                table: "Commentaires",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Client_ClientId",
                table: "Notification",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Users_UserId",
                table: "Notification",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Pays_PaysId",
                table: "Photos",
                column: "PaysId",
                principalTable: "Pays",
                principalColumn: "id_pays",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetClients_Client_ClientId",
                table: "ProjetClients",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projets_Pays_id_pays",
                table: "Projets",
                column: "id_pays",
                principalTable: "Pays",
                principalColumn: "id_pays",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Client_OwnerId",
                table: "Tickets",
                column: "OwnerId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_Pays_PaysNavigationIdPays",
                table: "Client");

            migrationBuilder.DropForeignKey(
                name: "FK_Client_Societes_SocieteId",
                table: "Client");

            migrationBuilder.DropForeignKey(
                name: "FK_Commentaires_Client_ClientId",
                table: "Commentaires");

            migrationBuilder.DropForeignKey(
                name: "FK_Commentaires_Users_UserId",
                table: "Commentaires");

            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Client_ClientId",
                table: "Notification");

            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Users_UserId",
                table: "Notification");

            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Pays_PaysId",
                table: "Photos");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjetClients_Client_ClientId",
                table: "ProjetClients");

            migrationBuilder.DropForeignKey(
                name: "FK_Projets_Pays_id_pays",
                table: "Projets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Client_OwnerId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Notification_ClientId",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_Notification_UserId",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_Commentaires_ClientId",
                table: "Commentaires");

            migrationBuilder.DropIndex(
                name: "IX_Commentaires_UserId",
                table: "Commentaires");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Client",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Commentaires");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Commentaires");

            migrationBuilder.RenameTable(
                name: "Client",
                newName: "Clients");

            migrationBuilder.RenameIndex(
                name: "IX_Client_SocieteId",
                table: "Clients",
                newName: "IX_Clients_SocieteId");

            migrationBuilder.RenameIndex(
                name: "IX_Client_PaysNavigationIdPays",
                table: "Clients",
                newName: "IX_Clients_PaysNavigationIdPays");

            migrationBuilder.AddColumn<int>(
                name: "UtilisateurId",
                table: "Notification",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UtilisateurId",
                table: "Commentaires",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Clients",
                table: "Clients",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projet_Association",
                table: "Projets",
                sql: "((SocieteId IS NOT NULL AND ClientId IS NULL) OR (SocieteId IS NULL AND ClientId IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UtilisateurId",
                table: "Notification",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Commentaires_UtilisateurId",
                table: "Commentaires",
                column: "UtilisateurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Pays_PaysNavigationIdPays",
                table: "Clients",
                column: "PaysNavigationIdPays",
                principalTable: "Pays",
                principalColumn: "id_pays",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Societes_SocieteId",
                table: "Clients",
                column: "SocieteId",
                principalTable: "Societes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Commentaires_Users_UtilisateurId",
                table: "Commentaires",
                column: "UtilisateurId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Users_UtilisateurId",
                table: "Notification",
                column: "UtilisateurId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Pays_PaysId",
                table: "Photos",
                column: "PaysId",
                principalTable: "Pays",
                principalColumn: "id_pays");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjetClients_Clients_ClientId",
                table: "ProjetClients",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projets_Pays_id_pays",
                table: "Projets",
                column: "id_pays",
                principalTable: "Pays",
                principalColumn: "id_pays",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_OwnerId",
                table: "Tickets",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
