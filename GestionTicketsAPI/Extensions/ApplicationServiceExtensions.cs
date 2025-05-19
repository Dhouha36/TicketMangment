using System.Text.Json.Serialization;
using GestionTicketsAPI.Controllers;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Helpers;
using GestionTicketsAPI.Interfaces;
using GestionTicketsAPI.Repositories;
using GestionTicketsAPI.Repositories.Interfaces;
using GestionTicketsAPI.Services;
using GestionTicketsAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Extensions
{
  public static class ApplicationServiceExtensions
  {
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
      services.AddControllers();
      services.AddEndpointsApiExplorer();
      services.AddSwaggerGen();
      services.AddDbContext<DataContext>(options =>
          options.UseMySql(config.GetConnectionString("DefaultConnection"),
              new MySqlServerVersion(new Version(8, 0, 21)))
      );

      services.AddCors();
      services.AddHttpContextAccessor();
      services.AddTransient<EmailService>();
      services.AddScoped<ExcelExportServiceClosedXML>();
      services.AddScoped<ITokenService, TokenService>();
      services.AddScoped<IPhotoService, PhotoService>();
      services.AddScoped<IUserRepository, UserRepository>();
      services.AddScoped<IClientService, ClientService>();
      services.AddScoped<IClientRepository, ClientRepository>();
      services.AddScoped<IUserService, UserService>();
      services.AddScoped<IAccountRepository, AccountRepository>();
      services.AddScoped<IAccountService, AccountService>();
      services.AddScoped<IPaysRepository, PaysRepository>();
      services.AddScoped<IPaysService, PaysService>();
      services.AddScoped<IProjetRepository, ProjetRepository>();
      services.AddScoped<IProjetService, ProjetService>();
      services.AddScoped<ISocieteRepository, SocieteRepository>();
      services.AddScoped<ISocieteService, SocieteService>();
      services.AddScoped<IContratRepository, ContratRepository>();
      services.AddScoped<IContratService, ContratService>();
      services.AddScoped<ITicketRepository, TicketRepository>();
      services.AddScoped<ITicketService, TicketService>();
      services.AddScoped<ICategorieProblemeRepository, CategorieProblemeRepository>();
      services.AddScoped<ICategorieProblemeService, CategorieProblemeService>();

      // Enregistrement pour l'entité Priorite
      services.AddScoped<IPrioriteRepository, PrioriteRepository>();
      services.AddScoped<IPrioriteService, PrioriteService>();

      // Enregistrement pour l'entité Qualification
      services.AddScoped<IQualificationRepository, QualificationRepository>();
      services.AddScoped<IQualificationService, QualificationService>();

      services.AddScoped<IRoleRepository, RoleRepository>();
      services.AddScoped<IRoleService, RoleService>();
      services.AddScoped<IStatutDesTicketRepository, StatutDesTicketRepository>();
      services.AddScoped<IStatutDesTicketService, StatutDesTicketService>();

      services.AddScoped<ICommentService, CommentService>();
      services.AddScoped<IDashboardService, DashboardService>();
      services.AddScoped<INotificationService, NotificationService>();

      services
        .AddControllers()
        .AddJsonOptions(opts =>
        {
          // Permet d’envoyer "CDD", "CDI" ou "Projet" en JSON
          opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });


      services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
      services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));
      services.Configure<FormOptions>(options =>
      {
        options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 Mo
      });

      return services;
    }
  }
}
