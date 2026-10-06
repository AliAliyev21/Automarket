using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.RefreshSession;

// Web client üçün token cookie-dən oxunur (SEC-NET-04), ona görə HTTP request modeli yoxdur
[LogRedact]
internal sealed record RefreshSessionCommand(string RefreshToken);
