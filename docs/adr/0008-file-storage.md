# 0008. Fayl saxlama: IFileStorage + lokal fayl sistemi

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: FR-IMG-01/02, FR-LST-04 AC4, SEC-FILE-03/05/06, SEC-PII-05, NFR-HC, Q10, Q21

## Kontekst

Elan şəkilləri saxlanılmalı və təqdim olunmalıdır: hər elanda 1–10 şəkil, hər birinin 3 variantı (large, medium, thumb). 50 000 aktiv elan üçün bu, təxminən 150–300 GB (variantlarla birlikdə) qiymətləndirilir. Tələblər:

- server tərəfindən yaradılan təsadüfi fayl adları, path traversal-ın qarşısı (SEC-FILE-03);
- icra oluna bilən qovluqdan kənarda saxlama, server tərəfindən təyin olunmuş `Content-Type`, `nosniff` (SEC-FILE-05);
- Draft şəkilləri üçün təxmin edilə bilməyən URL (≥ 128 bit), directory listing olmaması (SEC-FILE-06, Q10);
- silinmiş elanın şəkilləri dərhal public girişdən çıxarılır, 30 gün sonra fiziki silinir (FR-LST-04 AC4);
- health check fayl storage-ı yoxlayır (NFR-HC).

Production hosting və fayl saxlama yeri hələ seçilməyib (Q21).

## Qərar

1. **`IFileStorage` abstraksiyası** (`AutoMarket.BuildingBlocks/Storage`): `SaveAsync(key, stream, contentType)`, `OpenReadAsync(key)`, `DeleteAsync(key)`, `ExistsAsync(key)`, `MoveAsync(from, to)`. Açar məntiqi yoldur (`listings/{imageKey}/{variant}.webp`) və yalnız server tərəfindən yaradılır.
2. **MVP implementasiyası — `LocalFileStorage`:** fayllar konfiqurasiya olunmuş kök qovluqda saxlanılır (`Storage:Local:RootPath`). Bu qovluq tətbiqin qovluğundan və `wwwroot`-dan kənardadır. Bir neçə instansiya üçün kök qovluq **shared volume** olur (Docker volume, NFS və ya hosting-in shared disk-i). Açar `[a-z0-9/_.-]` allow-list ilə yoxlanılır, `Path.GetFullPath` nəticəsinin kök qovluğun daxilində qaldığı təsdiqlənir (path traversal-a qarşı ikinci qoruma).
3. **Təqdim:** `GET /media/{imageKey}/{variant}.webp` endpoint-i. Şəkilin statusu yoxlanılır (silinmiş → 404), stream ilə qaytarılır, `Content-Type: image/webp` və `X-Content-Type-Options: nosniff` header-ləri əlavə olunur. Fayl sistemi birbaşa təqdim olunmur, ona görə directory listing mümkün deyil.
4. **Silinmə:** elan silinəndə şəkillər DB-də `deleted` kimi işarələnir (media endpoint-i dərhal 404 qaytarır). Fiziki silinmə 30 gün sonra `listings.purge-deleted-images` job-u ilə aparılır.
5. **Health:** ready check kök qovluqda kiçik test faylı yazır, oxuyur və silir.
6. **S3 adapteri gələcək üçün nəzərdə tutulub:** `S3FileStorage` (AWSSDK.S3, Apache 2.0) S3-uyğun istənilən storage ilə işləyəcək. Keçid konfiqurasiya ilə aparılır (`Storage:Provider = Local | S3`), mövcud fayllar bir dəfəlik miqrasiya skripti ilə köçürülür.

## Alternativlər

### Əvvəldən S3-uyğun object storage

- Üstünlüklər: üfüqi miqyaslama, shared volume problemi yoxdur, CDN inteqrasiyası və imzalı URL-lər asandır.
- Çatışmazlıqlar: hosting seçilməyib (Q21). Bulud provayderi seçilmədən S3 implementasiyası lokal emulyator tələb edir. MinIO-nun icma versiyası AGPL-3.0 lisenziyalıdır və 2025-ci ildə funksionallığı məhdudlaşdırılıb (admin UI çıxarılıb, binary paylanması dayandırılıb). Alternativ lokal emulyatorlar (SeaweedFS — Apache 2.0, Garage — AGPL-3.0) əlavə servis deməkdir.
- Niyə seçilmədi: indi qərar vermək tezdir. Abstraksiya keçidi ucuz edir.

### Şəkilləri PostgreSQL-də saxlamaq (`bytea` / large object)

- Niyə seçilmədi: DB ölçüsü və backup-lar şişir, şəkil təqdimatı DB bağlantılarını tutur, CDN-ə keçid çətinləşir.

### Statik faylların reverse proxy (nginx) ilə birbaşa təqdimi

- Üstünlüklər: tətbiq prosesinə yük düşmür, `sendfile` ilə sürətlidir.
- Çatışmazlıqlar: silinmiş elanın şəkillərini "dərhal" bağlamaq üçün faylları köçürmək və ya proxy-də əlavə yoxlama aparmaq lazım gəlir. Proxy konfiqurasiyası hosting-ə bağlıdır.
- Niyə seçilmədi (MVP üçün): API vasitəsilə təqdim REQUIREMENTS-i sadə şəkildə ödəyir. Yük artarsa, proxy və ya CDN-ə keçid yeni ADR ilə aparılır (ARCHITECTURE §13 A7).

## Nəticələr

Müsbət:

- Lokal mühitdə əlavə servis lazım deyil (fayllar `./.data/media`-da saxlanılır).
- Təhlükəsizlik tələbləri (təsadüfi açar, nosniff, directory listing-in olmaması, dərhal 404) bir yerdə idarə olunur.
- S3-ə keçid tətbiq kodunu dəyişmədən mümkündür.

Mənfi və risklər:

- Bir neçə instansiya üçün shared volume lazımdır. Onun performansı və etibarlılığı (NFS) hosting-dən asılıdır. Volume əlçatmaz olarsa, ready check `Unhealthy` qaytarır.
- Şəkillər tətbiq prosesi vasitəsilə verilir. Bu, CPU və bağlantı yükü yaradır. Azaldılması: `Cache-Control: public, max-age=300`, stream ilə ötürmə, gələcəkdə CDN.
- Backup strategiyası (volume snapshot) hosting seçimi ilə birlikdə müəyyən edilməlidir.
- Brauzer və ya ara proxy-lər şəkili `max-age` müddətinə (5 dəq) qədər cache-də saxlaya bilər. Server tərəfində bloklama dərhal baş verir. Bu, qəbul edilmiş riskdir.
