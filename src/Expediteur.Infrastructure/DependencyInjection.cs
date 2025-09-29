using Expediteur.Domain.Contracts;
using Expediteur.Domain.Services;
using Expediteur.Infrastructure.Data;
using Expediteur.Infrastructure.Email;
using Expediteur.Infrastructure.Pdf;
using Expediteur.Infrastructure.Scheduling;
using Expediteur.Infrastructure.Services;
using Expediteur.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Expediteur.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddTransient<IOrderAcknowledgementRepository, SqlOrderAcknowledgementRepository>();
        services.AddTransient<IJobHistoryRepository, SqlJobHistoryRepository>();
        services.AddTransient<IScheduleConfigurationRepository, SqlScheduleConfigurationRepository>();
        services.AddSingleton<IClock, UtcClock>();

        services.AddSingleton<IPdfRenderer, QuestPdfRenderer>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddTransient<ICommandAcknowledger, CommandAcknowledger>();

        return services;
    }
}
