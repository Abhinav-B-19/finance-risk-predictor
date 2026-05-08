using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────
// SERVICES
// ─────────────────────────────────────────

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<PredictionService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ─────────────────────────────────────────
// CORS
// ─────────────────────────────────────────

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// ─────────────────────────────────────────
// RENDER PORT CONFIG
// ─────────────────────────────────────────

var port =
    Environment.GetEnvironmentVariable("PORT")
    ?? "10000";

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// ─────────────────────────────────────────
// MIDDLEWARE
// ─────────────────────────────────────────

// Swagger enabled in production
app.UseSwagger();

app.UseSwaggerUI();

// Enable CORS
app.UseCors("AllowAll");

// DO NOT use HTTPS redirection on Render
// app.UseHttpsRedirection();

app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();