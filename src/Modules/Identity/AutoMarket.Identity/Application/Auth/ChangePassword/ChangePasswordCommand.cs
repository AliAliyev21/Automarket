using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.ChangePassword;

// RefreshToken cari sessiyanı (ailəni) müəyyən edir: cookie-dən gəlir, olmaya da bilər
[LogRedact]
internal sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword, string? RefreshToken);
