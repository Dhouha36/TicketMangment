using System.Text.Json;
using System.Text.Json.Serialization;
using GestionTicketsAPI.Extensions;
using GestionTicketsAPI.hubs;
using GestionTicketsAPI.Middleware;
using GestionTicketsAPI.Services;
using Hangfire;
using Hangfire.MySql;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// 1. Déclarez la policy CORS AVANT tout le reste
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy
          .WithOrigins(
             "http://localhost:4200",
             "https://localhost:4200",
             "http://localhost:8085",
             "https://simsoft-gt.tn" 
          )
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()
          .WithExposedHeaders("Pagination");
    });
});

// 2. Les autres services
builder.Services.AddHangfire(cfg =>
{
    cfg.UseStorage(
        new MySqlStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new MySqlStorageOptions { TablesPrefix = "Hangfire" }
        )
    );
});
builder.Services.AddHangfireServer();

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    );

// WebPush, SignalR, NotificationService…
builder.Services.AddPushServiceClient(opts =>
{
    opts.PublicKey  = builder.Configuration["WebPush:PublicKey"];
    opts.PrivateKey = builder.Configuration["WebPush:PrivateKey"];
    opts.Subject    = $"mailto:{builder.Configuration["WebPush:SubjectEmail"]}";
});
builder.Services.AddSignalR();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// 3. Appliquez la policy CORS tout de suite, avant les middlewares
app.UseCors("AllowClient");

// 4. Pipeline d’exceptions, HTTPS, auth, etc.
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

// 5. Vos endpoints
app.MapHub<NotificationHub>("/hubs/notifications")
   .RequireCors("AllowClient");
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapGet("/", () => "Bienvenue dans l'API GestionTicketsAPI !");

app.Run();
