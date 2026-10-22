# Port plan: Lidarr / Sonarr / Radarr commits since Readarr was archived (2025-06-27)

This plan merges three reviews into one list without duplicates:

| Upstream | Commits reviewed | Candidates | Test cherry-picked |
|---|---|---|---|
| Lidarr | 178 | ~45 | 85 |
| Sonarr | 871 | 166 | all 166 |
| Radarr | 245 | ~85 | all 245 |

When the same change exists in more than one upstream, the **source** column names the copy to port, in this order:
1. **Lidarr (L)**, because Readarr was forked from Lidarr and shares most file paths.
2. **Sonarr (S)**, otherwise.
3. **Radarr (R)**, only when the change is Radarr-only.

The other copies are listed so they can be compared.

Raw review output is in the session scratchpad: `review-{lidarr,sonarr,radarr}/`.

## How to read "applies"

| Value | Meaning |
|---|---|
| clean | `git cherry-pick` applies with no conflict |
| trivial | Conflicts are only file renames, `en.json`, or a moved package version (Readarr pins versions centrally in `Directory.Packages.props`) |
| manual | Needs a real port: the code is structured differently, the frontend is JS where upstream has TSX, or the API is V1 where upstream has V3/V5 |

## 0. Prerequisites: the .NET 10 bump (branch `net10`, in progress)

- **SDK 10.0.402:**
  - Retargeted to `net10.0`.
  - Ported Sonarr's .NET 10 commit `5d655f98f`: `WhereBuilder` support for `MemoryExtensions.Contains`, `X509CertificateLoader`, `KnownIPNetworks`, and the Transmission ETA overflow fix.
  - Bumped the packages the audit flagged: MailKit 4.18.1, ImageSharp 3.1.12, Microsoft.Extensions.* 10.0.13.
  - Pinned System.Drawing.Common, Pkcs and the JWT packages.
  - Removed package references the runtime now provides.
- **StyleCop.Analyzers.Unstable 1.2.0.556:** needed for C# list patterns. Done the way L `2fb9330e8` / S `5d7c94f8e` did it.
- **Still to include:** the macOS appdata migration for .NET 8+, R `2a886fb26` → `554a54b00` → `0297dba7f`. Without it, macOS users lose their config folder.

## 1. Security (do first)

| # | Change | Source | Also in | Applies | Notes |
|---|---|---|---|---|---|
| 1.1 | Path traversal in static, backup and log file mappers | L `27f47e27c` → `fd6f97640` | S `388ba2dfd`, R `1a2b90bf3`/`4b85fab05` | manual | Readarr has no `..` or containment check. Readarr's `GetResponse` is synchronous. |
| 1.2 | Path validation rejects relative segments; config-controlled `UiFolder` removed | S `b0bc05270` | | manual | |
| 1.3 | Zip-slip in `ArchiveService` (update and backup restore) | L `f61daede7` | S `f089df054`, R `94ef97b1f` | clean | 7 lines |
| 1.4 | Forwarded headers trusted from anyone (local-address auth bypass) | L `6bcead8bf` → `350860e52` | S `79f1d2e38`/`7ef3b6bd0`, R `7d8444c43`/`90cd8df1a` | trivial | `Startup` clears `KnownNetworks`/`KnownProxies` today |
| 1.5 | Allowed Hosts (DNS rebinding) and Trusted Networks | L `45d68ee9a` → `13193ee97` → `5d123ab76` → `ec91c2fb2` → `014f57975` → `a75cb23aa` | S `1579a8bb7`/`022f69e14` and follow-ups, R `91c6dc4ea`/`387d43d7a`/… | manual | Needs health-check reasons (S `beadc687a` / L `4759a2621` / R `72ae0c011`). Frontend is JS in Readarr. |
| 1.6 | Remove Basic auth; migrate existing configs to Forms | L `7cdb4e4b5` + `96e5a4df2` | S `dfb6fdfbe`, R `d478b404d`/`8000abc2b` | trivial | |
| 1.7 | Forms auth on by default | S `14771abc5` | | manual | 18 files |
| 1.8 | Constant-time comparison of API key and password hash | S `c340e50b7` | | trivial | |
| 1.9 | Raise PBKDF2 iterations to OWASP levels | S `a83ffea24` | | trivial | |
| 1.10 | Lockout after failed logins | S `0bdf2087a` | | manual | |
| 1.11 | Settings API no longer returns secrets | S `9efa5a069` | | manual | Needs certificate series 4.4 |
| 1.12 | Auth endpoints not cacheable; 401 instead of redirect for XHR | S `bc7c87def`, `f722e5137` | R `cb59779cf`, `74621669e` | trivial / clean | |
| 1.13 | Command body ignored when deserializing `CommandResource` | R `14afe5286` | S `b949ab097` | clean | Check overlap with foundation's #43 |
| 1.14 | Windows AppData no longer grants Everyone Modify | S `93b5da55b` | | manual | |
| 1.15 | Block more system directories as root or download folders | S `5f5fd1e5e` | | clean | |
| 1.16 | Log cleansing: RSS keys, email addresses, NZBGet token | S `0ade5b4c2`, `91aeb5260`, `0ad7a31fb` | R `2fc6df4f6` | trivial / clean | |
| 1.17 | Restrict ImageSharp decoders and suppress 3.x advisories | S `b59552cfe` | L `903c8cca7`, R `2c205edec` | done in `net10` | |
| 1.18 | Restore validates the DB and keeps the old one until the restore succeeds | S `460022a72` | | manual | |
| 1.19 | Fail executable and dangerous downloads by default | S `f26344ae7` … `1c01b720a` (+5 earlier prerequisites, MonoTorrent 3) | | manual | Large; BookImport needs adapting |

