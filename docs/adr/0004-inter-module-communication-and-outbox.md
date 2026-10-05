# 0004. Modullar arası əlaqə və outbox

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: NFR-PERF-03, NFR-JOB, FR-NOTIF-01 AC4/AC8, ST-02, FR-FX-02 AC2, SEC-LOG-03; [ADR-0001](0001-modular-monolith.md), [ADR-0005](0005-messaging-library-rabbitmq-client.md)

## Kontekst

Modullar öz datasının yeganə sahibidir ([ADR-0002](0002-solution-structure-and-module-boundaries.md)). Lakin bir-birinə ehtiyacları var:

- **Dərhal cavab lazım olan oxumalar:** elan yaradılarkən soraqça id-lərinin aktivliyi, elan detalında satıcının adı, "nömrəni göstər" zamanı telefon.
- **Fakt barədə xəbərdarlıq:** elan Active oldu → Search, Engagement, Moderation, Messaging və Notifications reaksiya verir; istifadəçi bloklandı → elanlar gizlədilir.
- **Moderasiya qərarı:** Moderation modulu Listings-in statusunu dəyişir və konfliktə (`CONCURRENCY_CONFLICT`) dərhal cavab almalıdır (ST-02).

Tələblər: ağır işlər istifadəçi sorğusunu yavaşlatmamalıdır (NFR-PERF-03), email uğursuzluğu təsdiq əməliyyatını bloklamamalıdır (FR-NOTIF-01 AC8), bildirişlər dublikat olmamalıdır (AC4, NFR-JOB).

Əsas problem **dual write**-dır: DB-yə yazıb broker-ə göndərəndə ikisindən biri uğursuz ola bilər.

## Qərar

### 1. Sinxron əlaqə — Contracts interfeysləri

- Provayder modul `*.Contracts`-da interfeys təyin edir (`ICatalogReader`, `IUserDirectory`, `IListingReader`, `IExchangeRateProvider`). İmplementasiya onun Infrastructure qatında yazılır və `internal`-dır. İstehlakçı interfeysi DI ilə alır.
- Default olaraq **yalnız oxuma** üçündür. DTO-lar immutable `record`-dur, entity deyil.
- Tez-tez oxunan data provayder tərəfdə `HybridCache` ilə keşlənir.
- **İstisna — sinxron command:** `IListingModeration` (approve, reject, unpublish). Qaydalar: (a) bütün yazma əməliyyatı provayderin (Listings) bir transaksiyasında aparılır: status, tarixçə, audit və outbox; (b) çağıran modul (Moderation) həmin sorğuda öz DB-sinə yazmır, öz vəziyyətini sonradan event-dən yeniləyir. Yeni sinxron command interfeysi yalnız ADR ilə əlavə olunur.

### 2. Asinxron əlaqə — integration event-lər + transactional outbox

- Event keçmiş zamanda olan faktdır (`ListingActivated`), command deyil. Kontrakt publisher-in `*.Contracts/Events` qovluğundadır və versiyalanır (`.v1`).
- **Outbox:** event biznes datası ilə **eyni transaksiyada** modulun `<schema>.outbox` cədvəlinə yazılır. Aggregate domen hadisələri toplayır, `SaveChanges` interceptor-u onları integration event-lərə çevirib outbox sətirləri kimi əlavə edir.
- **Publisher:** `OutboxPublisher` (BackgroundService) `FOR UPDATE SKIP LOCKED` ilə sətirləri götürür, RabbitMQ-ya publisher confirms ilə göndərir, sonra `processed_at` yazır. Bir neçə instansiya paralel işləyə bilər.
- **Çatdırılma semantikası:** at-least-once.
- **Inbox (idempotent consumer):** hər consumer modulun `<schema>.inbox` cədvəli var (`message_id` + `consumer` unikal). Consumer inbox sətrini və handler-in dəyişikliklərini **bir transaksiyada** yazır. Dublikat mesaj ack edilir və handler çağırılmır. Əlavə olaraq biznes səviyyəsində təbii açarlar istifadə olunur (`saved_search_matches` unikal açarı, projection-larda versiya müqayisəsi).
- **Retry və DLQ:** [ADR-0005](0005-messaging-library-rabbitmq-client.md) — TTL əsaslı gecikmə queue-ları (5 s → 1 saat, 5 cəhd), sonra `<queue>.dlq`.
- **Ordering:** zəmanət verilmir. Projection-lar `aggregateVersion` ilə köhnə event-i atır.
- **Audit:** `IAuditLog` audit qeydini həmin modulun outbox-una `audit.recorded.v1` kimi yazır. Bu, audit qeydinin biznes əməliyyatı ilə atomik olmasını təmin edir.

