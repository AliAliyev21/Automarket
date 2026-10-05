# 0007. Cache strategiyası: HybridCache + Redis

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: NFR-PERF (axtarış p95 ≤ 300 ms, detal ≤ 150 ms, soraqçalar ≤ 50 ms), FR-DICT-01 AC4, ST-05, SEC-AUTH-06, FR-LST-04 AC2/AC4

## Kontekst

Oxumaların təxminən 80%-i axtarış və detal sorğularıdır (pik 100 RPS). Soraqçalar demək olar ki, dəyişmir. İstifadəçi statusu hər autentifikasiya olunmuş sorğuda yoxlanılmalıdır (SEC-AUTH-06). Tətbiq bir neçə instansiyada işləyir, ona görə yalnız yaddaş cache-i instansiyalar arasında uyğunsuz olur. Yalnız Redis istifadə olunarsa, isə hər oxuma şəbəkə gedişi tələb edir.

Konsistentlik tələbləri:

- soraqça dəyişikliyi ən gec 5 dəqiqəyə görünməlidir (FR-DICT-01 AC4);
- bitmə tarixi keçmiş elan axtarışda dərhal görünməməlidir (ST-05);
- silinmiş elan və onun şəkilləri dərhal əlçatmaz olmalıdır (FR-LST-04);
- bloklanmış istifadəçinin tokeni dərhal rədd edilməlidir (SEC-AUTH-06).

## Qərar

**`HybridCache`** (`Microsoft.Extensions.Caching.Hybrid`, MIT, .NET-in hissəsi) istifadə olunur: **L1** prosesdaxili yaddaş, **L2** Redis (`Microsoft.Extensions.Caching.StackExchangeRedis`).

- Stampede qorunması: eyni açar üçün eyni anda yalnız bir factory çağırılır.
- Tag əsaslı invalidation: `RemoveByTagAsync("search")`, `RemoveByTagAsync("catalog")`.
- Açar konvensiyası: `<modul>:<obyekt>:v<versiya>:<id və ya hash>`. Versiya dəyişəndə (DTO strukturu dəyişəndə) köhnə açarlar avtomatik istifadəsiz qalır.
- Serializasiya: System.Text.Json (source generator ilə).

### L1 məhdudiyyəti və TTL qaydası

`RemoveAsync` / `RemoveByTagAsync` **L2-ni və yalnız cari instansiyanın L1-ni** təmizləyir. Digər instansiyaların L1 yaddaşında köhnə dəyər öz TTL-i bitənə qədər qala bilər. Buna görə:

- **L1 TTL həmişə qısadır: ≤ 30 saniyə.** Bu, invalidation-dan sonra köhnə məlumatın maksimum görünmə müddətidir.
- Daha ciddi konsistentlik tələb olunan data üçün L1 TTL daha qısadır: istifadəçi statusu — 5 s, axtarış və detal — 10 s.
- L2 TTL daha uzundur, çünki invalidation onu dərhal təmizləyir.

| Data | L2 TTL | L1 TTL | Invalidation |
|---|---|---|---|
| Soraqçalar | 5 dəq | 30 s | tag `catalog` (Admin dəyişikliyi). HTTP `Cache-Control: public, max-age=300` + `ETag` |
| Son məzənnə | 1 saat | 30 s | `ExchangeRatesUpdated` |
| İstifadəçi statusu | 5 dəq | 5 s | block/delete/role change zamanı açar silinir |
| İstifadəçinin public profili | 10 dəq | 30 s | profil dəyişikliyi |
| Axtarış səhifəsi | 60 s | 10 s | tag `search`: deaktivasiya, silinmə, rədd, məzənnə |
| Elan detalı | 2 dəq | 10 s | tag `listing:{id}` |
| Şəkilin statusu | 5 dəq | 10 s | elanın silinməsi |

Əlavə qaydalar:

