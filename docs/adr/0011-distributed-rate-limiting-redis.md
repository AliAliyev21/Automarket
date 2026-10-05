# 0011. Paylanmış rate limiting: Redis sliding window

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: SEC-RATE-01..13, SEC-AUTH-03, SEC-AUTHZ-05, NFR-HC, OWASP API4/API6

## Kontekst

REQUIREMENTS 4.5-də 11 rate limit qaydası var. Onların açarları IP, istifadəçi və email-dir, pəncərələri isə dəqiqədən günə qədər dəyişir. Bəzi əməliyyatlarda birdən çox limit eyni anda tətbiq olunur (məs. telefon: 20/saat və 100/gün). Pəncərə tipinin seçimi arxitektura sənədinə saxlanılıb.

Tətbiq bir neçə instansiyada işləyir. ASP.NET Core-un daxili limiter-ləri (`FixedWindowRateLimiter`, `SlidingWindowRateLimiter`, `TokenBucketRateLimiter`) yalnız **proses daxilində** işləyir: N instansiyada faktiki limit N dəfə artır. Telefon scraping-i və login brute-force kimi hücumlar üçün bu qəbul edilmir.

## Qərar

1. ASP.NET Core-un **`Microsoft.AspNetCore.RateLimiting` middleware-i** və adlı policy-lər (`AddPolicy`, `RequireRateLimiting("phone-reveal")`, `GlobalLimiter`) istifadə olunur. `429` cavabı (`OnRejected`) ProblemDetails `RATE_LIMITED` + `Retry-After` formatında qaytarılır.
2. Limiter öz `RedisSlidingWindowRateLimiter : RateLimiter` sinfimizdir (`BuildingBlocks.Web/RateLimiting`). Partition-lar `PartitionedRateLimiter.Create` ilə yaradılır və hər partition açarı Redis açarına çevrilir.
3. **Alqoritm — sliding window counter:** cari və əvvəlki sabit pəncərənin sayğacları saxlanılır. Təxmini say `prev × (1 − elapsed/window) + curr` düsturu ilə hesablanır. Hesablama və artırma bir Lua skriptində atomik aparılır (`EVALSHA`). Hər açar üçün iki tam ədəd saxlanılır və TTL = 2 × pəncərə olur.
   - Niyə bu alqoritm: fixed window-un pəncərə sərhədində ikiqat "burst" problemi yoxdur. Sliding log-dan fərqli olaraq yaddaş O(1)-dir (hər sorğunun vaxtını saxlamır). Dəqiqliyi təhlükəsizlik limitləri üçün kifayətdir.
4. Bir endpoint-də bir neçə limit `PartitionedRateLimiter.CreateChained` ilə birləşdirilir.
5. **Açarlar:** IP — yalnız `ForwardedHeaders` middleware-inin etibarlı proxy-lərdən (`KnownProxies`/`KnownNetworks`) bərpa etdiyi `RemoteIpAddress` (SEC-RATE-12). İstifadəçi — token-dəki `sub`. Email — normallaşdırılmış email-in SHA-256 hash-i (Redis-də PII saxlanılmır).
6. **Body-dən gələn açarlar** (login və şifrə bərpasında email) middleware-də deyil, handler daxilində `IRateLimitService.TryAcquireAsync(policy, key)` ilə yoxlanılır. Bu servis eyni Lua skriptindən istifadə edir.
7. **Lockout** (SEC-AUTH-03) rate limit deyil: o, Identity-nin DB-dəki sahələrində saxlanılır və rate limit ilə birlikdə tətbiq olunur.
8. **Redis əlçatmaz olduqda:** limiter lokal in-memory `SlidingWindowRateLimiter`-ə keçir (eyni limitlər, instansiya üzrə), `Warning` log yazılır, health `Degraded` olur. Bu, fail-open (limitsiz) və fail-closed (bütün sorğuların rədd edilməsi) arasında kompromisdir.
9. Rate limit hadisələri Redis sayğaclarında aqreqasiya olunur və dəqiqədə bir dəfə audit-ə yazılır (SEC-RATE-13). Health endpoint-ləri limitdən çıxarılır.

## Alternativlər

### Yalnız built-in in-memory limiter-lər

- Üstünlüklər: əlavə kod yoxdur, ən sürətli variantdır.
- Çatışmazlıqlar: N instansiyada limit N dəfə artır. Load balancer sticky session olmadan sorğuları paylayır, ona görə limitlər faktiki olaraq proqnozlaşdırılmaz olur.
- Niyə seçilmədi: təhlükəsizlik limitləri (login, telefon) üçün qəbul edilmir. Fallback kimi istifadə olunur.

### `RedisRateLimiting` icma paketi (cristipufu/aspnetcore-redis-rate-limiting)

- Lisenziya: MIT.
- Üstünlüklər: built-in `RateLimiter` abstraksiyasını Redis ilə genişləndirir, fixed/sliding/token bucket alqoritmləri hazırdır.
- Çatışmazlıqlar: xarici asılılıqdır və icma tərəfindən saxlanılır. Redis əlçatmaz olduqda fallback davranışı, açar formatı və aqreqasiya edilmiş audit üçün əlavə iş yenə də lazımdır. Sliding window variantı sorted set istifadə edir, yəni hər sorğu üçün bir element saxlayır və yaddaş O(n) olur.
- Niyə seçilmədi: bizə lazım olan kod kiçikdir (bir Lua skripti + `RateLimiter` implementasiyası). Fallback və aqreqasiya üzərində tam nəzarət vacibdir.

### Token bucket

- Niyə seçilmədi: REQUIREMENTS-dəki limitlər "N sorğu / müddət" formasındadır və gündəlik limitlər də var. Token bucket burst-ə icazə verir və gündəlik limitləri intuitiv ifadə etmir. Sliding window counter limitləri hərfi mənada daha yaxşı əks etdirir.

### Fixed window

- Niyə seçilmədi: pəncərə sərhədində qısa müddətdə limitin iki qatını keçmək mümkündür (məs. login üçün 59-cu və 61-ci saniyələrdə 10 + 10 cəhd).

### Reverse proxy / API gateway səviyyəsində rate limiting

- Niyə seçilmədi: hosting seçilməyib. İstifadəçi və email əsaslı açarlar tətbiq məntiqi tələb edir. Gateway səviyyəsində əlavə qlobal qoruma (DDoS) gələcəkdə əlavə edilə bilər, amma tətbiq səviyyəsindəki limitləri əvəz etmir.

## Nəticələr

Müsbət:

- Limitlər bütün instansiyalar üzrə vahiddir və konfiqurasiyadan oxunur.
- ASP.NET Core-un standart middleware-i və policy modeli istifadə olunur: endpoint-lərdə `RequireRateLimiting(...)`, `DisableRateLimiting()`.
- Redis yaddaşı limit açarı başına O(1)-dir.

Mənfi və risklər:

- Hər məhdudlaşdırılmış sorğu üçün bir Redis gedişi (təxminən 0.3–1 ms) əlavə olunur.
- Sliding window counter təxminidir (pəncərədə paylanmanın bərabər olduğunu fərz edir). Təhlükəsizlik limitləri üçün bu fərq əhəmiyyətsizdir və sənədləşdirilir.
- Redis fallback rejimində limitlər instansiya sayına vurulur. Bu müvəqqəti vəziyyət health və log ilə görünür.
- NFR-TEST-04: rate limit testləri real Redis (Testcontainers) ilə aparılır, o cümlədən iki "instansiya" (iki `WebApplicationFactory`) arasında limitin ortaq olması yoxlanılır.
