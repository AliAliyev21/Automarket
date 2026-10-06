# AutoMarket — Kod Qaydaları (Conventions)

| Sahə | Dəyər |
|---|---|
| Versiya | 0.1 (qaralama) |
| Tarix | 2026-10-05 |
| Status | Review gözləyir |
| Əsas sənədlər | [REQUIREMENTS.md](REQUIREMENTS.md) v0.3, [ARCHITECTURE.md](ARCHITECTURE.md) v0.1, [adr/](adr/README.md) |

> Bu sənəd kodun **necə yazıldığını** müəyyən edir. Arxitektura qərarları ARCHITECTURE və ADR-lərdədir, burada təkrarlanmır, yalnız istinad edilir.
> Nümunələrdəki tip adları istiqamət üçündür. Qayda ilə nümunə arasında fərq olarsa, qayda keçərlidir.
> Bu sənəd ilə ARCHITECTURE/ADR arasında ziddiyyət olarsa, ARCHITECTURE/ADR keçərlidir və bu sənəd düzəldilir.

### Qəbul edilmiş seçimlər (2026-10-05)

| Mövzu | Qərar |
|---|---|
| Modul daxili qovluqlar | Qat + feature: qatlar sabitdir, qatın içində feature/use case qovluqları (§3) |
| Optimistic concurrency | API-də `version` sahəsi: response-da qaytarılır, yazma request-lərinin body-sində göndərilir (§6.6) |
| Test adları | `Method_State_Expected` (§11.2) |
| Dil | Identifikatorlar, log mesajları və API mesajları ingiliscə. Kod şərhləri, XML doc, commit və PR mətnləri Azərbaycan dilində. Conventional Commits `type(scope)` prefiksləri ingiliscə (§12) |

---

## 1. Layihə səviyyəsi

### 1.1 `Directory.Build.props` (repo kökü)

Bütün proyektlər üçün ortaq parametrlər yalnız burada təyin olunur. `.csproj` fayllarında bu parametrlər təkrarlanmır və dəyişdirilmir.

| Parametr | Dəyər | Niyə |
|---|---|---|
| `TargetFramework` | `net10.0` | bir platforma |
| `LangVersion` | `latest` | |
| `Nullable` | `enable` | null xətaları kompilyasiya zamanı tutulur |
| `ImplicitUsings` | `enable` | SDK-nın standart global using-ləri (§1.4) |
| `TreatWarningsAsErrors` | `true` | xəbərdarlıq = build xətası (ARCHITECTURE §10.2) |
| `AnalysisLevel` | `latest-recommended` | ARCHITECTURE §7.2 SEC-DEP |
| `AnalysisModeSecurity` | `All` | təhlükəsizlik qaydaları tam aktivdir (SEC-DEP-04) |
| `EnforceCodeStyleInBuild` | `true` | `.editorconfig` qaydaları build-də yoxlanılır |
| `GenerateDocumentationFile` | `true` + `NoWarn` `CS1591` | `IDE0005` (lazımsız using) build-də işləsin deyə lazımdır. XML doc məcburi deyil |
| `RestorePackagesWithLockFile` | `true` | `packages.lock.json` repoda saxlanılır (SEC-DEP-03). CI-da `--locked-mode` |
| `Deterministic`, `ContinuousIntegrationBuild` (CI-da) | `true` | təkrarlana bilən build |

Qaydalar:

- `Nullable`, `TreatWarningsAsErrors` və analyzer səviyyəsi heç bir proyektdə söndürülmür.
- Test proyektləri üçün əlavə parametrlər `tests/Directory.Build.props`-dadır (kök faylı import edir): `IsPackable=false`, test paketləri, `InternalsVisibleTo` tələbləri.
- Xəbərdarlığı susdurmaq yalnız ən dar sahədə və səbəb yazılmaqla mümkündür:

```csharp
// Düzgün: dar sahə + səbəb
[SuppressMessage("Security", "CA5394", Justification = "Test data generator, kriptoqrafik deyil")]

// Yanlış: bütün fayl üçün, səbəbsiz
#pragma warning disable CA5394
```

