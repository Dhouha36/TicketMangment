using GestionTicketsAPI.Data;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GestionTicketsAPI.Controllers
{
    [ApiController]
    public class SearchController : BaseApiController
    {
        private readonly DataContext _context;
        public SearchController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("La requête de recherche ne peut pas être vide.");

            // Extraction du rôle et de l'identifiant de l'utilisateur via les claims.
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            int userId = 0;
            if (!string.IsNullOrEmpty(userIdClaim))
                int.TryParse(userIdClaim, out userId);

            // --- Recherche des Tickets ---
            // On récupère quelques propriétés supplémentaires pour appliquer le filtrage.
            var ticketsQuery = _context.Tickets
                .Where(t => t.Title.Contains(query) || t.Description.Contains(query))
                .Select(t => new
                {
                    Id = t.Id,
                    Type = "Ticket",
                    Title = t.Title,
                    Description = t.Description,
                    DateDebut = (DateTime?)null,
                    OwnerId = t.OwnerId,
                    ResponsibleId = t.ResponsibleId,
                    ProjetChefProjetId = t.Projet != null ? t.Projet.ChefProjetId : (int?)null
                });

            // Filtrage des tickets selon le rôle
            if (!string.Equals(userRole, "Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(userRole, "Client", StringComparison.OrdinalIgnoreCase))
                {
                    // Le client voit uniquement les tickets dont il est propriétaire.
                    ticketsQuery = ticketsQuery.Where(t => t.OwnerId == userId);
                }
                else if (string.Equals(userRole, "Collaborateur", StringComparison.OrdinalIgnoreCase))
                {
                    // Le collaborateur voit uniquement les tickets qui lui sont assignés.
                    ticketsQuery = ticketsQuery.Where(t => t.ResponsibleId == userId);
                }
                else if (string.Equals(userRole, "Chef de Projet", StringComparison.OrdinalIgnoreCase))
                {
                    // Le chef de projet voit les tickets qui lui sont assignés ou ceux dont il est chef de projet.
                    ticketsQuery = ticketsQuery.Where(t => t.ResponsibleId == userId || t.ProjetChefProjetId == userId);
                }
            }

            // Pour la concaténation, on ne garde que les champs communs.
            var filteredTicketsQuery = ticketsQuery.Select(t => new
            {
                t.Id,
                t.Type,
                t.Title,
                t.Description,
                t.DateDebut
            });

            // --- Recherche des Projets ---
            var projetsQuery = _context.Projets
                .Where(p => p.Nom.Contains(query) || (p.Description != null && p.Description.Contains(query)))
                .Select(p => new
                {
                    Id = p.Id,
                    Type = "Projet",
                    Title = p.Nom,
                    Description = p.Description ?? string.Empty,
                    DateDebut = (DateTime?)null,
                    ChefProjetId = p.ChefProjetId
                });

            // Filtrage des projets selon le rôle
            if (!string.Equals(userRole, "Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(userRole, "Chef de Projet", StringComparison.OrdinalIgnoreCase))
                {
                    projetsQuery = projetsQuery.Where(p => p.ChefProjetId == userId);
                }
                else if (string.Equals(userRole, "Collaborateur", StringComparison.OrdinalIgnoreCase))
                {
                    // Un collaborateur voit uniquement les projets dans lesquels il est associé via ProjetUser.
                    projetsQuery = projetsQuery.Where(p => _context.ProjetUser.Any(pu => pu.ProjetId == p.Id && pu.UserId == userId));
                }
                else if (string.Equals(userRole, "Client", StringComparison.OrdinalIgnoreCase))
                {
                    // Un client ne voit aucun projet.
                    projetsQuery = _context.Projets
                        .Where(p => false)
                        .Select(p => new
                        {
                            Id = 0,
                            Type = "Projet",
                            Title = string.Empty,
                            Description = string.Empty,
                            DateDebut = (DateTime?)null,
                            ChefProjetId = (int?)null
                        });
                }
            }

            var filteredProjetsQuery = projetsQuery.Select(p => new
            {
                p.Id,
                p.Type,
                p.Title,
                p.Description,
                p.DateDebut
            });

            // --- Recherche des autres entités ---
            // Seuls le Super Admin a accès aux autres entités.
            var usersQuery = _context.Users
                .Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query) || u.Email.Contains(query))
                .Select(u => new
                {
                    Id = u.Id,
                    Type = "User",
                    Title = u.FirstName + " " + u.LastName,
                    Description = u.Email,
                    DateDebut = (DateTime?)null
                });

            var societesQuery = _context.Societes
                .Where(s =>
                    s.Nom.Contains(query) ||
                    s.Adresse.Contains(query) ||
                    s.Ville.Contains(query) ||
                    s.CodePostal.Contains(query)
                )
                .Select(s => new
                {
                     Id = s.Id,
                     Type = "Societe",
                    Title = s.Nom,
                    // On affiche l'adresse complète en description
                    Description = $"{s.Adresse}, {s.CodePostal} {s.Ville}",
                     DateDebut = (DateTime?)null
                 });

            var paysQuery = _context.Set<Pays>()
                .Where(p => p.Nom.Contains(query) || (p.CodeTel != null && p.CodeTel.Contains(query)))
                .Select(p => new
                {
                    Id = p.IdPays,
                    Type = "Pays",
                    Title = p.Nom,
                    Description = p.CodeTel ?? string.Empty,
                    DateDebut = (DateTime?)null
                });

            var commentairesQuery = _context.Set<Commentaire>()
              .Join(_context.Tickets, c => c.TicketId, t => t.Id, (c, t) => new { c, t })
              .Where(ct =>
                  ct.c.Contenu.Contains(query) &&
                  (userRole == "Super Admin" ||
                  (userRole == "Client" && ct.t.OwnerId == userId) ||
                  (userRole == "Collaborateur" && ct.t.ResponsibleId == userId) ||
                  (userRole == "Chef de Projet" && (ct.t.ResponsibleId == userId || ct.t.Projet.ChefProjetId == userId))))
              .Select(ct => new
              {
                  Id = ct.c.TicketId,
                  Type = "Commentaire",
                  Title = "Commentaire " + ct.c.Id,
                  Description = ct.c.Contenu,
                  DateDebut = (DateTime?)null
              });

            var roleUsersQuery = from u in _context.Users
                                 join r in _context.Roles on u.RoleId equals r.Id
                                 where r.Name.Contains(query)
                                 select new
                                 {
                                     Id = u.Id,
                                     Type = "Role",
                                     Title = u.FirstName + " " + u.LastName,
                                     Description = "Rôle : " + r.Name,
                                     DateDebut = (DateTime?)null
                                 };

            var qualificationTicketsQuery = _context.Tickets
                              .Join(_context.Set<Qualification>(), t => t.QualificationId, q => q.Id, (t, q) => new { t, q })
                              .Where(tq =>
                                  tq.q.Name.Contains(query) &&
                                  (userRole == "Super Admin" ||
                                  (userRole == "Client" && tq.t.OwnerId == userId) ||
                                  (userRole == "Collaborateur" && tq.t.ResponsibleId == userId) ||
                                  (userRole == "Chef de Projet" && (tq.t.ResponsibleId == userId || tq.t.Projet.ChefProjetId == userId))))
                              .Select(tq => new
                              {
                                  Id = tq.t.Id,
                                  Type = "Qualification",
                                  Title = tq.t.Title,
                                  Description = "Qualification : " + tq.q.Name,
                                  DateDebut = (DateTime?)null
                              });

          var prioriteTicketsQuery = _context.Tickets
                                      .Join(_context.Set<Priorite>(), t => t.PriorityId, p => p.Id, (t, p) => new { t, p })
                                      .Where(tp =>
                                          tp.p.Name.Contains(query) &&
                                          (userRole == "Super Admin" ||
                                          (userRole == "Client" && tp.t.OwnerId == userId) ||
                                          (userRole == "Collaborateur" && tp.t.ResponsibleId == userId) ||
                                          (userRole == "Chef de Projet" && (tp.t.ResponsibleId == userId || tp.t.Projet.ChefProjetId == userId))))
                                      .Select(tp => new
                                      {
                                          Id = tp.t.Id,
                                          Type = "Priorite",
                                          Title = tp.t.Title,
                                          Description = "Priorité : " + tp.p.Name,
                                          DateDebut = (DateTime?)null
                                      });


          var categorieTicketsQuery = _context.Tickets
                            .Join(_context.Set<CategorieProbleme>(), t => t.ProblemCategoryId, cp => cp.Id, (t, cp) => new { t, cp })
                            .Where(tcp =>
                                tcp.cp.Nom.Contains(query) &&
                                (userRole == "Super Admin" ||
                                (userRole == "Client" && tcp.t.OwnerId == userId) ||
                                (userRole == "Collaborateur" && tcp.t.ResponsibleId == userId) ||
                                (userRole == "Chef de Projet" && (tcp.t.ResponsibleId == userId || tcp.t.Projet.ChefProjetId == userId))))
                            .Select(tcp => new
                            {
                                Id = tcp.t.Id,
                                Type = "CategorieProbleme",
                                Title = tcp.t.Title,
                                Description = "Catégorie : " + tcp.cp.Nom,
                                DateDebut = (DateTime?)null
                            });

            var statutTicketsQuery = _context.Tickets
                      .Join(_context.Set<StatutDesTicket>(), t => t.StatutId, s => s.Id, (t, s) => new { t, s })
                      .Where(ts =>
                          ts.s.Name.Contains(query) &&
                          (userRole == "Super Admin" ||
                          (userRole == "Client" && ts.t.OwnerId == userId) ||
                          (userRole == "Collaborateur" && ts.t.ResponsibleId == userId) ||
                          (userRole == "Chef de Projet" && (ts.t.ResponsibleId == userId || ts.t.Projet.ChefProjetId == userId))))
                      .Select(ts => new
                      {
                          Id = ts.t.Id,
                          Type = "StatutDesTicket",
                          Title = ts.t.Title,
                          Description = "Statut : " + ts.s.Name,
                          DateDebut = (DateTime?)null
                      });


            
            // Concaténation des requêtes.
            IQueryable<dynamic> unionQuery;
            if (userRole == "Super Admin")
            {
                unionQuery = filteredTicketsQuery
                    .Concat(filteredProjetsQuery)
                    .Concat(commentairesQuery)
                    .Concat(qualificationTicketsQuery)
                    .Concat(prioriteTicketsQuery)
                    .Concat(categorieTicketsQuery)
                    .Concat(statutTicketsQuery);
            }
            else
            {
                unionQuery = filteredTicketsQuery
                    .Concat(filteredProjetsQuery)
                    .Concat(commentairesQuery)
                    .Concat(qualificationTicketsQuery)
                    .Concat(prioriteTicketsQuery)
                    .Concat(categorieTicketsQuery)
                    .Concat(statutTicketsQuery);
            }

            // Exécuter la requête
            var unionList = await unionQuery.AsNoTracking().ToListAsync();

            // Projection finale
            var results = unionList
                .Select(x => new SearchResultDTO
                {
                    Id = x.Id,
                    Type = x.Type,
                    Title = x.Title,
                    Description = x.Description
                })
                .OrderBy(x => x.Title)
                .ToList();

            return Ok(results);
        }
    }
}
