# 0013. Logging və observability: Serilog + OpenTelemetry

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: NFR-LOG, NFR-CORR, NFR-HC, SEC-LOG-01/02, SEC-PII-05, NFR-JOB, NFR-ENV-03

## Kontekst

Tələblər:

- JSON formatında structured log-lar, sabit sahələrlə: timestamp UTC, level, message template, correlation id, trace id, user id, endpoint, status, müddət, mühit, versiya (NFR-LOG);
- hər HTTP sorğusu üçün bir yekun qeyd, route şablonu ilə (query string olmadan);
- həssas məlumatın log-a düşməməsi (SEC-LOG-01) və log injection-a qarşı qoruma (SEC-LOG-02);
- correlation id və W3C Trace Context-in HTTP, event-lər, job-lar və email üzrə ötürülməsi (NFR-CORR);
- log-ların 30 gün saxlanması (SEC-PII-05). Saxlama yeri hosting ilə müəyyən olunacaq (Q21).

Lokal mühitdə developer-lər log və trace-lərə rahat baxa bilməlidir.

## Qərar

1. **Kodda yalnız `Microsoft.Extensions.Logging.ILogger<T>`** istifadə olunur. Modullar Serilog-dan birbaşa asılı deyil (arxitektura testi `Serilog` namespace-inin yalnız Host-da istifadəsini yoxlayır). Tez-tez yazılan log-lar üçün `LoggerMessage` source generator istifadə olunur.
2. **Serilog** host-da logging provider kimi qoşulur (`Serilog.AspNetCore`, `UseSerilog`):
   - sink: **stdout**, `CompactJsonFormatter` (JSON, bir sətir — bir qeyd). Toplayıcı (Loki, Elastic, bulud log servisi) hosting seçimi ilə müəyyən olunur və tətbiq kodunu dəyişmir;
   - enricher-lər: `FromLogContext`, mühit, tətbiq adı və versiyası, `CorrelationId`, `UserId`, `Module`;
   - `UseSerilogRequestLogging`: hər sorğu üçün bir qeyd, `RequestPath` əvəzinə route şablonu, query string-siz, health endpoint-ləri üçün `Verbose` səviyyəsində;
   - redaction: `[LogRedact]` atributu olan tipləri maskalayan destructuring policy, email maskalanması, `Authorization`/`Cookie` header-lərinin yazılmaması (HTTP body/header logging istifadə olunmur);
   - səviyyələr konfiqurasiyadan oxunur (`Serilog:MinimumLevel`), production default-u `Information`-dır;
   - **lokal mühitdə** əlavə sink: `Serilog.Sinks.Seq` → docker-compose-dakı **Seq** konteyneri. Seq sink-i yalnız `Development` konfiqurasiyasında aktivdir.
3. **OpenTelemetry** (Apache 2.0) traces və metrics üçün istifadə olunur:
   - instrumentasiya: ASP.NET Core, `HttpClient`, Npgsql (`Npgsql.OpenTelemetry`), RabbitMQ.Client 7 (daxili `ActivitySource`), öz `ActivitySource`-larımız (outbox, consumer, job-lar);
   - metrikalar: ASP.NET Core və runtime metrikaları (built-in `System.Diagnostics.Metrics`), öz metrikalarımız: outbox backlog, DLQ ölçüsü, job müddəti/xətası, email queue, rate limit rəddləri, cache hit nisbəti;
   - exporter: OTLP (endpoint konfiqurasiyadan). Lokal mühitdə trace-lər Seq-in OTLP ingestion endpoint-inə göndərilir. Digər mühitlərdə exporter hosting-in seçdiyi collector-a yönəldilir;
   - log-lar trace ilə `TraceId`/`SpanId` vasitəsilə əlaqələndirilir.
4. Correlation id middleware-i və envelope ötürülməsi [ARCHITECTURE §8.4](../ARCHITECTURE.md#84-correlation-id-və-tracing)-də təsvir olunub.

## Alternativlər

### Yalnız built-in `ILogger` + `JsonConsole` formatter + OpenTelemetry Logs

- Lisenziya: MIT/Apache 2.0, əlavə logging kitabxanası yoxdur.
- Üstünlüklər: ən az asılılıq, .NET-in standart yolu.
- Çatışmazlıqlar: enricher-lər, destructuring policy-ləri (redaction) və request logging üçün daha çox öz kodumuz lazımdır. Lokal mühitdə Seq inteqrasiyası OTLP ilə mümkündür, amma konfiqurasiyası daha az rahatdır. `JsonConsole` formatının sahə adları NFR-LOG-dakı sabit sxemə uyğunlaşdırılmalıdır.
- Niyə seçilmədi: Serilog yetkin enricher və redaction mexanizmləri verir, Apache 2.0 lisenziyalıdır. Kodda yalnız `ILogger` istifadə olunduğu üçün bu seçim istənilən vaxt geri qaytarıla bilər.

### NLog

- Lisenziya: BSD-3-Clause.
- Niyə seçilmədi: Serilog-un structured logging modeli (message template + property-lər) və ASP.NET Core inteqrasiyası daha geniş yayılıb. Funksional fərq kiçikdir.

### Lokal mühitdə Seq əvəzinə .NET Aspire Dashboard və ya Grafana stack (Loki + Tempo + Prometheus)

- Aspire Dashboard (MIT): OTLP log/trace/metric-ləri göstərir, amma data yaddaşda saxlanılır və restart-da itir.
- Grafana stack (AGPL-3.0 komponentləri): güclüdür, amma lokal mühit üçün 3–4 əlavə konteyner deməkdir.
- Seq: lokal mühitdə pulsuz **Individual** lisenziya ilə işləyir (bir istifadəçi). Bir konteynerdir, structured log axtarışı güclüdür, OTLP trace-lərini qəbul edir. Kommersiya məhsuludur: komandanın paylaşılan Seq serveri ödənişli lisenziya tələb edir.
- Qərar: lokal mühitdə **Seq**. Production toplayıcısı hosting ilə seçiləcək. Seq-ə bağlılıq yoxdur, çünki tətbiq stdout JSON + OTLP göndərir (ARCHITECTURE §13 A8).

## Nəticələr

Müsbət:

- Tətbiq kodu logging kitabxanasından asılı deyil (`ILogger`).
- Log formatı və sabit sahələr bir yerdə (host) idarə olunur, redaction mərkəzləşdirilib.
- Trace-lər HTTP → outbox → RabbitMQ → consumer → email zənciri üzrə izlənilir.

Mənfi və risklər:

- Serilog və OpenTelemetry paralel işləyir: log-lar Serilog ilə stdout-a, trace/metric-lər OTLP ilə göndərilir. Konfiqurasiya iki yerdə saxlanılır. Bu, sənədləşdirilir.
- Redaction-un tamlığı testlə yoxlanılmalıdır: integration test log çıxışını toplayır (test sink) və şifrə, token, telefon və email nümunələrinin açıq formada olmadığını yoxlayır.
- Seq lisenziyası yalnız fərdi lokal istifadə üçün pulsuzdur. Bu, komandaya izah olunmalıdır.
