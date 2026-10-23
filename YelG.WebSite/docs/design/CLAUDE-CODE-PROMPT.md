You are building the public corporate website for YelG Advisory as an **ASP.NET Core MVC (.NET 9)** application. Arabic is the primary language and English is secondary. Every homepage section must be fully manageable from an **admin panel**.

## Where things are
- This MVC project: `E:\YelG\YelG.AdminPortal\YelG.WebSite` (fresh .NET 9 MVC template), inside the `YelG.AdminPortal` folder. Inspect the parent folder/solution too.
- `docs/design/README.md` — decisions, page structure, verified content, items pending approval, backend needs.
- `docs/design/B-Ar.dc.html` — approved Arabic homepage (RTL, Alexandria + Sora).
- `docs/design/B-En.dc.html` — approved English homepage.
- `wwwroot/assets/` — all images and the brand video. The design files already reference them as `/assets/<file>`.

These two files are "Design Component" prototypes (`<x-dc>`, `<helmet><style>`, `<sc-if>`, `<sc-for>`, `{{holes}}`, and a `class Component extends DCLogic` script). Treat them as the exact spec for layout, CSS, copy and behaviour.

- Port the markup to Razor views and the CSS to static files.
- Port the client-side logic to small vanilla JS modules (no SPA framework).
- Do NOT invent, rewrite or "improve" any copy, claims or numbers. Their current text becomes the **seed data**.

## Target stack
- .NET 9, ASP.NET Core MVC, EF Core 9, **PostgreSQL** (Npgsql). The production server is a shared Contabo Ubuntu box with limited RAM, so do not use SQL Server.
- ASP.NET Core Identity for admin users. Roles: `Admin`, `Editor`.
- Keep `YelG.WebSite` as the web project (public site plus `Areas/Admin`). Add layered class libraries next to it, with dependency direction Web → Application → Domain and Infrastructure → Application:
  - `YelG.WebSite.Domain` — entities and value objects
  - `YelG.WebSite.Application` — services, DTOs, validation (FluentValidation), interfaces
  - `YelG.WebSite.Infrastructure` — EF Core DbContext, configurations, migrations, file storage, email
  - `YelG.WebSite.Tests` — xUnit
- If a solution or shared projects already exist in `YelG.AdminPortal`, propose how to fit into them instead of duplicating anything.
- Remove the template's Bootstrap and jQuery only if the design does not need them, and say so in the plan.
- Async throughout, `AsNoTracking` for reads, no N+1 queries, and output or memory caching on public pages that is invalidated when admin saves.

## Bilingual content model
Every editable text field stores both languages: either `*Ar` / `*En` properties, or one owned `LocalizedText { Ar, En }` value object. Propose which one in the plan.
- Routes: `/ar/...` (default, `dir="rtl"`, `lang="ar"`) and `/en/...` (`dir="ltr"`, `lang="en"`), handled by a culture route constraint and `RequestLocalization`.
- The language switch keeps the user on the same page.

## Admin panel — one management screen per section
In `Areas/Admin`, RTL Arabic UI, responsive, using the same brand tokens. Each item gets list, create, edit, delete, reorder and publish/hide, with Ar/En side-by-side fields.

