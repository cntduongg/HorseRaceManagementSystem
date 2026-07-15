using Application.Common;
using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Microsoft.Extensions.DependencyInjection;
namespace Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IRaceLifecycleCoordinator, RaceLifecycleCoordinator>();
        services.AddScoped<IRaceLiveChangeTracker, RaceLiveChangeTracker>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            // ⚠️ THỨ TỰ QUAN TRỌNG: RaceLiveBroadcastBehavior phải đứng NGOÀI UnitOfWorkBehavior
            // (đăng ký trước = nằm ngoài) để chỉ đẩy snapshot SAU khi đã commit.
            // Chuyển xuống dưới UnitOfWork → client nhận push rồi refetch trúng dữ liệu cũ.
            cfg.AddOpenBehavior(typeof(RaceLiveBroadcastBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        return services;
    }
}