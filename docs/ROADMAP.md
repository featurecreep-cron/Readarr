# Readarr revival roadmap

Status: in progress since 2026-10-22. Issues: https://github.com/featurecreep-cron/Readarr/issues, board: https://github.com/users/featurecreep-cron/projects/4. Owner: featurecreep-cron. Fork: featurecreep-cron/Readarr.

## Goal

Readarr was archived on 2025-06-27. The retirement notice says "the project's metadata has become unusable" and that the maintainers "are open to someone taking over and revitalizing the project."

This fork makes Readarr work again. It then fixes the design limits that drove users to other forks. The biggest of those is that ebooks and audiobooks can't live in the same instance.

The aim is to hand Servarr a finished product they can take back into the official channels. That is a constraint on how the work is done, not only on what is built:

- Code follows Sonarr's conventions and stays close enough to Sonarr that its fixes keep porting.
- The history is reviewable and every change has a test.
- The [Servarr contribution rules](../CONTRIBUTING.md) are enforced by CI.

## What the research found

Every item in this roadmap traces back to one of six reviews done on 2026-10-21. Section [Sources](#sources) has the details.

1. **Upstream's open issues are mostly bot tickets.** 930 of its 1,075 open issues ask to port one Sonarr commit each, and 890 of those conflict. There are 145 real user issues. The bot backlog is better handled by porting Sonarr's infrastructure directly than by working through 930 tickets.
2. **Metadata is what killed Readarr, and it is still the top complaint in every fork.**
   - The closed bookinfo.club proxy failed in 2023, 2024 and 2025.
   - Every live fork now depends on rreading-glasses. Its Goodreads backend has returned 401 since 2026-08-29, its Hardcover backend is overloaded, and it has one maintainer who has gone quiet.
   - When metadata fails, Readarr has deleted authors, books and files.
3. **Audiobooks are where users went.**
   - Upstream rejected "ebook and audiobook per author" (upstream #1329) as "a sizable rewrite" and told users to run two instances.
   - Every successor with real usage gives a book two slots instead: Chaptarr (550K pulls), Bindery (1.1M), and the ricetim fork.
   - The model was cloned from Lidarr: audio is just a quality on the same edition. There is no narrator, ASIN or runtime, a book can hold one format, and about 16 call sites assume exactly one monitored edition.
4. **Matching uses a music tagger's weights.** An ISBN mismatch weighs 10 and a wrong title weighs 3. Calibre writes UUIDs where an ASIN should be. Bookshelf raised the match threshold from 0.20 to 0.50 without saying so, and users got wrong books, including one that had an unrelated book's file deleted.
5. **Readarr has security holes that Sonarr has already closed:**
   - path traversal in the file mappers
   - zip-slip during extraction
   - `X-Forwarded-For` trusted from anywhere, so auth can be bypassed
   - no lockout, no constant-time comparisons
   - secrets returned by the settings API
   - CVE-2025-50983 (SQL injection through `sortKey`) and CVE-2026-30975/30976
6. **No fork's CI can fail.**
   - `test.sh` always exits 0.
   - Release builds skip the check for unexpected warnings.
   - A stack-overflowing test showed as green in one fork's CI.
7. **The forks all disagree on migration 041.** Faustvii, ricetim, iuliandita, aleksclark, jnaujok and bookshelf each used it for something different, and users already move databases between forks.

## Principles

- **Fix the foundation before building on it.** Nothing new gets built on a broken build, a CI that can't fail, or a metadata source that deletes libraries.
- **Port from Sonarr first, then Lidarr, then Radarr, before writing our own.** Lidarr is the closest match because Readarr was forked from it. Our own code is the last resort.
- **Work from other forks is never cherry-picked.** What they learned informs the design; the code is ours. Ports from Sonarr, Lidarr and Radarr are the exception: they are upstream Servarr.
- **Every behaviour change arrives with a test that fails without it.** CI enforces this with the test gate.
- **Re-architect once, in planned stages, behind a design that has been reviewed.** Don't patch the one-monitored-edition rule site by site.
- **Never lose user data.** A metadata error is never a deletion. A replaced file moves aside before anything is deleted, and nothing outside the destination folder is ever deleted.

## Phases at a glance

| Phase | Outcome | Release |
|---|---|---|
| 0. Project setup | Tracker, board, repo protection, first push | none |
| 1. Foundation | .NET 10, CI you can trust, no dependence on dead Servarr feeds, safe DB migrations | internal builds |
| 2. Security and data safety | The security holes Sonarr closed are closed here | internal builds |
| 3. Import and download stability | The import pipeline stops getting stuck, crashing and losing files | internal builds |
| 4. Metadata bridge | Readarr works again on a metadata service we control | **v0.5.0, first public release** |
| 5. Re-architecture design | A reviewed design for the new model, matching and the metadata v2 contract | runs alongside phases 3–4 |
| 6. Re-architecture | Audiobooks first-class, staged | v0.6 → v0.9 |
| 7. Metadata v2 | Several providers, Open Library fallback, users' own tokens | with v0.8/0.9 |
| 8. Features | Users' most-requested features, built on the new model | v0.9+ |
| 9. Merge-back | Servarr reviews a finished product | v1.0 |

There are also two continuous tracks: **upstream watch** and **issue triage** (see [the continuous tracks section](#continuous-tracks)).

### Why the re-architecture comes in phases 5–6

- **Not first.**
  - Without phases 1–4 there is no working app. Users can't run the redesign and nobody produces the real failure cases the matching rewrite needs as golden tests.
  - A redesign on an unreliable build, with a CI that can't fail and a metadata source that deletes libraries, would be judged by those failures, not by the design.
  - The metadata v2 contract can only be designed once we control the metadata service (phase 4).
- **Not last.**
  - Most of the requested features depend on the new model: narrator tokens, series pages, separate ebook and audiobook lists, library delivery, Hardcover lists keyed by our own IDs. Building them first means building them twice.
  - The audiobook data-loss bug, where an ebook import deletes the audiobook and vice versa, can only be fixed properly in the model.
- **Design starts early.** Phase 5 runs alongside phases 3 and 4. The first two stages of phase 6 are small and safe enough to start as soon as phase 3 is done.
  - 6.1 is a behaviour-preserving refactor.
  - 6.2 fixes the data-loss bug.

## Phase 0: Project setup

- [x] **Enable issues** on featurecreep-cron/Readarr. Forks have them off by default.
- [x] **Create a GitHub Project board** with Status, Phase, Area and Size fields, and **one milestone per phase**.
- [x] **Create the labels:**
  - `type/bug`, `type/feature`, `type/port`, `type/security`, `type/refactor`, `type/ci`
  - `area/*` (metadata, import, download, indexer, audiobook, ui, api, db, calibre, packaging)
  - `port/sonarr`, `port/lidarr`, `port/radarr`
  - `no-test-gate`
- [x] **File every item in this roadmap as an issue.**
  - Each issue states the problem, the acceptance criteria and, for ports, the upstream commit to port.
  - Each epic is a tracking issue with sub-issues.
- [x] **Branches:**
  - `develop` is the default branch.
  - `main` is the release branch, promoted from `develop` by the release workflow.
  - [x] Protection requires the `Build` and `Contribution rules` checks.
- [x] **Tag `v0.4.19`** on upstream's last commit, so version numbering continues from there.
- [ ] **Install the bot app and the promoter app** on the repo and add their secrets. Owner action.
- [x] **Delete the local `foundation` branch.** It is cherry-picks of smilerz commits and is replaced by `modernize`.

## Phase 1: Foundation

**Exit criteria:**
- `develop` builds on .NET 10 for every runtime.
- Every test either runs or is skipped deliberately.
- CI fails when it should, and the image smoke tests pass.

### 1.1 .NET 10

Done locally on the `modernize` branch, not pushed yet.

- [x] Upgrade StyleCop to `Unstable 1.2.0.556`, as Sonarr and Lidarr did (S `5d7c94f8e`, L `2fb9330e8`).
- [x] **Port Sonarr's .NET 10 commit `5d655f98f`:**
  - `WhereBuilder` support for `MemoryExtensions.Contains`. Pinning C# 13 instead would only hide the problem.
  - `X509CertificateLoader`.
  - `KnownIPNetworks`.
  - The Transmission ETA fix.
- [x] Port Sonarr's .NET 8 changes for serialization constructors and the auth handlers (S `6b9262700`).
- [x] Packages at the versions Sonarr uses: MailKit 4.18.1, ImageSharp 3.1.12 with decoders restricted (S `b59552cfe`), Microsoft.Extensions 10.0.13. Pinned Pkcs, Drawing.Common and JWT.
- [x] macOS appdata migration for .NET 8+ (L `1a4c1b6db`), renamed for Readarr.
- [x] **Explicit body binding on write actions.** On .NET 7+, every POST and PUT body binds as an empty object. Readarr registers every API resource type in its DI container, so ASP.NET treats those parameters as `[FromServices]`.
  - Port Lidarr `826b8b593` (S `7166a6c01`), which adds `[FromBody]` everywhere.
  - Add `DisableImplicitFromServicesParameters` as a backstop.
  - Add a regression test that POSTs a real command and saves a setting, and checks both arrive. A reflection test over all actions must keep a visited set: `ProviderBulkResource<T>` constrains its type parameter to itself.
- [ ] **Explicit self-contained publish**, plus a smoke test that starts the built image.
- [ ] **Stop depending on Servarr's Azure package feeds.** They host FluentMigrator, SQLite, Mono.Posix and coverlet.
  - Move to official FluentMigrator 6.2, then `Runner.Core`, 7.2 and 8.0.1 (L `61c23de16`, `8cd45394f`; S `bef298635`, `bb090a7c4`, `210c20f8e`). This drops the SqlClient and SqlServerCe baggage and the pins it needed.
  - Move to official System.Data.SQLite 2.0.2 and sqlite 3.53.4, with the host sqlite3 fallback (L `5e683145e`, `68f07a822`; S `255f1c37c`).
  - Replace Mono.Posix: follow Sonarr's Openur.Mono.Unix move, or use .NET built-ins as the xas fork did.
  - Replace the coverlet nightly with official coverlet.
- [ ] **Decide the minimum Postgres version.** FluentMigrator 7+ implies 15 (S `5f846ab51`).

### 1.2 CI you can trust

Mostly done locally.

- [x] GitHub Actions build ported from Sonarr: 10 runtimes; unit tests on Linux, macOS and Windows; Postgres 16, 17 and 18; integration tests. Green on `develop` since 2026-10-22.
- [x] Contribution rules as checks:
  - commit message style, with no merge or fixup commits
  - no AI attribution lines
  - LF line endings
  - no Lidarr/Sonarr/Radarr names in Readarr code
  - every translation key exists in `en.json`
  - PR template and branch name
  - the test gate
  - 80% coverage on changed lines
- [x] Release, automerge, CodeQL, Scorecard and Dependabot workflows. Actions pinned to commit SHAs.
- [ ] **Unit tests also run in a Debug build.** `ExceptionVerification`, which fails a test that logs an unexpected warning or error, only runs in Debug. Sonarr's CI builds Release, so it never checks this.
- [ ] **A test run that aborts or crashes fails the job.** `dotnet test` already does this; add a check for the summary line as well.
- [ ] **Live tests against the dead Goodreads API** (about 20) move to an excluded test category, and are deleted once phase 4 replaces their target.
- [ ] **Fix the tests that depend on run order**, and the AudioTag test that fails upstream as well.
- [ ] **Image smoke tests:**
  - start the image
  - POST a command
  - PUT a setting
  - check auth is enforced
  - migrate a database from the previous release
- [ ] **Test the gate scripts against real PRs.** `pr_test_gate.py` hasn't run end to end yet.

### 1.3 Safe database migrations

- [ ] **Migration numbering:** continue upstream's sequence, and **detect migrations from other forks** (their `VersionInfo` rows at 041 and above). Then refuse to run, or translate, with a clear message instead of corrupting the database.
- [ ] **A startup schema check:** compare mapped columns with the live schema, report problems as a health check, and test it in CI. This follows the ricetim fork.
- [ ] **Migration fixtures** from real Readarr 0.4.19, Faustvii, bookshelf (Goodreads and Hardcover images) and ricetim databases.

### 1.4 Independence from Servarr services

- [ ] Remove Sentry and analytics.
- [ ] Point the update check at our GitHub Releases. Servarr's update server isn't ours.
- [ ] `SystemTimeCheck`: read the `Date` header, because Servarr's `/time` endpoint is gone.
- [ ] Replace the Goodreads sign-in that went through Servarr's OAuth with something else, or remove it.

## Phase 2: Security and data safety

All of these are ports, in Servarr's commit order. Detailed sources are in [port-plan.md](port-plan.md), section 1.

**Exit criteria:**
- Everything in port-plan section 1 is done.
- An external security pass finds nothing new in these areas.

- [ ] **Path traversal in the static, backup and log file mappers** (L `27f47e27c` → `fd6f97640`), and path validation that rejects relative path segments (S `b0bc05270`).
- [ ] **Zip-slip** (L `f61daede7`).
- [ ] **Forwarded headers trusted only from private networks** (L `6bcead8bf` → `350860e52`). Then **Allowed Hosts** and **Trusted Networks** (the L `45d68ee9a` series), which need health-check reasons (L `4759a2621`).
- [ ] **Authentication:**
  - remove Basic auth and migrate existing setups to Forms (L `7cdb4e4b5` + `96e5a4df2`)
  - Forms auth by default (S `14771abc5`)
  - constant-time comparison for the API key and password hashes (S `c340e50b7`)
  - PBKDF2 iterations raised to OWASP's recommendation (S `a83ffea24`)
  - login lockout (S `0bdf2087a`)
  - auth responses never cached, and the login redirect fixed (S `bc7c87def`, `f722e5137`)
- [ ] **The settings API stops returning secrets** (S `9efa5a069`, after the certificate series).
- [ ] **The command body is ignored when deserialising a command** (R `14afe5286`).
- [ ] **Allowlist the `sortKey` parameter** (CVE-2025-50983), and an **SSRF allowlist for the media cover proxy**. The iuliandita fork did both; check whether Sonarr has its own version first.
- [ ] **Keep secrets out of logs:** RSS URLs, email addresses, the NZBGet token, tracker passkeys (S `0ade5b4c2`, `91aeb5260`, `0ad7a31fb`).
- [ ] **Windows AppData permissions:** stop granting Everyone the Modify right (S `93b5da55b`). Block more system directories from being used as folders (S `5f5fd1e5e`).
- [ ] **Backup restore** validates the database and keeps the old one until the restore succeeds (S `460022a72`).
- [ ] **Data safety (our own work):**
  - **A metadata error is never a deletion.** Distinguish not found, error and removed. Extend upstream's guards (upstream #4108, #4109) to every refresh path.
  - **Safe replace:** move the old file aside, place the new one, then recycle the old one, restoring it on failure. Recycle bin on by default.
  - **Never delete outside the destination folder, or another book's file.** A low match threshold has caused exactly this in another fork.

## Phase 3: Import and download stability

**Exit criteria:**
- A download either imports, or ends in a visible state with a reason and a retry.
- No `NullReferenceException`s in the import pipeline under the golden test corpus.

### 3.1 Import pipeline defects

All of these exist in plain Readarr. Each gets a failing-first test.

- [ ] A list book that names no edition crashes the add, and the sync summary misreports the result. The `.Single(x => x.Monitored)` call in `AddSkyhookData` also happens in plain Readarr.
- [ ] One book without a monitored edition stops import events for the rest of the batch. Use Sonarr-style isolation for each item.
- [ ] `UpgradeBookFile` crashes when there is no root folder, and ignoring an unmatched download fails.
- [ ] An import that throws ends in a visible ImportFailed state with its reason. Do it the way Sonarr does.
- [ ] Book files that were never loaded crash the import. Fix the cause: `BookInfoProxy.AddDbIds` copies the id but not `BookFiles`.
- [ ] A download without a remote book is retried forever.
- [ ] Map a download to its book from the grab event's release name, not the newest history item.
- [ ] Manual import doesn't say a file was already imported. Use path history plus size and name, not size alone.
- [ ] Manual import from a path drops the download id.
- [ ] Tests declare the warnings they expect (`ExpectedWarns`).

### 3.2 Download lifecycle

- [ ] **ImportFailed can be retried**, by hand and automatically up to a limit.
- [ ] **Stalled downloads:** a minimum-seeders check and a stalled timeout, ported from Sonarr.
- [ ] **Adopt orphaned downloads**, ones we didn't grab, and record where they came from .
- [ ] When manual import offers nothing because the folder holds only archives, say why.
- [ ] Manual import for an author not in the library: add the author and book unmonitored, then import.
- [ ] Sonarr ports:
  - stale queue entries (S `e38a2af1a` → `adf53108f` → `f416c322b`)
  - re-grab of an already-imported torrent
  - fall back to another download client on failure (S `6bb694a6f`)
  - pending-release fixes
  - validation of pushed releases

### 3.3 Database and reliability

- [ ] **`BasicRepository.UpdateFields` swallows exceptions** (verified, both overloads). Porting L `944970a1f` fixes it; add a regression test.
- [ ] SQLite:
  - retry on "database is locked"
  - busy timeout of 1000 ms
  - bulk updates in a transaction (L `944970a1f`, `3d48a982d`, `538e7534f`)
- [ ] Housekeeping doesn't run while other tasks run (S `04d98098e`).
- [ ] Edition write amplification: one import of Pride and Prejudice issues 12,794 single-row updates. Make it one set-based update (upstream #2101).

### 3.4 Download clients, indexers, HTTP

- [ ] **qBittorrent:**
  - the `/login` check
  - testing after changing credentials
  - 5.2 login
  - API keys
  - cookie reuse
  - Basic auth (L `1992127e9` … `d93342d8d`)
- [ ] Deluge reconnects, Transmission labels, Flood post-import tags.
- [ ] Use the indexer's default page size; decode HTML entities in release titles.
- [ ] HTTP:
  - parsing URLs that breaks under some locales
  - lazy JSON deserialization
  - the `HttpRequestMessage` leak
  - a 30-minute pooled connection lifetime
  - Happy Eyeballs (the S `5d8d2d66a` series)
- [ ] Restart handling (S `6cd1ab376` → `dbb5c33f1`).

### 3.5 Cheap upstream bugs

The open-issues review lists 30. The ones that won't be absorbed by later phases:
- [ ] Calibre entries without a file abort the scan (upstream #419, #468, high priority upstream).
- [ ] Multi-format releases are parsed and imported as their first format only (upstream #1440, #1707).
- [ ] One bad item fails a whole bulk operation or list sync (upstream #1703, #3013, #405).
- [ ] Search queries drop the title after a colon and miss initials (upstream #2057, #2115, #1572).
- [ ] UI text and label fixes (upstream #2584, #1204, #1377, #1378, #425, #1732).
- [ ] Settings bugs (upstream #2238, #1232, #2367, #3101, #1404, #1509).

## Phase 4: Metadata bridge → v0.5.0

**Exit criteria:**
- Adding an author, a book, a series and an import list works against our metadata service, on a fresh database and on a migrated one.
- A metadata outage shows up as a health check and never deletes anything.
- The image is published.

### 4.1 Our metadata service

**Decision: fork blampe/rreading-glasses to featurecreep-cron/rreading-glasses.**

Why fork rather than the alternatives:
- **Depending on it as-is** repeats the single-point-of-failure that killed Readarr. Its maintainer is quiet, its Goodreads backend returns 401, and its Hardcover backend is overloaded.
- **Writing our own service** would spend months re-solving problems it already solves: request coalescing, asynchronous author loading, batching, and the API quirks Readarr depends on.
- **Putting provider clients inside Readarr** doesn't fit merge-back, because every Servarr app talks to a Servarr-hosted proxy. It also puts Hardcover's per-user rate limits and yearly token expiry in front of every user.

Work in the fork:
- [ ] **Cherry-pick the open community fix PRs from upstream:** #585, #582/#583, #601, #557. Review #596–598 against our own retry and filters.
- [ ] **Reliability fixes:**
  - the counter race that left a flag stuck at 1 and caused a hang
  - a panicking refresh still completes
  - retry 429/5xx with backoff, honouring `Retry-After`
  - never cache an empty author
- [ ] **Bound the crawl** (upstream #580) and **purge the database** (upstream #554).
- [ ] **Respect Hardcover's limits:** 60 requests per minute, a burst of 10 and 5,000 a day on the free tier, and searches sent separately.
- [ ] **Publish the fork's image.** Readarr defaults to the hosted instance once one exists; until then, self-hosting is documented.
- [ ] **Ask Hardcover in writing** about hosting a shared instance, caching and redistribution.

### 4.2 Readarr side

- [ ] **Search goes through the metadata service**, not goodreads.com's `auto_complete`. Remove the dead `api.bookinfo.club` default.
- [ ] **A visible metadata setting** (not a hidden page) with a health check and a **status display** (upstream #3050, 15 reactions).
- [ ] **Widen `ForeignId` to `long`.**
- [ ] **Show a loading state for authors** while the service is still crawling them. Today the partial author the service returns first counts as "new books".
- [ ] **Explain why a book is hidden:** language, series or popularity filter.
- [ ] **Search debounce, a rate limiter and short-lived caches.** Bookshelf turned caching off and got 429s.
- [ ] **A memo of remote searches with an expiry**, so the same failed lookup isn't repeated. Bookshelf's version has no expiry, and its migration number collides with ours.

### 4.3 Shipping v0.5.0

- [ ] An LSIO-style image (PUID/PGID) for amd64 and arm64.
- [ ] Migration from Readarr 0.4.19, Faustvii and bookshelf databases, using the fixtures from 1.3.
- [ ] Release notes generated by `next_version.py`.
- [ ] A migration guide.

## Phase 5: Re-architecture design

Runs alongside phases 3–4.

**Exit criteria:** design documents (ADRs) accepted for each item below, and a golden test corpus checked in.

The requirements come from four sources, and every requirement cites them:
- the open-issues review: 18 requirements, R1–R18
- the closed-issues review: requirements, plus the things upstream rejected and why
- a metadata-profile spec (ordered languages, separate ebook and audio formats, edition policy, title policy, series and contributor roles)
- notes on Hardcover's schema: language and format live on editions, and each book has a default edition per format

### 5.1 The domain model

```text
Contributor ──< Credit (role: author, narrator, translator, editor, ...) >── Edition
Author = Contributor with works; aliases and merges

Series ──< SeriesEntry (position text + sort key, primary flag) >── Work
Series may have a parent series

Work (what you want)
  ├── Want (one per MediaType: ebook, audiobook)
  │     monitored, quality profile, root folder, naming, language preference,
  │     edition policy, chosen edition (optional)
  └── Edition (a published form)
        MediaType, format, language, ISBN-10/13, ASIN, runtime, abridged,
        publisher, release date, external IDs per provider
          └── MediaFile (what you have)
                one or more files (audiobook parts), provenance (grab, release, download)
```

Decisions to record:
- **What you want is at the work level, per media type.** One instance handles both; either type can be switched off. (upstream #1329, #3288, #1508, #2413, #2339, #4095)
- **Editions are chosen by a policy:** the source's default edition for that format if it qualifies, otherwise the best score in a preferred language. Pinning an edition is optional. Upstream switching editions automatically must never silently break a file's link. (upstream #3167, #3342, #2472, #3475, #1723)
- **"Exactly one monitored edition" is gone,** replaced by one accessor for the selected edition per media type.
- **Several files per work:** a type can be upgraded only by a file of the same type, and "combine" adds files, never deletes them. (upstream #1124, #1297, #368, #841)
- **Contributors are many-to-many, with roles.** Author aliases and merging. (upstream #3157, #1654, #842, #3274, #1619)
- **Series are first-class:** spanning authors, with sub-series, a primary series the user chooses, and monitoring at series level. (upstream #3129, #2607, #2225, #1524, #1236)
  - User overrides need a designed override layer with merge rules. Upstream rejected ad-hoc overrides (upstream #1769).
- **Omnibus, box sets and anthologies:** one file can satisfy several works. (upstream #3016, #3858, #1753, #1507)
- **Language and title policy live on editions.** A work never takes one edition's foreign title. Accents and umlauts are folded when searching and matching. (upstream #1065, #1238, #3273)
- **Editions are stored so they scale:** large works aren't fully stored or monitored, and writes are batched. (upstream #2101, #2783)
- **IDs:** stable internal IDs, plus external IDs per provider namespace. Never store one provider's IDs in another provider's columns.

### 5.2 The matching pipeline

Evidence, strongest first:
1. The grab you made. Identify against the book the download was grabbed for; don't re-match it. (upstream #2723, #3131; iuliandita and ricetim)
2. An exact identifier match (edition id, ISBN, ASIN) across all editions. Identifiers are validated first: Calibre's UUIDs are not ASINs. A differing identifier counts as unknown, not as a mismatch. (upstream #1962)
3. Title and author, including alternative titles and series-aware splitting (the Faustvii fork's approach).
4. Series position.
5. Weak signals.

Rules:
- **Media type is a gate, not a penalty.**
- **Score explanations are visible in manual import.**
- **The threshold stays fixed.** Upstream rejected a tunable one (upstream #588, #3436), and the signals above are what replace it.
- **Audiobooks are an ordered set of parts.** Folder structure beats ID3 tags. Fix part renumbering. (upstream #2152, #3346, #1910; ricetim `0124fb7854`)
- **Packs and multi-format releases:** parse every format, accept a release if any file qualifies, and map files individually. (upstream #1440, #1707)
- **Inspect a release before downloading it.** Refuse archive-only, mixed-media and bundle releases when they don't match, record the reason, and blocklist the release.
- **A golden test corpus** built from real logged cases (including one fork's 19 wrong grabs out of 27), so weights get tuned against evidence.

### 5.3 The metadata v2 contract

Designed together with the fork:
- Full edition lists, without de-duplicating audio editions.
- Contributors per edition, with roles.
- `reading_format`, `audio_seconds`, abridged.
- A default edition per format, a cover edition, a compilation flag.
- External ID mappings.
- ISBN/ASIN → work lookup.
- Audnexus enrichment by ASIN.

The v1 API stays available for older clients.

### 5.4 Migration, API and UI

- **Data migration** from the current model, including **merging an ebook instance and an audiobook instance into one**. This is the official workaround upstream gave users, so many have exactly that setup.
- **API:** v1 keeps working through a compatibility layer, for Prowlarr and the other tools that call it, and the new model gets a v2.
- **UI:** a separate state for each media type throughout: book page, author page, wanted, calendar, manual import.

## Phase 6: Re-architecture

Staged, and each stage ships on its own.

| Stage | Content | Release |
|---|---|---|
| 6.1 | **One accessor for the selected edition.** Replace the ~16 `Single(x => x.Monitored)` sites. Behaviour stays the same; this makes every later stage possible. | 0.5.x |
| 6.2 | **Media type as a dimension.** `MediaType` on quality, edition and file; separate cutoffs for ebooks and audiobooks; **an import replaces only files of the same type**, which fixes the bug where an audiobook import deleted the ebook. | **0.6** |
| 6.3 | **Contributors with roles, and audiobook fields:** narrator, ASIN, runtime, abridged. The metadata v2 endpoints, using Hardcover plus Audnexus. Naming tokens `{Narrator}`, `{ASIN}`, `{ISBN}`, `{Runtime}`, `{Medium}`. (upstream #2619, #1943, #1910) | 0.7 |
| 6.4 | **A want per media type**, each with its own profile, root folder and naming; **indexers and download clients routed by media type** (categories per type); UI throughout; the migration that **merges two instances**. | **0.8** |
| 6.5 | **The new matching pipeline** (5.2), including inspecting releases before grabbing and the golden tests. | 0.9 |
| 6.6 | **First-class series, author aliases and merging, languages per edition, scalable editions.** | 0.9 |

## Phase 7: Metadata v2

- [ ] **Open Library as a fallback.** Keyless, CC0, with bulk dumps available. The aleksclark fork showed it can implement every provider interface.
- [ ] **Users can supply their own Hardcover token**, to get past the shared limits.
- [ ] **An optional direct Audnexus client in Readarr**, so audiobook details survive an outage of the metadata service.
- [ ] **Wikidata for linking IDs across sources.** Google Books as optional enrichment.
- [ ] **A primary and a secondary metadata URL, with failover** (iuliandita's design).

## Phase 8: Features on the new model

In rough order of demand:
- [ ] **Write metadata into files without Calibre:** EPUB series, IDs, title, cover, language; audio tags and narrator, with sanitised tags; the M4B cover written last.
- [ ] **Library delivery targets** (Calibre-Web Automated, Booklore, Audiobookshelf, Kavita), with tracked state, retries and duplicate checks.
- [ ] **Import lists:** Hardcover (rebuilt on our IDs, paginated, with honest summaries), Open Library, NYT, Bookwyrm, RSS. (upstream #3854, #1709, #2646, #2877)
- [ ] **An OPDS server** (upstream #895, 23 reactions). Upstream PR #3344 is a reference.
- [ ] **Formats:** m4a, opus, mka (upstream #1212, #1879, #3391, #3118), KEPUB conversion (upstream #2372, PR upstream #4099), and the 255-character filename limit (upstream #3996).
- [ ] **A series page and collections** (upstream #2607). Tags, ratings and notes on books (upstream #1284).
- [ ] Optional merging of multi-part audiobooks into one chaptered M4B (jnaujok and Chaptarr do this). Booksonic sidecar files (upstream #1623).
- [ ] Monitoring settings that make sense to users.

## Phase 9: Merge-back → v1.0

- [ ] **Sonarr parity checks:** run the upstream-watch backlog down to zero for the shared code.
- [ ] **A review pass of the history:** small, reviewable commits, each with tests.
- [ ] **Metadata hosting:** offer Servarr the metadata service to host, as Lidarr's metadata server is (LidarrAPI.Metadata). Have written terms from Hardcover and a position on Audible/Audnexus first.
- [ ] **Present the finished product to Servarr.** Owner action. By then the human-review attestations in the PR template should be backed by the real review history.

## Continuous tracks

- **Upstream watch.** A weekly workflow (the iuliandita fork has one) lists new Sonarr, Lidarr and Radarr commits that touch shared code. Each one becomes a `type/port` issue or gets marked N/A. This replaces upstream's 930 bot tickets.
- **Issue triage.** Upstream's 145 real user issues are filed into the phase where they belong, or recorded as out of scope with a reason.
- **Dependencies.** Dependabot, with a cooldown before updates are proposed. Runtime updates cut a patch release.

## Out of scope

These follow upstream's decisions, with its reasons:
- **Native scraping indexers** such as Libgen (upstream #848, 239 reactions) and IRC/DCC (upstream #1364). Use Prowlarr or Jackett over Torznab instead. Libgen is upstream's most-requested feature, so this needs a clear answer in the README.
- **Scraping Goodreads or any other site for metadata.** APIs and open datasets only.
- **Comics, manga, magazines and podcasts** (upstream #46, #389, #3722, #2245).
- **A built-in reader.** OPDS is in scope.
- **A user-tunable match threshold.**
- **"Search all missing" without rate limits.**

## Tracking the work

**GitHub Issues plus a GitHub Project on featurecreep-cron/Readarr**, with one milestone per phase. Every checkbox above becomes an issue with acceptance criteria. This document stays the overview and is updated at the end of each phase.

**fh / frictionless: not now, revisit later.** fh's approach (human acceptance of every candidate, independent review, verification in a disposable environment) fits Servarr's "a human owns every line" rule well, and it reads its work from a GitHub Project board, so this tracker is ready for it. It doesn't fit yet, as of 2026-10-21:
- **Its verifier runs with no network.** Readarr can't build without restoring NuGet and yarn packages, so every verification would fail until we prepare an offline package cache.
- **It is changing every day.** The README says it is frozen, but it has had commits daily, there is active Phase 1 "slices" work, and its core flow has open bugs (upstream #329, #325, #321).
- **Its review tooling targets Go.**

**What would change the decision:** fh supports offline .NET and Node restores, and its sliced-item flow is stable. Then pilot it on one self-contained phase 3 item before using it more widely.

## Sources

Six reviews were done on 2026-10-21. Their findings, with the issue, PR and commit numbers above, are what this document is built from. The raw data they pulled was kept only for that planning session.

| Review | Scope | Key output |
|---|---|---|
| Lidarr, Sonarr and Radarr commits since 2025-06-27 | 178, 871 and 245 commits | [port-plan.md](port-plan.md) |
| Upstream open issues | 1,075 (145 from users) | R1–R18, top 40 by demand, 30 cheap bugs |
| Upstream closed issues and PRs | 2,155 issues, 877 PRs | rejected features and why, PRs worth reviving, maintainer statements |
| smilerz/bookshelf and pennydreadful/bookshelf | 47 and 93 issues, 23 and 64 PRs, 35 commits | per-issue status, root causes of the failing tests, what to reimplement and what to avoid |
| Other forks and successors | Faustvii, ricetim, iuliandita, aleksclark, jnaujok, xas; Chaptarr, Bindery, Listenarr | what to reimplement, the migration-041 collision, demand signals |
| Metadata service | rreading-glasses, the smilerz fork, Hardcover, Open Library, Audnexus and others | the fork decision, API contract, audiobook gaps |
