using HappyPaws.Core.Entities;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using HappyPaws.Infrastructure.Data.Interceptors;
using HappyPaws.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TimestampInterceptor>();

        services.AddDbContext<HappyPawsDbContext>((sp, options) =>
        {
            // DATABASE_URL is a standard PostgreSQL connection URL.
            // Npgsql accepts it directly via UseNpgsql(string).
            var databaseUrl = configuration["DATABASE_URL"]
                ?? "postgresql://happypaws:happypaws_dev@localhost:5432/happypaws";

            if (databaseUrl.StartsWith("postgres://") || databaseUrl.StartsWith("postgresql://"))
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                databaseUrl = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.LocalPath.TrimStart('/')};Username={(userInfo.Length > 0 ? userInfo[0] : "")};Password={(userInfo.Length > 1 ? userInfo[1] : "")};";
            }

            options.UseNpgsql(databaseUrl, npgsqlOptions => npgsqlOptions.UseNetTopologySuite());
            options.AddInterceptors(sp.GetRequiredService<TimestampInterceptor>());
        });

        services.AddMemoryCache();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddHttpClient("Gemini");
        services.AddKeyedScoped<IUrgencyClassifier, GeminiUrgencyClassifier>("gemini");
        services.AddKeyedScoped<IUrgencyClassifier, RuleBasedUrgencyClassifier>("ruleBased");
        services.AddScoped<IUrgencyClassificationService, ResilientUrgencyClassificationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IStorageService, S3StorageService>();
        services.AddScoped<IEmailSender, SesEmailSender>();
        services.AddScoped<IPushNotificationService, FcmPushNotificationService>();
        services.AddScoped<IReputationService, ReputationService>();
        services.AddScoped<IBadgeEvaluationService, BadgeEvaluationService>();

        return services;
    }

    public static IServiceCollection AddDevServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailSender, LocalEmailSender>();
        services.AddScoped<IPushNotificationService, LocalPushNotificationService>();
        return services;
    }
}
