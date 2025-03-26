using System;
using GestionTicketsAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Data
{
  public class DataContext : DbContext
  {
    public DataContext(DbContextOptions options) : base(options) { }

    // DbSet existants
    public DbSet<User> Users { get; set; }
    public DbSet<Pays> Pays { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Societe> Societes { get; set; }
    public DbSet<Commentaire> Commentaires { get; set; }
    public DbSet<Projet> Projets { get; set; }
    public DbSet<Contrat> Contrats { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<ProjetUser> ProjetUser { get; set; }
    public DbSet<CategorieProbleme> CategorieProblemes { get; set; }

    // DbSet ajoutés
    public DbSet<Priorite> Priorities { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<StatutDesTicket> StatutsDesTickets { get; set; }

    public DbSet<SocieteUser> SocieteUsers { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // ----- Configuration de la table de jointure ProjetUser -----
      modelBuilder.Entity<ProjetUser>()
          .HasKey(pu => new { pu.ProjetId, pu.UserId });

      modelBuilder.Entity<ProjetUser>()
          .HasOne(pu => pu.Projet)
          .WithMany(p => p.ProjetUsers)
          .HasForeignKey(pu => pu.ProjetId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<ProjetUser>()
          .HasOne(pu => pu.User)
          .WithMany(u => u.ProjetUsers)
          .HasForeignKey(pu => pu.UserId)
          .OnDelete(DeleteBehavior.Cascade);

      // ----- Relation Pays <-> Photo -----
      modelBuilder.Entity<Photo>()
          .HasOne(p => p.Pays)
          .WithOne(p => p.paysPhoto)
          .HasForeignKey<Photo>(p => p.PaysId);

      // ----- Relation Projet <-> Societe -----
      modelBuilder.Entity<Projet>()
          .HasOne(p => p.Societe)
          .WithMany(s => s.Projets)
          .HasForeignKey(p => p.SocieteId)
          .OnDelete(DeleteBehavior.Cascade);

      // ----- Relation Projet <-> Pays -----
      modelBuilder.Entity<Projet>()
          .HasOne(p => p.Pays)
          .WithMany()
          .HasForeignKey(p => p.IdPays)
          .OnDelete(DeleteBehavior.Cascade);

      // Contrainte pour que le projet appartienne soit à une société soit à un client
      modelBuilder.Entity<Projet>()
          .ToTable(t => t.HasCheckConstraint("CK_Projet_Association",
              "((SocieteId IS NOT NULL AND ClientId IS NULL) OR (SocieteId IS NULL AND ClientId IS NOT NULL))"));

      // ----- Cascade : Societe <-> Pays -----
      modelBuilder.Entity<Societe>()
          .HasOne(s => s.Pays)
          .WithMany(p => p.Societes)
          .HasForeignKey(s => s.PaysId)
          .OnDelete(DeleteBehavior.Cascade);

      // ----- Relation User <-> Pays -----
      modelBuilder.Entity<User>()
          .HasOne(u => u.PaysNavigation)
          .WithMany()
          .HasForeignKey(u => u.Pays)
          .OnDelete(DeleteBehavior.Restrict);

      // Relation Contrat -> Client (User)
      modelBuilder.Entity<Contrat>()
          .HasOne(c => c.Client)
          .WithMany(u => u.Contrats)
          .HasForeignKey(c => c.ClientId)
          .OnDelete(DeleteBehavior.Cascade);

      // Relation Contrat -> SocietePartenaire
      modelBuilder.Entity<Contrat>()
          .HasOne(c => c.SocietePartenaire)
          .WithMany(s => s.ContratsPartenaire)
          .HasForeignKey(c => c.SocietePartenaireId)
          .OnDelete(DeleteBehavior.Cascade);

      // ----- Relation Notification -> Utilisateur -----
      modelBuilder.Entity<Notification>()
          .HasOne(n => n.Utilisateur)
          .WithMany() // Pas de collection de notifications dans User
          .HasForeignKey(n => n.UtilisateurId)
          .OnDelete(DeleteBehavior.Cascade);

      // ----- Relation Commentaire -> Utilisateur et Ticket -----
      modelBuilder.Entity<Commentaire>()
          .HasOne(c => c.Utilisateur)
          .WithMany() // Si vous n'avez pas de collection de Commentaires dans User
          .HasForeignKey(c => c.UtilisateurId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<Commentaire>()
          .HasOne(c => c.Ticket)
          .WithMany() // Vous pouvez ajouter une collection de Commentaires dans Ticket si besoin
          .HasForeignKey(c => c.TicketId)
          .OnDelete(DeleteBehavior.Cascade);

      // ----- Configuration des relations de l'entité Ticket -----
      // Relation Ticket -> Priorite
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Priority)
          .WithMany() // Pas de collection dans Priorite
          .HasForeignKey(t => t.PriorityId)
          .OnDelete(DeleteBehavior.Restrict);

      // Relation Ticket -> Qualification
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Qualification)
          .WithMany() // Pas de collection dans Qualification
          .HasForeignKey(t => t.QualificationId)
          .OnDelete(DeleteBehavior.Restrict);

      // Relation Ticket -> CategorieProbleme
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.ProblemCategory)
          .WithMany() // Pas de collection dans CategorieProbleme
          .HasForeignKey(t => t.ProblemCategoryId)
          .OnDelete(DeleteBehavior.Restrict);


      // Relation Ticket -> StatutDesTicket
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Statut)
          .WithMany() // Pas de collection dans StatutDesTicket
          .HasForeignKey(t => t.StatutId)
          .OnDelete(DeleteBehavior.Restrict);

      // Relation Ticket -> Owner (utilisateur créateur)
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Owner)
          .WithMany() // Pas de collection spécifique dans User pour les tickets créés
          .HasForeignKey(t => t.OwnerId)
          .OnDelete(DeleteBehavior.Cascade);


      // Relation Ticket -> Responsible (utilisateur assigné)
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Responsible)
          .WithMany() // Pas de collection spécifique dans User pour les tickets assignés
          .HasForeignKey(t => t.ResponsibleId)
          .OnDelete(DeleteBehavior.Restrict);

      // Relation Ticket -> Projet
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Projet)
          .WithMany(p => p.Tickets)
          .HasForeignKey(t => t.ProjetId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<SocieteUser>()
          .ToTable("societe_user")
          .HasKey(su => su.Id);

      modelBuilder.Entity<SocieteUser>()
          .HasOne(su => su.Societe)
          .WithMany(s => s.SocieteUsers)
          .HasForeignKey(su => su.SocieteId);

      modelBuilder.Entity<SocieteUser>()
          .HasOne(su => su.User)
          .WithMany(u => u.SocieteUsers)
          .HasForeignKey(su => su.UserId);
    }
  }
}
