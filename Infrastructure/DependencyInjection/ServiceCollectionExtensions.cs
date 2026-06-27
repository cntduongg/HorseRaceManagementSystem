using Application.Common;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShareKernel.Repository;
using ShareKernel.UnitOfWork;
using Application.Common.Interfaces;

namespace Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Missing connection string: ConnectionStrings:DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IApplicationDbContext>(sp =>
    sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IUnitOfWork, ApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRaceResultReadService, RaceResultReadService>();
        services.AddScoped<ITournamentReadService, TournamentReadService>();
        services.AddScoped<IRaceReadService, RaceReadService>();
        services.AddScoped<IEntryReadService, EntryReadService>();
        services.AddScoped<IHorseRepository, HorseRepository>();
        services.AddScoped<IEntryRepository, EntryRepository>();
        services.AddScoped<IReviewHistoryRepository, ReviewHistoryRepository>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        return services;
    }
}