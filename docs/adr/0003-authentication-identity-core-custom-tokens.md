# 0003. Autentifikasiya: ASP.NET Core Identity Core + öz token həlli

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: FR-AUTH-01..07, FR-ACC-02, FR-ADM-01/02, SEC-AUTH-01..08, SEC-NET-04, SEC-SEC-03/05, Q1, Q2, Q3, Q23
- Revizya: 2026-10-05 — review zamanı şifrə hash-i üzrə qərar dəyişdirildi. Öz hasher yazılmır, Identity-nin standart `PasswordHasher`-i (PBKDF2-HMAC-SHA512, 210 000 iterasiya) istifadə olunur. SEC-AUTH-02 buna uyğun yeniləndi (REQUIREMENTS Q23)

## Kontekst

REQUIREMENTS-də autentifikasiyaya qarşı ətraflı və qeyri-standart tələblər var:

- Şifrə hash-i: Argon2id (m ≥ 19 MiB, t ≥ 2), **PBKDF2-HMAC-SHA512 ≥ 210 000** və ya **PBKDF2-HMAC-SHA256 ≥ 600 000 iterasiya**, versiyalanmış parametrlər, login zamanı rehash (SEC-AUTH-02).
- NIST 800-63B siyasəti: kompozisiya qaydası yoxdur, lokal top-100k sızmış şifrə siyahısı (SEC-AUTH-01, Q1).
- Eskalasiya olunan lockout: 15 dəq → ikiqat artım → ən çox 24 saat (SEC-AUTH-03).
- 15 dəqiqəlik JWT access token, minimum claim-lər, sabit alqoritm (SEC-AUTH-04).
- Opaque refresh token: rotation, token ailəsi və reuse detection, 14 gün sliding / 60 gün absolute, ən çox 10 sessiya (SEC-AUTH-05, Q3).
- Web üçün refresh token yalnız `HttpOnly` cookie-də (SEC-NET-04, Q2).
- Bloklanmış istifadəçinin access tokeni dərhal rədd edilir (SEC-AUTH-06).
- Enumeration qarşısı və eyni cavab müddəti (SEC-AUTH-08).
- JWT açarlarının rotasiyası (SEC-SEC-05).

ASP.NET Core Identity-nin hazır imkanları: `UserManager`, user/role store (EF Core), lockout sahələri, security stamp, email/şifrə tokenləri, `MapIdentityApi` (.NET 8+). Ancaq:

- Default `PasswordHasher` V3 formatı PBKDF2-HMAC-**SHA512** istifadə edir, default iterasiya sayı 100 000-dir. Iterasiya sayı `PasswordHasherOptions.IterationCount` ilə konfiqurasiya olunur, PRF isə konfiqurasiya olunmur. OWASP Password Storage Cheat Sheet PBKDF2-HMAC-SHA512 üçün ən azı 210 000 iterasiya tövsiyə edir.
- `MapIdentityApi`-nin bearer tokenləri JWT deyil (Data Protection ilə qorunan proprietary tokenlərdir). Refresh rotation-u, reuse detection-u və cookie rejimində bizim cookie atributlarımız yoxdur. Endpoint-lərin cavabları enumeration tələblərinə tam uyğun deyil.
- Identity-nin lockout-u sabit müddətlidir, eskalasiya yoxdur.

## Qərar

**ASP.NET Core Identity Core** (`AddIdentityCore<User>()`, UI və `MapIdentityApi` olmadan) istifadə olunur. Token və login axınları isə öz kodumuzla yazılır.

Identity-dən istifadə olunan hissələr:

- `UserManager<User>`, `RoleManager<Role>` və EF Core store-ları (`identity` schema-sında, cədvəl adları dəyişdirilmiş halda).
- Email normallaşdırılması, unikallıq, `AccessFailedCount`/`LockoutEnd` sahələri, `SecurityStamp`.
- **Standart `PasswordHasher<User>`** (V3 formatı): PBKDF2-HMAC-SHA512, 128 bit salt, 256 bit çıxış, `PasswordHasherOptions.IterationCount = 210_000` (konfiqurasiyadan oxunur, aşağı hədd 210 000 options validasiyası ilə yoxlanılır). V3 formatı PRF-i, iterasiya sayını və salt-ı hash-in header-ində saxlayır, yəni parametrlər versiyalanır. Saxlanılmış hash-in iterasiya sayı konfiqurasiyadakından azdırsa, `VerifyHashedPassword` `SuccessRehashNeeded` qaytarır və `LoginService` hash-i yeniləyir. Hash müqayisəsi Identity daxilində sabit vaxtda aparılır.
- `IPasswordValidator<User>` genişlənmə nöqtəsi.

