using AutoMarket.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.Identity.Application.Admin;

// REQUIREMENTS 4.8 (Q25). R-05 pozuntuları (özünü bloklamaq, öz Admin rolunu ləğv etmək) CommonErrors.Forbidden qaytarır
internal static class AdminErrors
{
    public static readonly Error UserNotFound =
        new("USER_NOT_FOUND", "User not found.", StatusCodes.Status404NotFound);
}