| Section | What the admin manages |
|---|---|
| Site settings | Logo, tagline, contact email, phone, website, LinkedIn URL, careers email, default language, SEO defaults |
| Hero | Eyebrow, headline (with highlighted part), supporting text, two CTAs (label + target), self-check link text, hero image, the 3 stats (value + caption) |
| Services marquee | Ordered list of labels, or generated from services |
| About | Heading, paragraph, values list, qualifications/credentials (code + description), the two collage images |
| Founder | Photo, quote, supporting line, name, title |
| Service categories & services | Categories (key, name). Services (icon key, category, name, short description, long description, ordered deliverables list) |
| Why YelG | Heading, lead, differentiator cards (title + text, highlight flag), image, floating card text |
| How we work | Heading, lead, ordered steps (title + text) |
| Purpose | Mission, vision, goals list, brand video + poster |
| Ways to work with us | Cards (title, text, bullet points, CTA label, pre-selected service, highlight flag) |
| Industries & clients | Sector chips, client category cards, approval note. Optional client logos with an `IsApprovedForWeb` flag; only approved logos render |
| Self-check | Questions (area, text, linked service), answer options with scores, result bands (min score, title, text), disclaimer text |
| FAQ | Ordered question/answer pairs |
| Insights | Posts (image, category label, date, title, external URL) |
| Careers | Section text, hiring image, open roles (title, tags, location, experience, apply link/email, active flag) |
| Client portal | Status (ComingSoon / Live), text, portal URL. The sign-in button renders only when Live and a URL is set |
| Enquiries | Inbox of consultation requests: list, filter, view, status (New / Contacted / Closed), internal notes, CSV export |
| Media library | Upload images and video (type and size validation, generated WebP thumbnails), pick from library in all editors |
| Users | Admin and Editor management (Admin only) |

Also:
- An audit log of who changed what and when.
- A "Preview" button that opens the public page in the chosen language before publishing.

## Public site
- One homepage per language, rendered from the database. All sections, IDs, anchors, interactions, breakpoints (1180 / 900 / 760 / 380px), hover effects (image shine, service-card navy fill) and reduced-motion rules must match the prototypes.
- No custom mouse cursor, and no "9 financial services" chip in the hero (both intentionally removed).
- Arabic must keep: the letter-spacing reset, the `.lat` isolation for Latin terms, LTR email and phone fields, and the mirrored hero and about positions.
- Accessibility: skip link, landmarks, aria on tabs and the form, focus styles, 44px touch targets.
- SEO: per-language titles and meta, `hreflang` alternates, Open Graph, `sitemap.xml`, `robots.txt`.

## Consultation form (now with a real backend)
- POST to a controller with antiforgery, server-side validation that matches the client messages (Ar/En), honeypot and rate limiting.
- Save each request to the `Enquiries` table. Send a notification email to the address in Site settings via SMTP (the company runs mailcow), with credentials taken from configuration.
- Show the success message only after the save succeeds. If the email fails, the enquiry is still saved and the error is logged.
- Store consent with a timestamp.
- The self-check result and the "Discuss this service" buttons pre-fill the form exactly as the prototype does.

## Seed data
Write one idempotent seeder that loads ALL current Arabic and English content from the two prototype files, plus a first Admin user whose credentials come from configuration and environment variables, never hard-coded. Do not seed anything the README lists as "needs company approval" beyond what the prototypes already show.

## Step 1 — audit and plan (stop and wait for my approval)
1. Inspect this repository and tell me what already exists.
2. Propose the solution structure, the full entity model (ERD as a table), the localization approach, the admin routes and screens, and the migration plan.
3. Map every prototype section and interaction to its entity, view and JS module.
4. List anything you cannot reproduce exactly, and why.

Do not write code until I approve the plan. Do not modify anything under `docs/design/`.

## Step 2 — build in phases, and show me each phase before continuing
1. Solution skeleton, Domain, DbContext, migrations, seeder.
2. Public site (Arabic first, then English), pixel-faithful to the prototypes.
3. Admin panel, section by section, plus the media library.
4. Enquiries backend, email, security hardening.
5. Tests: services, validation and seeder idempotency.

## Step 3 — verify
- `dotnet build` and `dotnet test` pass.
- Check the pages at 375, 768, 1024 and 1440px in both languages: no horizontal scroll, and nothing overlapping in the hero.
- Every admin edit appears on the public site after save.
- Report what is done, what differs from the prototypes, and what still needs company approval or server setup. Provide `appsettings.Production.json.example` and keep real secrets out of git.

Work on a new branch. Do not push, deploy, or touch the shared server without asking me.