## 2. Database and migrations

| # | Change | Source | Also in | Applies |
|---|---|---|---|---|
| 2.1 | Official FluentMigrator 6.2, then `Runner.Core`, Postgres generator id, 7.2, 8.0.1 (minimum Postgres 15) | L `61c23de16` → `8cd45394f`; then S `bef298635`, `5f846ab51`, `bb090a7c4`, `210c20f8e` | R `c991a8927`… | manual. Must come after .NET 10 (needs Microsoft.Extensions 8+). Removes the private Servarr feeds and the SqlClient/SqlServerCe baggage, so the JWT and Drawing pins can be dropped. |
| 2.2 | Official System.Data.SQLite 2.0.2, host sqlite3 fallback, sqlite 3.53.4 | L `5e683145e`, `68f07a822`; S `255f1c37c` | R `1bae9499e`/`901733bac`/`ca451608d` | manual (props) |
| 2.3 | Retry SQLite writes on "database is locked"; BusyTimeout 1000 ms; bulk updates in a transaction | L `944970a1f`, `3d48a982d`, `538e7534f` | S `2e1289b92`/`12ed8cdc2`/`d8b16d792` | trivial / clean |
| 2.4 | **Readarr bug (verified):** both `BasicRepository.UpdateFields` overloads, single-row and bulk, catch the exception, add the SQL to `e.Data`, and never re-throw. A failed UPDATE silently looks like success. | fixed by porting 2.3 (L `944970a1f` rewrites both methods), with a regression test | | trivial |
| 2.5 | Housekeeping doesn't run while other tasks run | S `04d98098e` | | clean |
| 2.6 | No unique constraints for primary keys in SQLite migrations | S `c70de927a` | | clean |
| 2.7 | Postgres connection-string option (and remove the duplicate `PostgresOptions` registration) | L `7149a912b` | S `d4f14246f`, R `16c4ab2b0` + `fb908fd5f` | trivial |

## 3. Download clients and queue

| # | Change | Source | Also in | Applies |
|---|---|---|---|---|
| 3.1 | qBittorrent: /login check, test after credential change, 5.2 login, API key, cookie reuse, Basic auth regression | L `1992127e9` → `467c8d497` → `b106629af` → `498de3fc5` → `621185e6a` → `d93342d8d` | S 8 commits, R 7 commits | first 3 clean, rest manual |
| 3.2 | Stale queue entries for downloads removed from the client | S `e38a2af1a` → `adf53108f` → `f416c322b` | | trivial / clean / manual |
| 3.3 | Re-grab of an already-imported torrent; parsing already-imported items | S `2f9e12a57`, `1cd5bc2e6` | R `45da623c7`, `d2d9dce33`, `25062f2d7` | trivial. Overlaps the foundation fixes, so compare first. |
| 3.4 | Fall back to another download client on failure | S `6bb694a6f` | | trivial |
| 3.5 | Pending-release correctness and performance | S `8718f28fb` → `1de3fb966` → `7e24a7d9d` | | manual |
| 3.6 | Validate indexer and client on pushed releases | S `b2f2c21a6` → `6214803a5` → `94a76f1f1` | | manual |
| 3.7 | Deluge reconnect; Transmission labels; Flood post-import tags | S `6856af8bb`, `b049b1ba1`, `5867cd5f4` | | trivial |
| 3.8 | Indexer's own default page size | L `0cfa30b92` | S `59c22e859`, R `a3fb071ae` | clean |
| 3.9 | HTML-encoded release titles | S `b135e5a2a` | | clean |

