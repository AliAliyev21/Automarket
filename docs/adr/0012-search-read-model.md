# 0012. Axtarış üçün ayrıca read model

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: FR-SRCH-01..03, FR-FX-01 AC6, FR-FX-02 AC2, FR-ADM-01 AC2/AC3, ST-05, NFR-PERF (axtarış p95 ≤ 300 ms), NFR-PAG, SEC-INP-04, Q16

## Kontekst

Search modulu Active, silinməmiş və sahibi bloklanmamış elanlar üzərində çoxlu filtr, sıralama və pagination ilə işləməlidir. Elanın sahibi və status maşını Listings modulundadır. Axtarış üçün xüsusi tələblər:

- qiymət filtri və sıralama **AZN ekvivalenti** ilə aparılır. Yeni məzənnədən sonra bütün USD/EUR elanlarının AZN dəyəri ən gec 1 saata yenilənməlidir (FR-FX-02 AC2);
- məzənnə heç vaxt olmayıbsa, USD/EUR elanları qiymət filtrində iştirak etmir (FR-FX-01 AC6);
- bitmə tarixi keçmiş elan axtarışda dərhal görünməməlidir (ST-05);
- bloklanmış sahibin elanları gizlədilir, status isə dəyişmir (FR-ADM-01 AC2);
- full-text axtarış MVP-də yoxdur (Q16);
- hədəf: 50 000 aktiv elan, axtarış p95 ≤ 300 ms.

Modul sərhəd qaydasına görə ([ADR-0002](0002-solution-structure-and-module-boundaries.md)) Search Listings-in cədvəllərini oxuya bilməz.

## Qərar

Search modulunun **öz read model-i** var: `search.listing_search` cədvəli (denormalizasiya olunmuş, yalnız axtarış üçün lazım olan sahələrlə).

- **Sahələr:** `listing_id`, `version`, `owner_id`, `make_id`, `model_id`, `year`, `body_type_id`, `fuel_type_id`, `gearbox_id`, `city_id`, `mileage`, `engine_volume`, `price`, `currency`, `price_azn` (nullable), `rate_date`, `published_at`, `expires_at`, `owner_blocked`, `cover_image_key`.
- **Yenilənmə:** Listings event-ləri ilə ([ADR-0004](0004-inter-module-communication-and-outbox.md)):
  - `ListingActivated` → upsert;
  - `ListingUpdated` → atributların yenilənməsi;
  - `ListingDeactivated` / `ListingRejected` / `ListingDeleted` → sətrin silinməsi;
  - `ListingVisibilityRestored` → upsert;
  - sahib bloklananda `ListingDeactivated(OwnerBlocked)` → sətrin silinməsi.
  
  Hər upsert `WHERE incoming.version > stored.version` şərti ilə aparılır (köhnə event-lər atılır).
- **AZN ekvivalenti:** read model-də saxlanılır. `ExchangeRatesUpdated` event-i gələndə bir SQL ilə yenilənir: `UPDATE ... SET price_azn = round(price * @rate, 2) WHERE currency = @c`. PostgreSQL-in `numeric` üçün `round` funksiyası yarımı sıfırdan uzağa yuvarlaqlaşdırır və bu, kodda `MidpointRounding.AwayFromZero` qaydasına uyğundur. Saatlıq `search.fx-reprice` job-u event itsə belə yenilənməni təmin edir. Məzənnə yoxdursa, `price_azn = NULL` olur və qiymət filtri `price_azn IS NOT NULL` şərti ilə işləyir. Yuvarlaqlaşdırma qaydası bir yerdə (`Money.ToAzn`) sənədləşdirilir və Listings detalında da eyni qayda tətbiq olunur.
- **Sorğu:** EF Core LINQ, yalnız allow-list-dən seçilmiş filtrlər və sıralama ilə (SEC-INP-04). Həmişə `expires_at > now` şərti əlavə olunur (ST-05: expire job-unun gecikməsindən asılı olmadan). Sıralama: `ORDER BY <allow-list sütunu>, listing_id`. Offset pagination ilk 10 000 nəticə ilə məhdudlaşır (NFR-PAG). `total` sayı `COUNT(*)` ilə hesablanır və cache-lənir.
- **İndekslər:** sıralama sütunları + `listing_id` üzrə B-tree, əsas filtr sütunları üzrə B-tree. Lazım olsa çoxsütunlu indekslər yük testinin nəticəsinə əsasən əlavə olunur. 50 000 sətir üçün PostgreSQL bu sorğuları rahat ödəyir.
- **Adlar:** read model-də yalnız id-lər saxlanılır. Marka, model və şəhər adları cavab formalaşanda `ICatalogReader`-dən (cache) götürülür. Belə olduqda soraqça adının dəyişməsi read model-i yeniləməyi tələb etmir.
- **Rebuild:** read model yenidən qurula bilər. Listings.Contracts-da `IListingReader.StreamActiveForSearchAsync()` interfeysi var. Bu, yalnız admin/deploy əmri ilə işləyən rebuild prosesi üçündür, adi sorğularda istifadə olunmur.

