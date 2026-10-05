# 0005. Messaging kitabxanası: birbaşa RabbitMQ.Client

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: NFR-JOB, NFR-CORR, FR-NOTIF-01 AC4/AC8; [ADR-0004](0004-inter-module-communication-and-outbox.md)

## Kontekst

[ADR-0004](0004-inter-module-communication-and-outbox.md)-ə görə modullar arası event-lər RabbitMQ üzərindən outbox/inbox ilə ötürülür. Bizə lazım olanlar:

- topologiyanın (exchange, queue, binding) yaradılması;
- publisher confirms ilə etibarlı publish;
- async consumer, manual ack, prefetch;
- gecikmə ilə retry, DLQ;
- outbox (EF Core + PostgreSQL) və inbox (idempotentlik);
- correlation id və W3C trace context-in ötürülməsi.

Kitabxana seçimi qaydası: açıq və pulsuz lisenziya, mümkün qədər az əlavə asılılıq, kommersiya lisenziyasına keçmiş kitabxanalar seçilərsə, bu açıq yazılmalıdır.

## Qərar

**Rəsmi `RabbitMQ.Client` 7.x** (Apache 2.0 / MPL 2.0 dual license, pulsuz) birbaşa istifadə olunur. Üzərində nazik öz qatımız yazılır: `AutoMarket.BuildingBlocks/Messaging`.

| Komponent | Təsvir |
|---|---|
| `IIntegrationEvent`, `MessageEnvelope` | `messageId`, `type`, `version`, `occurredAt`, `correlationId`, `traceparent`, `payload` (System.Text.Json) |
| `OutboxInterceptor` | `SaveChanges` zamanı domen hadisələrini outbox sətirlərinə çevirir |
| `OutboxPublisher<TDbContext>` | `FOR UPDATE SKIP LOCKED`, batch publish, publisher confirms (`CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true)`), `mandatory = true` + `BasicReturn` işlənməsi |
| `RabbitMqTopology` | startup-da `automarket.events` (topic), `automarket.retry` (direct), hər modul üçün `<m>.events` (quorum), `<m>.events.retry.{1..5}` (TTL + DLX → default exchange), `<m>.events.dlq` |
| `RabbitMqConsumerHost<TModule>` | `AsyncEventingBasicConsumer`, manual ack, prefetch, handler-ə tip əsasında dispatch, inbox transaksiyası, retry/DLQ qərarı |
| `IIntegrationEventHandler<TEvent>` | modulların yazdığı handler interfeysi |
| Bağlantı idarəsi | bir uzunömürlü `IConnection` (automatic recovery aktivdir), publish və consume üçün ayrıca kanallar, publisher kanalları thread-safe deyil, ona görə pool istifadə olunur |
| Telemetriya | RabbitMQ.Client 7-nin daxili `ActivitySource`-u + envelope-dakı `traceparent` ilə span-ların bağlanması |

Kodun həcmi təxminən 600–900 sətir (testlərdən başqa) qiymətləndirilir. Bunun əsas hissəsi retry/DLQ və outbox-dur.

## Alternativlər

### MassTransit

- Lisenziya: **v8 — Apache 2.0** (pulsuz). **v9-dan (2025-ci ildə elan olunub) kommersiya lisenziyasıdır.** Müəlliflər v8 üçün dəstəyin 2026-nın sonuna qədər davam edəcəyini bildiriblər.
- Üstünlüklər: yetkin kitabxanadır, EF Core outbox/inbox, retry, redelivery, saga, test harness hazırdır.
- Çatışmazlıqlar: v8 seçilsə, təxminən bir il ərzində ya kommersiya v9-a, ya da başqa kitabxanaya keçmək lazım gələcək. Abstraksiya çox böyükdür və topologiya konvensiyaları öz qaydalarını tətbiq edir.
- Niyə seçilmədi: uzunmüddətli lisenziya riski var.

### Wolverine (JasperFx)

- Lisenziya: MIT (pulsuz). Kommersiya əlavələri (CritterWatch və s.) ayrıca satılır, core açıqdır.
- Üstünlüklər: PostgreSQL outbox/inbox, RabbitMQ transport, retry, DLQ hazırdır. Həm də mediator əvəzi kimi işləyə bilər.
- Çatışmazlıqlar: framework kimi davranır: handler discovery, runtime code generation, öz konvensiyaları. Öyrənmə əyrisi var, debugging daha çətindir. Modul sərhədləri ilə (`internal` handler-lər, modul başına DbContext) inteqrasiya üçün əlavə konfiqurasiya lazımdır. İcması nisbətən kiçikdir.
- Niyə seçilmədi: bizə lazım olan funksiyalar kiçikdir, framework-ün "magic"-i şəffaflığı azaldır. Gələcəkdə öz qatımız böyüyərsə, Wolverine-ə keçid əsas alternativ kimi qalır.

### Rebus

- Lisenziya: MIT (pulsuz). Rebus Pro (dashboard, fleet manager) kommersiyadır, core açıqdır.
- Üstünlüklər: yüngüldür, RabbitMQ transport, PostgreSQL outbox, retry, error queue hazırdır.
- Çatışmazlıqlar: icması kiçikdir, EF Core ilə inteqrasiya (eyni transaksiyada outbox) əlavə iş tələb edir, topologiya Rebus-un öz konvensiyasına uyğundur.
- Niyə seçilmədi: yaxşı alternativdir, amma fayda öz nazik qatımızdan əhəmiyyətli dərəcədə çox deyil.

### NServiceBus

- Kommersiya lisenziyalıdır. Seçim qaydasına uyğun gəlmir.

### Brokersiz (yalnız PostgreSQL əsaslı queue, məs. `LISTEN/NOTIFY` + outbox cədvəli)

- Niyə seçilmədi: çərçivədə RabbitMQ müəyyən edilib. Fan-out, retry topologiyası və gələcəkdə servis ayrılması üçün broker daha uyğundur.

## Nəticələr

Müsbət:

- Lisenziya riski yoxdur, asılılıq bir paketdir və rəsmi client-dir.
- Topologiya, retry və DLQ tam şəffafdır və RabbitMQ management UI-da birbaşa görünür.
- Modul strukturumuzla uyğundur: handler-lər `internal`, outbox hər modulun öz DbContext-indədir.

Mənfi və risklər:

- Outbox, inbox, retry, DLQ və bağlantı bərpası bizim kodumuzdur. Azaldılması: BuildingBlocks-da ayrıca unit və integration testləri (Testcontainers RabbitMQ ilə): publisher confirm itkisi, broker restart-ı, dublikat çatdırılma, poison message, retry sayğacı.
- Saga, scheduling və request/response kimi inkişaf etmiş funksiyalar yoxdur. MVP-də onlara ehtiyac yoxdur.
- `RabbitMQ.Client` 7.x API-si 6.x-dən fərqlidir (tam async). Komanda bunu öyrənməlidir.

Yenidən baxılma şərti: saga/workflow ehtiyacı yaranarsa və ya messaging qatı 1500 sətirdən çox böyüyərsə, Wolverine-ə keçid yeni ADR ilə qiymətləndirilir.
