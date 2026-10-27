using System.Text.Json.Serialization;
using GroupMate.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=groupmate;Username=groupmate;Password=groupmate";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddControllers(o =>
    {
        // Obligāto lauku pārbaudi veicam paši, lai kļūdu paziņojumi būtu latviski un saprotami.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(new
        {
            message = "Nekorekti ievaddati: pārbaudi, vai visi lauki ir aizpildīti pareizā formātā."
        });
    });

// Priekšpuse izstrādes režīmā (Vite) darbojas uz cita porta.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Datubāzes izveide un demonstrācijas dati. Docker vidē PostgreSQL var startēt lēnāk, tāpēc mēģinām vairākas reizes.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (int attempt = 1; ; attempt++)
    {
        try
        {
            db.Database.EnsureCreated();
            SeedData.Seed(db);
            logger.LogInformation("Datubāze gatava.");
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning("Datubāze vēl nav pieejama ({Message}). Mēģinājums {Attempt}/10…", ex.Message, attempt);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}

app.UseCors();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();
