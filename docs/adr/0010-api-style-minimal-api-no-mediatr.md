# 0010. API stili: Minimal API, MediatR və AutoMapper olmadan

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: SEC-AUTHZ-01/04, SEC-INP-01/03/07, SEC-ERR-01, NFR-VER, NFR-DOC, NFR-PERF

## Kontekst

API-nin nəqliyyat qatı, use case-lərin çağırılması, input validasiyası və entity → DTO mapping-i üçün bir yanaşma seçilməlidir. .NET icmasında populyar seçim "Controllers/Minimal API + MediatR + AutoMapper + FluentValidation"-dır. Lakin:

- **MediatR** v13-dən (2025) **kommersiya lisenziyasına** keçib (Lucky Penny Software; kiçik şirkətlər üçün pulsuz "community" variantı var, amma şərtlər müəllif tərəfindən müəyyən edilir və dəyişə bilər). Köhnə açıq versiyalar (v12 və əvvəlki) artıq yenilənmir.
- **AutoMapper** v15-dən (2025) eyni şəkildə kommersiya lisenziyasına keçib.
- Tələblərə görə hər endpoint-in ayrıca input modeli (SEC-INP-03) və output modeli (SEC-AUTHZ-04) olmalıdır. Konvensiya əsaslı avtomatik mapping isə sahə sızmasını gizlədə bilər.

## Qərar

1. **Minimal API.** Hər modul `<M>Module.Map<M>Endpoints(IEndpointRouteBuilder)` metodunda öz route qrupunu (`MapGroup("/api/v{version:apiVersion}/listings")`) qeyd edir. Endpoint-lər `Api/` qovluğunda feature-lərə görə qruplaşdırılır (`Api/Listings/CreateListingEndpoint.cs`). Hər endpoint-də açıq şəkildə authorization policy (və ya `AllowAnonymous`), rate limit policy, `Produces`/`ProducesProblem` metadata-sı göstərilir.
2. **Mediator yoxdur.** Use case-lər sadə interfeyslərdir: `ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>` (`BuildingBlocks.Application`-da). Handler-lər DI ilə birbaşa endpoint-ə inject olunur. Cross-cutting davranışlar (validation, transaksiya, logging) mediator pipeline-ı ilə deyil, aşağıdakı yollarla həll olunur:
   - validation — endpoint filter (`ValidationFilter<T>`);
   - transaksiya — handler daxilində `DbContext.SaveChangesAsync` (bir modul, bir transaksiya), outbox interceptor ilə;
   - logging/tracing — ASP.NET Core və OpenTelemetry instrumentasiyası; lazım olsa DI decorator (əl ilə, kitabxanasız).
3. **Validation — FluentValidation** (Apache 2.0, pulsuz, lisenziya dəyişikliyi elan olunmayıb). Validator-lar `internal`-dır və modul daxilində assembly scan ilə qeydiyyatdan keçir. Mürəkkəb şərtli qaydalar (min ≤ max, elektromobil üçün mühərrik həcminin tələb olunmaması, `OTHER` səbəbi üçün şərhin məcburi olması) burada ifadə olunur.
4. **Mapping əl ilə yazılır.** Response modelləri `record`-dur, mapping isə statik `ToResponse()` metodları və ya projection-lar (`Select(x => new ListingResponse(...))`) ilə aparılır. Projection EF sorğusunda yalnız lazım olan sütunları seçir (performans) və sahə sızmasını kompilyasiya zamanı görünən edir.
5. **Nəticə tipi:** handler-lər `Result<T>` qaytarır, endpoint onu `TypedResults.Ok(...)` və ya `TypedResults.Problem(...)` ilə çevirir. `TypedResults` OpenAPI metadata-sını avtomatik verir.

## Alternativlər

### Controllers (MVC)

- Üstünlüklər: tanış strukturdur, filter-lər və model binding zəngindir, atributlarla konfiqurasiya olunur.
- Çatışmazlıqlar: daha çox boilerplate var, modullar üçün application part qeydiyyatı lazımdır, `internal` controller-lər üçün əlavə konfiqurasiya tələb olunur. Minimal API .NET 8–10-da əsas inkişaf istiqamətidir: built-in OpenAPI, `TypedResults`, endpoint filter-lər, AOT dəstəyi.
- Niyə seçilmədi: Minimal API route qrupları modulların öz endpoint-lərini qeyd etməsi üçün daha sadədir.

### MediatR (+ pipeline behaviors)

- Lisenziya: v13+ kommersiyadır. Pulsuz istifadə şərtləri (community license) gəlir həddinə bağlıdır və müəllif tərəfindən dəyişdirilə bilər. Açıq v12 yenilənmir.
- Üstünlüklər: pipeline behavior-lar ilə cross-cutting, handler-lərin ayrılması.
- Çatışmazlıqlar: lisenziya riski. Runtime-da dolayı çağırış "go to definition"-u çətinləşdirir. Bizim ehtiyacımız (handler-lərin ayrılması) sadə interfeyslərlə tam ödənir.
- Açıq alternativlər: `Mediator` (martinothamar, MIT, source generator ilə), `Wolverine` (MIT). Lazım olarsa onlara baxıla bilər, amma hazırda mediator ehtiyacı yoxdur.
- Niyə seçilmədi: lisenziya riski və əlavə dəyər azdır.

### AutoMapper / Mapster

- AutoMapper: v15+ kommersiyadır. Mapster: MIT.
- Çatışmazlıqlar: konvensiya əsaslı mapping yeni sahəni avtomatik olaraq response-a çıxara bilər (SEC-AUTHZ-04 riski). Xətalar runtime-da aşkarlanır. EF projection-larında gözlənilməz SQL yarada bilər.
- Niyə seçilmədi: əl ilə mapping açıqdır, kompilyasiya zamanı yoxlanılır və bizim modellərimizin sayı idarə oluna biləcək həddədir. Mapperly (Apache 2.0, source generator) gələcəkdə ehtiyac yaranarsa əsas alternativdir, çünki o, compile-time kod yaradır və runtime "magic" yoxdur.

### .NET 10 built-in Minimal API validation (`AddValidation()`)

- Lisenziya: MIT (.NET-in hissəsi). DataAnnotations və source generator əsasında işləyir.
- Üstünlüklər: əlavə paket lazım deyil.
- Çatışmazlıqlar: mürəkkəb şərtli və sahələr arası qaydalar `IValidatableObject` və ya öz atributlarımızla yazılmalıdır, bu da model tiplərini validasiya məntiqi ilə qarışdırır. Xəta kodlarını (`errors[].code`) idarə etmək daha çətindir.
- Niyə seçilmədi: FluentValidation daha ifadəlidir, açıq lisenziyalıdır və validator-lar ayrıca test olunur. Lazım olsa, sadə modellər üçün built-in validasiyaya keçid gələcəkdə nəzərdən keçirilə bilər.

## Nəticələr

Müsbət:

- Lisenziya riski olan asılılıq yoxdur. Əlavə paketlər yalnız FluentValidation və Asp.Versioning.Http-dir.
- Çağırış zənciri açıqdır (endpoint → handler → repository), debugging asandır.
- Input/output modelləri açıq olduğu üçün mass assignment və sahə sızması riskləri azalır.

Mənfi və risklər:

- Mapping kodu daha çoxdur. Azaldılması: response modelləri kiçikdir, projection-lar sorğu ilə birlikdə yazılır.
- Mediator pipeline-ı olmadığı üçün cross-cutting davranışları əlavə etmək üçün decorator və ya filter yazmaq lazımdır. Bu, açıq qaydalarla sənədləşdirilir.