- Qadağan olunmuş API-lər (`DateTime.Now/UtcNow`, `DateTimeOffset.UtcNow`, `Guid.NewGuid`, `FromSqlRaw`, `ExecuteSqlRaw`, `new HttpClient()`) kök qovluqdakı `BannedSymbols.txt` ilə analyzer səviyyəsində qadağan edilir (ARCHITECTURE §7.2 SEC-INP-04, §8.9). Eyni qayda ArchUnitNET testində də var (§10.4). Analyzer paketi `Microsoft.CodeAnalysis.BannedApiAnalyzers` (MIT) bütün proyektlərə `Directory.Build.props` vasitəsilə `PrivateAssets=all` ilə qoşulur ([ARCHITECTURE §12](ARCHITECTURE.md#12-kitabxanalar-və-lisenziyalar), 2026-10-05 təsdiq olunub).

### 1.2 Central Package Management (`Directory.Packages.props`)

- `ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true`.
- Bütün versiyalar yalnız `Directory.Packages.props`-da yazılır. `.csproj`-da versiya yoxdur, `VersionOverride` qadağandır.

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="FluentValidation" Version="12.0.0" />

<!-- AutoMarket.Listings.csproj -->
<PackageReference Include="FluentValidation" />
```

- `nuget.config`-də yalnız `nuget.org` mənbəyi və `packageSourceMapping` var (SEC-DEP-03).
- **Lisenziya qaydası.** Yeni paket əlavə etməzdən əvvəl: (1) lisenziya yoxlanılır, (2) [ARCHITECTURE §12](ARCHITECTURE.md#12-kitabxanalar-və-lisenziyalar) cədvəlinə sətir əlavə olunur, (3) kommersiya və ya "gəlir həddi" lisenziyalı paket seçilmir. §12-dəki "seçilməyən kitabxanalar" (MediatR, AutoMapper, MassTransit v9+, FluentAssertions v8+, ImageSharp, Hangfire, Duende IdentityServer, Moq) heç bir proyektdə istifadə olunmur.
- Major versiya yenilənməsində lisenziya yenidən yoxlanılır.

### 1.3 `.editorconfig`

Kökdə bir `.editorconfig` var (`root = true`). Əsas qaydalar (severity `warning`, yəni `TreatWarningsAsErrors` ilə build xətası):

| Qayda | Dəyər |
|---|---|
| Kodlaşdırma, sətir sonu | `charset = utf-8`, `insert_final_newline = true`, `trim_trailing_whitespace = true`, 4 boşluq |
| Namespace | file-scoped (`csharp_style_namespace_declarations = file_scoped`) |
| `using` | namespace-dən kənarda, `System.*` birinci, lazımsız using xətadır (`IDE0005`) |
| Fiqurlu mötərizə | həmişə (`csharp_prefer_braces = true`) |
| `var` | hər yerdə `var` (`csharp_style_var_* = true`) |
| Access modifier | həmişə açıq yazılır (`dotnet_style_require_accessibility_modifiers = always`) |
| Private sahələr | `_camelCase`; sabitlər və static readonly — `PascalCase` |
| Async metodlar | `Async` sonluğu (naming rule, `IDE1006`) |
| Primary constructor | DI üçün istifadə olunan class-larda üstünlük verilir (`IDE0290`) |
| `sealed` | miras üçün nəzərdə tutulmayan class-lar `sealed`-dır (`CA1852`) |
| Log template | interpolasiya qadağandır (`CA2254` error) |
| `ConfigureAwait` | tələb olunmur (`CA2007 = none`), tətbiq kodunda sync context yoxdur |

Test qovluğu üçün (`[tests/**.cs]`): `CA1707` (adda alt xətt) söndürülür, çünki test adları `Method_State_Expected` formatındadır. `CA1515` (tipləri internal et) test class-ları üçün söndürülür.

Format yoxlaması: `dotnet format --verify-no-changes` CI-da işləyir.

### 1.4 Global using-lər

- SDK-nın implicit using-ləri (`System`, `System.Linq`, `System.Threading.Tasks` və s.) aktivdir.
- **Production proyektlərində əlavə global using yoxdur.** Səbəb: modul proyekti `Domain`, `Application`, `Infrastructure` və `Api` qatlarını eyni assembly-də saxlayır. `global using Microsoft.EntityFrameworkCore;` Domain fayllarına da tətbiq olunardı və qat asılılığını gizlədərdi.
- Test proyektlərində `GlobalUsings.cs` icazəlidir, yalnız test alətləri üçün: `Xunit`, `Shouldly`, `NSubstitute`.

```csharp
// tests/AutoMarket.Listings.UnitTests/GlobalUsings.cs
global using NSubstitute;
global using Shouldly;
global using Xunit;
```

### 1.5 Ümumi C# qaydaları

- Tiplər default olaraq `internal sealed`-dir. Modul proyektində yeganə `public` tip `<M>Module`-dur (ARCHITECTURE §3.3). Contracts proyektindəki tiplər `public`-dir.
- DTO-lar, command/query-lər, event-lər və Contracts tipləri immutable `record`-dur.
- Rəqəmli limitlər, TTL-lər, cron ifadələri kodda sabit yazılmır, `IOptions<T>`-dən oxunur (REQUIREMENTS başlığı, ARCHITECTURE §8.6).
- Pul yalnız `decimal`-dır, `float`/`double` qadağandır (NFR-MISC).
- Kod şərhi **niyə**-ni izah edir, **nə**-ni yox. Tələbə bağlı qaydada tələb identifikatoru yazılır:

```csharp
// FR-LST-03 AC5: şəkil, marka, model və ya VIN dəyişəndə elan yenidən moderasiyaya düşür
if (changes.RequiresModeration)
{
    MoveToPending(now);
}
```

---

## 2. Adlandırma

### 2.1 Ümumi cədvəl

| Element | Qayda | Nümunə |
|---|---|---|
| Namespace | qovluq strukturu ilə eyni | `AutoMarket.Listings.Application.Listings.SubmitListing` |
| Class, record, enum | `PascalCase`, isim | `Listing`, `ListingStatus`, `Money` |
| Interfeys | `I` + `PascalCase` | `IListingRepository`, `ICatalogReader` |
| Metod | `PascalCase`, fel | `Submit`, `Approve`, `GetByIdAsync` |
| Async metod | `Async` sonluğu, son parametr `CancellationToken cancellationToken` | `Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken)` |
| Lokal dəyişən, parametr | `camelCase` | `listingId` |
| Private sahə | `_camelCase` | `_listings` |
| Sabit | `PascalCase` | `MaxImages` |
| Generic parametr | `T` + ad | `TCommand`, `TResult` |
| Bool | `Is/Has/Can` prefiksi | `IsDeleted`, `HasImages` |
| Options class | `<M>Options` və ya `<Sahə>Options`, section adı ilə eyni | `ListingsOptions` → `"Listings"` |

Abreviaturalar `PascalCase` ilə yazılır: `VinNumber` yox, `Vin`; `HttpClient`, `JwtKeyRing`, `FxRate` (iki hərfli `IO` istisnadır).

### 2.2 Use case tipləri

| Tip | Ad | Nümunə |
|---|---|---|
| HTTP input modeli | `<UseCase>Request` | `CreateListingRequest` |
| Validator | `<UseCase>RequestValidator` | `CreateListingRequestValidator` |
| Command | `<UseCase>Command` | `SubmitListingCommand` |
| Query | `<UseCase>Query` | `SearchListingsQuery` |
| Handler | `<UseCase>Handler` | `SubmitListingHandler` |
| Output modeli | `<Məna>Response` | `ListingDetailsResponse`, `ListingSummaryResponse`, `ListingVersionResponse` |
| Endpoint | `<UseCase>Endpoint` | `SubmitListingEndpoint` |
| Xəta sabitləri (Application) | `<Sahə>Errors` | `ListingErrors`, `ImageErrors` |
| Domen xətaları | `<Aggregate>DomainErrors` | `ListingDomainErrors` |
| Port (repository) | `I<Aggregate>Repository` | `IListingRepository` |
| Port (oxuma) | `I<Aggregate>Queries` | `IListingQueries` |
| Unit of work | `I<M>UnitOfWork` | `IListingsUnitOfWork` |

`UseCase` adı **fel + isim** formasındadır: `CreateListing`, `SubmitListing`, `RevealPhone`, `GetListing`, `SearchListings`.

### 2.3 Contracts

- Sinxron interfeyslər: `I<İsim>Reader`, `I<İsim>Directory`, `I<İsim>Provider` (ARCHITECTURE §3.1: `ICatalogReader`, `IUserDirectory`, `IExchangeRateProvider`). Command interfeysi yalnız ADR ilə (`IListingModeration`).
- Contracts DTO-ları mənaya görə adlanır, `Dto`/`Request`/`Response` sonluğu olmur: `UserPublicProfile`, `UserContact`, `ListingBrief`.
- Batch metodlar: `GetManyAsync(IReadOnlyCollection<Guid> ids, ...)`, `GetStatusesAsync(...)`.

### 2.4 Event-lər

| Növ | Qayda | Nümunə |
|---|---|---|
| Integration event (record) | keçmiş zaman, sonluqsuz, `*.Contracts.Events`, `IIntegrationEvent` | `ListingActivated`, `UserBlocked` |
| Yeni versiya | `V<n>` sonluğu, köhnəsi silinmir | `ListingActivatedV2` |
| Routing key | `<modul>.<aggregate>.<hadisə>.v<n>`, kebab-case | `listings.listing.activated.v1`, `identity.auth-email.requested.v1` |
| Domen hadisəsi | keçmiş zaman + `DomainEvent` | `ListingSubmittedDomainEvent` |
| Consumer | `<Event>Consumer` | `UserBlockedConsumer` |
| Job | `<Fel><İsim>Job`, `Name` = ARCHITECTURE §9.2 | `ExpireListingsJob` → `"listings.expire"` |

Domen hadisəsinin `DomainEvent` sonluğu var, çünki modul öz Contracts-ını görür və eyni adlı integration event ilə toqquşma olmamalıdır.

### 2.5 Endpoint-lər və JSON

- Route: `/api/v1/<resurs>` — cəm, kebab-case isim (§6.2).
- Endpoint adı (`WithName`, OpenAPI `operationId`) = use case adı: `CreateListing`, `SubmitListing`.
- Tag (`WithTags`) = resurs qrupu: `Listings`, `Moderation`, `Auth`.
- JSON sahələri və query parametrləri `camelCase`: `pageSize`, `makeIds`, `priceMin`.
- Enum-lar JSON-da string kimi göndərilir. REQUIREMENTS dəyəri müəyyən edibsə, həmin literal istifadə olunur (`price_asc`, `instant`, `FWD`, `AZN`, `INAPPROPRIATE_CONTENT`). Müəyyən etməyibsə, enum üzvünün adı (`Draft`, `Pending`).

```csharp
internal enum SearchSort
{
    [JsonStringEnumMemberName("newest")] Newest,
    [JsonStringEnumMemberName("price_asc")] PriceAsc,
    [JsonStringEnumMemberName("price_desc")] PriceDesc,
}
```

- Xəta kodları `UPPER_SNAKE_CASE` (SEC-ERR-01). Validasiya sahə kodları da `UPPER_SNAKE_CASE`: `REQUIRED`, `OUT_OF_RANGE`, `INVALID_FORMAT`, `TOO_LONG`, `TOO_MANY_ITEMS`.

### 2.6 Verilənlər bazası

| Element | Qayda | Necə təmin olunur |
|---|---|---|
| Schema | modul adı, kiçik hərf | `HasDefaultSchema("listings")` ortaq `ModuleDbContext` bazasında |
| Cədvəl | `snake_case`, cəm | `listing_images` — `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`) |
| Sütun | `snake_case` | `owner_id`, `expires_at` — eyni konvensiya |
| PK / FK / indeks | `pk_<cədvəl>`, `fk_<cədvəl>_<hədəf>_<sütun>`, `ix_<cədvəl>_<sütunlar>` | konvensiya avtomatik yaradır, əl ilə ad verilmir |
| Vaxt sütunu | `_at` sonluğu, `timestamptz` | `published_at`, `deleted_at` |
| Bool sütun | `is_`/`has_` prefiksi | `is_active` |
| Başqa modulun id-si | `<obyekt>_id`, FK yoxdur | `owner_id uuid` (ARCHITECTURE §4.1) |
| Migration tarixçəsi | `<schema>.__ef_migrations_history` | `MigrationsHistoryTable(...)` |

- `ToTable("...")`/`HasColumnName("...")` ilə əl ilə ad yalnız istisna hallarda yazılır (məs. Identity cədvəllərinin `AspNetUsers` → `users` dəyişdirilməsi, ARCHITECTURE §4.2).
- Enum-lar DB-də `text` kimi saxlanılır (`HasConversion<string>()` + `HasMaxLength`). Səbəb: sorğu və indekslərdə oxunaqlıdır (`where status = 'Active'`, ARCHITECTURE §4.5), enum üzvlərinin sırası dəyişəndə data pozulmur.

---

## 3. Modul daxili qovluq strukturu

### 3.1 Seçim: qat + feature

Qatlar (`Domain`, `Application`, `Infrastructure`, `Api`) ARCHITECTURE §2.3 və ArchUnitNET qaydalarına görə sabitdir. Qatın **içində** fayllar feature-ə (biznes sahəsinə) və use case-ə görə qruplaşdırılır.

Əsaslandırma:

- Qat namespace-ləri arxitektura testlərinin əsasıdır (`AutoMarket.<M>.Domain` → EF Core-dan asılı deyil). Tam vertical slice bu qaydaları pozardı.
- Bir use case-in bütün Application faylları (request, validator, command, handler, response) bir qovluqdadır. Dəyişiklik bir yerdə aparılır, review asandır.
- Texniki qovluqlar (`Commands/`, `Handlers/`, `Validators/`) 20+ use case-li modulda bir use case-i 4 qovluğa səpələyir.
- ADR-0010-dakı `Api/Listings/CreateListingEndpoint.cs` nümunəsi ilə uyğundur.

### 3.2 Nümunə ağac (Listings)

```text
src/Modules/Listings/AutoMarket.Listings/
├── ListingsModule.cs                      # yeganə public tip: AddListingsModule, MapListingsEndpoints
├── Domain/
│   ├── Listings/
│   │   ├── Listing.cs                     # aggregate root
│   │   ├── ListingStatus.cs
│   │   ├── ListingImage.cs                # aggregate daxili entity
│   │   ├── ListingStatusChange.cs         # tarixçə (ST-03)
│   │   ├── ListingDomainErrors.cs
│   │   └── Events/
│   │       └── ListingSubmittedDomainEvent.cs
│   └── ValueObjects/
│       ├── Vin.cs
│       ├── Mileage.cs
│       └── EngineVolume.cs
├── Application/
│   ├── Abstractions/
│   │   ├── IListingRepository.cs
│   │   ├── IListingQueries.cs
│   │   ├── IListingsUnitOfWork.cs
│   │   └── IImageProcessor.cs
│   ├── Listings/
│   │   ├── ListingErrors.cs
│   │   ├── CreateListing/
│   │   │   ├── CreateListingRequest.cs
│   │   │   ├── CreateListingRequestValidator.cs
│   │   │   ├── CreateListingCommand.cs
│   │   │   └── CreateListingHandler.cs
│   │   ├── SubmitListing/
│   │   └── GetListing/
│   │       ├── GetListingQuery.cs
│   │       ├── GetListingHandler.cs
│   │       └── ListingDetailsResponse.cs
│   ├── Images/
│   │   ├── ImageErrors.cs
│   │   └── UploadImages/
│   └── ListingsOptions.cs
├── Infrastructure/
│   ├── Persistence/
│   │   ├── ListingsDbContext.cs
│   │   ├── ListingsDbContextFactory.cs    # IDesignTimeDbContextFactory
│   │   ├── Configurations/
│   │   │   └── ListingConfiguration.cs
│   │   ├── Repositories/
│   │   │   └── ListingRepository.cs
│   │   ├── Queries/
│   │   │   └── ListingQueries.cs          # AsNoTracking + projection
│   │   └── Migrations/
│   ├── Contracts/                         # Listings.Contracts interfeyslərinin implementasiyası
│   │   ├── ListingReader.cs
│   │   └── ListingModeration.cs
│   ├── Events/
│   │   └── ListingIntegrationEventMapper.cs   # domen hadisəsi → integration event
│   ├── Consumers/
│   │   └── UserBlockedConsumer.cs
│   ├── Jobs/
│   │   └── ExpireListingsJob.cs
│   └── Images/
│       └── NetVipsImageProcessor.cs
└── Api/
    ├── Listings/
    │   ├── ListingsEndpoints.cs           # route qrupu: /listings
    │   ├── CreateListingEndpoint.cs
    │   └── SubmitListingEndpoint.cs
    └── Images/
        └── UploadImagesEndpoint.cs
```

```text
src/Modules/Listings/AutoMarket.Listings.Contracts/
├── IListingReader.cs
├── IListingModeration.cs
├── ListingBrief.cs
└── Events/
    ├── ListingActivated.cs
    └── ListingSubmitted.cs
```

Qaydalar:

- Feature qovluğu Domain-də aggregate adı ilə (`Domain/Listings/`), Application və Api-də eyni adla (`Application/Listings/`, `Api/Listings/`) adlanır.
- Bir use case-in Application faylları `Application/<Feature>/<UseCase>/` qovluğundadır. Bir neçə use case-in istifadə etdiyi response `Application/<Feature>/`-dadır.
- Port interfeysləri `Application/Abstractions/`-dadır, implementasiyaları `Infrastructure/`-dadır.
- Bir fayl — bir əsas tip. Kiçik köməkçi tip (məs. command-in daxili record-u) eyni faylda ola bilər.

---

## 4. Domain

### 4.1 Entity və aggregate

- Aggregate root `AggregateRoot` bazasından (BuildingBlocks.Domain), daxili entity `Entity` bazasından törəyir.
- Vəziyyət yalnız metodlarla dəyişir: property-lər `{ get; private set; }`, kolleksiyalar `IReadOnlyCollection<T>` kimi açılır, daxildə `List<T>`.
- Yaratma statik factory metodu ilə aparılır. EF üçün `private` parametrsiz konstruktor olur.
- Domen kodu `TimeProvider`, DI və ya I/O görmür: cari vaxt və xarici yoxlamaların nəticəsi parametr kimi verilir.
- Başqa aggregate-ə (və başqa modulun obyektinə) yalnız id ilə istinad edilir: `Guid OwnerId`, `int MakeId`. Naviqasiya property-si yalnız aggregate daxilindədir.
- Id-lər `Guid` (UUIDv7), BuildingBlocks-dakı id generator ilə yaradılır. Strongly-typed id istifadə olunmur.
- Entity-lərdə EF və ya serializasiya atributu yoxdur (konfiqurasiya Infrastructure-dadır, ADR-0002).

```csharp
internal sealed class Listing : AggregateRoot
{
    private readonly List<ListingImage> _images = [];

    private Listing() { } // EF

    public Guid OwnerId { get; private set; }
    public ListingStatus Status { get; private set; }
    public IReadOnlyCollection<ListingImage> Images => _images;

    public static Listing CreateDraft(Guid id, Guid ownerId, ListingDetails details, DateTimeOffset now)
    {
        var listing = new Listing { Id = id, OwnerId = ownerId, Status = ListingStatus.Draft, CreatedAt = now };
        listing.ApplyDetails(details);
        return listing;
    }

    public void Submit(DateTimeOffset now)
    {
        // ST-01: yalnız Draft və Rejected-dən
        if (Status is not (ListingStatus.Draft or ListingStatus.Rejected))
        {
            throw new DomainException(ListingDomainErrors.InvalidStatusTransition);
        }

        EnsureCompleteForSubmission(); // FR-LST-02 AC3
        ChangeStatus(ListingStatus.Pending, now, reason: null);
        Raise(new ListingSubmittedDomainEvent(Id, OwnerId, now));
    }
}
```

### 4.2 Aggregate qaydaları

- Bir command bir aggregate-i dəyişir. Eyni modul daxilində başqa aggregate-ə təsir domen hadisəsi ilə, eyni transaksiyada aparılır (ADR-0004 §3).
- Status maşını bir yerdədir: keçid metodları (`Submit`, `Withdraw`, `Approve`, `Reject`, `Expire`, `Renew`) aggregate-dədir, hər keçid tarixçə qeydi (ST-03) və domen hadisəsi yaradır. REQUIREMENTS 3.3.2-də olmayan keçid `INVALID_STATUS_TRANSITION` verir.
- Aggregate-dən kənar data tələb edən qaydalar (Draft ≤ 20, Pending + Active ≤ 5, soraqçanın aktivliyi) handler-də yoxlanılır (§5.3).
- Draft natamam ola bilər (FR-LST-01 AC2), ona görə Draft-da məcburi sahələr nullable-dır. Tamlıq `Submit` zamanı yoxlanılır.

### 4.3 Value object

- `sealed record` (və ya kiçik olanda `readonly record struct`), immutable, bərabərlik dəyərə görə.
- Yalnız etibarlı dəyər yaradıla bilər: statik `Create` factory invariantı yoxlayır.
- Validator formatı artıq yoxlayıb, value object isə ikinci qoruma qatıdır.

```csharp
internal sealed record Vin
{
    private Vin(string value) => Value = value;

    public string Value { get; }

    public static Vin Create(string value)
    {
        // 17 simvol, I/O/Q olmadan (REQUIREMENTS 3.3.1)
        if (!VinFormat.IsValid(value))
        {
            throw new DomainException(ListingDomainErrors.InvalidVin);
        }

        return new Vin(value);
    }
}
```

`Money` BuildingBlocks.Domain-dədir: `decimal Amount` + `Currency`, yuvarlaqlaşdırma qaydası bir yerdə (FR-FX-02 AC3).

### 4.4 Domen xətaları

- Domen invariantının pozulması `DomainException(DomainError)` ilə ifadə olunur (ARCHITECTURE §8.1). Exception handler onu ProblemDetails-ə və koda çevirir.
- `DomainError` HTTP-dən asılı deyil: `Code`, `Message` (ingiliscə) və növ (`Conflict` — cari vəziyyətlə ziddiyyət, 409; `Validation` — yanlış dəyər, 400). Növ → HTTP status xəritəsi BuildingBlocks.Web-dədir.
- Domen xətaları `Domain/<Aggregate>/<Aggregate>DomainErrors.cs`-də statik sahələrdir, kodlar REQUIREMENTS 4.8 siyahısındandır.

```csharp
internal static class ListingDomainErrors
{
    public static readonly DomainError InvalidStatusTransition =
        DomainError.Conflict("INVALID_STATUS_TRANSITION", "The listing status transition is not allowed.");

    public static readonly DomainError ImageMinRequired =
        DomainError.Conflict("IMAGE_MIN_REQUIRED", "A submitted listing must have at least one image.");
}
```

---

## 5. Application

### 5.1 Handler forması (MediatR olmadan)

- Handler `ICommandHandler<TCommand, TResult>` və ya `IQueryHandler<TQuery, TResult>` implementasiya edir (BuildingBlocks.Application), `internal sealed`-dir və DI-da `Scoped` qeydiyyatdan keçir.
- Bir handler — bir use case. Handler başqa handler-i çağırmır. Ortaq məntiq domen metoduna və ya ayrıca `internal` servisə çıxarılır.
- Handler nəqliyyatdan asılı deyil: `HttpContext`, `IResult`, status kodu, cookie görmür (ARCHITECTURE §2.3). Eyni handler endpoint-dən, consumer-dən və ya job-dan çağırıla bilər.
- Cari istifadəçinin id-si command-a **Api qatında** `ICurrentUser`-dən yazılır. Request modelində istifadəçi id-si olmur (SEC-AUTHZ-02, SEC-INP-03).
- Yazma: aggregate repository ilə yüklənir, domen metodu çağırılır, `I<M>UnitOfWork.SaveChangesAsync` bir dəfə çağırılır. Outbox və audit sətirləri eyni `SaveChanges`-də yazılır (§7.4).

```csharp
internal sealed record SubmitListingCommand(Guid ListingId, Guid UserId, uint Version);

internal sealed class SubmitListingHandler(
    IListingRepository listings,
    IListingsUnitOfWork unitOfWork,
    IOptions<ListingsOptions> options,
    TimeProvider time) : ICommandHandler<SubmitListingCommand, ListingVersionResponse>
{
    public async Task<Result<ListingVersionResponse>> HandleAsync(
        SubmitListingCommand command, CancellationToken cancellationToken)
    {
        // SEC-AUTHZ-02: sahiblik sorğunun özündədir, başqasının elanı = NotFound
        var listing = await listings.GetOwnedAsync(command.ListingId, command.UserId, cancellationToken);
        if (listing is null)
        {
            return ListingErrors.NotFound;
        }

        if (listing.Version != command.Version)
        {
            return CommonErrors.ConcurrencyConflict;
        }

        // FR-LST-02 AC2: limit aggregate-dən kənar datadır, handler-də yoxlanılır
        var activeCount = await listings.CountPendingOrActiveAsync(command.UserId, cancellationToken);
        if (activeCount >= options.Value.Limits.MaxActive)
        {
            return ListingErrors.ActiveLimitReached;
        }

        listing.Submit(time.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ListingVersionResponse(listing.Id, listing.Status, listing.Version);
    }
}
```

### 5.2 Portlar

| Port | Məqsəd | Qayda |
|---|---|---|
| `I<Aggregate>Repository` | yazma üçün aggregate-i yükləmək və əlavə etmək | tracking, tam aggregate, sahiblik şərtli metodlar (`GetOwnedAsync`) |
| `I<Aggregate>Queries` | oxuma use case-ləri | `AsNoTracking` + projection, birbaşa `*Response` qaytarır |
| `I<M>UnitOfWork` | `SaveChangesAsync` | modulun `DbContext`-i implementasiya edir. Hər modulun öz interfeysi var (bir DI konteynerində toqquşmasın deyə) |
| Xarici servislər | `IImageProcessor`, `ICbarClient`, `IEmailTransport` | Infrastructure-da implementasiya, testdə fake |

Repository generic deyil (`IRepository<T>` yoxdur), yalnız use case-lərin real ehtiyacı olan metodları ehtiva edir.

### 5.3 Result və exception

| Hal | Mexanizm | Nümunə |
|---|---|---|
| Gözlənilən biznes nəticəsi (tapılmadı, sahib deyil, limit, vəziyyət uyğun deyil, köhnə versiya) | `Result<T>` + `Error` | `ListingErrors.NotFound`, `ListingErrors.ActiveLimitReached` |
| Input formatı/diapazonu | validator → `400 VALIDATION_FAILED` (handler-ə çatmır) | `pageSize > 50` |
| Domen invariantının pozulması | `DomainException(DomainError)` | icazəsiz status keçidi |
| DB concurrency konflikti `SaveChanges` zamanı | `DbUpdateConcurrencyException` → global handler → `409 CONCURRENCY_CONFLICT` | iki moderator eyni anda (ST-02) |
| Gözlənilməz xəta, infrastruktur xətası | exception, tutulmur → `500 INTERNAL_ERROR` | DB əlçatmazdır |
| Ləğv | `OperationCanceledException`, tutulmur | client bağlantını kəsdi |

Qaydalar:

- Gözlənilən nəticə üçün exception atılmır. Exception `Result`-a çevirmək üçün tutulmur (yuxarıdakı cədvəldən kənar).
- `try/catch` yalnız konkret exception tipi üçün və konkret reaksiya ilə yazılır (məs. xarici servis xətasında fallback, FR-FX-01 AC3). Boş `catch` və `catch (Exception)` + davam qadağandır (BackgroundService-in dövr sərhədi istisnadır: log + növbəti dövr).
- Endpoint `Result`-u BuildingBlocks.Web-dəki `ToProblem()` helper-i ilə ProblemDetails-ə çevirir (§6.4).

### 5.4 Xəta kodlarının mərkəzləşdirilməsi

- Ümumi kodlar `BuildingBlocks.Application.ErrorCodes`-dadır (`VALIDATION_FAILED`, `UNAUTHORIZED`, `FORBIDDEN`, `CONCURRENCY_CONFLICT`, `RATE_LIMITED`, `INTERNAL_ERROR`, `PAYLOAD_TOO_LARGE`, `UNSUPPORTED_MEDIA_TYPE`) və `CommonErrors`-da `Error` kimi.
- Modul kodları `Application/<Feature>/<Feature>Errors.cs`-dədir: `Error(code, message, httpStatus)` (ARCHITECTURE §8.1).
- Hər kod kodda **bir dəfə** təyin olunur. Başqa yerdə string literal kimi təkrar yazılmır. Arxitektura testi formatı və unikallığı yoxlayır.
- Kod yalnız REQUIREMENTS 4.8 siyahısından götürülür. Yeni kod lazım olarsa, əvvəl təsdiq alınır və REQUIREMENTS 4.8 yenilənir (SEC-ERR-04). Mövcud kodun mənası dəyişdirilmir.
- `message` ingiliscədir, qısadır, daxili detal (id, SQL, exception mətni) ehtiva etmir (SEC-ERR-02).

```csharp
internal static class ListingErrors
{
    public static readonly Error NotFound =
        new("LISTING_NOT_FOUND", "Listing not found.", StatusCodes.Status404NotFound);

    public static readonly Error ActiveLimitReached =
        new("ACTIVE_LISTING_LIMIT_REACHED", "Active listing limit reached.", StatusCodes.Status409Conflict);
}
```

### 5.5 Validasiya

ARCHITECTURE §8.2-dəki üç səviyyə tətbiq olunur:

1. **Format və diapazon** — FluentValidation validator-u `Request` üçün (`internal sealed`, use case qovluğunda). Side-effect yoxdur, DB və Contracts çağırılmır. Hər qaydanın sabit sahə kodu var.
2. **İstinad və vəziyyət** (soraqça id-si aktivdirmi, limit, status) — handler-də.
3. **İnvariant** — domendə.

```csharp
internal sealed class CreateListingRequestValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingRequestValidator(TimeProvider time)
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1950, time.GetUtcNow().Year + 1)
            .WithErrorCode(ValidationCodes.OutOfRange);

        RuleFor(x => x.Description)
            .MaximumLength(3000)
            .WithErrorCode(ValidationCodes.TooLong);
    }
}
```

### 5.6 Mapping

Mapping əl ilə yazılır (ADR-0010). Harada:

| Çevirmə | Harada | Necə |
|---|---|---|
| `Request` + route + cari istifadəçi → `Command` | Api endpoint | `new SubmitListingCommand(id, currentUser.Id, request.Version)` və ya Request faylında `ToCommand(...)` |
| Entity → `Response` (oxuma) | Infrastructure `*Queries` | EF projection: `Select(l => new ListingDetailsResponse(...))` |
| Aggregate → `Response` (yazmadan sonra) | Application, response faylında | statik `ListingVersionResponse.From(listing)` |
| Domen hadisəsi → integration event | Infrastructure `*IntegrationEventMapper` | outbox interceptor-u çağırır |
| Contracts DTO → API response | Application | Contracts tipi API-də birbaşa qaytarılmır |

- Entity heç vaxt serializasiya olunmur və endpoint-dən qaytarılmır (SEC-AUTHZ-04).
- Response-a yeni sahə əlavə etmək ayrıca qərardır: sahənin kimə görünə biləcəyi REQUIREMENTS 4.11 ilə yoxlanılır.

---

## 6. API

### 6.1 Endpoint qrupları və endpoint forması

- Modul `<M>Module.Map<M>Endpoints(IEndpointRouteBuilder)`-da versiyalı route qrupunu qurur. Hər feature-in qrup faylı (`ListingsEndpoints.cs`) prefiksi, tag-ı və qrup səviyyəli metadata-nı təyin edir, sonra endpoint-ləri map edir.
- Hər endpoint ayrıca faylda `internal static class`-dır, bir `Map...` extension metodu və bir `HandleAsync` metodu var. Lambda ilə inline endpoint yazılmır.
- Endpoint-də biznes məntiqi yoxdur: request → command, handler çağırışı, `Result` → HTTP cavabı.
- Hər endpoint-də açıq yazılır: authorization (policy və ya `AllowAnonymous`), rate limit policy (lazımdırsa), validation filter, `Produces`/`ProducesProblem`, `WithName`.

```csharp
internal static class SubmitListingEndpoint
{
    public static RouteGroupBuilder MapSubmitListing(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/submit", HandleAsync)
            .WithName("SubmitListing")
            .RequireAuthorization(Policies.User)
            .RequireRateLimiting(RateLimitPolicies.ListingWrite)
            .AddEndpointFilter<ValidationFilter<SubmitListingRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<Results<Ok<ListingVersionResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        SubmitListingRequest request,
        ICurrentUser currentUser,
        ICommandHandler<SubmitListingCommand, ListingVersionResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitListingCommand(id, currentUser.Id, request.Version);
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}
```

### 6.2 Route qaydaları

| Qayda | Nümunə |
|---|---|
| Prefiks `/api/v{version}`, ilk versiya `v1` (NFR-VER) | `/api/v1/listings` |
| Resurs — cəm, kebab-case isim | `/listings`, `/saved-searches`, `/threads/{id}/messages` |
| Route id — tipli constraint | `{id:guid}` |
| Status keçidi və yan təsirli əməliyyat — alt resurs feli, `POST` | `POST /listings/{id}/submit`, `/withdraw`, `/renew`, `POST /listings/{id}/phone` |
| Cari istifadəçinin profili və onun public resurs üzrə görünüşü — `/me` | `GET /me`, `GET /me/listings` |
| Yalnız sahibinə aid kolleksiyalar — birbaşa, sahib tokendən | `/favorites`, `/saved-searches`, `/notifications`, `/threads` |
| Moderasiya və admin — rol prefiksi | `/moderation/queue`, `/moderation/listings/{id}/approve`, `/admin/users/{id}/block` |
| İstifadəçi id-si route-da öz datası üçün olmur | `/users/{userId}/favorites` — **qadağan** |
| Auth | `/auth/register`, `/auth/login`, `/auth/refresh` (cookie path `/api/v1/auth`) |

HTTP metodları: `GET` oxuma (yan təsirsiz), `POST` yaratma və əməliyyat, `PUT` resursun redaktə olunan sahələrinin tam dəstini əvəz etmək (məs. elan redaktəsi), `PATCH` yalnız açıq nullable sahələrlə kiçik qismən dəyişiklik (məs. bildiriş ayarları; JSON Patch istifadə olunmur), `DELETE` silmə.

### 6.3 HTTP status kodları

| Status | Nə vaxt |
|---|---|
| `200 OK` | oxuma; body qaytaran yeniləmə və əməliyyat (`submit` → `{ id, status, version }`) |
| `201 Created` | yaratma: `Location` header + `{ id, version }` |
| `202 Accepted` | enumeration qorunan auth axınları: register, resend-confirmation, forgot-password (SEC-AUTH-08) |
| `204 No Content` | body-siz uğur: `DELETE`, confirm-email, idempotent seçilmiş əlavə/çıxarma |
| `400` | `VALIDATION_FAILED` (+ `errors`), `Validation` növlü domen xətası, `TOKEN_INVALID_OR_EXPIRED` |
| `401` | `UNAUTHORIZED` (o cümlədən etibarsız və ya vaxtı keçmiş refresh cookie), `INVALID_CREDENTIALS`, `ACCOUNT_BLOCKED` (status middleware), `REFRESH_TOKEN_REUSED` |
| `403` | `FORBIDDEN` — rol çatmır, `Origin` yoxlaması; `EMAIL_NOT_CONFIRMED`, `ACCOUNT_LOCKED_OUT` (şifrə düzgündür, amma giriş qadağandır) |
| `404` | `*_NOT_FOUND` — yoxdur **və ya** istifadəçiyə aid deyil (SEC-AUTHZ-02), `PHONE_NOT_AVAILABLE` |
| `409` | `CONCURRENCY_CONFLICT`, `INVALID_STATUS_TRANSITION`, limitlər (`*_LIMIT_REACHED`), `ALREADY_REPORTED`, `LISTING_UNDER_REVIEW` |
| `413` / `415` | `PAYLOAD_TOO_LARGE` / `UNSUPPORTED_MEDIA_TYPE` |
| `429` | `RATE_LIMITED` + `Retry-After` |
| `500` | `INTERNAL_ERROR` + `traceId`, detal yoxdur |

Hər kodun statusu `Error` təyinində sabitdir. Eyni kod müxtəlif endpoint-lərdə müxtəlif status ilə qaytarılmır.

### 6.4 ProblemDetails

Bütün xətalar RFC 9457 formatındadır (SEC-ERR-01, ARCHITECTURE §8.1). Endpoint ProblemDetails-i əl ilə qurmur, yalnız `result.ToProblem()` istifadə edir.

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "code": "ACTIVE_LISTING_LIMIT_REACHED",
  "message": "Active listing limit reached.",
  "traceId": "0af7651916cd43dd8448eb211c80319c"
}
```

Validasiya xətası:

```json
{
  "status": 400,
  "code": "VALIDATION_FAILED",
  "message": "One or more fields are invalid.",
  "traceId": "...",
  "errors": {
    "year": [{ "code": "OUT_OF_RANGE", "message": "Year must be between 1950 and 2027." }]
  }
}
```

### 6.5 Pagination

Format bütün siyahı endpoint-lərində eynidir (NFR-PAG): `PagedResponse<T>` (BuildingBlocks.Web). Bütün sahələr həmişə qaytarılır, istifadə olunmayan rejimin sahələri `null`-dur.

| Rejim | Harada | Request | Response |
|---|---|---|---|
| Offset | axtarış, admin siyahıları, seçilmişlər, moderasiya növbəsi | `?page=1&pageSize=20` | `{ items, page, pageSize, hasNext, total, nextCursor: null }` |
| Cursor | mesajlar, bildirişlər, thread-lər (NFR-PAG SHOULD) | `?cursor=...&pageSize=20` | `{ items, page: null, pageSize, hasNext, total: null, nextCursor }` |

- `page` 1-dən başlayır. `pageSize` default 20, maksimum 50, aşan dəyər `VALIDATION_FAILED` (səssiz kəsmə yoxdur). `page × pageSize ≤ 10 000`.
- Sıralama deterministikdir: hər `ORDER BY`-ın sonunda `id` var (FR-SRCH-02 AC2).
- Cursor opaque-dir (sıralama açarı + id, base64url). Client onu təhlil etmir. Yanlış və ya dəyişdirilmiş cursor `VALIDATION_FAILED` verir.
- Sıralama və filtr dəyərləri enum allow-list-dən gəlir (SEC-INP-04).

### 6.6 Optimistic concurrency (`version`)

- Concurrency-yə həssas resursun (elan, soraqça dəyəri və s.) response-unda `version` (`uint`, PostgreSQL `xmin`) qaytarılır.
- Yazma request-i body-də `version` göndərir: `PUT /listings/{id}` → `{ ..., "version": 4182 }`, `POST /listings/{id}/submit` → `{ "version": 4182 }`, `POST /moderation/listings/{id}/approve` → `{ "version": 4182 }`.
- Handler yüklənmiş versiyanı müqayisə edir. `SaveChanges`-də isə xmin yoxlanılır. Hər iki halda nəticə `409 CONCURRENCY_CONFLICT`-dir (ST-02, FR-LST-03 AC6).
- `ETag`/`If-Match` istifadə olunmur.

### 6.7 Validation filter və content type

- Body qəbul edən hər endpoint-də `ValidationFilter<TRequest>` var. Request-in validator-u yoxdursa, bu, review xətasıdır.
- JSON endpoint-ləri yalnız `application/json`, şəkil endpoint-i yalnız `multipart/form-data` qəbul edir (`Accepts` metadata + filter, SEC-INP-07).
- Request modelində sistem sahələri (`Id`, `OwnerId`, `UserId`, `Status`, `Role`, `CreatedAt`, `PriceAzn`) yoxdur (SEC-INP-03, arxitektura testi).

### 6.8 Authorization endpoint-də

- `FallbackPolicy = RequireAuthenticatedUser` — policy yazılmayan endpoint anonim deyil (SEC-AUTHZ-01).
- Policy adları sabitlərdən gəlir: `Policies.User`, `Policies.Moderator`, `Policies.Admin`. String literal yazılmır.
- Qrup səviyyəsində ümumi policy, endpoint səviyyəsində fərqlilik:

```csharp
// ListingsEndpoints.cs
var group = routes.MapGroup("/listings").WithTags("Listings").RequireAuthorization(Policies.User);
group.MapCreateListing();   // qrupun policy-si: User
group.MapGetListing();      // endpoint daxilində .AllowAnonymous() — public

// GetListingEndpoint.cs
group.MapGet("/{id:guid}", HandleAsync)
    .WithName("GetListing")
    .AllowAnonymous();
```

- Sahiblik (BOLA) endpoint-də deyil, handler/repository sorğusunda yoxlanılır (§5.1).
- Şəxsi data qaytaran endpoint-lərdə `.NoStore()` (SEC-NET-02).
- Yeni endpoint `AuthorizationMatrix`-ə əlavə olunmalıdır, əks halda əhatə testi CI-da uğursuz olur (ARCHITECTURE §10.3).

### 6.9 JSON formatları

| Tip | Format | Nümunə |
|---|---|---|
| Id | UUID string | `"0192f3a4-..."` |
| Vaxt | ISO 8601 UTC, `DateTimeOffset` | `"2026-10-05T09:00:00Z"` |
| Tarix (vaxtsız) | `DateOnly` | `"2026-10-05"` (məzənnə tarixi) |
| Pul | `decimal` JSON number + valyuta | `{ "amount": 12500.00, "currency": "USD" }`, `"priceAzn": 21250.00`, `"rateDate": "2026-10-05"` |
| Optional sahə | sahə həmişə qaytarılır, dəyər `null` ola bilər | `"vin": null` |

---

## 7. EF Core

### 7.1 Konfiqurasiya

- Hər entity üçün `Infrastructure/Persistence/Configurations/`-da `internal sealed class <Entity>Configuration : IEntityTypeConfiguration<T>`. `OnModelCreating`-də yalnız `ApplyConfigurationsFromAssembly` (öz DbContext-inin namespace-i ilə filtrlənir) və baza konvensiyaları.
- Data annotation istifadə olunmur.
- Hər `string` sütununun `HasMaxLength`-i var. Hər `decimal` sütununun dəqiqliyi açıq yazılır: pul `(14, 2)`, məzənnə `(12, 6)` (ARCHITECTURE §4.3).
- Value object-lər `ComplexProperty` və ya `OwnsOne` ilə, private kolleksiyalar `Navigation(...).UsePropertyAccessMode(PropertyAccessMode.Field)` ilə map olunur.
- Concurrency: `xmin` token (ARCHITECTURE §4.3). Npgsql-in cari versiyasında tövsiyə olunan forma `uint Version` property + `IsRowVersion()`-dur.
- Soft delete: `deleted_at` + global query filter. Filtrdən yan keçmək (`IgnoreQueryFilters`) yalnız şərhlə və konkret səbəblə (məs. silinmiş elanın şəkillərinin təmizlənməsi).
- Lazy loading proxy-ləri istifadə olunmur.

```csharp
internal sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Version).IsRowVersion();
        builder.Property(x => x.Description).HasMaxLength(3000);
        builder.ComplexProperty(x => x.Price, price =>
        {
            price.Property(p => p.Amount).HasPrecision(14, 2);
            price.Property(p => p.Currency).HasConversion<string>().HasMaxLength(3);
        });
        builder.HasQueryFilter(x => x.DeletedAt == null);
        builder.HasIndex(x => new { x.OwnerId, x.Status }).HasFilter("deleted_at is null");
    }
}
```

### 7.2 Migration-lar

- Hər modulun migration-ları öz `Infrastructure/Persistence/Migrations` qovluğundadır (ARCHITECTURE §4.4).
- Ad: `PascalCase`, fel ilə başlayır, nəyi dəyişdiyini deyir. Modulun ilk migration-u `InitialCreate`-dir. Tarix prefiksini EF özü əlavə edir.

| Düzgün | Yanlış |
|---|---|
| `InitialCreate`, `AddListingVin`, `AddPhoneRevealsTable`, `CreateSearchSortIndexes`, `DropListingLegacyPrice` | `Migration1`, `Fix`, `Update`, `Changes` |

- `main`-ə daxil olmuş migration redaktə və ya silinmir, yeni migration yazılır.
- Hər migration-un SQL-i review-dan əvvəl yoxlanılır (`dotnet ef migrations script`).
- Geriyə uyğunluq: expand → migrate → contract (ARCHITECTURE §4.4). Sütunun silinməsi və ya adının dəyişməsi kod artıq ondan istifadə etmədikdən sonra ayrıca release-də aparılır.
- EF-in ifadə edə bilmədiyi şeylər (trigger, collation, partial indeks şərti) `migrationBuilder.Sql(...)` ilə, yalnız modulun öz schema-sında.

### 7.3 Sorğu qaydaları

- Oxuma sorğuları `AsNoTracking()` + projection (`Select`) ilə yalnız lazımi sütunları seçir. Oxuma üçün `Include` istifadə olunmur.
- Yazma üçün aggregate tam yüklənir (tracking). Bir neçə kolleksiyalı aggregate üçün `AsSplitQuery()`.
- **N+1 qadağandır:** dövr daxilində sorğu və ya Contracts çağırışı yoxdur. Toplu yükləmə `Contains` ilə və ya Contracts-ın batch metodları (`GetStatusesAsync(userIds)`) ilə aparılır.

```csharp
// Yanlış: hər match üçün ayrıca çağırış
foreach (var match in matches)
{
    var status = await users.GetStatusAsync(match.UserId, cancellationToken);
}

// Düzgün: bir batch çağırışı
var statuses = await users.GetStatusesAsync(matches.Select(m => m.UserId).ToHashSet(), cancellationToken);
```

- Siyahı sorğuları həmişə səhifələnir və deterministik `ORDER BY ..., id` ilə qurulur.
- Raw SQL yalnız `FromSql`/`SqlQuery`/`ExecuteSql` interpolasiyası ilə (parametrləşdirilir). `FromSqlRaw`/`ExecuteSqlRaw` qadağandır (SEC-INP-04). İstifadəçi inputu sütun adı, sıralama və ya cədvəl adı kimi istifadə olunmur.
- Cross-schema `JOIN` və başqa modulun cədvəlinə müraciət qadağandır (ARCHITECTURE §4.1).
- `ExecuteUpdateAsync`/`ExecuteDeleteAsync` interceptor-ları, concurrency yoxlamasını və outbox-u keçir. Yalnız event tələb etməyən toplu əməliyyatlarda istifadə olunur (məs. `search.fx-reprice`, təmizləmə job-ları).
- Say yoxlamaları (`CountAsync`) indekslə dəstəklənən filtrlərlə yazılır (ARCHITECTURE §4.5).

### 7.4 Transaksiya və outbox

- **Bir command = bir `SaveChangesAsync` = bir transaksiya**, bir modulun DbContext-ində (ARCHITECTURE §4.2).
- Integration event-lər handler-dən birbaşa göndərilmir. Aggregate domen hadisəsi yaradır, `SaveChanges` interceptor-u onu outbox sətrinə çevirir. RabbitMQ-ya birbaşa publish qadağandır (ADR-0004).
- Audit qeydi `IAuditLog.Record(...)` ilə `SaveChanges`-dən **əvvəl** əlavə olunur ki, eyni transaksiyaya düşsün.
- İki modulun DbContext-i bir transaksiyada istifadə olunmur. Başqa modula təsir yalnız event iləndir (sinxron istisna: `IListingModeration`, ADR-0004).
- Açıq transaksiya (`BeginTransactionAsync`) yalnız bir neçə `SaveChanges` zəruri olduqda. Consumer-in inbox + handler transaksiyası BuildingBlocks-dadır, consumer kodu onu təkrar açmır.
- Consumer idempotentdir: inbox + mümkün olan yerdə təbii unikal açar (`INSERT ... ON CONFLICT DO NOTHING`) və ya versiya müqayisəsi (ARCHITECTURE §5.3).

---

## 8. Async və resurslar

### 8.1 CancellationToken

- Hər async metodun son parametri `CancellationToken cancellationToken`-dir (default dəyərsiz, `= default` yalnız public API-də lazım olarsa).
- Token zəncir boyu ötürülür: endpoint → handler → repository → EF/HttpClient. Minimal API token-i avtomatik bağlayır (`RequestAborted`).
- `BackgroundService`-də `stoppingToken` istifadə olunur.
- `CancellationToken.None` yalnız əməliyyatın yarımçıq qalmaması vacib olduqda (məs. commit-dən sonra kompensasiya) və şərhlə.
- `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `async void` qadağandır. Fire-and-forget (`_ = Task.Run(...)`) qadağandır; asinxron iş outbox, queue və ya `BackgroundService` ilə aparılır.
- `ValueTask` yalnız ölçülmüş performans səbəbi ilə.

### 8.2 TimeProvider

- Vaxt yalnız inject olunmuş `TimeProvider`-dən: `time.GetUtcNow()`. `DateTime.Now/UtcNow`, `DateTimeOffset.UtcNow` qadağandır (ARCHITECTURE §8.9).
- Gözləmə: `Task.Delay(delay, time, cancellationToken)`, timer: `time.CreateTimer(...)`.
- Domen metodları vaxtı parametr kimi alır (`Submit(DateTimeOffset now)`).
- Bakı vaxtı yalnız cədvəl hesablamasında: `TimeZoneInfo.FindSystemTimeZoneById("Asia/Baku")`. Saxlanılan bütün vaxtlar UTC-dir.
- Testdə `FakeTimeProvider`.

### 8.3 IDisposable və resurslar

- `IDisposable`/`IAsyncDisposable` obyektlər `using var` / `await using var` ilə. Asinxron dispose mövcuddursa, ona üstünlük verilir.
- `HttpClient` yalnız `IHttpClientFactory` typed client ilə (`CbarClient`). `new HttpClient()` qadağandır.
- `DbContext` DI-dan scoped alınır. `BackgroundService`, consumer və job-da hər iş vahidi üçün yeni scope: `await using var scope = scopeFactory.CreateAsyncScope();`.
- Singleton scoped servisi saxlamır (captive dependency). DI `ValidateScopes` və `ValidateOnBuild` Development-də aktivdir.
- Şəkil emalı: NetVips `Image` obyektləri dispose olunur, müvəqqəti fayllar `finally`-də silinir, stream-lər dispose olunur.
- `CancellationTokenSource` (xüsusən `CreateLinkedTokenSource`), `SemaphoreSlim`, `Timer` dispose olunur.

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var handler = scope.ServiceProvider.GetRequiredService<ExpireListingsHandler>();
using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
timeout.CancelAfter(options.Value.Timeout);
await handler.HandleAsync(timeout.Token);
```

---

## 9. Logging

### 9.1 Message template

- Kodda yalnız `ILogger<T>` (Serilog yalnız Host-da, ADR-0013).
- Mesaj template-i sabitdir, ingiliscədir, property adları `PascalCase`-dir. String interpolasiya və birləşdirmə qadağandır (`CA2254`).
- Tez-tez yazılan log-lar `LoggerMessage` source generator ilə.
- Exception birinci arqument kimi ötürülür, mesajın içinə yazılmır.

```csharp
// Düzgün
logger.LogInformation("Listing {ListingId} submitted by {UserId}", listing.Id, command.UserId);

// Yanlış
logger.LogInformation($"Listing {listing.Id} submitted");

// Hot path
internal static partial class ListingsLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Exchange rate fetch failed, using rate from {RateDate}")]
    public static partial void FxFetchFailedUsingFallback(this ILogger logger, Exception exception, DateOnly rateDate);
}
```

### 9.2 Səviyyələr

| Səviyyə | Nə vaxt | Nümunə |
|---|---|---|
| `Trace` / `Debug` | yalnız diaqnostika, production-da söndürülüb | sorğu parametrlərinin detalı |
| `Information` | biznes baxımından əhəmiyyətli hadisə, əməliyyat başına ən çox bir | elan təsdiqləndi, job bitdi (`ItemsProcessed`) |
| `Warning` | bərpa olunan problem, sistem işləməyə davam edir | FX fallback (FR-FX-01 AC3), Redis fallback, consumer retry |
| `Error` | əməliyyat uğursuz oldu, müdaxilə lazım ola bilər | DLQ mesajı, job `Failed`, məzənnə 3 gündən köhnə (FR-FX-01 AC4), gözlənilməz 500 |
| `Critical` | proses işləyə bilmir | məcburi konfiqurasiya yoxdur |

- Exception bir dəfə, sərhəddə log olunur (global handler, consumer host, job host). "Log + rethrow" yazılmır.
- Gözlənilən biznes nəticələri (`Result` xətaları, validasiya) `Warning`/`Error` deyil, request log-u kifayətdir.

### 9.3 PII və secret-lər (SEC-LOG-01/02)

Log-a **heç vaxt** yazılmır: şifrə, access/refresh token, birdəfəlik tokenlər və onları ehtiva edən URL, `Authorization`/`Cookie` header-ləri, imza açarları, connection string, mesaj mətni, tam telefon nömrəsi, `AuthEmailRequested` payload-ı.

- Email yalnız `EmailMask.Mask(email)` ilə.
- Şifrə, token, telefon və ya mesaj mətni olan tiplər `[LogRedact]` ilə işarələnir.
- Bütün obyekt destructure olunmur (`{@Request}`, `{@Command}` qadağandır), yalnız lazımi id-lər yazılır.
- Request/response body log-a yazılmır, HTTP logging middleware istifadə olunmur.
- Development text formatında istifadəçi inputu `LogSanitizer` ilə təmizlənir.

### 9.4 Correlation id

- Correlation id middleware, envelope və job host tərəfindən təyin olunur (ARCHITECTURE §8.4). Kod öz correlation id-sini yaratmır və ötürmək üçün parametr əlavə etmir.
- Consumer və job daxilində əlavə kontekst `BeginScope` ilə: `using (logger.BeginScope(new Dictionary<string, object> { ["ListingId"] = id }))`.

---

## 10. Təhlükəsizlik checklist-i

Hər yeni endpoint və ya feature üçün PR-da yoxlanılır. Bənd tətbiq olunmursa, PR-da səbəb yazılır.

**Autentifikasiya və avtorizasiya**
- [ ] Endpoint-də açıq policy (`Policies.*`) və ya əsaslandırılmış `AllowAnonymous` var (SEC-AUTHZ-01).
- [ ] Endpoint `AuthorizationMatrix`-ə əlavə olunub və REQUIREMENTS 2.2 ilə uyğundur.
- [ ] R-02 (öz elanını moderasiya etmə), R-05 (sonuncu Admin) kimi rol qaydaları yoxlanılıb.

**Sahiblik (BOLA)**
- [ ] İstifadəçi id-si yalnız `ICurrentUser`-dən gəlir, request/route/query-dən götürülmür (SEC-AUTHZ-02).
- [ ] Repository sorğusunda sahiblik/iştirakçılıq şərti var.
- [ ] Başqasının resursu `*_NOT_FOUND` (404) qaytarır, mövcudluq açıqlanmır.

**Input**
- [ ] Request-in validator-u və `ValidationFilter`-i var, bütün sahələr üçün tip/uzunluq/diapazon/allow-list (SEC-INP-01).
- [ ] Request modelində sistem sahələri yoxdur (SEC-INP-03), mass assignment testi yazılıb.
- [ ] Massiv uzunluqları məhdudlaşdırılıb, mətn sahələri sanitize olunur (SEC-INP-02, SEC-INP-05).
- [ ] Dinamik SQL, istifadəçi inputu ilə sıralama/filtr adı yoxdur (SEC-INP-04).
- [ ] Fayl qəbul edirsə: SEC-FILE-01..06 (magic bytes, ölçü stream-də, decode, re-encode, təsadüfi ad).

**Output**
- [ ] Ayrıca response record-u var, entity qaytarılmır (SEC-AUTHZ-04).
- [ ] Response sahələri REQUIREMENTS 4.11 ilə uyğundur (email, telefon, şikayətçi, daxili sahələr yoxdur).
- [ ] Şəxsi data qaytarırsa `.NoStore()`; cache-də şəxsi data saxlanılmır.

**Sui-istifadəyə qarşı**
- [ ] Rate limit policy təyin olunub və ya qlobal limit kifayətdir (SEC-RATE, ARCHITECTURE §7.4).
- [ ] FR limitləri (Draft 20, aktiv 5, seçilmiş 200, saxlanmış axtarış 10 və s.) konfiqurasiyadan oxunur.
- [ ] Enumeration riski olan axında cavab, status və müddət eynidir (SEC-AUTH-08).

**Audit və log**
- [ ] SEC-LOG-03 siyahısındakı hadisədirsə, `IAuditLog.Record` biznes transaksiyasındadır.
- [ ] Log-da PII/secret yoxdur, email maskalanıb (§9.3).
- [ ] Integration event payload-ında email, telefon, mesaj mətni yoxdur (ARCHITECTURE §5.4).

**Xətalar və konfiqurasiya**
- [ ] Yalnız REQUIREMENTS 4.8-dəki xəta kodları istifadə olunub, mesajda daxili detal yoxdur.
- [ ] Yeni secret yoxdur və ya yalnız user-secrets/env ilə verilir, `ValidateOnStart` ilə yoxlanılır (SEC-SEC).
- [ ] Xarici çağırış varsa: timeout, ölçü limiti, cavab validasiyası, URL konfiqurasiyadan (SEC-EXT-01).
- [ ] Testlər: 401 / 403 / 404 (BOLA) / mass assignment (NFR-TEST-03).

---

## 11. Testlər

### 11.1 Layihə strukturu

ARCHITECTURE §2.1 və §10:

```text
tests/
├── AutoMarket.Listings.UnitTests/
│   ├── Domain/Listings/ListingTests.cs
│   ├── Domain/ValueObjects/VinTests.cs
│   ├── Application/Listings/SubmitListing/SubmitListingHandlerTests.cs
│   ├── Application/Listings/CreateListing/CreateListingRequestValidatorTests.cs
│   └── Builders/ListingBuilder.cs
├── AutoMarket.BuildingBlocks.UnitTests/
├── AutoMarket.IntegrationTests/
│   ├── Infrastructure/              # fixture-lər, ApiClient, outbox gözləmə helper-i
│   ├── Listings/SubmitListingTests.cs
│   ├── Authorization/               # AuthorizationMatrix, əhatə testi
│   └── Security/                    # fayl, header, rate limit, lockout
├── AutoMarket.ArchitectureTests/
└── load/                            # k6
```

- Unit test qovluqları `src`-dəki qovluq strukturunu təkrarlayır.
- Test class-ı: unit — `<TestEdilənTip>Tests` (`ListingTests`, `SubmitListingHandlerTests`); integration — `<UseCase>Tests` (`SubmitListingTests`).

### 11.2 Adlandırma: `Method_State_Expected`

| Hissə | Mənası |
|---|---|
| `Method` | unit testdə metod (`Submit`), integration testdə use case/endpoint (`SubmitListing`) |
| `State` | ilkin vəziyyət və ya giriş (`WhenNoImages`, `OtherUsersListing`, `Anonymous`) |
| `Expected` | gözlənilən nəticə (`ThrowsImageMinRequired`, `Returns404`, `MovesToPending`) |

```csharp
[Fact]
public void Submit_WhenNoImages_ThrowsImageMinRequired()

[Fact]
public async Task SubmitListing_OtherUsersListing_Returns404()

[Theory]
[MemberData(nameof(ForbiddenTransitions))]
public void ChangeStatus_ForbiddenTransition_ThrowsInvalidStatusTransition(ListingStatus from, ListingStatus to)
```

Test metodunda `Async` sonluğu yazılmır.

### 11.3 Unit və ya integration

| Unit | Integration |
|---|---|
| domen qaydaları: status maşını (icazəli + icazəsiz bütün kombinasiyalar), limitlər, `Money` və yuvarlaqlaşdırma, value object-lər | hər endpoint: ən azı bir uğur + bir xəta ssenarisi (NFR-TEST-02) |
| validator-lar | avtorizasiya matrisi, BOLA, mass assignment (NFR-TEST-03) |
| handler-lərin budaqları (fake portlarla) | real DB, migration-lar, EF konfiqurasiyası, sorğular, unikal indekslər |
| şifrə siyasəti, token generator, sanitizer, cron hesablamaları | outbox → RabbitMQ → consumer axını, inbox idempotentliyi |
| saxlanmış axtarış uyğunluq məntiqi | təhlükəsizlik testləri: fayl, header-lər, rate limit, lockout, refresh reuse (NFR-TEST-04) |

- Integration testlərdə in-memory DB provider istifadə olunmur, yalnız Testcontainers (NFR-TEST-02).
- NSubstitute yalnız port interfeysləri üçün. `DbContext`, `HttpContext` və domen obyektləri mock olunmur.
- CBAR və SMTP fake ilə (NFR-TEST-06).

### 11.4 Test data builder-lər

- Hər aggregate üçün etibarlı default dəyərli builder: `ListingBuilder`, `UserBuilder`. Test yalnız onun üçün vacib olan sahəni dəyişir.
- Integration testlərdə istifadəçi və data API və ya builder + DB ilə yaradılır. Hər test öz datasını yaradır, testlər arasında ortaq vəziyyət yoxdur.

```csharp
var listing = new ListingBuilder()
    .WithStatus(ListingStatus.Draft)
    .WithoutImages()
    .Build();

Should.Throw<DomainException>(() => listing.Submit(Now))
    .Error.ShouldBe(ListingDomainErrors.ImageMinRequired);
```

### 11.5 Determinizm

- Vaxt `FakeTimeProvider`, təsadüfi dəyərlər idarə olunan generator ilə (NFR-TEST-08).
- `Task.Delay` ilə gözləmə yoxdur, asinxron axın "outbox boşalana qədər gözlə" helper-i ilə yoxlanılır.
- Ləğv token-i: `TestContext.Current.CancellationToken` (xUnit v3).
- Assertion-lar Shouldly ilə.

### 11.6 Mütləq test olunanlar

- [ ] REQUIREMENTS 3.3.2 status cədvəlinin hər keçidi və hər icazəsiz keçid.
- [ ] İcazə matrisinin hər sətri (401 / 403 / 404 / uğur).
- [ ] Hər xəta kodunun ən azı bir yolu.
- [ ] Hər validator qaydası (sərhəd dəyərləri: min, max, min−1, max+1).
- [ ] Limitlər və onların konfiqurasiyadan oxunması.
- [ ] Consumer-in dublikat mesajda ikinci effekt yaratmaması; job-un təkrar icrada dublikat yaratmaması.
- [ ] Azərbaycan hərfləri (`İ/i`, `I/ı`, ə, ğ, ş) unikallıq və axtarışda (ARCHITECTURE §4.3).
- [ ] Pul çevirməsi və yuvarlaqlaşdırma (FR-FX-02 AC3).
- [ ] Log-da PII/secret olmaması (ADR-0013).

---

## 12. Git

### 12.1 Branch adları

`<növ>/<qısa-təsvir>` — kiçik hərf, kebab-case, ingiliscə.

| Növ | Məqsəd | Nümunə |
|---|---|---|
| `feature/` | yeni funksionallıq | `feature/listings-submit` |
| `fix/` | xəta düzəlişi | `fix/refresh-token-reuse-cookie` |
| `chore/` | build, paket, konfiqurasiya, refaktorinq | `chore/central-package-management` |
| `docs/` | yalnız sənədlər | `docs/conventions` |

`main`-ə birbaşa push edilmir, dəyişiklik yalnız PR ilə daxil olur.

### 12.2 Conventional Commits

Format:

```text
<type>(<scope>): <qısa təsvir>

<niyə edildiyi, nəyi dəyişdiyi — optional>

<footer: BREAKING CHANGE, Refs — optional>
```

- `type` (ingiliscə): `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `build`, `ci`, `perf`.
- `scope` — modul və ya sahə, kiçik hərf: `identity`, `catalog`, `listings`, `search`, `moderation`, `engagement`, `messaging`, `notifications`, `buildingblocks`, `api`, `tests`, `docs`.
- Təsvir Azərbaycan dilində, kiçik hərflə, əmr formasında, sonunda nöqtə yoxdur, ≤ 72 simvol.
- API-də breaking change (NFR-VER): `type!` və `BREAKING CHANGE:` footer-i.

```text
feat(listings): elanın moderasiyaya göndərilməsini əlavə et

Draft və Rejected statusundan Pending-ə keçid, Pending + Active ≤ 5 limiti
və ListingSubmitted event-i outbox ilə (FR-LST-02).
```

```text
fix(identity): paralel refresh zamanı cookie-nin silinməsini düzəlt
```

```text
docs: kod qaydalarını və CLAUDE.md-ni əlavə et
```

### 12.3 PR checklist

PR təsviri Azərbaycan dilindədir: nə dəyişdi, niyə, hansı tələblərə aiddir (`FR-*`, `SEC-*`).

- [ ] `dotnet build` xəbərdarlıqsız keçir, `dotnet format --verify-no-changes` təmizdir.
- [ ] Unit, architecture və integration testləri keçir; yeni kod üçün testlər yazılıb (§11.6).
- [ ] Təhlükəsizlik checklist-i (§10) yoxlanılıb.
- [ ] Yeni endpoint: `AuthorizationMatrix`, OpenAPI metadata (`Produces`, `ProducesProblem`), OpenAPI sənədi yenilənib.
- [ ] Yeni xəta kodu yoxdur və ya REQUIREMENTS 4.8-ə təsdiqlə əlavə olunub.
- [ ] Migration: ad qaydası, SQL review olunub, geriyə uyğundur (expand → migrate → contract).
- [ ] Yeni paket: lisenziya yoxlanılıb, ARCHITECTURE §12 yenilənib.
- [ ] Yeni integration event: `Contracts/Events`, versiya, PII-siz payload, ARCHITECTURE §5.4 yenilənib.
- [ ] Konfiqurasiya dəyişikliyi: options validasiyası, `appsettings.json`-da secret yoxdur.
- [ ] Arxitektura qərarı dəyişirsə: yeni ADR yazılıb.
- [ ] Commit-lər Conventional Commits formatındadır.
