using System.Text.Json;
using GestionTicketsAPI.Extensions;
using GestionTicketsAPI.Middleware;
using GestionTicketsAPI.Services;
using Hangfire;
using Hangfire.MySql;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddHangfire(configuration =>
{
    configuration.UseStorage(
        new MySqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlStorageOptions
        {
            TablesPrefix = "Hangfire" // Préfixe pour les tables de Hangfire
        })
    );
});
builder.Services.AddHangfireServer();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddSignalR();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("http://localhost:4200", "https://localhost:4200", "http://localhost:8085")
            .AllowCredentials()
            .WithExposedHeaders("Pagination");
    });
});

var app = builder.Build();





app.MapGet("/", () => "Bienvenue dans l'API GestionTicketsAPI !");

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseStaticFiles();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Mappage du hub SignalR pour les notifications en temps réel
app.MapHub<NotificationHub>("/notificationHub");


app.Run();
