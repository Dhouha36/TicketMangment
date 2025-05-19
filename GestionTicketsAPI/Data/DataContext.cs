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
    public DbSet<Client> Clients { get; set; }
    public DbSet<Projet> Projets { get; set; }
    public DbSet<Contrat> Contrats { get; set; }
    public DbSet<Notification> Notification { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<ProjetUser> ProjetUser { get; set; }
    public DbSet<ProjetClient> ProjetClients { get; set; }
    public DbSet<CategorieProbleme> CategorieProblemes { get; set; }

    public DbSet<Priorite> Priorities { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<StatutDesTicket> StatutsDesTickets { get; set; }

    public DbSet<SocieteUser> SocieteUsers { get; set; }
    public DbSet<PushSubscriptionEntity> PushSubscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<Client>()
          .ToTable("Clients");

      // ProjetUser many-to-many User<->Projet
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

      // ProjetClient many-to-many Client<->Projet
      modelBuilder.Entity<ProjetClient>()
          .HasKey(pc => new { pc.ProjetId, pc.ClientId });
      modelBuilder.Entity<ProjetClient>()
          .HasOne(pc => pc.Projet)
          .WithMany(p => p.ProjetClients)
          .HasForeignKey(pc => pc.ProjetId)
          .OnDelete(DeleteBehavior.Cascade);
      modelBuilder.Entity<ProjetClient>()
          .HasOne(pc => pc.Client)
          .WithMany(c => c.ProjetClients)
          .HasForeignKey(pc => pc.ClientId)
          .OnDelete(DeleteBehavior.Cascade);

      // SocieteUser one-to-many Societe<->User
      modelBuilder.Entity<SocieteUser>()
          .ToTable("societe_user")
          .HasKey(su => su.Id);
      modelBuilder.Entity<SocieteUser>()
          .HasOne(su => su.Societe)
          .WithMany(s => s.SocieteUsers)
          .HasForeignKey(su => su.SocieteId)
          .OnDelete(DeleteBehavior.Cascade);
      modelBuilder.Entity<SocieteUser>()
          .HasOne(su => su.User)
          .WithMany(u => u.SocieteUsers)
          .HasForeignKey(su => su.UserId)
          .OnDelete(DeleteBehavior.Cascade);

      // Projet ↔ Societe
      modelBuilder.Entity<Projet>()
          .HasOne(p => p.Societe)
          .WithMany(s => s.Projets)
          .HasForeignKey(p => p.SocieteId)
          .OnDelete(DeleteBehavior.Cascade);

      // Projet ↔ Pays
      modelBuilder.Entity<Projet>()
          .HasOne(p => p.Pays)
          .WithMany()  // pas de collection Pays -> Projets
          .HasForeignKey(p => p.IdPays)
          .OnDelete(DeleteBehavior.Restrict);

      // Commentaire : auteur = User ou Client
      modelBuilder.Entity<Commentaire>()
          .HasOne(c => c.User)
          .WithMany()   // pas de Ticket.Commentaires
          .HasForeignKey(c => c.UserId)
          .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Commentaire>()
          .HasOne(c => c.Client)
          .WithMany()
          .HasForeignKey(c => c.ClientId)
          .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Commentaire>()
          .HasOne(c => c.Ticket)
          .WithMany()   // on n’utilise pas Ticket.Commentaires
          .HasForeignKey(c => c.TicketId)
          .OnDelete(DeleteBehavior.Cascade);

      // Notification : destinataire = User ou Client
      modelBuilder.Entity<Notification>()
          .HasOne(n => n.User)
          .WithMany()   // pas de User.Notifications
          .HasForeignKey(n => n.UserId)
          .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Notification>()
          .HasOne(n => n.Client)
          .WithMany()
          .HasForeignKey(n => n.ClientId)
          .OnDelete(DeleteBehavior.Restrict);

      // Photo ↔ Pays (one-to-one)
      modelBuilder.Entity<Photo>()
          .HasOne(p => p.Pays)
          .WithOne(p => p.paysPhoto)
          .HasForeignKey<Photo>(p => p.PaysId)
          .OnDelete(DeleteBehavior.Cascade);

      // User ↔ Pays
      modelBuilder.Entity<User>()
          .HasOne(u => u.PaysNavigation)
          .WithMany()  // pas de Pays.Users
          .HasForeignKey(u => u.Pays)
          .OnDelete(DeleteBehavior.Restrict);

      // Contrat ↔ Client et Contrat ↔ SocietePartenaire
      modelBuilder.Entity<Contrat>()
          .HasOne(c => c.User)
          .WithMany(u => u.Contrats)
          .HasForeignKey(c => c.UserId)
          .OnDelete(DeleteBehavior.Cascade);

      // Ticket relations
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Priority)
          .WithMany()
          .HasForeignKey(t => t.PriorityId)
          .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Qualification)
          .WithMany()
          .HasForeignKey(t => t.QualificationId)
          .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.ProblemCategory)
          .WithMany()
          .HasForeignKey(t => t.ProblemCategoryId)
          .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Statut)
          .WithMany()
          .HasForeignKey(t => t.StatutId)
          .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Owner)
          .WithMany(c => c.Tickets)
          .HasForeignKey(t => t.OwnerId)
          .OnDelete(DeleteBehavior.Cascade);
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Responsible)
          .WithMany()
          .HasForeignKey(t => t.ResponsibleId)
          .OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Ticket>()
          .HasOne(t => t.Projet)
          .WithMany(p => p.Tickets)
          .HasForeignKey(t => t.ProjetId)
          .OnDelete(DeleteBehavior.Cascade);

      // SocieteId obligatoire sur Client
      modelBuilder.Entity<Client>()
          .Property(c => c.SocieteId)
          .IsRequired();
      modelBuilder.Entity<Client>()
          .HasOne(c => c.PaysNavigation)
          .WithMany()
          .HasForeignKey(c => c.Pays)
          .OnDelete(DeleteBehavior.Restrict);

      modelBuilder.Entity<Projet>()
          .HasMany(p => p.Contrats)
          .WithOne(c => c.Projet)
          .HasForeignKey(c => c.ProjetId)
          .OnDelete(DeleteBehavior.SetNull);
    }
  }
}