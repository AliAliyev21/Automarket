using AutoMarket.Catalog;
using AutoMarket.Engagement;
using AutoMarket.Identity;
using AutoMarket.Listings;
using AutoMarket.Messaging;
using AutoMarket.Moderation;
using AutoMarket.Notifications;
using AutoMarket.Search;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentityModule(builder.Configuration)
    .AddCatalogModule(builder.Configuration)
    .AddListingsModule(builder.Configuration)
    .AddSearchModule(builder.Configuration)
    .AddModerationModule(builder.Configuration)
    .AddEngagementModule(builder.Configuration)
    .AddMessagingModule(builder.Configuration)
    .AddNotificationsModule(builder.Configuration);

var app = builder.Build();

app.MapIdentityEndpoints();
app.MapCatalogEndpoints();
app.MapListingsEndpoints();
app.MapSearchEndpoints();
app.MapModerationEndpoints();
app.MapEngagementEndpoints();
app.MapMessagingEndpoints();
app.MapNotificationsEndpoints();

// Prosesin işlədiyini yoxlamaq üçün; health check-lər ayrıca addımda əlavə olunacaq (ARCHITECTURE §8.5)
app.MapGet("/ping", () => TypedResults.Ok("pong"))
    .WithName("Ping")
    .AllowAnonymous();

await app.RunAsync();
