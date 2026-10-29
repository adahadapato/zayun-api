# Zayun API

The backend for **Zayun** ([zayun.net](https://zayun.net)), the technology portfolio of Enobong Adahada. It serves
the website's content (projects, research, experience and skills) from SQL Server, runs the admin area behind a
sign-in, and keeps the Contact inbox.

| Front end | Repository | Talks to |
|---|---|---|
| Website + admin area (React + Vite) | `adahadapato/zayun-web` | `https://api.zayun.net` |

Production base address: **`https://api.zayun.net`** · local: **`https://localhost:7211`**

---

## Contents

1. [What the API does](#1-what-the-api-does)
2. [Tech stack](#2-tech-stack)
3. [Architecture](#3-architecture)
4. [Solution layout](#4-solution-layout)
5. [Getting started (local development)](#5-getting-started-local-development)
6. [Configuration](#6-configuration)
7. [Database, migrations and seed data](#7-database-migrations-and-seed-data)
8. [Authentication and security](#8-authentication-and-security)
9. [API reference](#9-api-reference)
10. [Errors](#10-errors)
11. [Caching](#11-caching)
12. [Pictures and email](#12-pictures-and-email)
    - [Tags and the AI tag check](#12a-tags-and-the-ai-tag-check)
13. [Deployment (CI/CD)](#13-deployment-cicd)
14. [Adding a feature: the checklist](#14-adding-a-feature-the-checklist)
15. [Troubleshooting](#15-troubleshooting)

---

## 1. What the API does

**The website (no sign-in)**
- **Projects**: featured and engineering projects, in order, each with its tags, links and pictures (a main
  picture, a second one beside it, or a gallery of phone screens). One project by its web address
  (`/api/projects/rccg-hope-house`).
- **Research**: publications (with "read it here" links and awards) and presentations (with the poster, slide or
  certificate).
- **Experience** and **Skills** (skill groups plus the core expertise list).
- **Awards**: awards and recognition, with an optional certificate or photo. Featured ones also show on the About
  and Experience pages.
- **Contact**: the form saves the message to the inbox, emails the owner (Reply goes straight to the visitor) and
  sends the visitor a confirmation.

**The admin area (Admin role)**
- Add, edit, delete, publish / unpublish (drafts) and drag-to-reorder every kind of content.
- Upload project, presentation and award pictures: they're resized, converted to **WebP** and given a thumbnail.
- The Contact inbox: New / Replied / Closed with a private note.
- **Private documents** (CV, certificates, transcripts…): PDF, Word, JPEG or PNG, stored outside the website's
  folders and opened only through signed links that expire after 5 minutes.
- A dashboard with the numbers (content totals, published, new messages, documents).

On first start the database is filled with **exactly what zayun.net showed before the API** (11 projects,
2 publications, 3 presentations, 4 experience areas, 5 skill groups, 8 core expertise items, 2 awards, 17 pictures).
Each block is only filled while its table is empty, so a new block (like Awards) is filled on the next start of an
existing database too.

---

## 2. Tech stack

| Area | Choice | Version |
|---|---|---|
| Runtime | .NET | 10 |
| Web | ASP.NET Core **minimal APIs** | 10 |
| Architecture | Clean architecture: Domain → Application → Persistence / Infrastructure → Presentation | |
| CQRS / pipeline | MediatR | 14.2.0 |
| Validation | FluentValidation | 12.2.0 |
| Database | SQL Server with Entity Framework Core | 10.0.x |
| Identity | ASP.NET Core Identity (EF stores) + JWT bearer tokens + rotating refresh tokens | 10.0.x |
| Caching | `HybridCache` (in memory) | 10.11.0 |
| Email | MailKit (SMTP) | 4.18.1 |
| Images | Magick.NET Q8 (AnyCPU) | 15.0.0 |
| Logging | Serilog (console + rolling files) | 10.0.0 |
| API docs | OpenAPI document + Swagger UI | 10.x / 10.3.0 |
| Rate limiting | ASP.NET Core built-in rate limiter | 10 |
| Hosting | IIS on SmarterASP.NET (self-contained, win-x86) | |
| CI/CD | GitHub Actions | |

---

## 3. Architecture

### 3.1 Layers and the dependency rule

```
                ┌──────────────────────────────┐
                │   Zayun.Presentation         │  minimal-API endpoints, auth, CORS, OpenAPI,
                │   (the web host)             │  rate limiting, ProblemDetails, Program.cs
                └──────────────┬───────────────┘
                               │ sends commands / queries (MediatR)
                ┌──────────────▼───────────────┐
                │   Zayun.Application          │  commands, queries, handlers, validators, DTOs,
                │                              │  pipeline behaviours, caching
                └──────────────┬───────────────┘
                               │ uses interfaces only
                ┌──────────────▼───────────────┐
                │   Zayun.Domain               │  entities, value objects, enums, rules,
                │   (no dependencies)          │  Result/Error, repository & service interfaces
                └──────────────▲───────────────┘
                               │ implement the Domain's interfaces
     ┌─────────────────────────┴───────────────────────────┐
┌────┴──────────────────┐                     ┌────────────┴───────────────┐
│ Zayun.Persistence     │                     │ Zayun.Infrastructure       │
│ EF Core DbContext,    │                     │ Identity/JWT, email (SMTP),│
│ configurations,       │                     │ file storage, images,      │
│ repositories,         │                     │ settings                   │
│ migrations, seeding   │                     │                            │
└───────────────────────┘                     └────────────────────────────┘
```

- **Domain** knows nothing else. Rules live in the entities (`Project.AddImage`, `Project.ReorderImages`,
  `Presentation.SetImage`, `ContactMessage.SetStatus`…) and return `Result` / `Result<T>` for expected failures.
- **Application** orchestrates: one handler per use case, talking only to Domain interfaces (`IProjectRepository`,
  `IImageProcessor`, `IFileStorage`, `IEmailSender`, `ICurrentUser`…).
- **Persistence** and **Infrastructure** implement those interfaces. Nothing above them references EF Core,
  MailKit, Magick.NET or Identity directly.
- **Presentation** maps HTTP to MediatR and `Result` to HTTP. Endpoints contain no business logic.

### 3.2 What happens to a request

```
HTTP request
  → Serilog request logging → exception handler → HTTPS redirect (not in Development)
  → static files (/uploads) → OpenAPI/Swagger → CORS → authentication/authorization → rate limiter
  → endpoint (Presentation/Endpoints/…)              e.g. POST /api/admin/projects
  → ISender.Send(command)
      → LoggingBehavior            logs the request and how long it took
      → ValidationBehavior         FluentValidation → 400 with field errors
      → CachingBehavior            [Cacheable] queries are served from HybridCache
      → CacheInvalidationBehavior  [InvalidatesCache] commands clear tags after success
      → Handler                    loads aggregates → calls Domain methods → saves (IUnitOfWork)
  → Result → .ToHttp() → 200/204, or ProblemDetails (400/401/403/404/409/429)
```

### 3.3 Conventions worth knowing

- **Result pattern.** Handlers return `Result`/`Result<T>`; `ResultExtensions.ToHttp()` turns an `Error` into
  ProblemDetails with a stable `code` (e.g. `Project.SlugTaken`). Exceptions are for bugs only.
- **Errors live in one place per area**: `Zayun.Domain/Shared/Errors/*Errors.cs`.
- **Every configuration key** is in `Zayun.Domain/Constants/AppSettings.cs`; every cache tag in
  `Zayun.Application/Common/Caching/CacheTags.cs`; every role in `Zayun.Domain/Constants/Roles.cs`.
- **Website content shares one base**, `ContentEntity`: a place in its list (`DisplayOrder`) and a published flag
  (drafts). Publish, delete and reorder go through `Application/Common/Content/ContentActions`.
- **Soft delete** on every entity (`IsDeleted`, `DeletedAt`) with a global query filter; `CreatedAt` / `UpdatedAt`
  are set by an interceptor.
- **Enums travel as names** in JSON (`"Featured"`, `"Poster"`); numbers are still accepted.
- **Who "me" is** always comes from the access token (`ICurrentUser`), never from the URL or body.

---

## 4. Solution layout

```
Zayun.sln
├── Zayun.Domain/
│   ├── Constants/          AppSettings (all config keys), Roles, Policies
│   ├── Entities/           ContentEntity, Project, ProjectImage, Publication, PublicationLink, Presentation,
│   │                       ExperienceArea, SkillGroup, CoreExpertise, ContactMessage, RefreshToken
│   ├── Enums/              ProjectCategory, ProjectImageKind, PublicationType, PresentationType, ContactTopic, …
│   ├── Interfaces/         repositories, IFileStorage (+ SaveImageAsync), IImageProcessor, IEmailSender,
│   │                       ICurrentUser, IAuthService
│   ├── Shared/Errors/      one *Errors.cs per area
│   ├── Shared/Results/     Result, Result<T>, PagedResult, PageRequest
│   ├── Shared/Text/        Slugs (web addresses), TextRules (the checks entities make)
│   └── ValueObjects/       ImageFile, ImageDimensions
├── Zayun.Application/
│   ├── Common/Behaviours/  Logging, Validation, Caching, CacheInvalidation
│   ├── Common/Caching/     [Cacheable], [InvalidatesCache], CacheTags
│   ├── Common/Content/     ContentActions (publish, delete, reorder)
│   ├── Common/Validation/  shared rules (web links, label lists, reorder lists, uploads)
│   └── Features/<Area>/    Commands/ Queries/ Dtos/ Validators/ — Auth, Projects, Publications, Presentations,
│                           Experience, Skills, Awards, Tags, Contact, Dashboard
├── Zayun.Persistence/
│   ├── Configurations/     one IEntityTypeConfiguration per entity (+ EntityConfiguration, ContentEntityConfiguration)
│   │   └── Identity/       ApplicationUser, ApplicationRole, role/admin seeders, RefreshTokenConfiguration
│   ├── Repositories/       Repository, ContentRepository + one per aggregate
│   ├── Migrations/         EF Core migrations (applied automatically at start-up) — add the first one, see §7
│   ├── Seeding/            DbSeeder, StarterDataSeeder, Data/starter-data.json, Data/images/*.webp
│   ├── Interceptors/       audit timestamps
│   └── ZayunDbContext.cs
├── Zayun.Infrastructure/
│   ├── Services/Identity/  AuthService (ASP.NET Core Identity)
│   └── Settings/           Application, Email (SMTP or log-only), Identity (JWT, lockout, passwords),
│                           Images (Magick.NET → WebP), Storage (local disk under wwwroot)
├── Zayun.Presentation/     the web host
│   ├── Endpoints/<Area>/   minimal-API endpoint groups (website + admin routes side by side)
│   ├── Extensions/         EndpointExtensions (MapZayunEndpoints), ResultExtensions (ToHttp), static files
│   ├── Middleware/         GlobalExceptionHandler
│   ├── Settings/           Authentication, CORS, OpenAPI, RateLimiting
│   ├── appsettings*.json, web.config, Zayun.Presentation.http
│   └── Program.cs
└── Zayun.Worker/           reserved for scheduled jobs (empty for now)
```

---

## 5. Getting started (local development)

### 5.1 Prerequisites

- **.NET 10 SDK** — `dotnet --version` should print `10.x`.
- **SQL Server**: LocalDB (comes with Visual Studio), SQL Server Express/Developer, or a Docker container.
- **EF Core tools**: `dotnet tool restore` (the manifest is in `Zayun.Presentation/dotnet-tools.json`) or
  `dotnet tool install --global dotnet-ef`.
- Optional, for email: a local test inbox such as **smtp4dev**, with `Email:Enabled` true, `Email:Host`
  `localhost`, `Email:Port` 25 and `Email:Security` `None`. With `Email:Enabled` false (the default) emails are only
  written to the log.

### 5.2 Restore

```bash
dotnet restore Zayun.sln
```

### 5.3 Secrets (User Secrets — never in appsettings.json)

```bash
cd Zayun.Presentation

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=Zayun;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key"                "<at least 32 random characters>"
dotnet user-secrets set "Storage:Local:SigningKey" "<another 32+ random characters, different from Jwt:Key>"
dotnet user-secrets set "SeedData:AdminPassword" "<password for the first admin: 10+ chars, upper, lower, digit>"

# optional (only when Email:Enabled is true)
dotnet user-secrets set "Email:Password"         "<SMTP password>"

# optional: the AI tag check (without it, only the built-in tag rules are used)
dotnet user-secrets set "Ai:ApiKey"              "<Anthropic API key, sk-ant-…>"
```

A random 64-character key in Windows PowerShell (run it once per key):

```powershell
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

### 5.4 Create the first migration (once)

The repository ships **without migrations**, so create the first one before the first run:

```bash
dotnet tool restore --tool-manifest Zayun.Presentation/dotnet-tools.json
dotnet ef migrations add Initial --project Zayun.Persistence --startup-project Zayun.Presentation
```

### 5.5 Run

```bash
dotnet run --project Zayun.Presentation --launch-profile https
```

| What | Where |
|---|---|
| API (https) | `https://localhost:7211` |
| Swagger UI | `https://localhost:7211/swagger` (Development only) |
| OpenAPI JSON | `https://localhost:7211/openapi/v1.json` |
| Health | `https://localhost:7211/health` |

On start-up the API **applies pending migrations and seeds**: the Admin role, the first admin
(`SeedData:AdminEmail` + the password you set) and the starter content. Sign in through `POST /api/auth/login`, or
use `Zayun.Presentation.http`.

### 5.6 The website against your local API

`npm run dev` in `zayun-web` (Vite, `http://localhost:5173`) is already allowed by CORS in
`appsettings.Development.json`. Point the website at `https://localhost:7211`.

---

## 6. Configuration

All keys are documented in `Zayun.Domain/Constants/AppSettings.cs`. Settings are **checked at start-up**
(`ValidateOnStart`), so a missing or invalid value stops the app immediately with a clear message.

| Section | What it controls | Secrets in it |
|---|---|---|
| `AppSettings` | `ApiBaseUrl` (picture links), `WebsiteBaseUrl` — no trailing slash | — |
| `ConnectionStrings` | `DefaultConnection` (SQL Server) | **yes** |
| `Jwt` | `Key` (32+ chars), `Issuer`, `Audience`, `ExpiryMinutes` (15), `RefreshTokenDays` (14) | `Key` |
| `Identity` | password rules (10+ chars, digit, upper, lower), lockout (5 tries → 15 min) | — |
| `Email` | `Enabled`, `FromName`, `FromAddress`, `OwnerInbox` (where Contact messages go), SMTP `Host`/`Port`/`Security`/`Username`/`Password` | `Password` |
| `Cors` | `AllowedOrigins` (the website) | — |
| `Storage` | `Local:PublicRootPath` (`wwwroot/uploads`), `PrivateRootPath` (`App_Data/private-documents`, must be outside wwwroot), `PrivateDownloadPath` (`/private-files`), `SigningKey`, `PrivateLinkMinutes` (5) | `SigningKey` |
| `Images` | `MaxUploadBytes` (15 MB), `MaxSide` (1920), `ThumbnailMaxSide` (640), `WebpQuality` (82) | — |
| `SeedData` | first admin (`AdminEmail`, `AdminPassword`, names), starter data switch and paths | `AdminPassword` |
| `OpenApi` | `Enabled` (off in production), title, paths, contact | — |
| `Serilog` | log levels and sinks (`logs/zayun-.log`, kept 14 days) | — |

| File | Used | Notes |
|---|---|---|
| `appsettings.json` | always | committed; secrets are `""` placeholders |
| `appsettings.Development.json` | Development | local API address, Swagger on, local CORS; **removed from the published output** |
| `appsettings.Production.json` | production | committed with `""` placeholders; the CI pipeline writes the secrets into the published copy |
| User Secrets | Development | `UserSecretsId` is in `Zayun.Presentation.csproj` |
| Environment variables | anywhere | double underscore for `:` — e.g. `Jwt__Key` |

> ⚠️ **Before switching email on:** set `Email:FromAddress` to an address your SMTP server may send from, and
> `Email:Host`/`Port`/`Username` plus the `EMAIL_PASSWORD` secret.

---

## 7. Database, migrations and seed data

### 7.1 Migrations

Migrations live in `Zayun.Persistence/Migrations` and are **applied automatically when the API starts**
(`DbSeeder.SeedDatabaseAsync`). Add one after changing an entity or a configuration:

```bash
dotnet ef migrations add <Name> --project Zayun.Persistence --startup-project Zayun.Presentation
dotnet ef database update     --project Zayun.Persistence --startup-project Zayun.Presentation   # optional: start-up does it too
```

| Table | Holds |
|---|---|
| `Projects` / `ProjectImages` | projects (tags as a JSON column) and their pictures |
| `Publications` | publications (links as a JSON column) |
| `Presentations` | presentations (picture columns, all null when there is none) |
| `ExperienceAreas`, `SkillGroups` | text + skills (JSON column) |
| `CoreExpertise` | the short list on the Skills page |
| `Awards` | awards (picture columns, all null when there is none; `IsFeatured` = also on About / Experience) |
| `Tags` | the shared tag list: `Name` as shown, `NormalizedName` (UPPERCASE key, unique), `Aliases` (JSON) |
| `ContactMessages` | the inbox |
| `Documents` | private documents (file details as columns; the files are in `App_Data/private-documents`) |
| `RefreshTokens` + `AspNet*` | sign-in (Identity) |

### 7.2 Seed data

| Step | When | What |
|---|---|---|
| Roles | every start | `Admin` |
| First admin | the admin email doesn't exist yet | an account with the `Admin` role (`SeedData:Admin*`) |
| `StarterDataSeeder` (`Seeding/Data/starter-data.json`) | `SeedData:StarterDataEnabled`; **each block only when its table is empty** (deleted rows count) | the site's current content; pictures from `Seeding/Data/images` go through the same WebP pipeline as uploads |

Because blocks only run on empty tables, anything the admin changes or deletes is **never brought back** by a
restart.

| Step | When | What |
|---|---|---|
| `TagListSeeder` | the `Tags` table is empty | builds the shared tag list from the names projects, experience areas and skill groups already use, tidying them on the way ("REST APIs" → "REST API", "EF Core" → "Entity Framework Core", "MS SQL" → "SQL Server") |

---

## 8. Authentication and security

| Step | Endpoint | Notes |
|---|---|---|
| Sign in | `POST /api/auth/login` | Admin accounts only; returns tokens |
| Renew | `POST /api/auth/refresh` | refresh tokens rotate; a reused or revoked token ends every session of that account |
| Sign out | `POST /api/auth/logout` | revokes the refresh token |
| Who am I | `GET /api/auth/me` 🔒 | the admin area's session check |
| Password | `PUT /api/auth/password` 🔒 | signs out every session |

- **Access tokens** (JWT) last **15 minutes**; **refresh tokens** last **14 days**; only a SHA-256 hash of each
  refresh token is stored.
- **No public sign-up.** The first admin comes from `SeedData`; more admins can be added in the database (or a
  future admin screen).
- **Lockout** after 5 wrong passwords, for 15 minutes; `POST /api/auth/login` is also rate limited (10 per 5 minutes).
- **CORS**: only the origins in `Cors:AllowedOrigins`.
- **Contact form**: 5 messages per 10 minutes per connection, plus a hidden `website` field that catches robots.
- **Picture uploads** are re-encoded (WebP) and stripped of metadata, so original files are never served.
- **Private documents** never sit under wwwroot; only signed links that expire after 5 minutes can download them.

---

## 9. API reference

Everything is under **`/api`**. The full list with request/response shapes is in Swagger (`/swagger` in
Development). 🛡 = Admin role.

<details>
<summary><b>The website</b> (no sign-in, cached)</summary>

| Method | Path | |
|---|---|---|
| GET | `/projects?category=Featured\|Engineering` | published projects in order, with pictures |
| GET | `/projects/{slug}` | one project |
| GET | `/publications` | publications with links |
| GET | `/presentations` | presentations with pictures |
| GET | `/experience` | experience areas |
| GET | `/skills` | `{ groups, coreExpertise }` |
| GET | `/awards` | awards with pictures (`isFeatured` for the About / Experience highlights) |
| POST | `/contact/messages` | the Contact form (rate limited) |
</details>

<details>
<summary><b>Admin</b> — <code>/api/admin/*</code> 🛡</summary>

| Area | Endpoints |
|---|---|
| Dashboard | GET `/overview` |
| Projects | GET `/projects` · `/projects/{id}`; POST `/projects`; PUT `/projects/{id}` · `/{id}/published` · `/order`; DELETE `/projects/{id}` |
| Project pictures | POST `/projects/{id}/images` (multipart: `file`, `altText`, `label`, `kind`); PUT `/{id}/images/{imageId}` · `/{id}/images/{imageId}/file` (multipart: `file`, swaps the picture) · `/{id}/images/order`; DELETE `/{id}/images/{imageId}` |
| Publications | GET/POST `/publications`; PUT `/publications/{id}` · `/{id}/published` · `/order`; DELETE `/publications/{id}` |
| Presentations | GET/POST `/presentations`; PUT `/presentations/{id}` · `/{id}/published` · `/{id}/image` (multipart: `file`, `altText`) · `/order`; DELETE `/presentations/{id}` · `/{id}/image` |
| Experience | GET/POST `/experience`; PUT `/experience/{id}` · `/{id}/published` · `/order`; DELETE `/experience/{id}` |
| Skills | GET `/skills`; POST `/skills/groups` · `/skills/expertise`; PUT `/skills/groups/{id}` · `/{id}/published` · `/order` (same for `/skills/expertise`); DELETE `/skills/groups/{id}` · `/skills/expertise/{id}` |
| Awards | GET/POST `/awards`; PUT `/awards/{id}` · `/{id}/published` · `/{id}/image` (multipart: `file`, `altText`) · `/order`; DELETE `/awards/{id}` · `/{id}/image` |
| Tags | GET `/tags`; POST `/tags/check` `{ name }` (Existing / New / Doubtful) · `/tags` `{ name }` · `/tags/{id}/merge` `{ intoTagId }`; PUT `/tags/{id}` `{ name }` (rename everywhere); DELETE `/tags/{id}` (only when unused) |
| Inbox | GET `/contact-messages?status=&page=&pageSize=` · `/contact-messages/{id}`; PUT `/{id}/status`; DELETE `/{id}` |
| Documents | GET `/documents?category=` · `/documents/{id}/link` (a 5-minute download link); POST `/documents` (multipart: `file`, `title`, `category`, `notes`); PUT `/documents/{id}` · `/{id}/file` (multipart: `file`); DELETE `/documents/{id}` |
</details>

Bodies for the shared actions: **publish** `{ "isPublished": true }` · **reorder** `{ "ids": ["…", "…"] }` (every
item's Id, drafts included, in the new order).

Plus `GET /health` (outside `/api`, not in Swagger), the pictures at `/uploads/…` and signed document downloads at
`/private-files/…?name=&expires=&sig=` (no sign-in: the signature is the permission; a wrong or expired link is a
404).

---

## 10. Errors

Every failure is a **ProblemDetails** body with a stable `code`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Another project already uses this web address. Please choose a different one.",
  "code": "Project.SlugTaken"
}
```

Validation failures (400) add `errors: { "Field": ["message", …] }`. `detail` is written for people and can be
shown as-is; front ends act on `code`.

| Status | When |
|---|---|
| 400 | validation failed, or a picture that can't be used |
| 401 | not signed in, or the token ran out |
| 403 | signed in, but not an admin |
| 404 | not found (or a draft, on the public routes) |
| 409 | a business rule (e.g. `Project.SlugTaken`, `Project.ImageKindTaken`) |
| 429 | rate limit (`RateLimit.TooManyRequests`) |
| 500 | a bug — logged with full details; the response doesn't leak them |

---

## 11. Caching

The public queries are cached for 60 minutes with `HybridCache` through `[Cacheable(tag)]`; every admin command that
changes the same content carries `[InvalidatesCache(tag)]`, so edits show on the website straight away. Tags:
`projects`, `publications`, `presentations`, `experience`, `skills`, `awards` (`CacheTags.cs`).

**Never cached:** admin screens and the inbox.

---

## 12. Pictures and email

- **Pictures** are stored under `wwwroot/uploads` (`projects/{projectId}/…`, `presentations/{id}/…`, `awards/{id}/…`) and served at
  `{ApiBaseUrl}/uploads/…` with a one-year cache (file names never repeat). Magick.NET fixes rotation, strips
  metadata, shrinks to 1920 px (never enlarges), makes a 640 px thumbnail and writes both as WebP; transparency is
  kept. JPEG, PNG, WebP and HEIC are accepted, up to 15 MB.
- **Private documents** are stored exactly as uploaded under `App_Data/private-documents` (outside wwwroot, hidden
  by web.config). The type is read from the file's contents, never its name; a `.docx` must really contain a Word
  document. A download link is signed (HMAC-SHA256) over the path, the download name and the expiry, so none of
  them can be changed, and it stops working after `Storage:Local:PrivateLinkMinutes`. Deleting a document removes
  its file too.
- **Email** (`IEmailSender`): SMTP through MailKit when `Email:Enabled` is true; otherwise only logged. A Contact
  message is always saved first; a failed email is logged and never loses it.

> The `wwwroot/uploads`, `App_Data` (private documents) and `logs` folders hold **runtime data** and must survive every deployment
> (the pipeline is set up to keep them).

---

## 12a. Tags and the AI tag check

Project tags, experience skills and skill-group skills all come from **one shared list** (`Tags`), so the same
thing is never written two ways.

- **The key.** Each tag has a name as shown ("ASP.NET Core") and an UPPERCASE key without spaces, dots, dashes or
  underscores ("ASPNETCORE"). The key is unique: "asp.net core", "ASP NET Core" and "Asp.Net-Core" are one tag.
- **Built-in rules** (`TagNames`, free, no AI): common synonyms and well-known spellings ("RESTful APIs" → "REST API",
  "MS SQL" → "SQL Server", "typescript" → "TypeScript"), and singular / plural ("Transformer" → "Transformers").
- **The AI check** (`ITagAdvisor` → `ClaudeTagAdvisor`): only for names the rules don't know, while the admin types
  (`POST /api/admin/tags/check`). Claude says whether it means the same as an existing tag, how a new one is usually
  spelled ("pytroch" → "PyTorch"), or that it isn't a skill at all (the admin is asked before adding it). One short
  request with structured JSON output; only the typed name and the tag names are sent.
- **Saving** never calls the AI: names are matched with the rules and new ones added to the list in the same save,
  so saving is fast and works even when the AI can't be reached.
- **Admin → Tags**: rename (everywhere it's used; the old name stays as an alias), merge two tags that mean the same,
  delete a tag nothing uses.

| Setting | Default | |
|---|---|---|
| `Ai:Enabled` | `true` | use the AI check when a key is set |
| `Ai:ApiKey` | empty | **secret** (User Secrets / `ANTHROPIC_API_KEY` in GitHub). Empty = built-in rules only; the API still starts |
| `Ai:Model` | `claude-haiku-5-5` | fast and cheap is plenty for tags |
| `Ai:TimeoutSeconds` | `10` | after this the name is treated as new |

---

## 13. Deployment (CI/CD)

`.github/workflows/zayun-api-ci.yml` → **Zayun API CI/CD**, on every push to `main` (pull requests build only;
manual runs allowed):

1. Restore and build `Zayun.sln` in Release.
2. Publish `Zayun.Presentation` **self-contained for `win-x86`** (the host needs no .NET install; Magick.NET AnyCPU
   works in the default 32-bit pool).
3. Upload the build as an artifact **before** any secrets are added.
4. Write the secrets into the published `appsettings.Production.json` and remove `appsettings.Development.json`.
5. Deploy to **SmarterASP.NET** with MSDeploy, keeping `wwwroot/uploads`, `App_Data` (private documents) and `logs`.
6. If the `API_BASE_URL` repository variable is set, wait for `/health` to answer.

> The workflow expects `Zayun.sln` at the **root of the repository**. If the solution sits in an `Api/` folder of a
> bigger repository, move the workflow to the repository's `.github/workflows` and add
> `defaults: run: working-directory: Api` to the job.

**GitHub repository secrets**

| Secret | Goes to |
|---|---|
| `DEFAULT_CONNECTION` | `ConnectionStrings:DefaultConnection` |
| `JWT_KEY` | `Jwt:Key` (32+ characters) |
| `STORAGE_SIGNING_KEY` | `Storage:Local:SigningKey` (32+ characters, different from the JWT key) |
| `SEED_ADMIN_PASSWORD` | `SeedData:AdminPassword` |
| `EMAIL_PASSWORD` *(optional)* | `Email:Password` |
| `ANTHROPIC_API_KEY` *(optional)* | `Ai:ApiKey` (the AI tag check) |
| `WEBSITE_NAME`, `SERVER_COMPUTER_NAME`, `SERVER_USERNAME`, `SERVER_PASSWORD` | SmarterASP.NET deployment |

**Before each deploy that changes the model:** add the migration locally (§7) and commit it; the API applies it on
start-up.

---

## 14. Adding a feature: the checklist

1. **Show the folder structure first** and agree it before writing code.
2. **Domain**: entity/value object with its rules (throwing only for bugs; website content derives from
   `ContentEntity`), enum values only added at the end, errors in `Shared/Errors/<Area>Errors.cs`, repository
   interface.
3. **Persistence**: `<Entity>Configuration` (lengths, indexes), repository, `DbSet`, register in
   `DependencyInjection.cs`, **add a migration**.
4. **Application**: `Features/<Area>/{Commands,Queries,Dtos,Validators}`; validator messages written for people;
   `[Cacheable]` / `[InvalidatesCache]` with a tag from `CacheTags`.
5. **Infrastructure**: only if a new outside service or setting is needed (settings class + `AppSettings` keys +
   `ValidateOnStart`).
6. **Presentation**: endpoint group in `Endpoints/<Area>`, registered in `EndpointExtensions.MapZayunEndpoints`,
   with `WithName`, `WithSummary`, `Produces…` so Swagger stays accurate.
7. Never break the dependency rule: nothing in Domain or Application references EF Core, Identity, MailKit or
   Magick.NET.

---

## 15. Troubleshooting

| Problem | Fix |
|---|---|
| Start-up fails at step "1: migrations", or tables are missing | the first migration hasn't been added yet (§5.4) |
| `PendingModelChangesWarning` on start-up | the model changed but no migration was added (§7.1) |
| Start-up fails with "… must be set" / options validation | a required setting or secret is missing (§6) |
| "Storage:Local needs … SigningKey" on start-up | set `Storage:Local:SigningKey` (32+ characters) |
| A document link says 404 | it expired (5 minutes): ask for a new one with `/api/admin/documents/{id}/link` |
| "Admin seeding skipped" in the log | `SeedData:AdminPassword` isn't set |
| Browser: CORS error | add the website's origin to `Cors:AllowedOrigins` |
| Pictures don't load on the website | `AppSettings:ApiBaseUrl` must be the API's real public address |
| A picture is refused | must be a JPEG, PNG, WebP or HEIC under `Images:MaxUploadBytes` |
| 429 while testing the Contact form | wait 10 minutes, or restart the API (limits are in memory) |
| Startup failure on the server with nothing in `logs/` | read `App_Data/startup-diagnostics.log` |

---

**Licence:** not specified yet. © Enobong Adahada.
