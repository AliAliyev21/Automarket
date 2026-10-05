# AutoMarket — Claude Code üçün qeydlər

AutoMarket Azərbaycan bazarı üçün avtomobil elanları platformasının backend-idir (turbo.az kimi). Məhsul REST Web API-dir: C# / .NET 10, ASP.NET Core Minimal API, PostgreSQL 17, Redis, RabbitMQ. Arxitektura modulyar monolitdir: 8 modul (Identity, Catalog, Listings, Search, Moderation, Engagement, Messaging, Notifications), hər modulun öz schema-sı və açıq kontraktı (`*.Contracts`) var. Modullar arası əlaqə Contracts interfeysləri (sinxron oxuma) və outbox → RabbitMQ integration event-ləri ilə aparılır.

## Sənədlər — hansı sualda hansına baxmalı

| Sual | Sənəd |
|---|---|
| Nə edilməlidir? Biznes qaydası, AC, limit, xəta kodu, icazə matrisi, təhlükəsizlik tələbi | [docs/REQUIREMENTS.md](docs/REQUIREMENTS.md) (`FR-*`, `SEC-*`, `NFR-*`, `Q*`) |
| Necə qurulub? Modul sərhədləri, schema, event-lər, axınlar, middleware, cache, job-lar | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| Bu qərar niyə belədir? Alternativlər | [docs/adr/](docs/adr/README.md) |
| Kod necə yazılır? Adlandırma, qovluqlar, handler/endpoint forması, EF, log, testlər, git | [docs/CONVENTIONS.md](docs/CONVENTIONS.md) |
| Hansı paket istifadə oluna bilər? | [ARCHITECTURE §12](docs/ARCHITECTURE.md#12-kitabxanalar-və-lisenziyalar) |

Sənədlər Azərbaycan dilindədir. Tapşırığa aid bölməni oxumadan kod yazma.

## Komandalar

Kod hələ yoxdur — komandalar solution yarandıqdan sonra keçərlidir (struktur: ARCHITECTURE §2.1).

```bash
docker compose up -d                                   # PostgreSQL, Redis, RabbitMQ, Mailpit, Seq
dotnet restore
dotnet build AutoMarket.slnx                           # warnings = errors
dotnet format --verify-no-changes
dotnet test                                            # hamısı (integration üçün Docker lazımdır)
dotnet test tests/AutoMarket.Listings.UnitTests        # bir proyekt
dotnet test tests/AutoMarket.ArchitectureTests
dotnet test --filter "FullyQualifiedName~SubmitListing"
dotnet run --project src/Host/AutoMarket.Api           # Development-də migration-lar avtomatik
dotnet list package --vulnerable --include-transitive
```

Migration (hər modul ayrıca, ad qaydası: CONVENTIONS §7.2):

```bash
dotnet ef migrations add AddListingVin --project src/Modules/Listings/AutoMarket.Listings --context ListingsDbContext --output-dir Infrastructure/Persistence/Migrations
dotnet ef migrations script --project src/Modules/Listings/AutoMarket.Listings --context ListingsDbContext --idempotent
dotnet ef migrations remove --project src/Modules/Listings/AutoMarket.Listings --context ListingsDbContext
```

`main`-ə daxil olmuş migration-u redaktə etmə və silmə.

## Ən vacib qaydalar

**Modul sərhədləri**
- `AutoMarket.<A>` başqa modulun yalnız `AutoMarket.<B>.Contracts` proyektinə reference verir. Başqa modulun class-ı, DbContext-i və cədvəli istifadə olunmur.
- Cross-schema `JOIN` və FK yoxdur. Başqa modulun datası: Contracts interfeysi və ya event-lərlə saxlanılan öz projection.
- Bir command = bir modulun bir transaksiyası. Başqa modula təsir yalnız outbox event-i ilə. RabbitMQ-ya birbaşa publish yoxdur.
- Modul proyektində yeganə `public` tip `<M>Module`-dur, qalan hamısı `internal sealed`.
- Qatlar: Domain → heç nə (EF/ASP.NET yoxdur); Application → Domain; Infrastructure → Application; Api → Application.

**Contracts**
- Contracts yalnız başqa modulun real ehtiyacı olan interfeys və immutable `record` DTO-ları ehtiva edir, entity yox.
- Sinxron interfeyslər yalnız oxuma üçündür. Yeni sinxron command interfeysi yalnız ADR ilə (mövcud istisna: `IListingModeration`).
- Integration event: keçmiş zaman (`ListingActivated`), `*.Contracts/Events`, versiyalı (`.v1`), payload-da email/telefon/mesaj mətni yoxdur. Versiya daxilində yalnız optional sahə əlavə olunur.

**Xəta kodları**
- Gözlənilən xəta → `Result<T>` + modulun `<Sahə>Errors` sabiti. Domen invariantı → `DomainException`. Gözlənilməz → exception, tutulmur.
- Kodlar yalnız REQUIREMENTS 4.8 siyahısından. **Yeni kod uydurma** — lazım olarsa soruş, təsdiqdən sonra REQUIREMENTS 4.8 yenilənir. Hər kod kodda bir dəfə təyin olunur.
- Başqasının resursu `*_NOT_FOUND` (404) qaytarır (BOLA). İstifadəçi id-si yalnız `ICurrentUser`-dən.

**Lisenziya**
- Yeni NuGet paketi əlavə etməzdən əvvəl soruş. Paket ARCHITECTURE §12-də olmalıdır və ya lisenziyası yoxlanıb ora əlavə edilməlidir.
- Qadağandır: MediatR, AutoMapper, MassTransit v9+, FluentAssertions v8+, SixLabors.ImageSharp, Hangfire, Duende IdentityServer, Moq, istənilən kommersiya/"gəlir həddi" lisenziyalı paket.
- Versiyalar yalnız `Directory.Packages.props`-da.

**Digər**
- Hər endpoint-də açıq policy və ya `AllowAnonymous`, validator + `ValidationFilter`, ayrıca Request/Response record. Yeni endpoint `AuthorizationMatrix`-ə əlavə olunur.
- Rəqəmli limitlər `IOptions`-dan, kodda sabit yoxdur. Pul yalnız `decimal`.
- `DateTime.UtcNow`, `Guid.NewGuid`, `FromSqlRaw`, `new HttpClient()` qadağandır → `TimeProvider`, id generator, `FromSql`, `IHttpClientFactory`.
- Hər async metodda `CancellationToken`. Log-da şifrə, token, telefon, mesaj mətni yoxdur; email maskalanır.
- Kod şərhləri Azərbaycan dilində, identifikatorlar, log və API mesajları ingiliscə.
- Yeni feature üçün CONVENTIONS §10 təhlükəsizlik checklist-ini keç.

## İş qaydası

1. **Plan.** Kod yazmazdan əvvəl qısa plan ver: hansı tələblər (`FR-*`, `SEC-*`), hansı fayllar yaradılır/dəyişir, hansı testlər yazılır. Böyük və ya çox modullu dəyişiklikdə təsdiq gözlə.
2. **Kod.** Plana və sənədlərə uyğun yaz. Tapşırıqdan kənar refaktorinq etmə — lazım görürsənsə, ayrıca təklif et.
3. **Yoxlama.** Sonda `dotnet build` və aid testləri (`dotnet test`) işlət. Nəticəni olduğu kimi bildir: uğursuz test və ya işlədilə bilməyən addım varsa, açıq yaz.
4. **Yeni qərar lazımdırsa soruş.** Sənədlərdə cavabı olmayan məsələdə (yeni paket, yeni xəta kodu, yeni event, yeni endpoint forması, schema dəyişikliyi, ADR-ə toxunan seçim) özün qərar vermə — variantları və tövsiyəni yaz, soruş.
5. **Sənədlərə zidd iş görmə.** İstifadəçi açıq istəsə belə, ziddiyyəti əvvəlcə göstər və təsdiq al.
6. **Ziddiyyət görsən, xəbər ver.** Sənədlə kod arasında və ya sənədlər arasında uyğunsuzluq görsən, səssizcə birini seçmə: harada olduğunu (fayl + bölmə) və təklifini yaz.

## GIT QAYDASI

**Git ilə heç bir əməliyyat etmə:** `branch`, `checkout`, `switch`, `add`, `commit`, `push`, `pull`, `merge`, `rebase`, `reset`, `restore`, `stash`, `tag`, həmçinin `gh`. Bunları istifadəçi özü edir.

Yalnız oxuma (`git status`, `git diff`, `git log`) dəyişiklikləri yoxlamaq üçün icazəlidir.

İş bitəndə cavabın sonunda yaz:

1. Dəyişən faylların siyahısı (yaradılan / dəyişdirilən / silinən).
2. Conventional Commits formatında commit mesajı təklifi (CONVENTIONS §12.2): `type(scope)` ingiliscə, təsvir Azərbaycan dilində.

```text
feat(listings): elanın moderasiyaya göndərilməsini əlavə et

Draft/Rejected → Pending keçidi, Pending + Active ≤ 5 limiti, ListingSubmitted event-i (FR-LST-02).
```
