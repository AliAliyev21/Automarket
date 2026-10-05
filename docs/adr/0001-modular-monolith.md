# 0001. Modulyar monolit

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: NFR-PERF, NFR-AVAIL, NFR-ENV-03, NFR-TEST-02

## Kontekst

AutoMarket-in MVP-si REST API-dir. Burada bir neçə aydın biznes sahəsi var: hesablar, soraqçalar, elanlar, axtarış, moderasiya, seçilmişlər və saxlanmış axtarışlar, mesajlaşma, bildirişlər. Yük hədəfi orta səviyyədədir: 50 000 aktiv elan, pik 100 RPS (NFR-PERF). Əlçatanlıq hədəfi 99.5% / aydır (NFR-AVAIL). Komanda kiçikdir və production hosting hələ seçilməyib (Q21).

Eyni zamanda sahələr arasında asinxron əlaqələr var (elan aktiv olanda axtarış, bildiriş və saxlanmış axtarış yenilənir). Kod bazası sürətlə böyüyəcək, ona görə sərhədlərin əvvəldən aydın olması vacibdir.

## Qərar

Sistem **modulyar monolit** kimi qurulur:

- Bir ASP.NET Core host prosesi (bir deploy vahidi), horizontal olaraq N instansiya.
- Modullar: Identity, Catalog, Listings, Search, Moderation, Engagement, Messaging, Notifications. Ümumi infrastruktur BuildingBlocks-dadır.
- Hər modulun öz datası (PostgreSQL schema-sı, öz DbContext-i) və açıq kontraktı (`*.Contracts` proyekti) var.
- Modullar bir-biri ilə yalnız Contracts interfeysləri (in-process) və RabbitMQ üzərindən integration event-lər vasitəsilə əlaqə saxlayır ([ADR-0004](0004-inter-module-communication-and-outbox.md)).
- Sərhədlər proyekt reference-ləri, `internal` görünürlük və arxitektura testləri ilə məcbur edilir ([ADR-0002](0002-solution-structure-and-module-boundaries.md)).

## Alternativlər

### Klassik (qatlı) monolit

Bir proyekt və ya `Domain/Application/Infrastructure/Api` üzrə bir neçə proyekt olur, biznes sahələri arasında sərhəd olmur.

- Üstünlüklər: ən sadə başlanğıcdır, transaksiyalar asandır, cross-table JOIN-lər mümkündür.
- Çatışmazlıqlar: zaman keçdikcə sahələr bir-birinə qarışır ("big ball of mud"). Axtarış, bildiriş və moderasiya kimi sahələri ayrıca miqyaslamaq və ya çıxarmaq çətinləşir.
- Niyə seçilmədi: REQUIREMENTS-də sahələr aydın ayrılır və asinxron əlaqələr çoxdur. Sərhədləri sonradan qurmaq indi qurmaqdan xeyli bahadır.

### Microservice-lər

Hər sahə ayrıca servis, öz bazası və deploy pipeline-ı ilə qurulur.

- Üstünlüklər: müstəqil deploy və miqyaslama, texnologiya müstəqilliyi.
- Çatışmazlıqlar: paylanmış sistemin bütün yükü yaranır: şəbəkə xətaları, paylanmış tracing, servis kəşfi, hər servis üçün ayrıca CI/CD, versiyalanmış API-lər, paylanmış transaksiyaların olmaması, lokal mühitin mürəkkəbliyi (NFR-ENV-03). 100 RPS üçün bu yükə ehtiyac yoxdur.
- Niyə seçilmədi: kiçik komanda və MVP mərhələsi üçün əməliyyat xərci faydadan çoxdur. Modulyar monolit gələcəkdə lazım olan modulun servisə çıxarılmasına imkan saxlayır.

### Serverless / funksiyalar

- Niyə seçilmədi: hosting seçilməyib, vendor lock-in yaradır, uzun işləyən consumer-lər və job-lar üçün uyğun deyil, lokal mühit çətinləşir.

## Nəticələr

Müsbət:

- Bir deploy, bir pipeline, sadə lokal mühit (`docker compose up` + `dotnet run`).
- In-process çağırışlar sürətlidir, şəbəkə gecikməsi yoxdur.
- Integration testlər bütün sistemi bir prosesdə yoxlayır (NFR-TEST-02).
- Modul sərhədləri aydındır. Gələcəkdə Search və ya Notifications modulunu ayrıca servisə çıxarmaq üçün əsas hazırdır: datası ayrıdır, əlaqəsi event-lərlədir.

Mənfi və risklər:

- Bütün modullar eyni prosesdə işləyir: bir modulda yaddaş sızması və ya CPU yükü (məs. şəkil emalı) digərlərinə təsir edə bilər. Azaldılması: şəkil emalı və login hash-i üçün concurrency limiter-lər, health check-lər, metrikalar.
- Modullar ayrıca miqyaslanmır. Lazım olsa, eyni image-in rollarına görə ayrı instansiyalar (məs. yalnız consumer/job işlədən "worker" instansiyası) konfiqurasiya ilə qaldırıla bilər. Bunun üçün host modulları və background servisləri konfiqurasiya flag-ları ilə aktiv edir.
- Sərhədlərin pozulması texniki olaraq mümkündür (məs. raw SQL ilə başqa schema-ya müraciət). Azaldılması: arxitektura testləri, code review, SHOULD olaraq hər schema üçün ayrıca DB rolu.
- Eventual consistency: modullar arası məlumat bir neçə saniyə gecikə bilər (məs. elan aktiv olandan sonra axtarışda görünməsi). Bu, REQUIREMENTS-də icazə verilən hədlər daxilindədir.

Yenidən baxılma şərtləri: bir modulun yükü və ya release tezliyi digərlərindən kəskin fərqlənərsə, həmin modulun ayrıca servisə çıxarılması yeni ADR ilə qiymətləndirilir.
