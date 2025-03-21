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
                .ForMember(dest => dest.Contrat, opt => opt.MapFrom(src => src.Contrats != null && src.Contrats.Any() 
                                                                              ? src.Contrats.First() 
                                                                              : null));
            CreateMap<UserUpdateDto, User>();
            CreateMap<Photo, PhotoDto>();
            CreateMap<Pays, PaysDto>()
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.paysPhoto != null ? src.paysPhoto.Url : null));
            CreateMap<PaysUpdateDto, Pays>();
            CreateMap<Projet, ProjetDto>()
                .ForMember(dest => dest.NomPays, opt => opt.MapFrom(src => src.Societe.Pays.Nom))
                .ForMember(dest => dest.NomSociete, opt => opt.MapFrom(src => src.Societe.Nom));
            CreateMap<ProjetDto, Projet>()
                .ForMember(dest => dest.IdPays, opt => opt.Ignore());
            CreateMap<Societe, SocieteDto>()
                .ForMember(dest => dest.Pays, opt => opt.MapFrom(src => src.Pays));
            CreateMap<SocieteDto, Societe>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            
            // Mise à jour du mapping pour SocieteDetailsDto :
            CreateMap<Societe, SocieteDetailsDto>()
                .ForMember(dest => dest.Utilisateurs, opt => opt.MapFrom(src => src.SocieteUsers.Select(su => su.User)))
                .ForMember(dest => dest.Projets, opt => opt.MapFrom(src => src.Projets))
                .ForMember(dest => dest.Contrat, opt => opt.MapFrom(src => src.ContratsPartenaire.FirstOrDefault()))
                .ForMember(dest => dest.PaysId, opt => opt.MapFrom(src => src.PaysId))
                .ForMember(dest => dest.Pays, opt => opt.MapFrom(src => src.Pays));

            CreateMap<Contrat, ContratDto>().ReverseMap();

            // Mise à jour pour l'entité Ticket et ses DTOs :
            CreateMap<Ticket, TicketDto>()
                .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => src.Owner))
                .ForMember(dest => dest.ProblemCategory, opt => opt.MapFrom(src => src.ProblemCategory))
                .ForMember(dest => dest.Projet, opt => opt.MapFrom(src => src.Projet))
                .ForMember(dest => dest.Responsible, opt => opt.MapFrom(src => src.Responsible))
                .ReverseMap()
                    .ForMember(dest => dest.Owner, opt => opt.Ignore())
                    .ForMember(dest => dest.ProblemCategory, opt => opt.Ignore())
                    .ForMember(dest => dest.Projet, opt => opt.Ignore())
                    .ForMember(dest => dest.Responsible, opt => opt.Ignore());

            CreateMap<TicketCreateDto, Ticket>()
                // Le fichier attaché est géré séparément, donc on l'ignore ici
                .ForMember(dest => dest.Attachments, opt => opt.Ignore());
        }
    }
}
