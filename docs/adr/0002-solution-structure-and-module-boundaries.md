# 0002. Solution strukturu və modul sərhədləri

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: NFR-TEST-01, SEC-AUTHZ-04, SEC-INP-03; [ADR-0001](0001-modular-monolith.md)

## Kontekst

Modulyar monolitdə ([ADR-0001](0001-modular-monolith.md)) iki sərhəd növü məcbur edilməlidir:

1. **Modullar arası:** modul başqa modulun class-larına və DB cədvəllərinə birbaşa müraciət etmir.
2. **Modul daxilində qatlar arası:** domen qaydaları framework-dən asılı olmur, HTTP detalları use case-lərə keçmir.

Sərhədlər nə qədər güclü məcbur edilərsə, bir o qədər çox proyekt yaranır. 8 modul üçün bu, build vaxtına, IDE performansına və strukturun başa düşülməsinə təsir edir.

## Qərar

### Proyektlər

- **Host:** `AutoMarket.Api` — composition root.
- **BuildingBlocks (2 proyekt):**
  - `AutoMarket.BuildingBlocks` — domen primitivləri, application abstraksiyaları, persistence bazası, messaging (outbox/inbox/RabbitMQ), jobs, audit, caching, storage, security helper-ləri (qovluqlar kimi);
  - `AutoMarket.BuildingBlocks.Web` — HTTP ilə bağlı ümumi kod (ProblemDetails, validation filter, rate limiting, security header-lər, correlation id).
- **Hər modul — 2 proyekt:**
  - `AutoMarket.<M>` — modulun bütün kodu. Qatlar **qovluq və namespace** kimi ayrılır: `Domain/`, `Application/`, `Infrastructure/`, `Api/`. Kökdə yeganə public tip olan `<M>Module` (DI qeydiyyatı və endpoint mapping) yerləşir;
  - `AutoMarket.<M>.Contracts` — modulun açıq kontraktı: sinxron interfeyslər, onların DTO-ları, integration event-lər.

### Reference qaydaları

| Proyekt | Reference verə bilər |
|---|---|
| `AutoMarket.Api` | bütün `AutoMarket.<M>`, `BuildingBlocks.Web` |
| `AutoMarket.<M>` | `BuildingBlocks`, `BuildingBlocks.Web`, öz `Contracts`, digər modulların **yalnız** `Contracts` proyektləri |
| `AutoMarket.<M>.Contracts` | `BuildingBlocks` (yalnız primitivlər və `IIntegrationEvent` üçün) |
| `BuildingBlocks.Web` | `BuildingBlocks` |
| `BuildingBlocks` | — |

### Məcburetmə

| Sərhəd | Mexanizm |
|---|---|
| Modullar arası | **Proyekt reference-ləri**: başqa modulun əsas proyekti kompilyator üçün görünmür. Əlavə olaraq: modul proyektində `<M>Module`-dan başqa bütün tiplər `internal`-dır, `InternalsVisibleTo` yalnız modulun testlərinə verilir. Arxitektura testi bu iki qaydanı da yoxlayır |
| Qatlar arası (modul daxilində) | **ArchUnitNET testləri**: Domain → heç bir digər qat və framework yoxdur; Application → Domain; Infrastructure → Application, Domain; Api → Application. `DbContext` yalnız Infrastructure-dadır, Api entity qaytarmır |
| DB | Hər modulun DbContext-i öz schema-sına map olunur, cross-schema JOIN və FK yoxdur. Production-da SHOULD: hər schema üçün ayrıca DB rolu |

Testlər: hər modul üçün `AutoMarket.<M>.UnitTests`, ümumi `AutoMarket.IntegrationTests` və `AutoMarket.ArchitectureTests`.

## Alternativlər

### Hər modul üçün 5 proyekt (`Domain`, `Application`, `Infrastructure`, `Api`, `Contracts`)

Qatlar ayrıca proyektlərdir, qat qaydaları proyekt reference-ləri ilə məcbur edilir.

