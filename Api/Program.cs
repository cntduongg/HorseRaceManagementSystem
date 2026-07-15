using System.Text;
using Api.Middlewares;
using Application.DependencyInjection;
using Infrastructure.Data;
using Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Application.Common;
using Application.DTOs;
using Infrastructure.Data.Seed;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddSignalR(); // Flow 4 — push diễn biến đua trực tiếp (RaceLiveHub).
//builder.Services.AddHttpContextAccessor();
var allowedOrigins = builder.Configuration
                         .GetSection("Cors:AllowedOrigins")
                         .Get<string[]>()
                     ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Bắt buộc cho SignalR browser client (negotiate gửi withCredentials: true).
            // KHÔNG đổi WithOrigins thành AllowAnyOrigin(): kèm AllowCredentials() sẽ ném
            // InvalidOperationException lúc khởi động (spec cấm "*" đi với credentials).
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HorseRace API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT access token only. Do not type Bearer.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(openApiDocument => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", openApiDocument),
            new List<string>()
        }
    });
});
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new()
//     {
//         Title = "HorseRace API",
//         Version = "v1"
//     });
//
//     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Description = "JWT Authorization header. Format: Bearer {token}",
//         Name = "Authorization",
//         In = ParameterLocation.Header,
//         Type = SecuritySchemeType.Http,
//         Scheme = "bearer",
//         BearerFormat = "JWT"
//     });
//
//     c.OperationFilter<BearerSecurityOperationFilter>();
// });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Hiện thực kênh push của Application bằng SignalR (Application không phụ thuộc ASP.NET).
builder.Services.AddScoped<Application.Common.Interfaces.IRaceLiveNotifier,
                           Api.Services.SignalRRaceLiveNotifier>();

// Flow 7 — tác vụ nền cộng điểm ví +100 mỗi thứ Hai.
builder.Services.AddHostedService<Api.Services.WeeklyTopUpBackgroundService>();

// Flow 3/4 — tự động đóng ĐK + start Race khi tới ScheduledStartTime.
builder.Services.Configure<Api.Services.RaceAutoStartOptions>(
    builder.Configuration.GetSection(Api.Services.RaceAutoStartOptions.SectionName));
builder.Services.AddHostedService<Api.Services.RaceAutoStartBackgroundService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

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
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            // SignalR qua WebSocket không gửi được header Authorization → token đi bằng query string.
            // Guard StartsWithSegments là phần quan trọng về bảo mật: thiếu nó thì ?access_token=
            // trở thành auth hợp lệ trên MỌI endpoint (token lọt vào history/referrer/log proxy,
            // và mở bề mặt CSRF cho các POST đổi dữ liệu).
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments(Api.Hubs.RaceLiveHub.Path))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },

            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = string.IsNullOrEmpty(context.ErrorDescription)
                        ? "Authentication token is missing or invalid."
                        : context.ErrorDescription,
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problem);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "You do not have permission to access this resource.",
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problem);
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<PasswordResetOptions>(
    builder.Configuration.GetSection(
        PasswordResetOptions.SectionName));


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await db.Database.MigrateAsync();

    var seedTestDataEnabled =
        builder.Configuration.GetValue<bool>("SeedTestData");

    if (app.Environment.IsProduction() &&
        seedTestDataEnabled)
    {
        throw new InvalidOperationException(
            "SeedTestData cannot be enabled in Production.");
    }

    var shouldSeedTestData =
        app.Environment.IsDevelopment() ||
        seedTestDataEnabled;

    if (shouldSeedTestData)
    {
        await DatabaseSeeder.SeedAsync(
            db,
            passwordHasher);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("FrontendPolicy");

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<Api.Hubs.RaceLiveHub>(Api.Hubs.RaceLiveHub.Path);


app.Run();