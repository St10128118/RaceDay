using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var usesEphemeralJwtKey = false;
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Set Jwt:Key");
    }

    jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    builder.Configuration["Jwt:Key"] = jwtKey;
    usesEphemeralJwtKey = true;
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RaceDay";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT access token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.IPasswordHasher<RaceDay.Api.Models.User>, Microsoft.AspNetCore.Identity.PasswordHasher<RaceDay.Api.Models.User>>();

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (usesEphemeralJwtKey)
{
    app.Logger.LogWarning("Using a temporary JWT signing key for local development. Tokens expire when the API restarts.");
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedDevelopmentOrganiserAsync(scope.ServiceProvider, builder.Configuration, app.Logger);

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task SeedDevelopmentOrganiserAsync(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger logger)
{
    var email = configuration["DevelopmentSeed:OrganiserEmail"];
    var password = configuration["DevelopmentSeed:OrganiserPassword"];
    if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
    {
        logger.LogInformation("Development organiser seeding is disabled; configure both DevelopmentSeed:OrganiserEmail and DevelopmentSeed:OrganiserPassword to enable it.");
        return;
    }

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("Configure both DevelopmentSeed:OrganiserEmail and DevelopmentSeed:OrganiserPassword to seed the development organiser.");
    if (password.Length < 12)
        throw new InvalidOperationException("DevelopmentSeed:OrganiserPassword must be at least 12 characters long.");

    var normalisedEmail = email.Trim().ToLowerInvariant();
    var db = services.GetRequiredService<RaceDayDbContext>();
    if (await db.User.AnyAsync(user => user.Email == normalisedEmail)) return;

    var organiser = new RaceDay.Api.Models.User
    {
        Role = "Organiser",
        FirstName = "Development",
        LastName = "Organiser",
        Email = normalisedEmail
    };
    var hasher = services.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<RaceDay.Api.Models.User>>();
    organiser.PasswordHash = hasher.HashPassword(organiser, password);
    db.User.Add(organiser);
    await db.SaveChangesAsync();
    logger.LogInformation("Seeded development organiser account {Email}.", normalisedEmail);
}

public partial class Program { }
