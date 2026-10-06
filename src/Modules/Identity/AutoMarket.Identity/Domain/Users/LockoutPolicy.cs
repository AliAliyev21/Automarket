namespace AutoMarket.Identity.Domain.Users;

// SEC-AUTH-03 parametrləri; dəyərlər konfiqurasiyadan gəlir (Identity:Lockout)
internal sealed record LockoutPolicy(int MaxFailedAttempts, TimeSpan FailureWindow, TimeSpan BaseDuration, TimeSpan MaxDuration)
{
    // 1-ci kilid BaseDuration, hər növbəti ikiqat, MaxDuration-dan çox deyil
    public TimeSpan DurationForLevel(int level)
    {
        var duration = BaseDuration;
        for (var i = 1; i < level && duration < MaxDuration; i++)
        {
            duration *= 2;
        }

        return duration < MaxDuration ? duration : MaxDuration;
    }
}
