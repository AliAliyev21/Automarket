namespace AutoMarket.BuildingBlocks.Application;

// SEC-AUTH-06, FR-ADM-01 AC4: autentifikasiya olunmuş hər sorğuda istifadəçinin statusu yoxlanılır (ARCHITECTURE §7.2).
// Interfeys BuildingBlocks-dadır ki, BuildingBlocks.Web-dəki middleware modula asılı olmasın; implementasiya Identity-dədir
public interface IUserStatusReader
{
    public Task<UserAccessStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken);
}

public enum UserAccessStatus
{
    // Token qəbul olunur
    Active,

    // 401 ACCOUNT_BLOCKED
    Blocked,

    // İstifadəçi yoxdur və ya təsdiqlənməyib: 401 UNAUTHORIZED
    Inactive,
}
