﻿// <auto-generated />
using System;
using GestionTicketsAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GestionTicketsAPI.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20250201191350_ConfigureCascadeDeleteProjetUser")]
    partial class ConfigureCascadeDeleteProjetUser
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("GestionTicketsAPI.Entities.Commentaire", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Contenu")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("DateModification")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("TicketId")
                        .HasColumnType("int");

                    b.Property<int>("UtilisateurId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("TicketId");

                    b.HasIndex("UtilisateurId");

                    b.ToTable("Commentaires");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Contrat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("ClientId")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateDebut")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("DateFin")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("SocieteInitiatriceId")
                        .HasColumnType("int");

                    b.Property<int?>("SocietePartenaireId")
                        .HasColumnType("int");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("TypeContrat")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.HasIndex("SocieteInitiatriceId");

                    b.HasIndex("SocietePartenaireId");

                    b.ToTable("Contrats");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Notification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("DateEnvoi")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("UtilisateurId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UtilisateurId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Pays", b =>
                {
                    b.Property<int>("IdPays")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id_pays");

                    b.Property<string>("Nom")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.HasKey("IdPays");

                    b.ToTable("Pays");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Photo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("PaysId")
                        .HasColumnType("int");

                    b.Property<string>("PublicId")
                        .HasColumnType("longtext");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("PaysId")
                        .IsUnique();

                    b.ToTable("Photos");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Projet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("IdPays")
                        .HasColumnType("int")
                        .HasColumnName("id_pays");

                    b.Property<string>("Nom")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("SocieteId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("IdPays");

                    b.HasIndex("SocieteId");

                    b.ToTable("Projets");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.ProjetUser", b =>
                {
                    b.Property<int>("ProjetId")
                        .HasColumnType("int")
                        .HasColumnOrder(1);

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnOrder(2);

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("ProjetId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("ProjetUser");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Societe", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Adresse")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Nom")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Telephone")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Societes");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Ticket", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("DateCreation")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("DateModification")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Priorite")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("ProjetId")
                        .HasColumnType("int");

                    b.Property<string>("Statuts")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Titre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("UtilisateurId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ProjetId");

                    b.HasIndex("UtilisateurId");

                    b.ToTable("Tickets");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<bool>("Actif")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("NumTelephone")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<byte[]>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<byte[]>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<int>("Pays")
                        .HasColumnType("int");

                    b.Property<int?>("PaysIdPays")
                        .HasColumnType("int");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("SocieteId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Pays");

                    b.HasIndex("PaysIdPays");

                    b.HasIndex("SocieteId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Commentaire", b =>
                {
                    b.HasOne("GestionTicketsAPI.Entities.Ticket", "Ticket")
                        .WithMany("Commentaires")
                        .HasForeignKey("TicketId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GestionTicketsAPI.Entities.User", "Utilisateur")
                        .WithMany("Commentaires")
                        .HasForeignKey("UtilisateurId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Ticket");

                    b.Navigation("Utilisateur");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Contrat", b =>
                {
                    b.HasOne("GestionTicketsAPI.Entities.User", "Client")
                        .WithMany()
                        .HasForeignKey("ClientId");

                    b.HasOne("GestionTicketsAPI.Entities.Societe", "SocieteInitiatrice")
                        .WithMany()
                        .HasForeignKey("SocieteInitiatriceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GestionTicketsAPI.Entities.Societe", "SocietePartenaire")
                        .WithMany()
                        .HasForeignKey("SocietePartenaireId");

                    b.Navigation("Client");

                    b.Navigation("SocieteInitiatrice");

                    b.Navigation("SocietePartenaire");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Notification", b =>
                {
                    b.HasOne("GestionTicketsAPI.Entities.User", "Utilisateur")
                        .WithMany()
                        .HasForeignKey("UtilisateurId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Utilisateur");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Photo", b =>
                {
                    b.HasOne("GestionTicketsAPI.Entities.Pays", "Pays")
                        .WithOne("paysPhoto")
                        .HasForeignKey("GestionTicketsAPI.Entities.Photo", "PaysId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Pays");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Projet", b =>
                {
                    b.HasOne("GestionTicketsAPI.Entities.Pays", "Pays")
                        .WithMany()
                        .HasForeignKey("IdPays")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GestionTicketsAPI.Entities.Societe", "Societe")
                        .WithMany("Projets")
                        .HasForeignKey("SocieteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Pays");

                    b.Navigation("Societe");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.ProjetUser", b =>
                {
                    b.HasOne("GestionTicketsAPI.Entities.Projet", "Projet")
                        .WithMany("ProjetUsers")
                        .HasForeignKey("ProjetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GestionTicketsAPI.Entities.User", "User")
                        .WithMany("ProjetUsers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Projet");

                    b.Navigation("User");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Ticket", b =>
                {
                    b.HasOne("GestionTicketsAPI.Entities.Projet", null)
                        .WithMany("Tickets")
                        .HasForeignKey("ProjetId");

                    b.HasOne("GestionTicketsAPI.Entities.User", "Utilisateur")
                        .WithMany("Tickets")
                        .HasForeignKey("UtilisateurId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Utilisateur");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.User", b =>
                {
                    b.HasOne("GestionTicketsAPI.Entities.Pays", "PaysNavigation")
                        .WithMany()
                        .HasForeignKey("Pays")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("GestionTicketsAPI.Entities.Pays", null)
                        .WithMany("Utilisateurs")
                        .HasForeignKey("PaysIdPays");

                    b.HasOne("GestionTicketsAPI.Entities.Societe", "Societe")
                        .WithMany("Utilisateurs")
                        .HasForeignKey("SocieteId");

                    b.Navigation("PaysNavigation");

                    b.Navigation("Societe");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Pays", b =>
                {
                    b.Navigation("Utilisateurs");

                    b.Navigation("paysPhoto");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Projet", b =>
                {
                    b.Navigation("ProjetUsers");

                    b.Navigation("Tickets");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Societe", b =>
                {
                    b.Navigation("Projets");

                    b.Navigation("Utilisateurs");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.Ticket", b =>
                {
                    b.Navigation("Commentaires");
                });

            modelBuilder.Entity("GestionTicketsAPI.Entities.User", b =>
                {
                    b.Navigation("Commentaires");

                    b.Navigation("ProjetUsers");

                    b.Navigation("Tickets");
                });
#pragma warning restore 612, 618
        }
    }
}
