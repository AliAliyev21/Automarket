namespace AutoMarket.Identity.Domain.Users;

// FR-AUTH-01 AC2: email normallaşdırılır (trim, kiçik hərf)
internal static class EmailAddress
{
    private const char DottedCapitalI = 'İ';

    public static string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        // ARCHITECTURE §4.3: .NET-in invariant kiçik hərfə çevirməsi "İ" (U+0130) hərfini dəyişmir. Unicode-un sadə
        // xəritəsinə görə o "i"-yə çevrilir, əks halda "ƏLİ@..." və "əli@..." fərqli hesab sayılardı.
        // az mədəniyyəti istifadə olunmur: ASCII "I" → "ı" olardı
        return email.Trim().ToLowerInvariant().Replace(DottedCapitalI, 'i');
    }
}
