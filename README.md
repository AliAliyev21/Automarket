# AutoMarket

## Lokal mühiti işə salmaq

Lazımdır: .NET SDK (`global.json`-dakı versiya), Docker Desktop (və ya Docker Engine + Compose v2). Ətraflı: [ARCHITECTURE §11](docs/ARCHITECTURE.md#11-lokal-mühit).

### 1. HTTPS development sertifikatı

```bash
dotnet dev-certs https --trust
```

### 2. `.env` faylı

Docker Compose parolları `.env` faylından oxuyur. Fayl gitignored-dur və repoya düşmür (SEC-SEC-01).

```bash
cp .env.example .env
```

`.env`-də bütün `change-me-*` dəyərlərini öz parollarınızla əvəz edin. `.env` olmadıqda və ya dəyişən boş olduqda `docker compose` işə düşmür.

Kompüterdə artıq PostgreSQL işləyirsə və 5432 portu məşğuldursa, `.env`-ə `POSTGRES_PORT=5433` əlavə edin və aşağıdakı connection string-də də həmin portu yazın.

### 3. Asılılıqları qaldırmaq

```bash
docker compose up -d --wait
```

`--wait` bütün servislər `healthy` olana qədər gözləyir. Vəziyyətə baxmaq üçün:

```bash
docker compose ps
```

Portlar yalnız `127.0.0.1`-ə bağlıdır. Data named volume-larda saxlanılır (`pgdata`, `rabbitdata`, `mailpitdata`, `seqdata`). Redis-də persistence söndürülüb. Hər şeyi data ilə birlikdə silmək üçün: `docker compose down -v`.

> Postgres parolu volume ilk dəfə yaradılanda yazılır. Sonradan `.env`-də dəyişsəniz, volume-u silmək (`docker compose down -v`) lazımdır.

### 4. API secret-ləri (user-secrets)

Connection string-lər `appsettings*.json`-da saxlanılmır. Development-də onlar `dotnet user-secrets` ilə verilir. Parollar `.env`-dəki ilə eyni olmalıdır:

```bash
dotnet user-secrets set "Postgres:ConnectionString" "Host=localhost;Port=5432;Database=automarket;Username=automarket;Password=<POSTGRES_PASSWORD>" --project src/Host/AutoMarket.Api
```

```bash
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379,password=<REDIS_PASSWORD>" --project src/Host/AutoMarket.Api
```

```bash
dotnet user-secrets set "RabbitMq:ConnectionString" "amqp://automarket:<RABBITMQ_PASSWORD>@localhost:5672/" --project src/Host/AutoMarket.Api
```

Yoxlamaq üçün:

```bash
dotnet user-secrets list --project src/Host/AutoMarket.Api
```

Tətbiq startup-da konfiqurasiyanı yoxlayır (`ValidateOnStart`): dəyər boşdursa, formatı səhvdirsə və ya parol placeholder-dirsə (`change-me...`, `<...>`), proses işə düşmür və hansı açarın səhv olduğunu yazır (SEC-SEC-03).

JWT imza açarı (SEC-SEC-03: ən azı 256 bit, base64). Açarı yaratmaq üçün:

```bash
openssl rand -base64 32
```

Alınan dəyəri yazın (`KeyId` `appsettings.json`-dadır, burada yalnız açarın özü verilir):

```bash
dotnet user-secrets set "Jwt:SigningKeys:0:Key" "<base64 açar>" --project src/Host/AutoMarket.Api
```

Açar rotasiyası (SEC-SEC-05): `Jwt:SigningKeys` siyahısına `NotBefore` ilə yeni açar əlavə olunur, köhnə açar `RetireAfter` ilə keçid dövründən sonra çıxarılır.

Digər mühitlərdə eyni açarlar environment variable ilə verilir: `AutoMarket__Postgres__ConnectionString`, `AutoMarket__Redis__ConnectionString`, `AutoMarket__RabbitMq__ConnectionString`, `AutoMarket__Jwt__SigningKeys__0__Key`, real SMTP üçün `AutoMarket__Smtp__Host`, `AutoMarket__Smtp__UserName`, `AutoMarket__Smtp__Password`. CORS origin-ləri (`Cors:AllowedOrigins`) və təsdiq linkinin ünvanı (`Notifications:Links:ConfirmEmailUrl`) hər mühit üçün ayrıca verilir.

### 5. API-ni işə salmaq

```bash
dotnet run --project src/Host/AutoMarket.Api
```

Log-lar stdout-a JSON formatında yazılır və Development-də Seq-ə də göndərilir. Development-də bütün modulların migration-ları startup-da tətbiq olunur (`Database:MigrateOnStartup`).

Auth axınını yoxlamaq üçün: Scalar-da `POST /api/v1/auth/register` → Mailpit-də (`http://localhost:8025`) təsdiq məktubu (az + en) → linkdəki `token` ilə `POST /api/v1/auth/confirm-email` → `POST /api/v1/auth/login` → access token ilə `GET /api/v1/me`. Refresh token yalnız `rt` HttpOnly cookie-dədir və `POST /api/v1/auth/refresh` yalnız `Cors:AllowedOrigins`-dəki `Origin` ilə qəbul olunur.

### Ünvanlar

| Servis | Ünvan | Qeyd |
|---|---|---|
| API | `https://localhost:8443` (`http://localhost:8080`) | |
| OpenAPI UI (Scalar) | `https://localhost:8443/scalar` | yalnız Development |
| OpenAPI sənədi | `https://localhost:8443/openapi/v1.json` | yalnız Development |
| Health | `/health/live`, `/health/ready` | `/health/ready/details` yalnız Development |
| Mailpit (email UI) | `http://localhost:8025` | SMTP: `localhost:1025` |
| RabbitMQ Management | `http://localhost:15672` | `.env`-dəki `RABBITMQ_USER` / `RABBITMQ_PASSWORD` |
| Seq (log-lar) | `http://localhost:5341` | istifadəçi `admin`, parol `.env`-dəki `SEQ_ADMIN_PASSWORD` |

Seq lokal mühitdə pulsuz Individual lisenziya ilə işləyir və yalnız fərdi istifadə üçündür (ARCHITECTURE §13 A8).

### Testlər

```bash
dotnet test
```

Integration testləri (`tests/AutoMarket.IntegrationTests`) Testcontainers ilə öz PostgreSQL, Redis və RabbitMQ konteynerlərini qaldırır. Docker işləməlidir, `docker compose` stack-i lazım deyil.