Öz kodumuzla yazılan hissələr:

| Komponent | Təsvir |
|---|---|
| `PasswordPolicyValidator : IPasswordValidator<User>` | uzunluq 10–128, top-100k siyahısı (embedded resource, `FrozenSet`), email/ad ilə müqayisə. Identity-nin `RequireDigit` və s. qaydaları söndürülür |
| `LoginService` | dummy hash ilə eyni müddət, `EMAIL_NOT_CONFIRMED` / `ACCOUNT_BLOCKED` / `ACCOUNT_LOCKED_OUT` yalnız şifrə düzgün olduqda, eskalasiya olunan lockout (`lockout_level`), audit |
| `JwtTokenIssuer` + `JwtKeyRing` | `System.IdentityModel.Tokens.Jwt` / `Microsoft.IdentityModel.JsonWebTokens` (MIT, .NET-in hissəsi). HS256, `kid` ilə bir neçə açar (rotasiya), claim-lər: `sub`, `role`, `jti`, `iss`, `aud`, `exp`, `iat` |
| `RefreshTokenService` | 256 bit opaque token, SHA-256 hash, `family_id`, rotation, reuse → bütün ailənin ləğvi, sliding və absolute müddət, sessiya limiti |
| `OneTimeTokenService` | email təsdiqi və şifrə bərpası üçün təsadüfi token, hash, purpose, birdəfəlik istifadə. Identity-nin `DataProtectorTokenProvider`-i istifadə olunmur, çünki o, tokeni serverdə saxlamır və birdəfəlik istifadəni yalnız security stamp ilə təmin edir |
| `UserStatusMiddleware` | hər sorğuda status yoxlaması (Redis/HybridCache), SEC-AUTH-06 |
| Cookie və Origin yoxlaması | refresh/logout endpoint-ləri, SEC-NET-04 |

Login endpoint-ində hash hesablanması CPU-nu yükləyir (PBKDF2-SHA512 210k iterasiya bir nüvədə təxminən 100–200 ms çəkir, dəqiq dəyər yük testində ölçülür). Ona görə rate limit-dən əlavə `ConcurrencyLimiter` (məs. CPU sayı × 2) tətbiq olunur. Bu, p95 ≤ 500 ms hədəfini (NFR-PERF) yük altında qorumaq üçündür.

## Alternativlər

### Tam öz həll (Identity olmadan)

- Üstünlüklər: tam nəzarət, Argon2id seçmək asanlaşır.
- Çatışmazlıqlar: istifadəçi store-u, normallaşdırma, security stamp, lockout sahələri və rol idarəsi yenidən yazılmalıdır. Bunlar Identity-də yaxşı test olunub. Təhlükəsizlik baxımından kritik kodun həcmi artır.
- Argon2id .NET-də built-in deyil: xarici paket lazımdır (Konscious.Security.Cryptography — MIT, Isopoh.Cryptography.Argon2 — CC0/MIT). Identity-nin standart PBKDF2-SHA512 hasher-i SEC-AUTH-02-ni ödəyir və .NET-in özündədir.
- Niyə seçilmədi: Identity Core sabit hissəni hazır verir, bizim xüsusi tələblərimizi isə genişlənmə nöqtələri ilə tətbiq etməyə mane olmur.

### `MapIdentityApi` (Identity API endpoint-ləri)

- Niyə seçilmədi: tokenlər JWT deyil, refresh rotation/reuse detection yoxdur, cookie atributlarını, `Path`-ı və cavab formatını (ProblemDetails + `code`) idarə etmək olmur. Enumeration və lockout tələbləri tam qarşılanmır.

### Xarici Identity Provider (Keycloak, OpenIddict, Duende IdentityServer)

