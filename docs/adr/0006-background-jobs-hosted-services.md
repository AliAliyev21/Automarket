# 0006. Background job-lar: öz BackgroundService + Cronos

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: NFR-JOB, ST-05, FR-FX-01 AC2, FR-FX-02 AC2, FR-NOTIF-01 AC5, FR-AUTH-02 AC4, FR-ACC-02 AC3, FR-IMG-01 AC6, SEC-PII-05, NFR-MISC, NFR-HC

## Kontekst

NFR-JOB-də təxminən 12 planlaşdırılmış iş var: elanların Expired olması, bitmə xəbərdarlığı, məzənnənin alınması, AZN ekvivalentinin yenilənməsi, gündəlik digest, yetim şəkillərin təmizlənməsi, tokenlərin və təsdiqlənməmiş hesabların təmizlənməsi, anonimləşdirmə, saxlama müddəti başa çatmış datanın silinməsi.

Tələblər:

- cron cədvəli **Asia/Baku** saat qurşağı ilə (digest 09:00, məzənnə dərc olunandan sonra);
- bir neçə instansiyada eyni iş paralel iki dəfə icra olunmur;
- hər iş idempotentdir və təkrar icra dublikat effekt yaratmır;
- başlanğıc, nəticə, element sayı və xətalar log-a yazılır;
- health check job-ların vəziyyətini göstərir (NFR-HC).

İşlərin heç biri ayrıca "iş növbəsi" (fire-and-forget job enqueue) tələb etmir: asinxron işlər (email, bildiriş) artıq outbox/RabbitMQ və `email_queue` ilə həll olunur.

## Qərar

**Öz `ScheduledJobHost` (`BackgroundService`)** + cron ifadələri üçün **Cronos** (MIT) + koordinasiya üçün PostgreSQL istifadə olunur.

- `IScheduledJob`: `Name`, `ExecuteAsync(JobContext, CancellationToken)`. Cədvəl konfiqurasiyadan oxunur: `Jobs:<name>:Cron`, `Jobs:<name>:TimeZone` (default `Asia/Baku`), `Jobs:<name>:Enabled`.
- Cronos növbəti icra vaxtını `TimeZoneInfo` ilə hesablayır (DST keçidləri düzgün işlənir, Azərbaycanda DST yoxdur, amma kitabxana bunu ümumi halda dəstəkləyir). Gözləmə `TimeProvider` ilə aparılır, ona görə testdə `FakeTimeProvider` ilə idarə olunur.
- **Təkrarlanmanın qarşısı:**
  1. `platform.job_runs(job_name, scheduled_for)` unikal açarı: occurrence-i yalnız bir instansiya "götürə" bilər (`INSERT ... ON CONFLICT DO NOTHING`).
  2. İcra zamanı `pg_try_advisory_lock(hashtext(job_name))` ayrıca bağlantıda saxlanılır: əvvəlki icra bitməyibsə, yeni icra `Skipped` olur.
  3. Hər job-un öz biznes idempotentliyi var (status şərtli update, unikal açarlar).
- **Misfire:** startup-da son buraxılmış occurrence `MisfireGrace` daxilindədirsə bir dəfə icra olunur.
- **Müşahidə:** `job_runs`-da status, müddət, element sayı və xəta saxlanılır. Structured log, `Activity`, metrika (`job_duration`, `job_failures`) yazılır. Health check son uğurlu icraların yaşını yoxlayır.
- Uzun işlər batch-lərlə aparılır (məs. 500 sətir), hər batch ayrıca transaksiyadadır, `CancellationToken` hörmət olunur (graceful shutdown).

## Alternativlər

### Quartz.NET

- Lisenziya: Apache 2.0 (pulsuz).
- Üstünlüklər: yetkin scheduler-dir, cron + saat qurşağı, misfire siyasətləri, clustered `AdoJobStore` (PostgreSQL) ilə bir neçə instansiyada təkrarlanmanın qarşısı hazırdır.
- Çatışmazlıqlar: öz DB schema-sı var (11 `qrtz_*` cədvəli). Clustering üçün öz migration skriptlərini idarə etmək lazımdır. API və konfiqurasiyası bizim ehtiyacımızdan böyükdür. Job-ların DI ilə inteqrasiyası və test edilməsi əlavə qat tələb edir.
- Niyə seçilmədi: bizim job-larımız sadə cron işləridir. Koordinasiya üçün lazım olan (unikal occurrence + advisory lock) PostgreSQL-də bir neçə sətirlə həll olunur. Quartz güclü alternativ olaraq qalır.

### Hangfire

- Lisenziya: Hangfire.Core — LGPL-3.0 (pulsuz, kommersiya istifadəsinə icazə verir). PostgreSQL storage — icma paketi (`Hangfire.PostgreSql`, LGPL/MIT). Batch-lər, throttling və bəzi funksiyalar yalnız ödənişli **Hangfire Pro**-dadır.
- Üstünlüklər: dashboard, retry, fire-and-forget job-lar.
- Çatışmazlıqlar: storage-ı polling ilə işlədiyi üçün DB-yə daimi yük yaradır, öz schema-sı var. Dashboard production-da əlavə təhlükəsizlik səthi deməkdir. Fire-and-forget funksiyasına ehtiyacımız yoxdur, çünki RabbitMQ var.
- Niyə seçilmədi: funksiyaların çoxu bizə lazım deyil, əlavə storage və təhlükəsizlik yükü yaradır.

### Cronos-suz, yalnız interval və sabit saat

- Niyə seçilmədi: "hər saat 09:00–23:00 arası" kimi cədvəllər üçün öz parser-imizi yazmalı olardıq. Cronos kiçik (bir neçə class), MIT lisenziyalı və yalnız hesablama aparan kitabxanadır.

### Xarici scheduler (Kubernetes CronJob, sistem cron)

- Niyə seçilmədi: hosting seçilməyib (Q21). Job-lar tətbiqin daxili modellərindən istifadə edir. Lokal mühit sadə qalmalıdır.

## Nəticələr

Müsbət:

- Əlavə DB schema-sı və ağır asılılıq yoxdur: yalnız Cronos (MIT).
- Job-lar modulların daxilində yazılır və digər kod kimi DI və `TimeProvider` ilə test olunur.
- Koordinasiya mexanizmi şəffafdır: `job_runs` cədvəli həm audit, həm də health mənbəyidir.

Mənfi və risklər:

- Scheduler kodu (misfire, lock, shutdown) bizim məsuliyyətimizdədir. Azaldılması: BuildingBlocks testləri — iki host eyni occurrence-i götürməyə çalışır, proses icra zamanı dayanır, saat irəli çəkilir.
- Advisory lock bağlantıya bağlıdır: bağlantı qırılarsa lock azad olur və başqa instansiya eyni job-a başlaya bilər. Buna qarşı əsas qoruma biznes idempotentliyi və occurrence unikallığıdır.
- Dashboard yoxdur. `job_runs` üçün Admin endpoint-i (`GET /api/v1/admin/jobs`) və metrikalar bu ehtiyacı ödəyir.

Yenidən baxılma şərti: job sayı və mürəkkəbliyi kəskin artarsa (dependency chain, dinamik cədvəllər, istifadəçi tərəfindən planlaşdırılan işlər), Quartz.NET-ə keçid yeni ADR ilə qiymətləndirilir.
