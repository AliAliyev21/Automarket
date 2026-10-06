namespace AutoMarket.BuildingBlocks.Security;

// SEC-LOG-01: email log-a yalnız maskalanmış şəkildə yazılır (a***@mpay.az)
public static class EmailMask
{
    private const string Masked = "***";

    public static string Mask(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Masked;
        }

        var at = email.LastIndexOf('@');
        if (at <= 0)
        {
            return Masked;
        }

        return string.Concat(email.AsSpan(0, 1), Masked, email.AsSpan(at));
    }
}