- Axtarış nəticəsi cache-dən gəlsə belə, `expires_at ≤ now` olan elementlər cavabdan çıxarılır (ST-05). Ona görə cache bitmiş elanı göstərmir.
- Şəxsi data (telefon, mesajlar, bildirişlər, seçilmişlər, saxlanmış axtarışlar) cache-də saxlanılmır.
- Axtarış açarı kanonikdir: filtr massivləri sıralanır, qiymət AZN-ə çevrilib yuvarlaqlaşdırılır, sonra SHA-256 hash-i alınır. Bu, cache-in istifadəçi inputu ilə "partladılmasının" qarşısını alır və hit nisbətini artırır. Açar sayı Redis `maxmemory` + `allkeys-lru` ilə məhdudlaşdırılır.
- Redis əlçatmaz olduqda HybridCache L1 + factory ilə işləyir. Tətbiq dayanmır, yalnız health `Degraded` olur.

## Alternativlər

### Yalnız `IMemoryCache`

- Niyə seçilmədi: instansiyalar arasında invalidation yoxdur, hər instansiya ayrıca "isinir", istifadəçi statusu kimi data üçün uyğun deyil.

### Yalnız `IDistributedCache` (Redis)

- Üstünlüklər: bütün instansiyalar üçün vahid mənbədir, L1 uyğunsuzluğu yoxdur.
- Çatışmazlıqlar: hər oxuma şəbəkə gedişi tələb edir (soraqçalar p95 ≤ 50 ms hədəfi üçün lazımsız gecikmədir). Stampede qorunması və tag invalidation yoxdur, bunları özümüz yazmalı olardıq.
- Niyə seçilmədi: HybridCache eyni L2-ni istifadə edir və bu çatışmazlıqları aradan qaldırır.

### L1 invalidation üçün Redis pub/sub backplane

- Bütün instansiyaların L1-ni dərhal təmizləmək üçün öz pub/sub mexanizmimizi yazmaq olar.
- Niyə seçilmədi (MVP üçün): qısa L1 TTL (≤ 30 s) REQUIREMENTS-dəki bütün konsistentlik hədlərini ödəyir və kodu sadə saxlayır. Ehtiyac yaranarsa, backplane sonradan əlavə edilə bilər, bunun üçün API dəyişikliyi lazım deyil.

### Axtarışı keşləməmək

- Niyə seçilmədi: axtarış ən çox yüklənən endpoint-dir. Populyar filtr kombinasiyaları (məs. "bütün elanlar, ən yenilər") üçün qısa TTL-li cache DB yükünü əhəmiyyətli dərəcədə azaldır. Axtarış indekslər üzərində cache-siz də p95 hədəfinə uyğun dizayn olunur, cache əlavə ehtiyatdır.

### Response caching / Output caching middleware

- `OutputCache` (.NET 7+) HTTP səviyyəsində işləyir və Redis store-u var.
- Niyə seçilmədi: autentifikasiya olunmuş sorğularda (User rolu üçün axtarış) default olaraq cache etmir. Cavabdan bitmiş elanların çıxarılması (ST-05) kimi post-processing mümkün deyil. Soraqçalar üçün HTTP cache header-ləri (`Cache-Control`, `ETag`) client və CDN səviyyəsində kifayətdir.

## Nəticələr

Müsbət:

- .NET-in öz kitabxanası istifadə olunur, əlavə üçüncü tərəf asılılığı yoxdur.
- İsti oxumalar L1-dən mikrosaniyələrlə qaytarılır. L2 instansiyalar arasında paylaşılır, stampede qorunması daxilidir.

Mənfi və risklər:

- Digər instansiyaların L1-də 30 saniyəyə qədər köhnə dəyər qala bilər. Bu, qəbul edilmiş və sənədləşdirilmiş davranışdır. Kritik yoxlamalar (istifadəçi statusu) daha qısa L1 TTL ilə aparılır.
- Cache açarlarının və tag-ların düzgün seçilməsi code review tələb edir. Səhv tag köhnə datanın L2 TTL-i qədər qalmasına səbəb ola bilər.
- Redis yaddaşı monitorinq olunmalıdır (`maxmemory`, eviction metrikaları).