## 4. HTTP, host and lifecycle

| # | Change | Source | Also in | Applies |
|---|---|---|---|---|
| 4.1 | Locale-safe URL parsing; lazy JSON deserialization; `HttpRequestMessage` dispose leak; 30-minute pooled connection lifetime (DNS refresh) | L `5cb6af2b5`, `ad6dd56c5`; S `e747ec8f5`, `c0023b4c2`, `cca615b8e` | R `0c78421d5` | clean / trivial |
| 4.2 | Happy Eyeballs for dual-stack hosts, plus CancellationToken plumbing | S `5d8d2d66a` … `5477cd262` | | manual |
| 4.3 | External and internal restart handling; event handling on a disposed container; DbLogger unregister | S `6cd1ab376` → `dbb5c33f1` | L `4c42af443`+`1952b7607`, R `e80abdd60`, `a07ba4195`, `31f9e4166` | manual |
| 4.4 | PEM certificates, SSL validation, full certificate chain | S `a4a18d612` → `5568746ef` → `aea7ea743` | R `062357828`/`e92d5df9a` | manual |
| 4.5 | Clear expired cache entries periodically | L `88bf9e834` | S `4ebfc4847`, R `b3396e2fd` | trivial |
| 4.6 | Health check when there's no internet; health-check reasons | L `7e1e02853`, `4759a2621` | S `beadc687a`, R `72ae0c011` | trivial / manual |
| 4.7 | Overflow in SizeSuffix and disk info; hardlink fallback warning; pid-file and OSX path fixes | S `be4a56445`, `ae29755cf`, `8bab0a06d`, `5b135adda` | R `c6fef4309`, `0220f0daa` | clean / trivial |
| 4.8 | Log noise: API-key validation and auth-success messages moved to debug; dispose logging targets | L `80cf89c86`, `393db165f`; S `fdb9e3f9f` | | clean / trivial |

## 5. Book import (Lidarr-only, shared identification code)

| # | Change | Source | Applies |
|---|---|---|---|
| 5.1 | Null-safe tag handling in `Distance` | L `56480c7c0` | clean |
| 5.2 | Manual import: skip processing over 100 items; better logging; set release group | L `f29af8509`, `fc010a5ea`, `570f9a515` | manual (1 hunk each) |
| 5.3 | No empty folder when "delete empty folders" is on | L `f627a3cb8` | manual (1 hunk) |

## 6. Notifications (lower priority)

- Discord timestamp format (L `edb03b10d`).
- Plex: update every matching section; drive-letter slash (S `f382d6e8f`, `d8698d1c2`).
- Plex auth for COOP (S `2098303e2`, PlexTv part only).
- Ntfy custom headers; Pushover end-to-end encryption and TTL (S `ba9d00c8e`, `e79077d5f`, `317cdf155`).

## 7. Features, later and optional

- OIDC (S `9f0cb9284`): needs 1.5 and 1.6.
- Localhost auth bypass option (R `992580265` → `220db30c8`).
- RQBit and Tribler clients.
- Import via script (L `bed907e72` + `a709b7978`).
- MiniProfiler.
- Auto theme switching.
- Plugin system (L `fcfc60a27`): not recommended.

## 8. CI and repo hygiene to mirror from Sonarr

- **Build:** the composite actions `.github/actions/{build,test,package,archive}`, with NuGet caching (`22e0f2bc4`) and a parallel frontend lint and build (`b67fff717`).
- **Tests:** Postgres matrix 16, 17 and 18 (`ce4c2e4fc`, `1df3b116c`), matching the Postgres 15 minimum in 2.1.
- **Deploy:** deploy only from the right repository (`374b514c7`); find the previous tag within the same major version (`57816c149`).
- **Repo hygiene:** wrong-base-branch check, stale draft PRs, invalid and incomplete issues, the no-autopilot check, PR template, AGENTS.md.