- OpenIddict (Apache 2.0) və Keycloak (Apache 2.0) açıq lisenziyalıdır. Duende IdentityServer kommersiya lisenziyalıdır.
- Üstünlüklər: standart OAuth 2.1 / OIDC, gələcəkdə sosial login və mobil üçün hazır axınlar.
- Çatışmazlıqlar: MVP-də yalnız bir first-party client var. OIDC axınları (authorization code + PKCE) frontend üçün mürəkkəblik yaradır. Keycloak əlavə servis və əməliyyat yükü deməkdir. Bizim xüsusi tələblərimiz (cookie rejimi, enumeration, eskalasiya olunan lockout, top-100k siyahı, ProblemDetails kodları) xarici IdP-də tətbiq etmək çətindir.
- Niyə seçilmədi: MVP üçün həddən artıq mürəkkəbdir. Sosial login və ya üçüncü tərəf client-lər lazım olarsa (1.3 — MVP-dən sonra), OpenIddict-ə keçid yeni ADR ilə qiymətləndirilir.

### Öz `IPasswordHasher` (PBKDF2-HMAC-SHA256, 600 000 iterasiya)

Bu, ilkin təklif idi: `Rfc2898DeriveBytes.Pbkdf2` üzərində öz hasher-imiz və öz versiyalanmış formatımız (`v1$pbkdf2-sha256$...`).

- Üstünlüklər: SEC-AUTH-02-nin əvvəlki redaksiyasına hərfi uyğunluq, alqoritm seçimində tam nəzarət.
- Çatışmazlıqlar: təhlükəsizlik baxımından kritik kod əlavə olunur (format, parsing, sabit vaxtda müqayisə, rehash məntiqi). Identity-nin standart hasher-i bu işləri artıq görür və geniş istifadə olunub test edilib. PBKDF2-SHA512 210k təhlükəsizlik baxımından OWASP-ın PBKDF2-SHA256 600k tövsiyəsinə ekvivalent sayılır.
- Niyə seçilmədi: review zamanı standart hasher-in istifadəsi qərara alındı. SEC-AUTH-02 hər iki variantı qəbul edəcək şəkildə yeniləndi (Q23).

### Argon2id

- Niyə seçilmədi: xarici paket və öz `IPasswordHasher` adapteri tələb edir. Yaddaş tələbi (≥ 19 MiB hər login üçün) yük altında yaddaş istifadəsini artırır. Gələcəkdə lazım olarsa, standart hasher-i əhatə edən adapter ilə keçid mümkündür: köhnə V3 hash-ləri yoxlanılır və login zamanı yeni formata rehash olunur.

## Nəticələr

Müsbət:

- Bütün SEC-AUTH tələbləri birbaşa və test olunan kodla tətbiq olunur.
- Əlavə NuGet paketi lazım deyil: Identity, JwtBearer və kriptoqrafiya .NET-in özündədir.
- Şifrə hash-i üçün öz kodumuz yoxdur. İterasiya sayı konfiqurasiya ilə artırıla bilər və mövcud hash-lər login zamanı avtomatik rehash olunur.

Mənfi və risklər:

- Təhlükəsizlik baxımından kritik kod (refresh rotation, reuse, lockout) bizim məsuliyyətimizdədir. Azaldılması: NFR-TEST-04 testləri (reuse, lockout, enumeration timing), təhlükəsizlik code review-u.
- Hash PRF-i (SHA512) Identity tərəfindən sabitdir. Gələcəkdə Identity-nin default formatı dəyişərsə (yeni versiya), köhnə hash-lər yenə də oxunur. Unit test iterasiya sayının 210 000-dən az olmadığını və rehash-in işlədiyini yoxlayır.
- HS256 simmetrik açar istifadə edir: açarı bilən hər kəs token imzalaya bilər. Yalnız bir servis həm imzaladığı, həm də yoxladığı üçün bu qəbul edilə bilər. Tokeni başqa servislər də yoxlamalı olarsa, ES256/RS256-ya keçid (JWKS ilə) planlaşdırılır.
- Paralel refresh sorğuları reuse kimi qəbul edilir və bu, qəbul edilmiş davranışdır (ARCHITECTURE §13 A6). Frontend refresh sorğularını tək in-flight sorğu ilə, tablar arasında koordinasiya edərək göndərməlidir (ARCHITECTURE §6.1).
