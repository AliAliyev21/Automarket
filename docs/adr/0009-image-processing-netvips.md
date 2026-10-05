# 0009. Şəkil emalı: NetVips

- Status: Accepted
- Tarix: 2026-10-05
- Əlaqəli tələblər: FR-IMG-01 AC2/AC5, SEC-FILE-01/02/04/07, NFR-PERF (5 MB şəkil ≤ 2 s), NFR-TEST-04, Q9

## Kontekst

Yüklənən hər şəkil üçün:

- tam decode (decode olunmayan fayl rədd edilir, SEC-FILE-01);
- ölçülərin decode-dan əvvəl yoxlanılması: ≤ 8000 × 8000 və ≤ 40 MP (decompression bomb qarşısı, SEC-FILE-02);
- orientasiyanın tətbiqi, bütün EXIF/XMP/IPTC metadatasının silinməsi, yenidən encode (SEC-FILE-04);
- thumbnail və orta ölçülü variantların yaradılması (FR-IMG-01 AC5);
- JPEG, PNG, WebP oxumaq; SVG, GIF, HEIC qəbul etməmək (SEC-FILE-05).

Antivirus yoxlaması olmadığı üçün (Q9) decode və re-encode əsas qoruma qatıdır. Ona görə kitabxananın etibarlılığı vacibdir. Performans hədəfi: 5 MB şəkil emal daxil ≤ 2 s (p95).

.NET-in özündə server tərəfi üçün cross-platform şəkil kitabxanası yoxdur (`System.Drawing` Linux-da dəstəklənmir).

## Qərar

**NetVips** (libvips üçün .NET wrapper) istifadə olunur.

- Lisenziya: `NetVips` — MIT; native `NetVips.Native` paketindəki libvips — **LGPL-2.1**. Native kitabxana dinamik link olunur (paylaşılan `.so`/`.dll`), dəyişdirilmir. Bu, LGPL-in kommersiya istifadəsi şərtlərinə uyğundur. Docker image-də libvips paylanır, lisenziya mətni image-ə əlavə olunur.
- İstifadə qaydası:
  - əvvəlcə `Image.NewFromFile(..., access: Sequential)` ilə yalnız header oxunur, en/hündürlük/megapiksel yoxlanılır;
  - decode `fail = true` ilə aparılır (zədələnmiş fayl istisna yaradır → `IMAGE_INVALID`);
  - yüklənən format libvips-in aşkarladığı loader ilə də yoxlanılır və yalnız `jpegload`, `pngload`, `webpload` qəbul olunur (magic bytes yoxlamasına əlavə olaraq);
  - `Autorot()`, sRGB-yə çevrilmə, `ThumbnailImage` ilə 3 variant, `WebpSave(strip: true / keep: None)` ilə metadata-sız saxlama;
  - `Cache.Max`, `Cache.MaxMem` və `Concurrency` məhdudlaşdırılır, yalnız lazım olan operasiyalara icazə verilir (`Vips.BlockUntrusted = true`, "untrusted" loader-lər bloklanır);
  - emal tətbiq səviyyəsində `ConcurrencyLimiter` ilə məhdudlaşdırılır.
- İmplementasiya `IImageProcessor` portunun arxasındadır (Listings → Application port, Infrastructure adapter), ona görə kitabxana dəyişdirilə bilər.

## Alternativlər

### SixLabors.ImageSharp

- Lisenziya: **Six Labors Split License** (v3-dən): açıq mənbə layihələri və illik ümumi gəliri 1 mln $-dan az olan şirkətlər üçün pulsuzdur, digərləri üçün kommersiya lisenziyası lazımdır.
- Üstünlüklər: saf .NET-dir, native asılılığı yoxdur, API-si rahatdır.
- Çatışmazlıqlar: şirkətin gəlirindən asılı olaraq ödənişli ola bilər. Böyük şəkillərdə yaddaş istifadəsi libvips-dən yüksəkdir.
- Niyə seçilmədi: lisenziya seçim qaydasına uyğun deyil.

### SkiaSharp

- Lisenziya: MIT (Microsoft/.NET Foundation).
- Üstünlüklər: geniş istifadə olunur, sürətlidir, WebP dəstəyi var, re-encode zamanı metadata avtomatik itir.
- Çatışmazlıqlar: EXIF orientasiyası əl ilə tətbiq olunmalıdır (`SKCodec.EncodedOrigin`). Linux konteyneri üçün `SkiaSharp.NativeAssets.Linux` lazımdır. Ölçü limitlərini decode-dan əvvəl yoxlamaq mümkündür, amma daha çox əl işi tələb edir. Böyük şəkillərdə bütün bitmap yaddaşa yüklənir.
- Niyə seçilmədi: yaxşı alternativdir (ikinci seçim). NetVips streaming emal, aşağı yaddaş istifadəsi və hazır `autorot`/`strip`/`thumbnail` operasiyaları ilə bizim təhlükəsizlik və performans tələblərimizə daha uyğundur.

### Magick.NET (ImageMagick)

- Lisenziya: Apache 2.0 (wrapper), ImageMagick — ImageMagick License (Apache 2.0-ə bənzər).
- Çatışmazlıqlar: ağırdır, ImageMagick-in təhlükəsizlik zəiflikləri tarixçəsi (ImageTragick və s.) var və `policy.xml` ilə ciddi məhdudlaşdırma tələb edir. Böyük şəkillərdə performansı libvips-dən aşağıdır.
- Niyə seçilmədi: hücum səthi daha genişdir.

## Nəticələr

Müsbət:

- Sürətli və yaddaşa qənaətli emal: 5 MB JPEG adətən 200–500 ms-də 3 variantla emal olunur.
- Decompression bomb qorunması header yoxlaması ilə tam decode-dan əvvəl aparılır.
- Metadata-nın silinməsi və orientasiya hazır operasiyalarla aparılır.

Mənfi və risklər:

- Native asılılıq var: Docker base image-də libvips-in versiyası `NetVips.Native` paketi ilə gəlir. Zəifliklər üçün NuGet və image skanları aparılır (SEC-DEP-01/02).
- LGPL komponentidir: dinamik link qaydası saxlanılmalıdır, libvips statik link olunmamalı və dəyişdirilməməlidir.
- NFR-TEST-04 testləri məcburidir: zədələnmiş fayl, uzantısı dəyişdirilmiş fayl, poliqlot fayl, 20000×20000 PNG, GPS EXIF-li JPEG (çıxışda metadata olmadığı yoxlanılır), orientasiya 6/8 olan JPEG.
