using System.Text.Json;
using System.Text.Json.Serialization;
using GestionTicketsAPI.Extensions;
using GestionTicketsAPI.Middleware;
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();


app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:4200", "https://localhost:4200").WithExposedHeaders("Pagination"));

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();