using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Api.Filters;
using Api.Middlewares;

using Application.DependencyInjection;
using Application.Common;

using Infrastructure.Data;
using Infrastructure.DependencyInjection;
using Infrastructure.Data.Seed;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

/* ---------------- EXCEPTION ---------------- */
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

/* ---------------- CONTROLLERS ---------------- */
builder.Services.AddControllers();

/* ---------------- CORS ---------------- */
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();

/* ---------------- SWAGGER ---------------- */
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HorseRace API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {token}"
    });

    c.OperationFilter<BearerSecurityOperationFilter>();
});

/* ---------------- DEPENDENCY INJECTION ---------------- */
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

/* ---------------- JWT CONFIG ---------------- */
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey is missing.");

var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,
            ValidAudience = audience,

            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

            ClockSkew = TimeSpan.Zero,

            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Email
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("JWT FAILED:");
                Console.WriteLine(context.Exception.Message);
                return Task.CompletedTask;
            },

            OnMessageReceived = context =>
            {
                Console.WriteLine("TOKEN:");
                Console.WriteLine(context.Token);
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                Console.WriteLine("JWT CHALLENGE TRIGGERED");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

/* ---------------- BUILD APP ---------------- */
var app = builder.Build();

/* ---------------- DB MIGRATION + SEED ---------------- */
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await db.Database.MigrateAsync();

    var shouldSeed =
        app.Environment.IsDevelopment()
        || builder.Configuration.GetValue<bool>("SeedTestData");

    if (shouldSeed)
    {
        await DatabaseSeeder.SeedAsync(db, passwordHasher);
    }
}

/* ---------------- PIPELINE (FIXED) ---------------- */

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("FrontendPolicy");

app.UseExceptionHandler();

app.UseAuthentication();  
app.UseAuthorization();    

app.MapControllers();    

app.Run();