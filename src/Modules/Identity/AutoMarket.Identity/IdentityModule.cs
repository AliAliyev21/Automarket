using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Messaging;
using AutoMarket.BuildingBlocks.Persistence;
using AutoMarket.BuildingBlocks.Web.Endpoints;
using AutoMarket.Identity.Api.Auth;
using AutoMarket.Identity.Api.Me;
using AutoMarket.Identity.Application;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.ConfirmEmail;
using AutoMarket.Identity.Application.Auth.Login;
using AutoMarket.Identity.Application.Auth.RefreshSession;
using AutoMarket.Identity.Application.Auth.Register;
using AutoMarket.Identity.Application.Auth.ResendConfirmation;
using AutoMarket.Identity.Application.Me.GetMe;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Infrastructure.Events;
using AutoMarket.Identity.Infrastructure.Passwords;
using AutoMarket.Identity.Infrastructure.Persistence.Queries;
using AutoMarket.Identity.Infrastructure.Persistence.Repositories;
using AutoMarket.Identity.Infrastructure.Tokens;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using IdentityDbContext = AutoMarket.Identity.Infrastructure.Persistence.IdentityDbContext;
using IdentityOptions = AutoMarket.Identity.Application.IdentityOptions;

namespace AutoMarket.Identity;

// Modulun yeganə public tipi: DI qeydiyyatı və endpoint mapping burada aparılır (ADR-0002)
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddOptions(services, configuration);
        AddPersistence(services);
        AddIdentityCore(services);
        AddAuthentication(services);
        AddApplication(services);

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var api = endpoints.MapApiV1();
        api.MapAuthEndpoints();
        api.MapGetMe();

        return endpoints;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityOptions>()
            .Bind(configuration.GetSection(IdentityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<IdentityOptions>, IdentityOptionsValidator>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
    }

    private static void AddPersistence(IServiceCollection services)
    {
        services.AddModuleDbContext<IdentityDbContext>(IdentityDbContext.SchemaName);
        services.AddScoped<IIdentityUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IIntegrationEventMapper<IdentityDbContext>, IdentityIntegrationEventMapper>();
        services.AddOutboxPublisher<IdentityDbContext>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOneTimeTokenRepository, OneTimeTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserQueries, UserQueries>();
    }

    // ADR-0003: Identity Core (UI və MapIdentityApi olmadan). Kompozisiya qaydaları və daxili lockout söndürülüb:
    // şifrə siyasəti PasswordPolicyValidator-dadır, eskalasiya olunan lockout User-dədir
    private static void AddIdentityCore(IServiceCollection services)
    {
        services.AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = string.Empty;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequiredLength = 0;
                options.Lockout.AllowedForNewUsers = false;
            })
            .AddPasswordValidator<PasswordPolicyValidator>();

        // Bir command = bir SaveChanges: store kontekstə yazır, amma özü saxlamır (CONVENTIONS §7.4)
        services.AddScoped<IUserStore<User>>(serviceProvider =>
            new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<User, Role, IdentityDbContext, Guid>(serviceProvider.GetRequiredService<IdentityDbContext>())
            {
                AutoSaveChanges = false,
            });

        // SEC-AUTH-02: iterasiya sayı konfiqurasiyadan (≥ 210 000, options validasiyası)
        services.AddOptions<PasswordHasherOptions>()
            .Configure<IOptions<IdentityOptions>>((hasher, identity) => hasher.IterationCount = identity.Value.Password.IterationCount);

        services.AddSingleton<IPasswordPolicy, PasswordPolicy>();
        services.AddSingleton<DummyPasswordHash>();
        services.AddScoped<IPasswordService, PasswordService>();
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services.AddSingleton<JwtKeyRing>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
    }

    private static void AddApplication(IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(IdentityModule).Assembly, ServiceLifetime.Singleton, includeInternalTypes: true);

        services.AddScoped<SessionService>();
        services.AddScoped<EmailConfirmationIssuer>();

        services.AddScoped<ICommandHandler<RegisterCommand>, RegisterHandler>();
        services.AddScoped<ICommandHandler<ConfirmEmailCommand>, ConfirmEmailHandler>();
        services.AddScoped<ICommandHandler<ResendConfirmationCommand>, ResendConfirmationHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, SessionTokens>, LoginHandler>();
        services.AddScoped<ICommandHandler<RefreshSessionCommand, SessionTokens>, RefreshSessionHandler>();
        services.AddScoped<IQueryHandler<GetMeQuery, MeResponse>, GetMeHandler>();
    }
}
