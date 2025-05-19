using AutoMapper;
using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using System.Linq;

namespace GestionTicketsAPI.Helpers
{
  public class AutoMapperProfiles : Profile
  {
    public AutoMapperProfiles()
    {
      CreateMap<User, UserDto>()
          .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Name))
          .ForMember(dest => dest.Societe, opt => opt.MapFrom(src => src.SocieteUsers.Any() ? src.SocieteUsers.First().Societe : null))
          .ForMember(dest => dest.Contrat, opt => opt.MapFrom(src => src.Contrats != null && src.Contrats.Any() ? src.Contrats.First() : null));

      CreateMap<UserUpdateDto, User>()
          .ForMember(dest => dest.Pays, opt => opt.MapFrom(src => int.Parse(src.Pays)))
          .ForMember(dest => dest.Role, opt => opt.MapFrom<UserRoleResolver>())
          .ForMember(dest => dest.SocieteUsers, opt => opt.Ignore());
      CreateMap<Photo, PhotoDto>();
      CreateMap<Pays, PaysDto>()
          .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.paysPhoto != null ? src.paysPhoto.Url : null))
          .ForMember(dest => dest.CodeTel, opt => opt.MapFrom(src => src.CodeTel));
      CreateMap<PaysUpdateDto, Pays>();
      CreateMap<PaysDto, Pays>();
      CreateMap<Projet, ProjetDto>()
          .ForMember(dest => dest.NomPays, opt => opt.MapFrom(src => src.Societe.Pays.Nom))
          .ForMember(dest => dest.NomSociete, opt => opt.MapFrom(src => src.Societe.Nom))
          .ForMember(dest => dest.ChefProjetId, opt => opt.MapFrom(src => src.ChefProjetId))
          .ForMember(dest => dest.ChefProjet, opt => opt.MapFrom(src => src.ChefProjet))
          .ForMember(dest => dest.Contrat,
              opt => opt.MapFrom(src =>
                    src.Contrats.FirstOrDefault(c => c.Type == TypeContrat.Projet)
                ));
      CreateMap<ProjetDto, Projet>()
          .ForMember(dest => dest.IdPays, opt => opt.Ignore())
          .ForMember(dest => dest.ChefProjetId, opt => opt.MapFrom(src => src.ChefProjetId));
      CreateMap<ProjetUpdateDto, Projet>()
            .ForMember(dest => dest.IdPays, opt => opt.Ignore())
            .ForMember(dest => dest.ChefProjet, opt => opt.Ignore());
      CreateMap<Societe, SocieteDto>()
          .ForMember(dest => dest.Pays, opt => opt.MapFrom(src => src.Pays));
      CreateMap<SocieteDto, Societe>()
          .ForMember(dest => dest.Id, opt => opt.Ignore());

      // Mise à jour du mapping pour SocieteDetailsDto :
      CreateMap<Societe, SocieteDetailsDto>()
          .ForMember(dest => dest.Utilisateurs, opt => opt.MapFrom(src => src.SocieteUsers.Select(su => su.User)))
          .ForMember(dest => dest.Projets, opt => opt.MapFrom(src => src.Projets))
          .ForMember(dest => dest.PaysId, opt => opt.MapFrom(src => src.PaysId))
          .ForMember(dest => dest.Pays, opt => opt.MapFrom(src => src.Pays));

      CreateMap<Contrat, ContratDto>().ReverseMap();

      // Mise à jour pour l'entité Ticket et ses DTOs :
      CreateMap<Ticket, TicketDto>()
            .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => src.Owner))
            .ForMember(dest => dest.ProblemCategory, opt => opt.MapFrom(src => src.ProblemCategory))
            .ForMember(dest => dest.Projet, opt => opt.MapFrom(src => src.Projet))
            .ForMember(dest => dest.Responsible, opt => opt.MapFrom(src => src.Responsible))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToLocalTime()))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.HasValue
                                                                        ? src.UpdatedAt.Value.ToLocalTime()
                                                                        : (DateTime?)null))
            .ForMember(dest => dest.ApprovedAt, opt => opt.MapFrom(src => src.ApprovedAt.HasValue
                                                                        ? src.ApprovedAt.Value.ToLocalTime()
                                                                        : (DateTime?)null))
            .ForMember(dest => dest.SolvedAt, opt => opt.MapFrom(src => src.SolvedAt.HasValue
                                                                        ? src.SolvedAt.Value.ToLocalTime()
                                                                        : (DateTime?)null))
          .ReverseMap()
              .ForMember(dest => dest.Owner, opt => opt.Ignore())
              .ForMember(dest => dest.ProblemCategory, opt => opt.Ignore())
              .ForMember(dest => dest.Projet, opt => opt.Ignore())
              .ForMember(dest => dest.Responsible, opt => opt.Ignore());


      CreateMap<TicketCreateDto, Ticket>()
          // Le fichier attaché est géré séparément, donc on l'ignore ici
          .ForMember(dest => dest.Attachments, opt => opt.Ignore());

      CreateMap<Ticket, TicketExportDto>()
           .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => src.Priority != null ? src.Priority.Name : string.Empty))
           .ForMember(dest => dest.StatutName, opt => opt.MapFrom(src => src.Statut != null ? src.Statut.Name : string.Empty))
           .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.Owner != null ? $"{src.Owner.FirstName} {src.Owner.LastName}" : string.Empty))
           .ForMember(dest => dest.ProblemCategoryName, opt => opt.MapFrom(src => src.ProblemCategory != null ? src.ProblemCategory.Nom : string.Empty))
           .ForMember(dest => dest.QualificationName, opt => opt.MapFrom(src => src.Qualification != null ? src.Qualification.Name : string.Empty))
           .ForMember(dest => dest.ProjetName, opt => opt.MapFrom(src => src.Projet != null ? src.Projet.Nom : string.Empty))
           .ForMember(dest => dest.ResponsibleName, opt => opt.MapFrom(src => src.Responsible != null ? $"{src.Responsible.FirstName} {src.Responsible.LastName}" : string.Empty));
      CreateMap<Commentaire, CommentDto>()
          .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToLocalTime()))
          .ForMember(dest => dest.Utilisateur, opt => opt.MapFrom(src => src.User));

      CreateMap<ProjetDto, ProjectExportDto>()
          .ForMember(dest => dest.ChefProjet, opt => opt.MapFrom(src => src.ChefProjet != null
              ? $"{src.ChefProjet.FirstName} {src.ChefProjet.LastName}"
              : string.Empty))
          .ForMember(dest => dest.Societe, opt => opt.MapFrom(src => src.NomSociete))
          .ForMember(dest => dest.Pays, opt => opt.MapFrom(src => src.NomPays));
      CreateMap<User, UserExportDto>()
           .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
           .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Name))
           .ForMember(dest => dest.Actif, opt => opt.MapFrom(src => src.Actif ? "Oui" : "Non"))
           .ForMember(dest => dest.Contrat, opt => opt.MapFrom(src => src.Contrats != null && src.Contrats.Any() ? "Oui" : "Non"))
           .ForMember(dest => dest.DateDebut, opt => opt.MapFrom(src => src.Contrats != null && src.Contrats.Any() ? src.Contrats.First().DateDebut : (DateTime?)null))
           .ForMember(dest => dest.DateFin, opt => opt.MapFrom(src => src.Contrats != null && src.Contrats.Any() ? src.Contrats.First().DateFin : (DateTime?)null));
      CreateMap<UserDto, UserExportDto>()
          .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
          .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
          .ForMember(dest => dest.Actif, opt => opt.MapFrom(src => src.Actif ? "Oui" : "Non"))
          .ForMember(dest => dest.Contrat, opt => opt.MapFrom(src => src.Contrat != null ? "Oui" : "Non"))
          .ForMember(dest => dest.DateDebut, opt => opt.MapFrom(src => src.Contrat != null ? src.Contrat.DateDebut : (DateTime?)null))
          .ForMember(dest => dest.DateFin, opt => opt.MapFrom(src => src.Contrat != null ? src.Contrat.DateFin : (DateTime?)null));

      // Mapping à partir de l'entité Societe
      CreateMap<Societe, SocieteExportDto>()
          .ForMember(dest => dest.Nom, opt => opt.MapFrom(src => src.Nom))
          .ForMember(dest => dest.Adresse, opt => opt.MapFrom(src => src.Adresse))
          .ForMember(dest => dest.Telephone, opt => opt.MapFrom(src => src.Telephone))
          .ForMember(dest => dest.Pays, opt => opt.MapFrom(src => src.Pays != null ? src.Pays.Nom : string.Empty));

      // Mapping à partir du DTO SocieteDto
      CreateMap<SocieteDto, SocieteExportDto>()
          .ForMember(dest => dest.Nom, opt => opt.MapFrom(src => src.Nom))
          .ForMember(dest => dest.Adresse, opt => opt.MapFrom(src => src.Adresse))
          .ForMember(dest => dest.Telephone, opt => opt.MapFrom(src => src.Telephone))
          .ForMember(dest => dest.Pays, opt => opt.MapFrom(src => src.Pays != null ? src.Pays.Nom : string.Empty));
      CreateMap<Client, ClientDto>()
      .ForMember(dest => dest.PaysId,
                 opt => opt.MapFrom(src => src.Pays))              // FK int
      .ForMember(dest => dest.Pays,
                 opt => opt.MapFrom(src => src.PaysNavigation.Nom))
      .ForMember(dest => dest.SocieteId,
                 opt => opt.MapFrom(src => src.SocieteId))
      .ForMember(dest => dest.Societe,
                 opt => opt.MapFrom(src => src.Societe))
      .ForMember(dest => dest.Projets,
                 opt => opt.MapFrom(src => src.ProjetClients))    // ← map collection directly
      .ForMember(dest => dest.InitialPassword,
                 opt => opt.Ignore());

      CreateMap<ClientUpdateDto, Client>()
          // mappez le FK sur PaysId
          .ForMember(dest => dest.Pays,
                    opt => opt.MapFrom(src => src.PaysId))

          // mappez SocieteId
          .ForMember(dest => dest.SocieteId,
                    opt => opt.MapFrom(src => src.SocieteId))

          // ignorez la navigation sur Pays (évite l’erreur de type)
          .ForMember(dest => dest.Pays,
                    opt => opt.Ignore())

          // ignorez la navigation sur Société, si elle existe aussi
          .ForMember(dest => dest.Societe,
                    opt => opt.Ignore())

          // le reste en conditionnel comme avant
          .ForAllMembers(opt =>
              opt.Condition((src, dest, srcMember) => srcMember != null)
          );


      CreateMap<ProjetClient, ProjetMiniDto>()
           .ForMember(d => d.Id, o => o.MapFrom(s => s.Projet.Id))
           .ForMember(d => d.Nom, o => o.MapFrom(s => s.Projet.Nom));

    }
  }
}
