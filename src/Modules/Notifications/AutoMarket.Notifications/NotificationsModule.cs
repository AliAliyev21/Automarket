using AutoMarket.BuildingBlocks.Messaging;
using AutoMarket.BuildingBlocks.Persistence;
using AutoMarket.Identity.Contracts.Events;
using AutoMarket.Notifications.Application;
using AutoMarket.Notifications.Application.Abstractions;
using AutoMarket.Notifications.Application.AuthEmails;
using AutoMarket.Notifications.Infrastructure.Consumers;
using AutoMarket.Notifications.Infrastructure.Email;
using AutoMarket.Notifications.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoMarket.Notifications;

// Modulun yeganə public tipi: DI qeydiyyatı və endpoint mapping burada aparılır (ADR-0002)
public static class NotificationsModule
{
    public const string QueueName = "notifications.events";

    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection(NotificationsOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<NotificationsOptions>, NotificationsOptionsValidator>();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();

        services.AddModuleDbContext<NotificationsDbContext>(NotificationsDbContext.SchemaName);

        services.AddSingleton<AuthEmailRenderer>();
        services.AddSingleton<IEmailTransport, MailKitEmailTransport>();

        // ARCHITECTURE §3.2: Notifications AuthEmailRequested-i consume edir
        services.AddIntegrationEventConsumer<NotificationsDbContext>(QueueName, consumer => consumer
            .Handle<AuthEmailRequested, AuthEmailRequestedConsumer>());

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints;
    }
}