- Üstünlüklər: qat qaydası kompilyator səviyyəsində məcbur edilir (Domain proyektində EF Core paketi yoxdursa, onu istifadə etmək mümkün deyil). Asılılıqlar `.csproj`-da açıq görünür.
- Çatışmazlıqlar: 8 modul × 5 = 40 proyekt + BuildingBlocks + testlər, yəni 50-dən çox proyekt olur. Build vaxtı artır, IDE yavaşlayır. `internal` istifadə etmək çətinləşir: Application-dakı tiplər Infrastructure-a görünmək üçün `public` olmalıdır, bu da modullar arası sərhədi zəiflədir (başqa modul Contracts-dan kənar `public` tipləri görə bilərdi, əgər reference verilsəydi). Kiçik dəyişiklik üçün bir neçə proyektə toxunmaq lazım gəlir.
- Niyə seçilmədi: qat qaydalarını arxitektura testləri də eyni dəqiqliklə yoxlayır (CI-da merge-i bloklayır). 2 proyektli struktur `internal` ilə modul sərhədini daha güclü edir və strukturu sadələşdirir. Modullar arası sərhəd — ən vacib sərhəd — hər iki variantda proyekt reference-ləri ilə qorunur.

### Modul başına 1 proyekt (Contracts-sız)

Kontrakt interfeysləri modulun əsas proyektində `public` olur.

- Niyə seçilmədi: başqa modul kontrakt üçün bütün modul proyektinə reference verməli olardı və bu zaman modulun daxili tiplərinə də çıxış əldə edərdi (yalnız `internal` ilə qorunardı). Dövri asılılıq (Listings ↔ Catalog) yaranardı və bu, proyekt səviyyəsində mümkün deyil.

### Bir proyekt, modullar qovluq kimi

- Niyə seçilmədi: modullar arası sərhəd yalnız konvensiya və testlərlə qorunardı, `internal` mənasını itirərdi, çünki hamısı eyni assembly-də olardı.

### BuildingBlocks-u çox proyektə bölmək (`Domain`, `Application`, `Infrastructure`, `Messaging`, `Web`, `Audit`)

- Niyə seçilmədi: BuildingBlocks modulların hamısı tərəfindən istifadə olunur və müstəqil versiyalanmır. Bölmək real fayda vermədən proyekt sayını artırır. Yalnız HTTP-dən asılı kod (`.Web`) ayrılıb, çünki Contracts proyektləri ASP.NET Core-dan asılı olmamalıdır.

## Nəticələr

Müsbət:

- Proyekt sayı az qalır: 8 × 2 + 2 + 1 = 19 proyekt (testlərdən başqa).
- Modul daxilində tiplər `internal` ola bilir, yalnız `<M>Module` və Contracts açıqdır.
- Modul daxilində naviqasiya sadədir: use case-in endpoint-i, handler-i və repository-si bir proyektdədir.

Mənfi və risklər:

- Qat qaydası pozuntusu kompilyasiya zamanı deyil, test zamanı aşkarlanır. Azaldılması: arxitektura testləri hər PR-da işləyir və uğursuz olarsa merge bloklanır. Lokal olaraq da `dotnet test` ilə tez işləyir.
- Domain qatı texniki olaraq EF Core paketini görür (eyni proyektdə olduğu üçün). Azaldılması: ArchUnitNET qaydası `Microsoft.EntityFrameworkCore` asılılığını Domain namespace-ində qadağan edir. EF konfiqurasiyası Infrastructure-da fluent API ilə yazılır, entity-lərdə atribut olmur.
- EF Core design-time alətləri `internal` DbContext ilə işləyir, amma `IDesignTimeDbContextFactory` hər modulda yazılmalıdır.

Yenidən baxılma şərti: bir modul ayrıca servisə çıxarılarsa və ya qat pozuntuları tez-tez baş verərsə, həmin modul üçün 5 proyektli struktura keçid yeni ADR ilə qiymətləndirilir.