## Alternativlər

### Listings schema-sında read-only DB view

- Üstünlüklər: data həmişə dərhal konsistentdir, event emalı və projection kodu yoxdur.
- Çatışmazlıqlar: Search Listings-in schema-sından asılı olur (sərhəd pozulur). Listings cədvəllərinin dəyişməsi Search-i sındırır. AZN ekvivalentini view-da hesablamaq üçün Catalog-un məzənnə cədvəli ilə cross-schema JOIN lazımdır. Hesablanmış dəyər üzrə sıralama indeks istifadə edə bilmir (p95 hədəfinə risk). Search modulunun gələcəkdə ayrıca servisə və ya axtarış mühərrikinə köçürülməsi çətinləşir.
- Niyə seçilmədi: modul sərhədi və performans səbəbindən.

### Axtarış mühərriki (Elasticsearch / OpenSearch / Meilisearch / Typesense)

- Lisenziyalar: Elasticsearch — SSPL/Elastic License/AGPL (2024-dən AGPL variantı da var), OpenSearch — Apache 2.0, Meilisearch — MIT (community), Typesense — GPL-3.0.
- Üstünlüklər: full-text, facet-lər, çox böyük həcmdə yüksək performans.
- Çatışmazlıqlar: əlavə infrastruktur, əlavə əməliyyat yükü və eventual consistency. MVP-də full-text yoxdur (Q16) və 50 000 elan PostgreSQL üçün kiçik həcmdir.
- Niyə seçilmədi: MVP üçün lazım deyil. Read model yanaşması gələcəkdə keçidi asanlaşdırır: eyni event-lər axtarış mühərrikinə də yazıla bilər.

### Listings modulu axtarış endpoint-ini özü versin (ayrıca Search modulu olmadan)

- Niyə seçilmədi: Search-ə məxsus məntiq (cache, AZN yenilənməsi, xüsusi indekslər, yük profili) Listings-in yazma modeli ilə qarışır. Çərçivədə Search ayrıca modul kimi müəyyən edilib.

## Nəticələr

Müsbət:

- Axtarış sorğuları sadə və indekslənmiş tək cədvəl üzərində işləyir, AZN üzrə sıralama indeksdən istifadə edir.
- Modul sərhədi qorunur, Search Listings-in daxili strukturundan asılı deyil.
- Gələcəkdə OpenSearch və ya başqa mühərrikə keçid yalnız Search modulunu dəyişir.

Mənfi və risklər:

- Eventual consistency: təsdiqlənmiş elan axtarışda adətən 1 saniyədən az gecikmə ilə görünür. Silinmiş elan read model-dən event gələnə qədər görünə bilər. Azaldılması: detal sorğusu `LISTING_NOT_FOUND` qaytarır, cache tag invalidation tətbiq olunur.
- Projection kodu və rebuild mexanizmi əlavə iş tələb edir. Consumer-lər idempotentdir və versiya müqayisəsi aparır.
- Event itkisi read model-i pozur. Azaldılması: outbox/inbox, DLQ monitorinqi, admin rebuild əmri.
