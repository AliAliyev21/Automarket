using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.Logout;

// RefreshToken cookie-dən gəlir, olmaya da bilər
[LogRedact]
internal sealed record LogoutCommand(string? RefreshToken);
