# AutoMarket — Tələblər Sənədi (Requirements)

| Sahə | Dəyər |
|---|---|
| Versiya | 0.3 (qaralama) |
| Tarix | 2026-10-05 |
| Status | Açıq suallar üzrə qərarlar qəbul edilib (bax: 6), review gözləyir |
| Əhatə | MVP, backend Web API (C# / .NET 10) |

> Bu sənəd **nə** edilməli olduğunu təsvir edir, **necə** edilməli olduğunu yox. Arxitektura, verilənlər bazası, framework və kitabxana seçimi ayrıca sənəddə veriləcək.
> Rəqəmlərlə verilən limitlər (TTL, rate limit, ölçü) başlanğıc dəyərlərdir. Onlar konfiqurasiyadan oxunmalıdır, kodda sabit yazılmamalıdır.

### Terminlər

| Termin | Mənası |
|---|---|
| Elan (Listing) | Satışa çıxarılan bir avtomobil haqqında məlumat və onun şəkilləri |
| Soraqça (Dictionary) | İdarə olunan siyahılar: marka, model, kuzov, yanacaq, sürətlər qutusu, şəhər |
| Sahib (Owner) | Elanı yaradan istifadəçi |
| Thread | Bir elan üzrə alıcı ilə satıcı arasındakı yazışma |
| AZN ekvivalenti | Elanın qiymətinin CBAR məzənnəsi ilə manata çevrilmiş dəyəri |
| MUST / SHOULD | MUST — mütləqdir; SHOULD — tövsiyə olunur, imtina etmək üçün əsaslandırma lazımdır |

---

## 1. Məhsulun təsviri və MVP sərhədləri

### 1.1 Qısa təsvir

AutoMarket Azərbaycan bazarı üçün avtomobil elanları platformasıdır (turbo.az kimi). Fiziki şəxslər avtomobillərini satışa çıxarır. Alıcılar elanları axtarır, filtrləyir, seçilmişlərə əlavə edir və satıcıya platformanın daxilində yazır. Hər elan dərc olunmazdan əvvəl moderasiyadan keçir. Admin istifadəçiləri və soraqçaları idarə edir.

Bu mərhələdə məhsul yalnız **REST Web API**-dir. Web və mobil frontend-lər bu API-dən istifadə edəcək.

### 1.2 MVP-yə daxildir

- Qeydiyyat (email + şifrə), email təsdiqi, login, logout, şifrə bərpası, refresh token
- Profil: ad və əlaqə telefonunun redaktəsi, şifrə dəyişmə, hesabın silinməsi
- Soraqçalar: marka, model (markaya bağlı), kuzov, yanacaq, sürətlər qutusu, şəhər. Adlar az + en dillərindədir
- Elanlar: yaratma, redaktə, silmə (soft delete), 1–10 şəkil, status axını Draft → Pending → Active → Expired / Rejected
- Elan qiyməti AZN, USD və ya EUR ilə verilir; AZN ekvivalenti CBAR-ın gündəlik məzənnəsi ilə hesablanır
- Moderasiya: təsdiq, səbəb göstərilməklə rədd, aktiv elanın dərcdən çıxarılması
- Elandan şikayət (report)
- Axtarış: filtrlər, sıralama, pagination
- Seçilmişlər (favorites)
- Saxlanmış axtarış (bir istifadəçi üçün ən çox 10) və uyğun yeni elan haqqında bildiriş (email + in-app, dərhal və ya gündəlik digest)
- Satıcıya mesaj: hər elan üzrə ayrıca thread, yalnız mətn, polling; istifadəçini bloklama, thread-dən şikayət
- Satıcı telefonunun yalnız login olmuş istifadəçiyə göstərilməsi; hər açılış qeydə alınır
- Admin funksiyaları yalnız API vasitəsilə: istifadəçini bloklamaq, rol təyin etmək, soraqçaları idarə etmək

### 1.3 MVP-yə daxil deyil

- Salon/diler hesabları, korporativ profillər
- Ödənişli xidmətlər: VIP, irəli çəkmə, ödəniş inteqrasiyası
- Telefon ilə login, SMS/OTP təsdiqi
- Sosial login (Google, Facebook və s.)
- Real-time chat (WebSocket/SignalR), mesajda şəkil və fayl göndərmək
- Push bildirişlər, mobil tətbiq
- Admin və moderator üçün ayrıca UI
- Rus dili (soraqçalarda və digər yerlərdə)
- Şəxsi məlumatların ixracı (data portability) (Q12)
- Elanı "Satıldı" kimi qeyd etmək və ya müvəqqəti gizlətmək (Q5). MVP-də satıcı elanı yalnız silə bilər
- Email ünvanının dəyişdirilməsi (Q14)
- Açar sözlə (full-text) axtarış (Q16)
- Mətnlərdə qadağan olunmuş sözlərin avtomatik filtri (Q18)
- Şəkillərin antivirus ilə yoxlanılması (Q9)
- Avtomobilin tarixçəsi, VIN dekodlama, kredit kalkulyatoru, müqayisə
- Elanın statistikası (baxış sayı) satıcı üçün — SHOULD (sonrakı mərhələ)

---

## 2. Rollar və icazə matrisi

### 2.1 Rollar

| Rol | Təsvir |
|---|---|
| **Guest** | Login olmamış ziyarətçi |
| **User** | Email-i təsdiqlənmiş və bloklanmamış qeydiyyatlı istifadəçi |
| **Moderator** | User-in bütün hüquqları + elan moderasiyası və şikayətlərə baxış |
| **Admin** | Moderator-un bütün hüquqları + istifadəçi, rol və soraqça idarəetməsi |

Qaydalar:
- R-01. Rollar kumulyativdir: Admin ⊃ Moderator ⊃ User.
- R-02. Moderator və Admin **öz elanlarını** moderasiya edə bilməz. Onların elanını başqa moderator yoxlayır.
- R-03. Email-i təsdiqlənməmiş istifadəçi login edə bilmir (bax: FR-AUTH-03).
- R-04. Bloklanmış istifadəçi login edə bilmir və onun mövcud tokenləri dərhal etibarsız olur.
- R-05. Admin özünü bloklaya və ya öz Admin rolunu ləğv edə bilməz. Sistemdə həmişə ən azı bir aktiv Admin qalmalıdır.

### 2.2 İcazə matrisi

İşarələr: ✅ icazə var · ❌ icazə yoxdur · **Öz** — yalnız istifadəçinin özünə məxsus resurs üzərində · **İştirakçı** — yalnız thread-in iştirakçısı

| # | Əməliyyat | Guest | User | Moderator | Admin |
|---|---|:-:|:-:|:-:|:-:|
| **Hesab** |||||
| 1 | Qeydiyyat, email təsdiqi, şifrə bərpası | ✅ | — | — | — |
| 2 | Login / refresh / logout | ✅ (login) | ✅ | ✅ | ✅ |
| 3 | Öz profilini görmək və redaktə etmək | ❌ | Öz | Öz | Öz |
| 4 | Şifrəni dəyişmək, hesabı silmək | ❌ | Öz | Öz | Öz |
| **Soraqçalar** |||||
| 5 | Aktiv soraqçaları oxumaq | ✅ | ✅ | ✅ | ✅ |
| 6 | Soraqça yaratmaq, redaktə etmək, deaktiv etmək | ❌ | ❌ | ❌ | ✅ |
| **Elanlar** |||||
| 7 | Active elanların siyahısı və detalı | ✅ | ✅ | ✅ | ✅ |
| 8 | Draft / Pending / Rejected / Expired elanı görmək | ❌ | Öz | ✅ | ✅ |
| 9 | Elan yaratmaq | ❌ | ✅ | ✅ | ✅ |
| 10 | Elanı redaktə etmək, şəkil əlavə etmək və silmək | ❌ | Öz | Öz | Öz |
| 11 | Elanı moderasiyaya göndərmək, Expired elanı yeniləmək | ❌ | Öz | Öz | Öz |
| 12 | Elanı silmək | ❌ | Öz | Öz | Öz |
| 13 | Satıcının telefonunu görmək | ❌ | ✅ | ✅ | ✅ |
| 14 | Elandan şikayət etmək | ❌ | ✅ (özününkündən başqa) | ✅ | ✅ |
| **Moderasiya** |||||
| 15 | Pending növbəsini görmək | ❌ | ❌ | ✅ | ✅ |
| 16 | Elanı təsdiqləmək / səbəb ilə rədd etmək | ❌ | ❌ | ✅ (özününkündən başqa) | ✅ (özününkündən başqa) |
| 17 | Active elanı səbəb ilə dərcdən çıxarmaq (→ Rejected) | ❌ | ❌ | ✅ | ✅ |
| 18 | Şikayətlərə baxmaq və onları bağlamaq | ❌ | ❌ | ✅ | ✅ |
| 19 | Şikayət edilmiş thread-in mesajlarını oxumaq (audit edilir) | ❌ | ❌ | ✅ | ✅ |
| **Seçilmişlər və saxlanmış axtarış** |||||
| 20 | Seçilmişlərə əlavə etmək / çıxarmaq / siyahını görmək | ❌ | Öz | Öz | Öz |
| 21 | Saxlanmış axtarış yaratmaq / redaktə etmək / silmək | ❌ | Öz | Öz | Öz |
| 22 | In-app bildirişləri görmək və oxunmuş qeyd etmək | ❌ | Öz | Öz | Öz |
| **Mesajlar** |||||
| 23 | Elan üzrə satıcıya ilk mesajı yazmaq | ❌ | ✅ (özününkündən başqa) | ✅ | ✅ |
| 24 | Thread-i və mesajları oxumaq, cavab yazmaq | ❌ | İştirakçı | İştirakçı | İştirakçı |
| 25 | Başqa istifadəçini bloklamaq (mesaj üçün) | ❌ | ✅ | ✅ | ✅ |
| 26 | Thread-dən şikayət etmək | ❌ | İştirakçı | İştirakçı | İştirakçı |
| **Admin** |||||
| 27 | İstifadəçilərin siyahısı və detalları | ❌ | ❌ | ❌ | ✅ |
| 28 | İstifadəçini bloklamaq / blokdan çıxarmaq | ❌ | ❌ | ❌ | ✅ |
| 29 | Rol təyin etmək / ləğv etmək | ❌ | ❌ | ❌ | ✅ |
| 30 | Audit log-a baxmaq | ❌ | ❌ | ❌ | ✅ |

Matrisdən kənar qaydalar:
- R-06. Admin başqasının elanını **redaktə edə bilməz**. O, yalnız moderasiya hüquqlarından istifadə edir (17-ci sətir).
- R-07. Moderator və Admin başqa istifadəçilərin thread-lərini yalnız həmin thread-dən şikayət olunubsa oxuya bilər (19-cu sətir).

---

## 3. Funksional tələblər

Format: **User story** → **Qəbul kriteriyaları (AC)**. AC-lərdəki xəta kodları sabitdir (bax: SEC-ERR).

### 3.1 Autentifikasiya və hesab

#### FR-AUTH-01 Qeydiyyat
*Guest olaraq email və şifrə ilə qeydiyyatdan keçmək istəyirəm ki, elan yerləşdirə bilim.*
- AC1. Sahələr: email (MUST), şifrə (MUST), ad (MUST, 2–50 simvol), əlaqə telefonu (optional, `+994XXXXXXXXX` formatında), istifadə şərtləri ilə razılıq (MUST, `true` olmalıdır).
- AC2. Email normallaşdırılır (trim, kiçik hərf) və unikal olmalıdır.
- AC3. Şifrə SEC-AUTH-01 siyasətinə uymalıdır, əks halda `VALIDATION_FAILED` qaytarılır və sahə üzrə xəta göstərilir.
- AC4. Email artıq qeydiyyatdadırsa, cavab uğurlu qeydiyyatın cavabı ilə **eynidir** (user enumeration qarşısı). Mövcud hesabın sahibinə "kimsə bu email ilə qeydiyyatdan keçməyə çalışdı" məktubu göndərilir.
- AC5. Hesab "təsdiqlənməmiş" statusunda yaradılır, istifadəçiyə təsdiq linki göndərilir.
- AC6. Yeni istifadəçiyə yalnız `User` rolu verilir. Request-də rol və ya başqa sahə göndərilsə belə nəzərə alınmır (mass assignment, SEC-INP-03).
- AC7. MVP-də qeydiyyat bot-lara qarşı yalnız rate limit ilə qorunur (SEC-RATE-02), CAPTCHA yoxdur. CAPTCHA və ya digər bot qarşısı mexanizm barədə qərar production launch-dan əvvəl verilir; qeydiyyat endpoint-i elə qurulmalıdır ki, bu yoxlama sonradan breaking change olmadan əlavə edilə bilsin (Q13).

#### FR-AUTH-02 Email təsdiqi
*Yeni istifadəçi olaraq email-imi təsdiqləmək istəyirəm ki, hesabım aktivləşsin.*
- AC1. Təsdiq tokeni təsadüfi yaradılır (≥ 128 bit), birdəfəlikdir və 24 saat etibarlıdır. Serverdə yalnız hash-i saxlanılır.
- AC2. Etibarlı token ilə hesab təsdiqlənir. Token istifadə olunmuşdursa, vaxtı keçibsə və ya yanlışdırsa, `TOKEN_INVALID_OR_EXPIRED` qaytarılır.
- AC3. Təsdiq məktubunu yenidən göndərmək mümkündür. Rate limit tətbiq olunur, cavab email-in mövcud olub-olmamasından asılı olmayaraq eynidir. Yeni token köhnəsini etibarsız edir.
- AC4. 7 gün ərzində təsdiqlənməyən hesablar SHOULD avtomatik silinsin.

#### FR-AUTH-03 Login
*İstifadəçi olaraq email və şifrə ilə daxil olmaq istəyirəm.*
- AC1. Uğurlu login access token və refresh token qaytarır (SEC-AUTH-04, SEC-AUTH-05). Access token response body-də qaytarılır. Refresh token web client üçün yalnız cookie-də verilir və body-də qaytarılmır (SEC-NET-04, Q2).
- AC2. Yanlış email və yanlış şifrə üçün cavab, mesaj və cavab müddəti eynidir: `INVALID_CREDENTIALS`.
- AC3. Email təsdiqlənməyibsə `EMAIL_NOT_CONFIRMED` qaytarılır. Bu kod yalnız şifrə düzgün olduqda verilir.
- AC4. Hesab bloklanıbsa `ACCOUNT_BLOCKED` qaytarılır (yalnız şifrə düzgün olduqda).
- AC5. Hesab lockout-dadırsa `ACCOUNT_LOCKED_OUT` qaytarılır, cavabda kilidin bitmə vaxtı göstərilmir (SEC-AUTH-03).
- AC6. Uğurlu və uğursuz login cəhdləri audit olunur (SEC-LOG-03).

#### FR-AUTH-04 Refresh token
*İstifadəçi olaraq hər 15 dəqiqədən bir yenidən login etmək istəmirəm.*
- AC1. Etibarlı refresh token yeni access token və **yeni** refresh token qaytarır. Köhnə refresh token həmin anda etibarsız olur (rotation). Web client refresh tokeni cookie ilə göndərir, yeni refresh token cookie-yə yazılır, yeni access token body-də qaytarılır (SEC-NET-04).
- AC2. Artıq istifadə olunmuş refresh token yenidən göndərilərsə, həmin token ailəsinin bütün tokenləri ləğv edilir, istifadəçi bütün sessiyalardan çıxır və hadisə audit olunur (reuse detection).
- AC3. Bloklanmış, silinmiş və ya şifrəsi dəyişdirilmiş istifadəçinin refresh tokeni qəbul edilmir.

#### FR-AUTH-05 Logout
- AC1. Logout cari refresh tokeni ləğv edir və web client-də refresh token cookie-sini silir.
- AC2. "Bütün cihazlardan çıx" əməliyyatı istifadəçinin bütün refresh tokenlərini ləğv edir.
- AC3. Access token öz qısa ömrünün sonuna qədər etibarlı qala bilər (SEC-AUTH-04). Bu qəbul edilmiş riskdir.

#### FR-AUTH-06 Şifrə bərpası
*Şifrəmi unutmuş istifadəçi olaraq email vasitəsilə yeni şifrə təyin etmək istəyirəm.*
- AC1. Bərpa sorğusunun cavabı email-in mövcud olub-olmamasından asılı olmayaraq eynidir.
- AC2. Bərpa tokeni birdəfəlikdir, 1 saat etibarlıdır, ≥ 128 bit təsadüfi dəyərdir və serverdə hash kimi saxlanılır. Yeni sorğu əvvəlki tokeni etibarsız edir.
- AC3. Yeni şifrə SEC-AUTH-01 siyasətinə uymalıdır.
- AC4. Şifrə uğurla dəyişdikdən sonra istifadəçinin bütün refresh tokenləri ləğv edilir, lockout sayğacı sıfırlanır və istifadəçiyə "şifrəniz dəyişdirildi" məktubu göndərilir.

#### FR-AUTH-07 Şifrə dəyişmə (login olmuş istifadəçi)
- AC1. Cari şifrə tələb olunur.
- AC2. Uğurlu dəyişiklikdən sonra cari sessiyadan başqa bütün refresh tokenlər ləğv edilir və bildiriş məktubu göndərilir.

#### FR-ACC-01 Profil
- AC1. İstifadəçi öz adını və əlaqə telefonunu dəyişə bilər. Telefon təsdiqlənmir, yalnız `+994XXXXXXXXX` formatı qəbul edilir (Q20).
- AC2. Email dəyişmə MVP-də yoxdur (Q14).
- AC3. Profil cavabında şifrə hash-i, token, lockout sayğacı kimi daxili sahələr qaytarılmır.
- AC4. İstifadəçi profil ayarları ilə bu email bildirişlərini söndürə və yandıra bilər: elanın bitməsinə 3 gün qalmış xəbərdarlıq (ST-04) və yeni mesaj (FR-MSG-02 AC7). Default olaraq hər ikisi aktivdir. In-app bildirişlər və təhlükəsizlik məktubları (şifrə dəyişmə, lockout və s.) söndürülmür (Q17).

#### FR-ACC-02 Hesabın silinməsi
*İstifadəçi olaraq hesabımı və şəxsi məlumatlarımı silmək istəyirəm.*
- AC1. Cari şifrə ilə təsdiq tələb olunur.
- AC2. Hesab dərhal deaktiv olur, bütün tokenlər ləğv edilir, istifadəçinin bütün elanları soft delete edilir və saxlanmış axtarışları silinir.
- AC3. Şəxsi məlumatlar (ad, email, telefon) hesab silindikdən 30 gün sonra anonimləşdirilir (SEC-PII-05, Q11).
- AC4. Mesajlar qarşı tərəf üçün qalır, müəllif isə "Silinmiş istifadəçi" kimi göstərilir.
- AC5. Əməliyyat audit olunur.

### 3.2 Soraqçalar

#### FR-DICT-01 Soraqçaların oxunması
*Elan yaradan və ya axtarış edən istifadəçi olaraq dəyərləri siyahıdan seçmək istəyirəm.*
- AC1. Soraqçalar: **Marka**, **Model** (bir markaya bağlıdır), **Kuzov**, **Yanacaq**, **Sürətlər qutusu**, **Şəhər**.
- AC2. Hər dəyərin sabit identifikatoru, `nameAz`, `nameEn`, `isActive` və `sortOrder` sahələri var.
- AC3. Public endpoint-lər yalnız aktiv dəyərləri qaytarır. Modellər markaya görə filtrlənir.
- AC4. Soraqça cavabları keşlənə bilər (HTTP cache header-ləri ilə). Admin dəyişiklik edəndən sonra yeni məlumat ən gec 5 dəqiqəyə görünməlidir.

#### FR-DICT-02 Soraqçaların idarəsi (Admin)
- AC1. Admin dəyər yarada, adını dəyişə, sırasını dəyişə, deaktiv və yenidən aktiv edə bilər.
- AC2. Bir soraqçanın daxilində ad unikal olmalıdır (case-insensitive). Model üçün unikallıq marka daxilində yoxlanılır.
- AC3. Elanlarda istifadə olunan dəyər fiziki silinmir, yalnız deaktiv edilir. Deaktiv dəyər yeni elanda və ya redaktədə seçilə bilmir, amma mövcud elanlarda görünməyə davam edir.
- AC4. Heç bir elanda istifadə olunmayan dəyər silinə bilər.
- AC5. Bütün dəyişikliklər audit olunur.

### 3.3 Elanlar

#### 3.3.1 Elanın sahələri

| Sahə | Məcburi | Qayda |
|---|:-:|---|
| Marka, Model | ✅ | Aktiv soraqça dəyəri olmalıdır, model seçilmiş markaya aid olmalıdır |
| Buraxılış ili | ✅ | 1950 – (cari il + 1) |
| Kuzov, Yanacaq, Sürətlər qutusu | ✅ | Aktiv soraqça dəyəri |
| Şəhər | ✅ | Aktiv soraqça dəyəri |
| Yürüş (km) | ✅ | 0 – 2 000 000, tam ədəd |
| Mühərrik həcmi (sm³) | ✅* | 50 – 10 000. *Elektromobil üçün (yanacaq = elektrik) tələb olunmur |
| Güc (a.g.) | ❌ | 1 – 2 000 |
| Rəng | ❌ | Sabit siyahıdan (enum, soraqça deyil; Q8) |
| Ötürücü | ❌ | Sabit siyahıdan: `FWD` (ön), `RWD` (arxa), `AWD` (tam) (Q8) |
| Qiymət | ✅ | > 0, ən çox 2 onluq rəqəm, yuxarı hədd konfiqurasiyadan oxunur |
| Valyuta | ✅ | `AZN`, `USD`, `EUR` |
| Təsvir | ❌ | 0 – 3000 simvol, düz mətn (HTML qəbul edilmir və icra olunmur) |
| VIN | ❌ | 17 simvol, `I`, `O`, `Q` hərfləri olmadan. Active elanda hamıya tam göstərilir (Q7) |
| Şəkillər | Göndərmək üçün ≥ 1 | 1 – 10, bax FR-IMG |

Rəng və ötürücü dəyərlərinin tam siyahısı API sənədində saxlanılır. Siyahıya dəyişiklik yeni release tələb edir, Admin onu API ilə idarə etmir.

Sistem sahələri (client tərəfindən təyin edilə bilməz): id, sahib, status, rədd səbəbi, yaradılma/dərc/bitmə tarixləri, AZN ekvivalenti, silinmə əlaməti.

#### 3.3.2 Status axını

```
            submit                 approve
  Draft ───────────► Pending ───────────────► Active ──── 30 gün ───► Expired
    ▲                 │   ▲                    │  │                      │
    │    withdraw     │   │  əhəmiyyətli       │  │ moderator            │ renew
    └─────────────────┘   │  redaktə           │  │ dərcdən çıxarır      │
                          │◄───────────────────┘  ▼                      │
                          │                    Rejected (səbəb ilə)       │
                 reject   │                       │                      │
         (səbəb ilə) ─────┼──────────────────────►│                      │
                          │◄──── düzəliş + submit ┘                      │
                          │◄─────────────────────────────────────────────┘
```

| Keçid | Kim | Şərt |
|---|---|---|
| (yeni) → Draft | Sahib | Elan yaradılanda |
| Draft → Pending | Sahib | Məcburi sahələr doludur, ≥ 1 şəkil var, aktiv elan limiti aşılmayıb |
| Pending → Draft | Sahib | Elanı moderasiyadan geri çəkmək (withdraw) |
| Pending → Active | Moderator/Admin | Sahib özü deyil (R-02) |
| Pending → Rejected | Moderator/Admin | Səbəb məcburidir (sabit səbəb kodu + 0–500 simvol şərh) |
| Active → Pending | Sistem | Sahib əhəmiyyətli sahəni redaktə etdikdə (FR-LST-03) |
| Active → Rejected | Moderator/Admin | Dərcdən çıxarma, səbəb məcburidir |
| Active → Expired | Sistem | Dərc tarixindən 30 gün sonra |
| Rejected → Pending | Sahib | Redaktədən sonra yenidən göndərmə |
| Expired → Pending | Sahib | "Yenilə" əməliyyatı, limit yoxlanılır |
| İstənilən → (silinmiş) | Sahib | Soft delete. Bu status deyil, əlamətdir |

- ST-01. Burada göstərilməyən hər keçid `INVALID_STATUS_TRANSITION` ilə rədd edilir.
- ST-02. Status keçidləri paralel sorğulara qarşı qorunmalıdır: iki moderator eyni elanı eyni anda emal edə bilməz, ikinci sorğu `CONCURRENCY_CONFLICT` alır.
- ST-03. Hər status keçidi tarixçədə saxlanılır: kim, nə vaxt, haradan → haraya, səbəb.
- ST-04. Sahibə status dəyişikliyi barədə bildiriş göndərilir: Active, Rejected (səbəb ilə), Expired, həmçinin bitməyə 3 gün qalmış xəbərdarlıq. Bitmə xəbərdarlığının email versiyası istifadəçi tərəfindən söndürülə bilər (FR-ACC-01 AC4).
- ST-05. Active → Expired keçidi planlaşdırılmış prosesdə ən gec 1 saat gecikmə ilə baş verməlidir. Axtarış nəticələrində isə bitmə tarixi keçmiş elan dərhal göstərilməməlidir.
- ST-06. Yenidən Active olan elan üçün 30 günlük müddət yenidən başlayır.

#### FR-LST-01 Elan yaratma
*User olaraq avtomobilimi satmaq üçün elan yaratmaq istəyirəm.*
- AC1. Elan Draft statusunda yaranır, sahib cari istifadəçidir.
- AC2. Draft natamam ola bilər, amma hər doldurulmuş sahə validasiyadan keçməlidir.
- AC3. Bir istifadəçinin Draft sayı ən çox 20-dir (spam qarşısı).
- AC4. Request-də status, sahib, tarix və ya id kimi sistem sahələri göndərilsə, onlar nəzərə alınmır (SEC-INP-03).

#### FR-LST-02 Moderasiyaya göndərmə və limit
- AC1. Göndərmə yalnız Draft və ya Rejected statusundan mümkündür. Expired statusundan bu əməliyyat "yenilə" adlanır.
- AC2. İstifadəçinin **Pending + Active** elanlarının sayı 5-dən çox ola bilməz (Q4). Limit aşılarsa `ACTIVE_LISTING_LIMIT_REACHED` qaytarılır.
- AC3. Göndərmə anında bütün məcburi sahələr və ≥ 1 şəkil yoxlanılır.

#### FR-LST-03 Elan redaktəsi
*Sahib olaraq elanımı yeniləmək istəyirəm.*
- AC1. Yalnız sahib redaktə edə bilər. Başqa istifadəçi üçün cavab `LISTING_NOT_FOUND`-dur (SEC-AUTHZ-02).
- AC2. Draft, Rejected və Expired elanları sərbəst redaktə olunur, status dəyişmir.
- AC3. Pending elan redaktə edilə bilməz. Əvvəlcə withdraw edilməlidir (`LISTING_UNDER_REVIEW`).
- AC4. Active elanda **qiymət, valyuta, yürüş, təsvir, şəhər, rəng, güc** dəyişikliyi moderasiyasız tətbiq olunur və elan Active qalır. Qiymətin kəskin dəyişməsi də yenidən moderasiya tələb etmir (Q6).
- AC5. Active elanda **şəkil (əlavə, silmə, sıra dəyişikliyi deyil), marka, model və ya VIN** dəyişərsə, elan avtomatik Pending olur və axtarışdan çıxır.
- AC6. Paralel redaktə optimistic concurrency ilə qorunur. Köhnə versiya ilə göndərilən dəyişiklik `CONCURRENCY_CONFLICT` alır.

#### FR-LST-04 Elanın silinməsi
- AC1. Yalnız sahib silə bilər (və hesab silinəndə sistem). Silmə soft delete-dir.
- AC2. Silinmiş elan axtarışda, detal səhifəsində, seçilmişlərdə və bildirişlərdə görünmür. Public detal sorğusuna cavab `LISTING_NOT_FOUND`-dur.
- AC3. Elan üzrə thread-lər iştirakçılar üçün qalır, elan "silinib" kimi işarələnir və yeni mesaj yazmaq olmur.
- AC4. Şəkillər public girişdən dərhal çıxarılır. Fiziki silinmə elan silindikdən 30 gün sonra baş verir (SEC-PII-05, Q11).

#### FR-LST-05 Elanın baxışı
- AC1. Active elanın detalını hər kəs görə bilər. Cavabda satıcının adı, şəhəri və qeydiyyat tarixi göstərilir, email və telefon göstərilmir.
- AC2. Qiymət orijinal valyutada və AZN ekvivalenti ilə göstərilir, məzənnənin tarixi də qaytarılır.
- AC3. Sahib öz elanını istənilən statusda görür, Rejected elanda rədd səbəbi də göstərilir.
- AC4. Moderator/Admin istənilən statusda elanı görür.

#### FR-LST-06 Satıcının telefonunu göstərmək
*Login olmuş alıcı olaraq satıcıya zəng etmək istəyirəm.*
- AC1. Telefon ayrıca sorğu ilə qaytarılır və bu sorğu yalnız User və yuxarı rollar üçündür. Guest `UNAUTHORIZED` alır.
- AC2. Telefon yalnız Active elan üçün və satıcının telefonu varsa qaytarılır.
- AC3. Hər açılış qeydə alınır: kim, hansı elan, nə vaxt.
- AC4. Rate limit tətbiq olunur (SEC-RATE-01), çünki bu əməliyyat scraping üçün həssasdır.

### 3.4 Şəkillər

#### FR-IMG-01 Şəkil yükləmə
- AC1. Bir elanda 1–10 şəkil ola bilər. 11-ci şəkil `IMAGE_LIMIT_REACHED` ilə rədd edilir.
- AC2. Qəbul olunan formatlar: JPEG, PNG, WebP. Tələblər SEC-FILE-də verilib.
- AC3. Şəkil yalnız sahibin Draft, Rejected, Expired və ya Active elanına yüklənə bilər (Active-də AC5 FR-LST-03 tətbiq olunur).
- AC4. Sahib şəkillərin sırasını dəyişə bilər. Birinci şəkil əsas (cover) şəkildir.
- AC5. Hər şəkil üçün siyahıda göstərmək üçün kiçik versiya (thumbnail) və detal üçün orta versiya SHOULD yaradılsın.
- AC6. Draft-a yüklənib istifadə olunmayan şəkillər (yetim) 24 saatdan sonra silinir.

#### FR-IMG-02 Şəkil silmə
- AC1. Yalnız sahib silə bilər.
- AC2. Göndərilmiş (Pending/Active) elanın son şəklini silmək olmaz (`IMAGE_MIN_REQUIRED`).

### 3.5 Moderasiya və şikayətlər

#### FR-MOD-01 Moderasiya növbəsi
- AC1. Moderator Pending elanları göndərilmə tarixinə görə (köhnədən yeniyə) görür.
- AC2. Moderatorun öz elanları növbədə görünmür.
- AC3. Növbədə şikayət sayı və elanın əvvəlki rədd səbəbləri görünür.
- AC4. Moderasiya SLA-sı: Pending elana göndərilmədən sonra 24 saat ərzində baxılmalıdır (Q15). Bu əməliyyat hədəfidir; moderatorların sayı API tələbi deyil.

#### FR-MOD-02 Təsdiq və rədd
- AC1. Təsdiq elanı Active edir və dərc tarixini təyin edir.
- AC2. Rədd üçün səbəb kodu məcburidir: `INAPPROPRIATE_CONTENT`, `WRONG_CATEGORY`, `BAD_PHOTOS`, `PRICE_UNREALISTIC`, `DUPLICATE`, `CONTACT_INFO_IN_TEXT`, `PROHIBITED`, `OTHER`. `OTHER` üçün şərh məcburidir.
- AC3. Sahib səbəbi görür və elanı düzəldib yenidən göndərə bilər.
- AC4. Hər moderasiya qərarı audit olunur.

#### FR-MOD-03 Elandan şikayət
*User olaraq saxta və ya qeyri-qanuni elandan şikayət etmək istəyirəm.*
- AC1. Yalnız Active elandan, səbəb kodu ilə (+ optional şərh, ən çox 500 simvol) şikayət etmək olur.
- AC2. Bir istifadəçi eyni elandan yalnız bir dəfə şikayət edə bilər. İstifadəçi öz elanından şikayət edə bilməz.
- AC3. Moderator şikayəti "əsassız" kimi bağlaya və ya elanı dərcdən çıxara bilər (Active → Rejected).
- AC4. Şikayət edənin kimliyi elan sahibinə göstərilmir.

### 3.6 Axtarış

#### FR-SRCH-01 Filtrlər
*Alıcı olaraq meyarlarıma uyğun avtomobilləri tapmaq istəyirəm.*
- AC1. Axtarış yalnız Active və silinməmiş elanları qaytarır. Bütün rollar üçün nəticə eynidir.
- AC2. Filtrlər (hamısı optional, AND ilə birləşir):
  - marka (bir və ya bir neçə), model (bir və ya bir neçə, seçilmiş markalara aid olmalıdır)
  - kuzov, yanacaq, sürətlər qutusu, şəhər (hər birində bir neçə dəyər seçmək olar)
  - il: min–max
  - qiymət: min–max, **AZN ekvivalenti** ilə. İstifadəçi USD və ya EUR ilə daxil edərsə, server cari məzənnə ilə AZN-ə çevirir
  - yürüş: min–max
  - mühərrik həcmi: min–max
- AC3. Validasiya: min ≤ max, aralıqlar sahə qaydalarına uyğun olmalıdır, bilinməyən soraqça id-ləri `VALIDATION_FAILED` qaytarır. Çoxdəyərli filtrdə ən çox 20 dəyər ola bilər.
- AC4. Siyahı elementində cover thumbnail, marka/model, il, yürüş, şəhər, qiymət (orijinal və AZN) və dərc tarixi göstərilir. Şəxsi məlumat göstərilmir.

#### FR-SRCH-02 Sıralama
- AC1. Sıralama variantları: `newest` (default, dərc tarixinə görə azalan), `price_asc`, `price_desc` (AZN ekvivalentinə görə), `mileage_asc`, `year_desc`.
- AC2. Sıralama deterministikdir: bərabər dəyərlər üçün əlavə sabit açar istifadə olunur ki, səhifələr arasında təkrar və ya buraxılmış nəticə olmasın.
- AC3. Bilinməyən sıralama dəyəri `VALIDATION_FAILED` qaytarır.

#### FR-SRCH-03 Pagination
- AC1. Default səhifə ölçüsü 20, maksimum 50-dir. 50-dən böyük dəyər `VALIDATION_FAILED` qaytarır (səssizcə kəsilmir).
- AC2. Cavabda pagination metadatası var (ən azı: cari səhifə və ya cursor, səhifə ölçüsü, növbəti səhifənin olub-olmaması). Ümumi say SHOULD qaytarılsın.
- AC3. Dərin pagination məhdudlaşdırılır: offset ilə əldə olunan nəticələr ilk 10 000 elementlə məhduddur (bax: NFR-PAG).

### 3.7 Seçilmişlər

#### FR-FAV-01
*User olaraq bəyəndiyim elanları sonra baxmaq üçün saxlamaq istəyirəm.*
- AC1. İstifadəçi Active elanı seçilmişlərə əlavə edə və çıxara bilər. Əməliyyat idempotentdir: təkrar əlavə xəta vermir.
- AC2. Seçilmişlərin sayı ən çox 200-dür.
- AC3. Siyahı səhifələnir. Artıq Active olmayan elanlar siyahıda "əlçatan deyil" kimi göstərilir, detalları açılmır. Silinmiş elanlar göstərilmir.
- AC4. Başqasının seçilmişlərini görmək mümkün deyil. Elan sahibi elanını kimlərin seçdiyini görmür.

### 3.8 Saxlanmış axtarış və bildirişlər

#### FR-SAVED-01 Saxlanmış axtarış
*User olaraq axtarış filtrlərimi saxlamaq istəyirəm ki, uyğun yeni elanlar haqqında xəbər alım.*
- AC1. Saxlanmış axtarış FR-SRCH-01-dəki filtrlərdən ibarətdir, adı var (1–50 simvol) və bildiriş rejimi seçilir: `instant`, `daily`, `off`.
- AC2. Bir istifadəçinin ən çox 10 saxlanmış axtarışı ola bilər (`SAVED_SEARCH_LIMIT_REACHED`).
- AC3. Filtrlər yadda saxlananda validasiya olunur.
- AC4. Sahib saxlanmış axtarışı redaktə edə, silə və rejimini dəyişə bilər.

#### FR-NOTIF-01 Uyğun yeni elan haqqında bildiriş
- AC1. Trigger: elan **Active statusuna keçəndə** (ilk təsdiq və ya yenidən təsdiq).
- AC2. Elan saxlanmış axtarışın bütün filtrlərinə uyğundursa, sahibinə bildiriş yaradılır.
- AC3. Bildiriş göndərilmir, əgər: elan istifadəçinin özünə məxsusdur; istifadəçi bloklanıb, silinib və ya email-i təsdiqlənməyib; rejim `off`-dur.
- AC4. Eyni (saxlanmış axtarış, elan) cütü üçün bildiriş yalnız bir dəfə yaradılır (idempotentlik).
- AC5. `instant` rejimində bildiriş ən gec 15 dəqiqəyə göndərilir. `daily` rejimində gündə bir dəfə, Bakı vaxtı ilə 09:00-da toplu məktub göndərilir. Digest-də hər axtarış üçün ən çox 20 elan olur, qalanları üçün "daha çox" linki verilir.
- AC6. `instant` rejimində bir istifadəçiyə saatda ən çox 10 email göndərilir. Limitdən artıq bildirişlər növbəti digest-ə keçir.
- AC7. Hər email-də bir kliklə abunəlikdən çıxma linki var. Link həmin saxlanmış axtarışı `off` edir və login tələb etmir. Bunun üçün SEC-AUTH-07-yə uyğun token istifadə olunur: kriptoqrafik təsadüfi dəyərdir, serverdə yalnız hash-i saxlanılır, vaxtı məhduddur. Təkrar istifadə xəta vermir (əməliyyat idempotentdir) (Q22).
- AC8. Email göndərmə uğursuz olarsa, təkrar cəhd edilir (eksponensial gecikmə ilə, ən çox 5 dəfə). Bu, elanın təsdiqlənməsi əməliyyatını bloklamamalıdır.

#### FR-NOTIF-02 In-app bildirişlər
- AC1. Bildiriş növləri: saxlanmış axtarışa uyğun elan, elanın statusunun dəyişməsi (ST-04), yeni mesaj.
- AC2. İstifadəçi bildirişlərini səhifələnmiş şəkildə görür, oxunmamışların sayını alır, bildirişi tək-tək və ya hamısını oxunmuş qeyd edir.
- AC3. Bildirişlər 90 gündən sonra silinir.

### 3.9 Mesajlaşma

#### FR-MSG-01 Satıcıya yazmaq
*Alıcı olaraq elan üzrə satıcıya sual vermək istəyirəm.*
- AC1. Thread unikal olaraq (elan, alıcı) cütü ilə müəyyən edilir. Eyni alıcı eyni elan üçün ikinci thread aça bilmir, mövcud thread qaytarılır.
- AC2. Yalnız Active elan üçün yeni thread açmaq olar. Sahib öz elanına thread aça bilməz.
- AC3. Mesaj: düz mətn, 1–2000 simvol, kənar boşluqlar silinir. HTML icra olunmur.
- AC4. Thread-də ən çox 2 iştirakçı var: alıcı və elanın sahibi.

#### FR-MSG-02 Oxuma və cavab
- AC1. Thread-i və mesajlarını yalnız iştirakçılar görür. Başqası üçün cavab `THREAD_NOT_FOUND`-dur (SEC-AUTHZ-02). İstisna: FR-MSG-04.
- AC2. İstifadəçi öz thread-lərinin siyahısını son mesaj tarixinə görə sıralanmış və səhifələnmiş şəkildə görür. Hər thread-də oxunmamış mesaj sayı göstərilir.
- AC3. Mesajlar səhifələnir. Client yeni mesajları "müəyyən andan sonra gələnlər" sorğusu ilə polling edərək alır.
- AC4. Thread açılanda qarşı tərəfin mesajları oxunmuş qeyd olunur.
- AC5. Elan Active deyilsə (Expired, Rejected, silinmiş), mövcud thread-də yazışma davam edə bilər, amma silinmiş elanın thread-ində yeni mesaj yazmaq olmur (FR-LST-04 AC3).
- AC6. Mesaj göndərildikdən sonra redaktə edilə və silinə bilməz (MVP).
- AC7. Yeni mesaj alana in-app bildiriş yaradır. Email bildirişi SHOULD olsun: thread üzrə 30 dəqiqədə ən çox bir dəfə. İstifadəçi bu email bildirişini söndürə bilər (FR-ACC-01 AC4).

#### FR-MSG-03 Bloklama
- AC1. İstifadəçi başqa istifadəçini bloklaya və blokdan çıxara bilər.
- AC2. Bloklanmış şəxs blok edənə yeni thread aça və mövcud thread-də mesaj yaza bilmir (`USER_BLOCKED_YOU`). Bloklayan da bloklananla yazışa bilmir.
- AC3. Bloklama barədə bloklanana bildiriş göndərilmir.

#### FR-MSG-04 Thread-dən şikayət
- AC1. İştirakçı thread-dən səbəb kodu ilə şikayət edə bilər.
- AC2. Şikayətdən sonra Moderator/Admin həmin thread-in mesajlarını oxuya bilər. Hər oxuma audit olunur (kim, hansı thread, nə vaxt).
- AC3. Şikayət olunmamış thread-ləri heç bir rol oxuya bilməz.

### 3.10 Valyuta məzənnəsi

#### FR-FX-01 Məzənnələrin alınması
- AC1. USD/AZN və EUR/AZN məzənnələri hər gün Azərbaycan Mərkəzi Bankının (CBAR) rəsmi məzənnələrindən background job ilə alınır və tarixi ilə birgə cədvəldə saxlanılır.
- AC2. Job gündə ən azı bir dəfə, Bakı vaxtı ilə rəsmi məzənnələr dərc olunduqdan sonra işləyir. Uğursuz olarsa, gün ərzində təkrar cəhd edir (məs. hər saat).
- AC3. Məzənnə alına bilmirsə, **son uğurlu məzənnə** istifadə olunur və hadisə `Warning` səviyyəsində log-a yazılır.
- AC4. Son uğurlu məzənnə 3 gündən köhnədirsə, hadisə `Error` səviyyəsində log-a yazılır və health check "degraded" statusu göstərir (NFR-HC).
- AC5. CBAR-dan gələn cavab xarici və etibarsız data kimi validasiya olunur: format, valyuta kodları, müsbət və ağlabatan diapazonda dəyər (SEC-EXT-01). Validasiyadan keçməyən cavab qəbul edilmir və AC3 tətbiq olunur.
- AC6. Sistemdə heç vaxt məzənnə olmayıbsa (ilk işə salınma), USD/EUR elanları göndərilə bilər, amma AZN ekvivalenti hesablanana qədər qiymət filtrində iştirak etmir. Bu hal `Error` kimi log-a yazılır.

#### FR-FX-02 AZN ekvivalenti
- AC1. Elan orijinal məbləği və valyutanı saxlayır. AZN ekvivalenti ən son məzənnə ilə hesablanır.
- AC2. Yeni məzənnə yükləndikdən sonra bütün USD/EUR elanlarının AZN ekvivalenti ən gec 1 saat ərzində yenilənir. Bu, axtarış və sıralamaya da təsir edir.
- AC3. Hesablama və yuvarlaqlaşdırma `decimal` dəqiqliyi ilə aparılır və 2 onluq rəqəmə yuvarlaqlaşdırılır. Yuvarlaqlaşdırma qaydası sabitdir və sənədləşdirilir.
- AC4. API cavabında istifadə olunan məzənnənin tarixi qaytarılır.

### 3.11 Admin

#### FR-ADM-01 İstifadəçinin bloklanması
- AC1. Admin istifadəçini səbəb göstərməklə (məcburi, 1–500 simvol) bloklaya və blokdan çıxara bilər.
- AC2. Bloklanma anında: istifadəçinin bütün refresh tokenləri ləğv edilir, Active və Pending elanları axtarışdan gizlədilir (statusları dəyişmir, ayrıca "sahib bloklanıb" əlaməti qoyulur), istifadəçi mesaj göndərə bilmir və saxlanmış axtarış bildirişləri dayandırılır.
- AC3. Blokdan çıxanda elanlar yenidən görünür. Bu müddətdə bitmə tarixi keçmiş elanlar Expired olur.
- AC4. Bloklanmış istifadəçinin access tokeni qalan ömrü ərzində (≤ 15 dəq) qəbul edilməməlidir. Bunun üçün hər sorğuda istifadəçi statusu yoxlanılır və ya ekvivalent mexanizm tətbiq edilir (SEC-AUTH-06).
- AC5. Əməliyyat audit olunur. Admin özünü bloklaya bilməz (R-05).

#### FR-ADM-02 Rol idarəsi
- AC1. Admin istifadəçiyə Moderator və ya Admin rolu verə və onu ləğv edə bilər.
- AC2. Rol dəyişikliyi istifadəçinin cari sessiyalarına ən gec 15 dəqiqəyə təsir edir. Rol azaldılanda refresh tokenlər ləğv edilir.
- AC3. Əməliyyat audit olunur.

#### FR-ADM-03 İstifadəçilərin siyahısı
- AC1. Admin istifadəçiləri email, status (aktiv, bloklanmış, təsdiqlənməmiş) və rola görə axtarır. Siyahı səhifələnir.
- AC2. Cavabda şifrə hash-i, token və bu kimi məlumatlar qaytarılmır.

---

## 4. Təhlükəsizlik tələbləri

OWASP API Security Top 10 (2023) ilə uyğunluq cədvəli bölmənin sonundadır (4.12).

### 4.1 Autentifikasiya

- **SEC-AUTH-01 Şifrə siyasəti** (NIST SP 800-63B əsasında):
  - uzunluq: minimum 10, maksimum 128 simvol; bütün Unicode simvollarına icazə var, boşluq da daxil olmaqla;
  - məcburi kompozisiya qaydası yoxdur ("1 böyük hərf + 1 rəqəm" kimi); bunun əvəzinə şifrə ən çox istifadə olunan / sızmış şifrələr siyahısında (ən azı top 100 000) olmamalıdır. Yoxlama yalnız serverdə saxlanılan lokal siyahı ilə aparılır, şifrə və ya onun hash-i (prefiksi də daxil olmaqla) xarici servisə göndərilmir (Q1);
  - şifrə email-ə və ya istifadəçi adına bərabər və ya onu ehtiva edən ola bilməz;
  - dövri məcburi dəyişmə tələb olunmur.
- **SEC-AUTH-02 Şifrə hash-i**: şifrələr yalnız adaptiv, salt-lı hash ilə saxlanılır. Minimum: Argon2id (m ≥ 19 MiB, t ≥ 2, p = 1), PBKDF2-HMAC-SHA512 (≥ 210 000 iterasiya) və ya PBKDF2-HMAC-SHA256 (≥ 600 000 iterasiya), hər istifadəçi üçün unikal salt (Q23). Parametrlər versiyalanır ki, login zamanı rehash etmək mümkün olsun. Şifrə heç vaxt açıq və ya geri çevrilə bilən şəkildə saxlanılmır.
- **SEC-AUTH-03 Lockout**: hesab üzrə 15 dəqiqədə 5 ardıcıl uğursuz cəhddən sonra hesab 15 dəqiqəlik kilidlənir. Hər növbəti lockout müddəti ikiqat artır (ən çox 24 saat). Uğurlu login sayğacı sıfırlayır. Lockout istifadəçiyə email ilə bildirilir. Lockout IP əsaslı rate limit-dən (SEC-RATE) ayrıdır, ikisi birlikdə tətbiq olunur.
- **SEC-AUTH-04 Access token**: ömrü 15 dəqiqə; imzalanmış, imza alqoritmi serverdə sabit təyin olunub (`none` və alqoritm dəyişdirmə qəbul edilmir). İçində yalnız minimum claim-lər olur (istifadəçi id, rollar, token id, issuer, audience, exp); şəxsi məlumat (email, telefon) olmur. Hər sorğuda issuer, audience, müddət və imza yoxlanılır; saat sürüşməsi (clock skew) ≤ 30 saniyə.
- **SEC-AUTH-05 Refresh token**: təsadüfi (≥ 256 bit), opaque dəyərdir, serverdə yalnız hash-i saxlanılır. Ömrü 14 gün (sliding), mütləq maksimum 60 gün (Q3). Client-ə ötürülmə qaydası SEC-NET-04-də verilib. Hər istifadədə rotation edilir, reuse aşkarlananda bütün token ailəsi ləğv edilir (FR-AUTH-04). Logout, şifrə dəyişmə, bloklanma, hesabın silinməsi və rolun azaldılması tokenləri ləğv edir. Bir istifadəçinin eyni vaxtda ən çox 10 aktiv sessiyası ola bilər; ən köhnəsi avtomatik ləğv edilir.
- **SEC-AUTH-06 Status yoxlaması**: bloklanmış və ya silinmiş istifadəçinin access tokeni ömrü bitənə qədər də qəbul edilməməlidir (FR-ADM-01 AC4).
- **SEC-AUTH-07 Birdəfəlik tokenlər** (email təsdiqi, şifrə bərpası, abunəlikdən çıxma): kriptoqrafik təsadüfi dəyər, serverdə hash kimi saxlanılır, birdəfəlik istifadə, vaxt məhdudiyyəti (FR-AUTH-02/06), müqayisə sabit vaxtda (constant-time) aparılır.
- **SEC-AUTH-08 User enumeration qarşısı**: qeydiyyat, login, şifrə bərpası və təsdiq məktubunun yenidən göndərilməsi endpoint-ləri hesabın mövcud olub-olmamasını cavabın mətni, status kodu və ya cavab müddəti ilə açıqlamamalıdır.

### 4.2 Avtorizasiya

- **SEC-AUTHZ-01 Rol yoxlaması** (BFLA): hər endpoint üçün icazə siyasəti açıq şəkildə təyin olunur. Default qayda "rədd et"dir: siyasəti olmayan endpoint anonim girişə açıq deyil. Public endpoint-lər açıq şəkildə işarələnir. Admin/Moderator endpoint-ləri yalnız URL-in gizli olmasına deyil, rol yoxlamasına əsaslanır.
- **SEC-AUTHZ-02 Resurs sahibliyi** (BOLA/IDOR): elan, şəkil, thread, mesaj, seçilmiş, saxlanmış axtarış və bildirişlə bağlı hər əməliyyatda resursun cari istifadəçiyə aid olub-olmaması (və ya onun iştirakçı olması) **serverdə** yoxlanılır. İstifadəçi id-si request body, query və ya route-dan götürülmür, yalnız autentifikasiya olunmuş kimlikdən götürülür. Başqasının qeyri-public resursuna sorğu `*_NOT_FOUND` (HTTP 404) qaytarır ki, resursun mövcudluğu açıqlanmasın.
- **SEC-AUTHZ-03 Identifikatorlar**: public API-də resurs id-ləri ardıcıl tam ədəd olmamalıdır, təxmin edilə bilməyən olmalıdır. Bu SEC-AUTHZ-02-ni əvəz etmir, əlavə qoruma qatıdır.
- **SEC-AUTHZ-04 Sahə səviyyəsində avtorizasiya** (BOPLA, oxuma): hər endpoint-in cavabı ayrıca output modeli ilə formalaşır. Daxili sahələr (şifrə hash-i, token, lockout sayğacı, moderator qeydləri, şikayətçinin kimliyi, başqasının email/telefonu) heç vaxt qaytarılmır. Daxili entity birbaşa serializasiya olunmur.
- **SEC-AUTHZ-05 Biznes axınlarının qorunması**: telefonun göstərilməsi, mesaj göndərmə, qeydiyyat və şikayət kimi axınlar scraping və spam-a qarşı rate limit və limitlərlə qorunur (SEC-RATE, FR limitləri).
- **SEC-AUTHZ-06 Avtorizasiya testləri**: hər qorunan endpoint üçün mənfi testlər məcburidir (bax: NFR-TEST-03).

### 4.3 Input validation və mass assignment

- **SEC-INP-01**: bütün input serverdə validasiya olunur (client validasiyasına etibar edilmir): tip, uzunluq, format, diapazon və icazə verilən dəyərlər siyahısı (enum, soraqça id-ləri).
- **SEC-INP-02**: request body ölçüsü məhdudlaşdırılır: JSON üçün 64 KB, şəkil yükləmə üçün SEC-FILE-02. Massivlərin uzunluğu və JSON-un iç-içəlik dərinliyi məhdudlaşdırılır. Bilinməyən JSON sahələri nəzərə alınmır.
- **SEC-INP-03 Mass assignment**: hər yazma əməliyyatı üçün ayrıca input modeli (DTO) istifadə olunur. Bu model yalnız istifadəçinin dəyişə biləcəyi sahələri ehtiva edir. Sistem sahələri (id, sahib, status, rol, tarix, AZN ekvivalenti, təsdiq əlaməti) input modellərində yoxdur.
- **SEC-INP-04**: verilənlər bazası sorğuları yalnız parametrləşdirilmiş şəkildə qurulur. İstifadəçi inputu ilə dinamik sorğu, sıralama sahəsi və ya filtr adı qurulmur; sıralama və filtrlər yalnız icazə verilmiş siyahıdan seçilir.
- **SEC-INP-05**: mətn sahələri (təsvir, mesaj, ad, şərh) düz mətn kimi saxlanılır və qaytarılır. Unicode normallaşdırılır, idarəedici simvollar (yeni sətir və tab-dan başqa) silinir. XSS qarşısı frontend-in output encoding-i ilə təmin edilir; API HTML render etmir.
- **SEC-INP-06**: email şablonlarına daxil edilən istifadəçi məlumatı HTML-encode edilir. Email header-lərinə istifadəçi inputu yerləşdirilmir (header injection qarşısı).
- **SEC-INP-07**: API yalnız `application/json` (və şəkil üçün `multipart/form-data`) qəbul edir. Başqa content type `415` qaytarır.

### 4.4 Fayl yükləmə

- **SEC-FILE-01 Tip yoxlaması**: faylın tipi **magic bytes** ilə müəyyən edilir (JPEG `FF D8 FF`, PNG `89 50 4E 47 0D 0A 1A 0A`, WebP `RIFF....WEBP`). Fayl adının uzantısına və client-in göndərdiyi `Content-Type`-a etibar edilmir. Bundan əlavə, fayl şəkil kitabxanası ilə tam decode olunmalıdır; decode olunmayan fayl rədd edilir (`IMAGE_INVALID`).
- **SEC-FILE-02 Ölçü limiti**: bir fayl ≤ 10 MB, bir sorğuda ≤ 10 fayl. Ölçü stream oxunarkən yoxlanılır; limiti aşan sorğu tam oxunmadan dayandırılır. Şəkil ölçüləri ≤ 8000 × 8000 piksel və ≤ 40 megapiksel olmalıdır (decompression bomb qarşısı).
- **SEC-FILE-03 Adın yenidən yaradılması**: saxlanılan fayl adı server tərəfindən təsadüfi yaradılır (məs. təsadüfi id + uzantı). Orijinal fayl adı fayl sistemində və URL-də istifadə olunmur. Path traversal (`../`) mümkün deyil.
- **SEC-FILE-04 Metadata təmizlənməsi**: şəkil server tərəfindən yenidən encode olunur. Bütün EXIF, XMP, IPTC metadatası, o cümlədən GPS koordinatları, kamera seriya nömrəsi və tarix silinir. Orientasiya saxlanmadan əvvəl tətbiq olunur. Orijinal fayl saxlanılmır.
- **SEC-FILE-05 Saxlama və təqdim**: fayllar icra oluna bilən qovluqdan kənarda saxlanılır. Cavab server tərəfindən təyin olunmuş `Content-Type` və `X-Content-Type-Options: nosniff` ilə qaytarılır. SVG, GIF, HEIC və digər formatlar qəbul edilmir.
- **SEC-FILE-06**: Draft elanın şəkillərinə yalnız sahibi və moderatorlar baxa bilər. MVP-də bu, şəkil URL-lərinin təxmin edilə bilməməsi ilə təmin olunur: URL-də ≥ 128 bit təsadüfi identifikator olur, elan id-si və ya ardıcıl nömrə olmur. Şəkillər siyahı (directory listing) ilə əlçatan deyil. Vaxtı məhdud imzalı URL MVP-də tələb olunmur (Q10).
- **SEC-FILE-07**: MVP-də şəkillər antivirus ilə yoxlanılmır. Risk SEC-FILE-01 (tam decode) və SEC-FILE-04 (yenidən encode, orijinal saxlanılmır) ilə azaldılır (Q9).

### 4.5 Rate limiting

Bütün limitlər konfiqurasiyadan oxunur. Limit aşılarsa `429 Too Many Requests`, `RATE_LIMITED` kodu və `Retry-After` header-i qaytarılır. Pəncərə tipi (fixed, sliding, token bucket) arxitektura sənədində seçiləcək.

| ID | Əməliyyat | Açar | Limit (başlanğıc) |
|---|---|---|---|
| SEC-RATE-01 | Login | IP | 10 / dəqiqə |
| | Login | email (hesab) | 5 / 15 dəqiqə (+ lockout, SEC-AUTH-03) |
| SEC-RATE-02 | Qeydiyyat | IP | 5 / saat |
| SEC-RATE-03 | Şifrə bərpası sorğusu, təsdiq məktubunu yenidən göndərmək | IP + email | 3 / saat (email üzrə), 10 / saat (IP üzrə) |
| SEC-RATE-04 | Refresh | istifadəçi | 30 / dəqiqə |
| SEC-RATE-05 | Mesaj göndərmək | istifadəçi | 20 / dəqiqə, 200 / gün; yeni thread açmaq 30 / gün |
| SEC-RATE-06 | Axtarış | Guest: IP; User: istifadəçi | Guest 60 / dəqiqə, User 120 / dəqiqə |
| SEC-RATE-07 | Telefonun göstərilməsi | istifadəçi | 20 / saat, 100 / gün |
| SEC-RATE-08 | Şəkil yükləmə | istifadəçi | 60 / saat |
| SEC-RATE-09 | Elan yaratma/göndərmə | istifadəçi | 20 / gün |
| SEC-RATE-10 | Şikayət | istifadəçi | 20 / gün |
| SEC-RATE-11 | Qlobal (digər endpoint-lər) | IP | 300 / dəqiqə |

- SEC-RATE-12: client IP-si yalnız etibarlı proxy-lərdən gələn `X-Forwarded-For` header-indən götürülür; header istənilən mənbədən qəbul edilmir.
- SEC-RATE-13: rate limit hadisələri (xüsusən login və telefon üzrə) audit/təhlükəsizlik log-una yazılır.

### 4.6 Secret-lərin idarəsi

- **SEC-SEC-01**: kodda, repoda, `appsettings*.json`-da, Docker image-də və log-da heç bir secret olmur: DB parolu, token imza açarı, SMTP parolu, xarici API açarı.
- **SEC-SEC-02**: lokal mühitdə secret-lər `dotnet user-secrets` ilə, digər mühitlərdə environment variable və ya secret store ilə verilir. Repoda yalnız boş və ya placeholder dəyərli nümunə konfiqurasiya olur.
- **SEC-SEC-03**: tətbiq məcburi secret olmadan işə düşmür (fail fast) və default/zəif açarla işləmir. İmza açarı ≥ 256 bit olmalıdır.
- **SEC-SEC-04**: repoda secret scanning aktivdir (pre-commit və ya CI). `.gitignore` lokal secret fayllarını əhatə edir.
- **SEC-SEC-05**: imza açarlarının rotasiyası mümkündür: köhnə və yeni açar keçid dövründə paralel qəbul olunur.

### 4.7 Nəqliyyat, header-lər, CORS

- **SEC-NET-01**: bütün mühitlərdə (lokal daxil olmaqla) HTTPS. HTTP sorğusu ya HTTPS-ə yönləndirilir, ya da rədd edilir. Production-da HSTS: `max-age=31536000; includeSubDomains`. TLS ≥ 1.2.
- **SEC-NET-02** Security header-lər (bütün cavablarda):
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY` və `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (JSON API üçün)
  - `Referrer-Policy: no-referrer`
  - `Permissions-Policy` ilə lazımsız funksiyalar söndürülür
  - Autentifikasiya və şəxsi məlumat qaytaran cavablarda `Cache-Control: no-store`
  - `Server`, `X-Powered-By` kimi versiya açan header-lər silinir
- **SEC-NET-03 CORS**: yalnız konfiqurasiyada göstərilmiş origin-lərə icazə verilir (mühitə görə). Wildcard (`*`) istifadə olunmur, xüsusən credentials ilə. Metodlar və header-lər minimum siyahı ilə məhdudlaşdırılır. İcazəsiz origin-in preflight sorğusu CORS header-ləri almır.
- **SEC-NET-04 Tokenlərin client-ə ötürülməsi** (Q2):
  - Access token response body-də qaytarılır və client tərəfindən `Authorization: Bearer` header-i ilə göndərilir. Access token cookie-də saxlanılmır. Web frontend access tokeni SHOULD yaddaşda (memory) saxlasın, `localStorage`/`sessionStorage`-da yox.
  - **Web client**: refresh token yalnız cookie-də ötürülür, response body-də qaytarılmır və JavaScript ilə oxuna bilmir. Cookie atributları: `HttpOnly`, `Secure`, `SameSite=Strict`, `Path` yalnız auth endpoint-ləri ilə məhdudlaşdırılır (məs. `/api/v1/auth`), `Domain` göstərilmir. Cookie-nin `Max-Age`-i refresh tokenin ömrünə uyğundur (SEC-AUTH-05).
  - Logout, reuse aşkarlanması və tokenlərin ləğvi zamanı cookie silinir (boş dəyər + keçmiş tarix).
  - Cookie qəbul edən endpoint-lər (refresh, logout) CSRF-ə qarşı əlavə qorunur: `SameSite=Strict`-dən əlavə `Origin` header-i CORS icazə siyahısı (SEC-NET-03) ilə yoxlanılır; `Origin` olmayan və ya icazəsiz origin-dən gələn sorğu `403` alır.
  - **Browser olmayan client-lər** (mobil tətbiq — MVP-dən sonra): refresh token response body-də qaytarılır və body ilə göndərilir. Client tipinin müəyyən edilməsi mexanizmi arxitektura sənədində veriləcək. Web client üçün body rejimi əlçatan olmamalıdır.

### 4.8 Xəta cavabları (SEC-ERR)

- **SEC-ERR-01**: bütün xəta cavabları vahid JSON formatındadır, RFC 9457 (Problem Details) ilə uyğundur. Sahələr:
  - `code` — sabit, maşın üçün oxunaqlı kod, `UPPER_SNAKE_CASE` (məs. `LISTING_NOT_FOUND`)
  - `message` — ingiliscə, insan üçün oxunaqlı qısa mətn
  - `traceId` / correlation id
  - `errors` — validasiya xətaları üçün sahə → [kod, mesaj] siyahısı
  Tərcümə frontend-in işidir və `code` əsasında aparılır.
- **SEC-ERR-02**: cavablarda stack trace, exception mətni, SQL, fayl yolu, daxili host adı, kitabxana versiyası və ya konfiqurasiya dəyərləri olmur. Gözlənilməz xəta həmişə `500` + `INTERNAL_ERROR` + ümumi mesaj + `traceId` qaytarır; detallar yalnız log-a yazılır.
- **SEC-ERR-03**: development rejimində ətraflı xəta səhifəsi production konfiqurasiyasında heç vaxt aktiv ola bilməz.
- **SEC-ERR-04**: xəta kodlarının siyahısı API sənədində saxlanılır və versiyalanır. Mövcud kodun mənasını dəyişmək breaking change sayılır.

Xəta kodlarının başlanğıc siyahısı (tam deyil):
`VALIDATION_FAILED`, `UNAUTHORIZED`, `FORBIDDEN`, `INVALID_CREDENTIALS`, `EMAIL_NOT_CONFIRMED`, `ACCOUNT_BLOCKED`, `ACCOUNT_LOCKED_OUT`, `TOKEN_INVALID_OR_EXPIRED`, `REFRESH_TOKEN_REUSED`, `LISTING_NOT_FOUND`, `THREAD_NOT_FOUND`, `INVALID_STATUS_TRANSITION`, `LISTING_UNDER_REVIEW`, `ACTIVE_LISTING_LIMIT_REACHED`, `IMAGE_LIMIT_REACHED`, `IMAGE_MIN_REQUIRED`, `IMAGE_INVALID`, `IMAGE_TOO_LARGE`, `SAVED_SEARCH_LIMIT_REACHED`, `USER_BLOCKED_YOU`, `CONCURRENCY_CONFLICT`, `RATE_LIMITED`, `INTERNAL_ERROR`, `PHONE_NOT_AVAILABLE`, `PAYLOAD_TOO_LARGE`, `UNSUPPORTED_MEDIA_TYPE`, `DRAFT_LIMIT_REACHED`, `FAVORITES_LIMIT_REACHED`, `ALREADY_REPORTED` (Q24), `USER_NOT_FOUND` (Q25).

Əlavə olunan kodların mənası (Q24):

| Kod | HTTP | Nə vaxt |
|---|---|---|
| `PHONE_NOT_AVAILABLE` | 404 | Active elanın satıcısının telefonu yoxdur (FR-LST-06 AC2) |
| `PAYLOAD_TOO_LARGE` | 413 | Request body ölçü limitini aşır (SEC-INP-02, SEC-FILE-02). Tək şəkil faylının limiti aşması `IMAGE_TOO_LARGE` ilə qaytarılır |
| `UNSUPPORTED_MEDIA_TYPE` | 415 | İcazə verilməyən `Content-Type` (SEC-INP-07) |
| `DRAFT_LIMIT_REACHED` | 409 | Draft sayı limiti aşılıb (FR-LST-01 AC3) |
| `FAVORITES_LIMIT_REACHED` | 409 | Seçilmişlərin sayı limiti aşılıb (FR-FAV-01 AC2) |
| `ALREADY_REPORTED` | 409 | İstifadəçi bu elandan artıq şikayət edib (FR-MOD-03 AC2) |
| `USER_NOT_FOUND` | 404 | Admin əməliyyatının hədəfi olan istifadəçi yoxdur (FR-ADM-01, FR-ADM-02). R-05 pozuntuları (özünü bloklamaq, öz Admin rolunu ləğv etmək) `FORBIDDEN` (403) qaytarır |

### 4.9 Logging və audit

- **SEC-LOG-01 Log-a yazılmayan məlumatlar**: şifrə (yeni, köhnə, yanlış daxil edilmiş), access/refresh token, birdəfəlik tokenlər və onların daxil olduğu URL-lər, `Authorization` və `Cookie` header-ləri, imza açarları, connection string-lər, mesajların mətni, tam telefon nömrəsi. Email maskalanmış şəkildə yazılır (`a***@mpay.az`). Request/response body default olaraq log-a yazılmır.
- **SEC-LOG-02**: log-a yazılan istifadəçi inputu log injection-a qarşı təmizlənir (yeni sətir və idarəedici simvollar).
- **SEC-LOG-03 Audit olunan təhlükəsizlik hadisələri** (ayrıca audit jurnalında, dəyişdirilə bilməz, yalnız Admin oxuya bilər):
  - uğurlu/uğursuz login, lockout, logout, refresh token reuse
  - qeydiyyat, email təsdiqi, şifrə bərpası sorğusu və tamamlanması, şifrə dəyişmə
  - hesabın silinməsi
  - bloklama və blokdan çıxarma, rol dəyişikliyi
  - moderasiya qərarları, elanın dərcdən çıxarılması
  - soraqça dəyişiklikləri
  - şikayət edilmiş thread-in moderator tərəfindən oxunması
  - telefonun göstərilməsi
  - avtorizasiya rəddləri (403/404 BOLA cəhdləri) və rate limit hadisələri — aqreqasiya ilə
- **SEC-LOG-04**: audit qeydi: vaxt (UTC), hadisə tipi, icraçı istifadəçi id, hədəf resurs id, nəticə, IP, user agent, correlation id. Şəxsi məlumat minimal saxlanılır.
- **SEC-LOG-05**: audit qeydləri ən azı 1 il saxlanılır (SEC-PII-05, Q11).

### 4.10 Asılılıqlar və xarici inteqrasiyalar

- **SEC-DEP-01**: CI-də hər build-də NuGet paketləri bilinən zəifliklərə görə yoxlanılır (`dotnet list package --vulnerable --include-transitive` və ya ekvivalent alət). High/Critical zəiflik build-i dayandırır.
- **SEC-DEP-02**: asılılıqların avtomatik yenilənməsi üçün PR-lar (Dependabot/Renovate və ya ekvivalent) aktivdir. Docker base image-lər də yoxlanılır.
- **SEC-DEP-03**: paket versiyaları sabitlənir (lock file). Yalnız etibarlı mənbələrdən (nuget.org) paket götürülür.
- **SEC-DEP-04**: statik kod analizi (SAST) və .NET analyzer-ləri CI-də işləyir. Təhlükəsizlik xəbərdarlıqları build xətası sayılır.
- **SEC-EXT-01** (Unsafe Consumption of APIs): CBAR və SMTP kimi xarici servislərdən gələn cavablar etibarsız input sayılır. Bütün xarici çağırışlarda timeout, ölçü limiti və validasiya olur. Xarici URL-lər konfiqurasiyada sabitdir və istifadəçi inputundan qurulmur (SSRF qarşısı). TLS sertifikatı yoxlanılır.

### 4.11 Şəxsi məlumatlar

| Məlumat | Kim görə bilər | Qeyd |
|---|---|---|
| Email | Sahibi, Admin | Heç vaxt public deyil, satıcıya və alıcıya göstərilmir |
| Şifrə hash-i | Heç kim (API ilə) | Heç bir cavabda qaytarılmır |
| Ad | Hamı (elanda satıcı adı kimi), thread iştirakçıları | İstifadəçi istədiyi adı yazır |
| Əlaqə telefonu | Sahibi, Admin; Active elan üçün login olmuş User (açılış qeydə alınır) | Guest görmür, siyahıda və axtarışda qaytarılmır (FR-LST-06) |
| Mesajlar | Yalnız thread iştirakçıları; şikayət olunubsa Moderator/Admin (audit ilə) | Log-a yazılmır |
| Seçilmişlər, saxlanmış axtarışlar, bildirişlər | Yalnız sahibi | Admin üçün də API-də göstərilmir |
| IP, user agent | Admin (audit vasitəsilə) | Yalnız təhlükəsizlik məqsədilə |
| Login tarixçəsi, sessiyalar | Sahibi (öz sessiyaları), Admin | |
| Şəkil metadatası (EXIF/GPS) | Heç kim | Yükləmədə silinir (SEC-FILE-04) |
| Bloklama siyahısı | Yalnız bloklayan | Bloklanan bundan xəbər tutmur |
| Şikayətçinin kimliyi | Moderator, Admin | Elan sahibinə göstərilmir |
| Elanın məkanı | Hamı (yalnız şəhər) | Dəqiq ünvan və koordinat toplanmır |
| VIN | Hamı, maskalanmadan (Q7) | Optional sahədir. Şəxsi məlumat deyil, amma avtomobili identifikasiya edir |

- **SEC-PII-01**: data minimallaşdırılması — MVP üçün lazım olmayan şəxsi məlumat toplanmır (doğum tarixi, ünvan, şəxsiyyət vəsiqəsi).
- **SEC-PII-02**: elan təsvirində və mesajlarda telefon və ya email olması moderasiya səbəbi ola bilər (`CONTACT_INFO_IN_TEXT`), amma avtomatik bloklanmır. MVP-də qadağan olunmuş sözlərin avtomatik filtri yoxdur, məzmun moderasiya və şikayətlər ilə nəzarət olunur (Q18).
- **SEC-PII-03**: hesab silinəndə FR-ACC-02-yə uyğun anonimləşdirmə aparılır. Backup-lardakı data saxlama müddəti ilə silinir.
- **SEC-PII-04**: "Fərdi məlumatlar haqqında" Azərbaycan Respublikası Qanununa uyğunluq üzrə hüquqi rəy production launch-dan əvvəl alınmalıdır. Şəxsi məlumatların ixracı MVP-dən sonraya qalır (Q12).
- **SEC-PII-05 Saxlama müddətləri** (Q11). Müddətlər konfiqurasiyadan oxunur və NFR-JOB-dakı təmizləmə işləri ilə tətbiq olunur:

  | Məlumat | Müddət | Sonra |
  |---|---|---|
  | Silinmiş hesabın şəxsi məlumatları | 30 gün | Anonimləşdirilir (FR-ACC-02 AC3) |
  | Silinmiş elanın şəkilləri | 30 gün | Fiziki silinir (FR-LST-04 AC4) |
  | Mesajlar | Müddətsiz | Thread iştirakçıları üçün qalır (FR-ACC-02 AC4) |
  | Audit qeydləri | 1 il (minimum) | Silinir (SEC-LOG-05) |
  | Tətbiq log-ları | 30 gün | Silinir (NFR-LOG) |
  | In-app bildirişlər | 90 gün | Silinir (FR-NOTIF-02 AC3) |

### 4.12 OWASP API Security Top 10 (2023) uyğunluğu

| OWASP | Bu sənəddəki tələblər |
|---|---|
| API1 Broken Object Level Authorization | SEC-AUTHZ-02, SEC-AUTHZ-03, FR-MSG-02, NFR-TEST-03 |
| API2 Broken Authentication | SEC-AUTH-01 … 08, SEC-RATE-01/03/04 |
| API3 Broken Object Property Level Authorization | SEC-AUTHZ-04, SEC-INP-03 |
| API4 Unrestricted Resource Consumption | SEC-RATE-*, SEC-INP-02, SEC-FILE-02, NFR-PAG, FR limitləri |
| API5 Broken Function Level Authorization | SEC-AUTHZ-01, İcazə matrisi (2.2) |
| API6 Unrestricted Access to Sensitive Business Flows | SEC-AUTHZ-05, FR-LST-06, SEC-RATE-05/07/09/10 |
| API7 Server Side Request Forgery | SEC-EXT-01 |
| API8 Security Misconfiguration | SEC-NET-*, SEC-ERR-*, SEC-SEC-* |
| API9 Improper Inventory Management | NFR-VER, NFR-DOC |
| API10 Unsafe Consumption of APIs | SEC-EXT-01, FR-FX-01 AC5 |

---

## 5. Digər qeyri-funksional tələblər

### 5.1 Performans (NFR-PERF)

Hədəf yük (MVP): **50 000 aktiv elan**, **pik 100 RPS** (oxumaların ~80%-i axtarış və detal). Ölçmə production-a bənzər mühitdə, isti keşlə, server tərəfində (şəbəkə vaxtı daxil deyil) aparılır.

| Əməliyyat | p95 | p99 |
|---|---|---|
| Axtarış (filtr + sıralama + pagination) | ≤ 300 ms | ≤ 800 ms |
| Elan detalı | ≤ 150 ms | ≤ 400 ms |
| Soraqçalar | ≤ 50 ms | ≤ 150 ms |
| Login (şifrə hash daxil olmaqla) | ≤ 500 ms | ≤ 1000 ms |
| Mesaj göndərmə / thread oxuma | ≤ 200 ms | ≤ 500 ms |
| Şəkil yükləmə (5 MB, emal daxil) | ≤ 2 s | ≤ 4 s |

- NFR-PERF-02: pik yükdə 5xx xətalarının payı < 0.1%.
- NFR-PERF-03: email göndərmə, şəkil emalı (mümkün olan hissəsi), bildiriş uyğunlaşdırılması və məzənnə yenilənməsi istifadəçi sorğusunun cavab müddətinə təsir etmir (asinxron).
- NFR-PERF-04: yük testi (NFR-TEST-05) bu hədəfləri yoxlayır.
- NFR-AVAIL: hədəf əlçatanlıq 99.5% / ay (MVP).

### 5.2 Pagination (NFR-PAG)

- Bütün siyahı endpoint-ləri səhifələnir, səhifələnməmiş siyahı qaytarılmır (soraqçalardan başqa).
- Default 20, maksimum 50. Maksimumu aşan dəyər `VALIDATION_FAILED` qaytarır.
- Offset əsaslı pagination 10 000 nəticə ilə məhdudlaşır. Mesajlar və bildirişlər kimi uzun və tez dəyişən siyahılar üçün cursor əsaslı pagination SHOULD istifadə olunsun.
- Pagination cavab formatı bütün endpoint-lərdə eynidir.

### 5.3 Structured logging (NFR-LOG)

- Log-lar strukturlaşdırılmış formatda (JSON) yazılır. Sabit sahələr: timestamp (UTC, ISO 8601), level, message template, correlation id, trace id, istifadəçi id (varsa), endpoint, status kodu, müddət, mühit, tətbiq versiyası.
- Səviyyələr konfiqurasiya ilə idarə olunur, production default səviyyəsi `Information`-dır.
- Hər HTTP sorğusu üçün bir yekun log qeydi yazılır (metod, route şablonu, status, müddət). Tam URL query ilə yazılmır, çünki orada token ola bilər.
- Log məzmunu SEC-LOG-01/02 tələblərinə tabedir.
- Tətbiq log-ları 30 gün saxlanılır (SEC-PII-05, Q11). Audit jurnalı tətbiq log-larından ayrıdır və öz müddəti ilə saxlanılır (SEC-LOG-05).

### 5.4 Correlation id (NFR-CORR)

- Hər sorğu correlation id daşıyır. Client `X-Correlation-Id` header-i ilə göndərirsə və o, formatı (≤ 64 simvol, `[A-Za-z0-9-_]`) ödəyirsə, qəbul edilir; əks halda server yenisini yaradır.
- Correlation id cavab header-ində, xəta cavabında (`traceId`), bütün log qeydlərində və background job-lara və email göndərməyə ötürülən mesajlarda olur.
- W3C Trace Context (`traceparent`) dəstəklənir.

### 5.5 Health check (NFR-HC)

- `/health/live` — prosesin işlədiyini göstərir, asılılıqları yoxlamır.
- `/health/ready` — kritik asılılıqları yoxlayır: verilənlər bazası, fayl saxlama yeri, background job-un vəziyyəti. Bu endpoint son uğurlu məzənnənin yaşını da qaytarır: 3 gündən köhnədirsə `Degraded` (FR-FX-01 AC4).
- Public cavab yalnız ümumi statusu (`Healthy`/`Degraded`/`Unhealthy`) qaytarır. Detallı cavab yalnız daxili şəbəkə və ya autentifikasiya ilə əlçatandır (SEC-ERR-02).
- Health endpoint-ləri rate limit və autentifikasiya log-unu doldurmamalıdır.

### 5.6 API versioning (NFR-VER)

- Bütün endpoint-lər versiyalanır, ilk versiya `v1`-dir (URL-də: `/api/v1/...`).
- Versiya daxilində yalnız geriyə uyğun dəyişikliklərə icazə var: yeni optional sahə, yeni endpoint. Sahənin silinməsi, adının və ya tipinin dəyişməsi, xəta kodunun mənasının dəyişməsi yeni versiya tələb edir.
- Köhnə versiya ən azı 6 ay paralel dəstəklənir. Deprecation `Deprecation` və `Sunset` header-ləri ilə elan olunur.
- Test, debug və köhnə endpoint-lər production-da açıq qalmır.

### 5.7 API sənədləşməsi (NFR-DOC)

- OpenAPI spesifikasiyası avtomatik yaradılır və hər endpoint üçün request/response modelləri, xəta kodları və tələb olunan rolları göstərir.
- Production-da interaktiv sənəd UI-ı ya söndürülür, ya da autentifikasiya ilə qorunur.
- Mühitlərin, versiyaların və endpoint-lərin inventarı saxlanılır.

### 5.8 Email və lokal mühit (NFR-ENV)

- **NFR-ENV-01**: lokal mühitdə email göndərmək üçün **Mailpit** (Docker) istifadə olunur. Real məktub göndərilmir, bütün məktublar Mailpit-in web UI-da görünür.
- **NFR-ENV-02**: email göndərmə konfiqurasiya ilə dəyişdirilə bilər (lokal: Mailpit SMTP; digər mühitlər: real SMTP və ya provayder). Bunun üçün kod dəyişikliyi lazım deyil.
- **NFR-ENV-03**: lokal mühit bir əmrlə qaldırılır (məs. Docker Compose ilə asılılıqlar: verilənlər bazası, Mailpit). README-də addım-addım təlimat olur.
- **NFR-ENV-04**: mühitlər: `Development`, `Test` (CI), `Staging`, `Production`. Hər mühitin öz secret-ləri və CORS origin-ləri var.
- **NFR-ENV-05**: email şablonları iki dildədir: Azərbaycan və ingilis (Q19). Profildə dil seçimi olmadığı üçün hər məktub hər iki dildə mətn ehtiva edir (əvvəl az, sonra en). Hər məktubda platformanın adı olur, şəxsi məlumat minimum saxlanılır.

### 5.9 Vaxt, dəqiqlik, dil (NFR-MISC)

- Bütün vaxtlar UTC olaraq saxlanılır və ISO 8601 formatında qaytarılır. Planlaşdırılmış işlər (digest, məzənnə) Bakı vaxtı (`Asia/Baku`) ilə konfiqurasiya olunur.
- Pul məbləğləri yalnız `decimal` tipində saxlanılır və hesablanır, `float`/`double` istifadə olunmur.
- Soraqça adları `nameAz` və `nameEn` kimi qaytarılır. API mesajları ingiliscədir (SEC-ERR-01).
- Mətn sahələri UTF-8-dir və Azərbaycan hərflərini (ə, ı, ö, ü, ğ, ş, ç) tam dəstəkləyir. Axtarış və unikallıq yoxlaması bu hərfləri düzgün müqayisə edir.

### 5.10 Background işlər (NFR-JOB)

- Planlaşdırılmış işlər: elanların Expired olması, bitmə xəbərdarlığı, məzənnənin alınması və AZN ekvivalentinin yenilənməsi, saxlanmış axtarış bildirişləri və digest, yetim şəkillərin təmizlənməsi, vaxtı keçmiş tokenlərin və təsdiqlənməmiş hesabların təmizlənməsi, hesab anonimləşdirilməsi.
- Hər iş idempotentdir və təkrar işə salındıqda dublikat effekt yaratmır (məs. ikiqat email).
- Bir neçə instansiya işləyəndə eyni iş paralel iki dəfə icra olunmur.
- Hər işin başlanğıcı, nəticəsi, emal edilən element sayı və xətaları log-a yazılır.

### 5.11 Testlər (NFR-TEST)

- **NFR-TEST-01 Unit testlər**: domen qaydaları — status keçidləri (icazəli və icazəsiz bütün kombinasiyalar), limitlər, valyuta çevirməsi və yuvarlaqlaşdırma, şifrə siyasəti, saxlanmış axtarışın uyğunluq məntiqi. Domen və application qatında minimum 80% sətir əhatəsi.
- **NFR-TEST-02 Inteqrasiya testləri**: hər endpoint üçün ən azı bir uğurlu və bir xəta ssenarisi. Testlər real verilənlər bazası mühərriki ilə işləyir (in-memory əvəzedici yox), məs. konteynerdə.
- **NFR-TEST-03 Avtorizasiya testləri** (məcburi): hər qorunan endpoint üçün:
  - anonim → `401`;
  - lazımi rolu olmayan → `403`;
  - başqa istifadəçinin resursu → `404` (BOLA), xüsusən elan redaktəsi/silməsi, şəkil, thread, mesaj, seçilmiş, saxlanmış axtarış, bildiriş üçün;
  - mass assignment: sistem sahələri göndərildikdə nəzərə alınmadığı yoxlanılır.
  İcazə matrisi (2.2) testlərin mənbəyidir: matrisdəki hər sətir ən azı bir testlə əhatə olunur.
- **NFR-TEST-04 Təhlükəsizlik testləri**: fayl yükləmə (yanlış magic bytes, uzantı dəyişdirilmiş fayl, böyük fayl, decompression bomb, EXIF/GPS-in silinməsi), refresh token reuse, lockout, rate limit, xəta cavablarında daxili məlumatın olmaması, security header-lərin olması.
- **NFR-TEST-05 Yük testi**: NFR-PERF hədəfləri üçün ssenari (axtarış ağırlıqlı), release-dən əvvəl icra olunur.
- **NFR-TEST-06 Xarici asılılıqlar**: CBAR və SMTP testlərdə əvəz olunur (stub/fake). CBAR-ın əlçatan olmadığı, formatın səhv olduğu və dəyərin ağlabatan diapazondan kənar olduğu ssenarilər testlə yoxlanılır. Email testləri Mailpit və ya fake ilə aparılır.
- **NFR-TEST-07 CI**: bütün testlər, asılılıq yoxlaması (SEC-DEP-01), SAST və secret scanning hər PR-da işləyir. Uğursuz yoxlama merge-i bloklayır.
- **NFR-TEST-08**: testlər deterministikdir: vaxt (saat) və təsadüfi dəyər mənbələri testdə idarə olunur; testlər bir-birindən asılı deyil.

---

## 6. Qəbul edilmiş qərarlar

Əvvəlki "Açıq suallar" bölməsindəki suallar 2026-10-05 tarixində bağlanıb. Q2 və Q5 üzrə qərarları məhsul sahibi verib, qalan suallarda təklif olunan default dəyərlər qəbul edilib. Identifikatorlar (Q1–Q21) sənəddəki istinadlar üçün saxlanılıb. Q22–Q24 arxitektura sənədinin ([ARCHITECTURE.md](ARCHITECTURE.md)) review-u zamanı əlavə olunub.

| # | Mövzu | Qərar | Tətbiq olunduğu yer |
|---|---|---|---|
| Q1 | Sızmış şifrələrin yoxlanması | Yalnız lokal top-100k siyahı ilə. Xarici servis (HIBP və s.) istifadə olunmur | SEC-AUTH-01 |
| Q2 | Tokenlərin client-ə ötürülməsi | Web: refresh token `HttpOnly` + `Secure` + `SameSite=Strict` cookie-də, access token response body-də. Browser olmayan client-lər (mobil, MVP-dən sonra): refresh token body-də | FR-AUTH-03/04/05, SEC-AUTH-05, SEC-NET-04 |
| Q3 | Refresh tokenin ömrü | 14 gün sliding, 60 gün absolute | SEC-AUTH-05 |
| Q4 | Pulsuz aktiv elan limiti (Pending + Active) | 5 | FR-LST-02 AC2 |
| Q5 | "Satıldı" statusu, elanı müvəqqəti gizlətmək | MVP-dən sonra. MVP-də satıcı elanı yalnız silə bilər | 1.3 |
| Q6 | Active elanda moderasiyasız redaktə olunan sahələr | Siyahı olduğu kimi qalır, qiymətin kəskin dəyişməsi moderasiya tələb etmir | FR-LST-03 AC4 |
| Q7 | VIN | Optional, public, maskalanmadan göstərilir | 3.3.1, 4.11 |
| Q8 | Rəng və ötürücü | Sabit siyahı (enum), Admin idarə etmir. Ötürücü optional sahə kimi əlavə edilib | 3.3.1 |
| Q9 | Şəkillərin antivirus yoxlaması | MVP-də yoxdur | SEC-FILE-07, 1.3 |
| Q10 | Draft şəkillərinin URL-ləri | Təxmin edilə bilməyən URL (≥ 128 bit), imzalı URL MVP-də yoxdur | SEC-FILE-06 |
| Q11 | Saxlama müddətləri | Anonimləşdirmə 30 gün, silinmiş elan şəkilləri 30 gün, mesajlar müddətsiz, audit 1 il, tətbiq log-ları 30 gün | SEC-PII-05, FR-ACC-02, FR-LST-04, SEC-LOG-05, NFR-LOG |
| Q12 | Data portability, hüquqi yoxlama | İxrac MVP-dən sonra; hüquqi rəy production launch-dan əvvəl | SEC-PII-04, 1.3 |
| Q13 | Qeydiyyatda CAPTCHA | MVP-də yoxdur, yalnız rate limit. Yekun qərar launch-dan əvvəl verilir | FR-AUTH-01 AC7 |
| Q14 | Email dəyişmə | MVP-dən sonra | FR-ACC-01 AC2, 1.3 |
| Q15 | Moderasiya SLA-sı | 24 saat | FR-MOD-01 AC4 |
| Q16 | Full-text axtarış | MVP-dən sonra | 1.3 |
| Q17 | Email bildirişlərinin söndürülməsi | Bəli: bitmə xəbərdarlığı və yeni mesaj email-ləri profil ayarı ilə söndürülür | FR-ACC-01 AC4, ST-04, FR-MSG-02 AC7 |
| Q18 | Qadağan olunmuş sözlərin avtomatik filtri | Yoxdur, moderasiya kifayətdir | SEC-PII-02, 1.3 |
| Q19 | Email şablonlarının dili | az + en (hər məktub hər iki dildə) | NFR-ENV-05 |
| Q20 | Telefon nömrəsinin formatı | Yalnız `+994XXXXXXXXX` | FR-AUTH-01 AC1, FR-ACC-01 AC1 |
| Q21 | Production hosting, fayl saxlama yeri, email provayderi | Arxitektura sənədində qərar veriləcək | — |
| Q22 | Abunəlikdən çıxma tokeni (arxitektura review-u, 2026-10-05) | İmzalı token əvəzinə SEC-AUTH-07 variantı istifadə olunur: təsadüfi token, serverdə yalnız hash-i saxlanılır | FR-NOTIF-01 AC7 |
| Q23 | Şifrə hash alqoritmi (arxitektura review-u, 2026-10-05) | ASP.NET Core Identity-nin standart `PasswordHasher`-i: PBKDF2-HMAC-SHA512, 210 000 iterasiya (OWASP tövsiyəsi) | SEC-AUTH-02 |
| Q24 | Əlavə xəta kodları (arxitektura review-u, 2026-10-05) | `PHONE_NOT_AVAILABLE`, `PAYLOAD_TOO_LARGE`, `UNSUPPORTED_MEDIA_TYPE`, `DRAFT_LIMIT_REACHED`, `FAVORITES_LIMIT_REACHED`, `ALREADY_REPORTED` | 4.8 |
| Q25 | Admin əməliyyatlarının xəta kodu (mərhələ 3b, 2026-10-06) | `USER_NOT_FOUND` (404) əlavə olunur; R-05 pozuntuları üçün ayrıca kod yoxdur, `FORBIDDEN` istifadə olunur | 4.8 |
