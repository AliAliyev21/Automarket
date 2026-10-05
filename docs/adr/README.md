# Architecture Decision Records (ADR)

Bu qovluqda AutoMarket-in arxitektura qərarları saxlanılır. Hər ADR bir qərarı, onun kontekstini, nəzərdən keçirilmiş alternativləri və nəticələrini təsvir edir. Ümumi mənzərə üçün [ARCHITECTURE.md](../ARCHITECTURE.md) sənədinə baxın.

## Qaydalar

- Fayl adı `NNNN-qisa-ad.md` formatındadır. Nömrələr ardıcıldır və bir dəfə verildikdən sonra dəyişdirilmir.
- Qəbul edilmiş (`Accepted`) ADR redaktə olunmur. Qərar dəyişərsə, yeni ADR yazılır, köhnəsinin statusu `Superseded by NNNN` olur.
- Statuslar: `Proposed`, `Accepted`, `Deprecated`, `Superseded`.
- Kitabxana seçən ADR-də lisenziya vəziyyəti və alternativ açıq yazılır.

## İndeks

| # | Başlıq | Status |
|---|---|---|
| [0001](0001-modular-monolith.md) | Modulyar monolit | Accepted |
| [0002](0002-solution-structure-and-module-boundaries.md) | Solution strukturu və modul sərhədləri | Accepted |
| [0003](0003-authentication-identity-core-custom-tokens.md) | Autentifikasiya: ASP.NET Core Identity Core + öz token həlli | Accepted |
| [0004](0004-inter-module-communication-and-outbox.md) | Modullar arası əlaqə və outbox | Accepted |
| [0005](0005-messaging-library-rabbitmq-client.md) | Messaging kitabxanası: birbaşa RabbitMQ.Client | Accepted |
| [0006](0006-background-jobs-hosted-services.md) | Background job-lar: öz BackgroundService + Cronos | Accepted |
| [0007](0007-caching-strategy-hybridcache-redis.md) | Cache strategiyası: HybridCache + Redis | Accepted |
| [0008](0008-file-storage.md) | Fayl saxlama: IFileStorage + lokal FS | Accepted |
| [0009](0009-image-processing-netvips.md) | Şəkil emalı: NetVips | Accepted |
| [0010](0010-api-style-minimal-api-no-mediatr.md) | API stili: Minimal API, MediatR və AutoMapper olmadan | Accepted |
| [0011](0011-distributed-rate-limiting-redis.md) | Paylanmış rate limiting: Redis sliding window | Accepted |
| [0012](0012-search-read-model.md) | Axtarış üçün ayrıca read model | Accepted |
| [0013](0013-logging-and-observability.md) | Logging və observability: Serilog + OpenTelemetry | Accepted |

## Şablon

```markdown
# NNNN. Başlıq

- Status: Proposed | Accepted | Deprecated | Superseded by NNNN
- Tarix: YYYY-MM-DD
- Əlaqəli tələblər: FR-..., SEC-..., NFR-...

## Kontekst
Hansı problem həll olunur, hansı tələblər və məhdudiyyətlər var.

## Qərar
Nə seçildi və necə tətbiq olunacaq.

## Alternativlər
Hər alternativ: qısa təsvir, üstünlükləri, çatışmazlıqları, niyə seçilmədi.

## Nəticələr
Müsbət, mənfi, risklər və onların azaldılması, gələcəkdə qərarın yenidən baxılma şərtləri.
```