### 3. Modul daxili əlaqə

Modul daxilində domen hadisələri in-process işlənir (eyni transaksiyada). Broker yalnız modullar arası əlaqə üçündür.

## Alternativlər

### Hər şey sinxron (modullar bir-birinin interfeysini çağırır)

- Üstünlüklər: sadədir, güclü konsistentlik verir, broker lazım deyil.
- Çatışmazlıqlar: təsdiq sorğusu 5 modulu ardıcıl çağırar və birinin xətası hamısını uğursuz edər (FR-NOTIF-01 AC8 pozulur). Cavab müddəti artar (NFR-PERF-03), modullar sıx bağlanar (temporal coupling).
- Niyə seçilmədi: tələblər asinxronluğu açıq tələb edir.

### In-process event bus (broker olmadan, `Channel<T>` və ya mediator ilə)

- Üstünlüklər: əlavə infrastruktur yoxdur, sürətlidir.
- Çatışmazlıqlar: proses dayananda yaddaşdakı event-lər itir. Outbox ilə birlikdə istifadə olunsa belə, bir neçə instansiya arasında yük paylanması, retry/DLQ və gələcəkdə modulun ayrıca servisə çıxarılması üçün yenidən işləmək lazım gələr.
- Niyə seçilmədi: çərçivədə RabbitMQ müəyyən edilib. Outbox + broker modelini əvvəldən qurmaq gələcəkdə servis ayrılmasını asanlaşdırır. Qeyd: outbox publisher abstraksiya arxasındadır, ona görə testlərdə in-memory transport istifadə etmək mümkündür.

### Outbox olmadan birbaşa publish (transaksiyadan sonra)

- Niyə seçilmədi: commit-dən sonra, publish-dən əvvəl proses dayanarsa event itir (məs. elan Active olur, amma axtarışda görünmür). Publish-dən sonra commit uğursuz olarsa, "xəyali" event yaranır.

### Paylanmış transaksiya (2PC) / saga orchestrator

- Niyə seçilmədi: RabbitMQ 2PC-ni dəstəkləmir. Bizim axınlarda kompensasiya tələb edən çoxaddımlı biznes transaksiyası yoxdur, hər event müstəqil reaksiyadır.

### Ortaq "integration" cədvəlləri və ya DB view-ları ilə oxuma

- Niyə seçilmədi: schema sərhədini pozur, modulun daxili strukturunu dəyişməyi çətinləşdirir. Axtarış üçün bu yanaşmanın xüsusi təhlili [ADR-0012](0012-search-read-model.md)-dədir.

## Nəticələr

Müsbət:

- Biznes datası və event atomik yazılır, event itmir.
- Consumer xətası publisher-ə təsir etmir, retry avtomatik aparılır.
- Modullar zaman baxımından ayrılır: broker əlçatmaz olanda istifadəçi sorğuları işləməyə davam edir, event-lər outbox-da gözləyir.

Mənfi və risklər:

- Eventual consistency: event-in çatdırılmasına qədər modullar arasında qısa uyğunsuzluq olur (adətən < 1 s). Bu, UI və testlərdə nəzərə alınmalıdır. Integration testlərdə "outbox boşalana qədər gözlə" helper-i istifadə olunur.
- Outbox, inbox, retry və DLQ kodu bizim məsuliyyətimizdədir ([ADR-0005](0005-messaging-library-rabbitmq-client.md)).
- Hər consumer idempotent yazılmalıdır. Bu, code review qaydasıdır və inbox mexanizmi bunu əsasən təmin edir.
- DLQ-ya düşən mesajlar əl ilə müdaxilə tələb edir. Monitorinq və alert lazımdır.
