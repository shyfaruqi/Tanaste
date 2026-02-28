<div align="center">

# 🎬 Tanaste
### The Unified Media Intelligence Kernel

**A cross-media Hub for your digital life.**

*Tanaste automatically unifies your Ebooks, Audiobooks, Comics, TV Shows, and Movies into single intelligent Hubs — powered by a local-first engine that never touches the cloud.*

<br/>

[![License: AGPLv3](https://img.shields.io/badge/License-AGPLv3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Linux%20%7C%20macOS%20%7C%20Windows-lightgrey.svg)]()
[![Arr Compatible](https://img.shields.io/badge/Arr--Compatible-Radarr%20%7C%20Sonarr-orange.svg)]()

</div>

---

## 🧠 What is Tanaste?

You have a book. Then you find the movie adaptation. Then you grab the audiobook for the commute. Three files. Three folders. Three separate apps. Zero connection between them.

**Tanaste solves this.**

Drop your files into a Watch Folder, and Tanaste's Intelligence Engine automatically reads the metadata inside each file, scores it for reliability, and groups everything that belongs to the same story into a single **Hub**. The Hub for *Dune* holds your EPUB, your 4K video, your audiobook, and your comic — unified, cross-referenced, and browsable from one place.

Everything runs on your own machine. No account. No subscription. No data sent anywhere.

---

## ✨ Key Features

### 📊 The Bento Dashboard
A visual, browser-based library overview built with an asymmetric **Bento Grid** layout — wider tiles for your most recently visited Hubs, narrower tiles for the rest. Each card uses **glassmorphic styling** (translucent glass effect with soft glows) that adapts to the dominant colour of your media. A global **Command Palette** (activated with `Ctrl+K`) lets you jump to any Hub or page instantly.

> *The UI is built as a Blazor Server dashboard — it runs on your server and renders in any browser, with live updates pushed via SignalR the moment a new file is detected.*

### 🤖 The Intelligence Engine (Weighted Voter)
Tanaste never asks you to manually enter a title, year, or author. Instead, it uses a **Weighted Voter** system:

- Every piece of metadata from every source (embedded file tags, filenames, external providers) is recorded as a **Claim**
- Each Claim carries a trust weight based on how reliable its source is (e.g. an embedded OPF record outranks a filename guess)
- The Voter tallies all Claims for each metadata field and elects a winner — the **Canonical Value**
- If the vote is too close to call, the conflict is surfaced in the dashboard for a single human decision — the only time you ever need to intervene

All original Claims are preserved forever. Nothing is overwritten. Full audit history, always.

### 🔒 Privacy-First by Design
- **Local SQLite database** — your entire library catalogue lives in a single file on your own hard drive. No cloud sync, no telemetry
- **Secret Store** — API keys for external metadata providers (e.g. TMDB, MusicBrainz) are encrypted at rest using your OS's built-in protection layer. Never stored as plain text
- **Guest Key system** — any external tool that connects to Tanaste must present a named, revocable API key. You control exactly who has access and can revoke a key in seconds without affecting others

### 🚗 Automotive Mode *(Planned)*
A dedicated high-contrast display mode with oversized buttons and enlarged text — designed for safe, glanceable use on a media room TV or a tablet mounted in a vehicle. One toggle switches the entire dashboard into this mode; one toggle switches it back.

---

## 📸 Screenshots

> *Bento Grid dashboard screenshots will be added here once the full UI is complete.*
>
> **Coming in a future update:**
> - Universe overview (Bento Grid with Hub cards)
> - Hub detail page with Works list
> - Ingestion progress live feed
> - Command Palette overlay

---

## 🚀 Quick Start

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
# 1. Clone the repository
git clone https://github.com/shyfaruqi/tanaste.git
cd tanaste

# 2. Create your local configuration file
cp tanaste_master.example.json tanaste_master.json
```

Open `tanaste_master.json` and set these two values:

```json
{
  "database_path": "/your/path/tanaste.db",
  "data_root":     "/your/media/library"
}
```

```bash
# 3. Start the Intelligence Engine (headless, API-only mode)
dotnet run --project src/Tanaste.Api

# Engine is now running at:
#   http://localhost:61495
#   Swagger UI: http://localhost:61495/swagger

# 4. (Optional) Start the visual Dashboard
dotnet run --project src/Tanaste.Web

# Dashboard is now running at:
#   http://localhost:5016

# 5. Run the automated test suite
dotnet test
```

### Configuration Reference

`tanaste_master.json` accepts the following settings:

| Setting | What it controls | Default |
|---|---|---|
| `database_path` | Where the library database file is stored | `tanaste.db` |
| `data_root` | Root directory for organised media files | *(required)* |
| `ingestion.watch_directory` | The inbox folder Tanaste monitors for new files | *(required)* |
| `scoring.auto_link_threshold` | Confidence required to auto-assign a file to a Hub (0–1) | `0.85` |
| `scoring.conflict_threshold` | Confidence below which a metadata field is flagged for review (0–1) | `0.60` |
| `scoring.stale_claim_decay_days` | Days before a Claim's trust weight decays | `90` |
| `maintenance.vacuum_on_startup` | Compact the database on startup to reclaim space | `false` |

---

## 🏗️ Architecture

Tanaste is built on a **headless Engine + visual Dashboard** split. The two parts are completely independent.

```
┌─────────────────────────────────────────┐
│             Tanaste.Web                  │  ← Visual Dashboard (Blazor Server)
│         browser dashboard               │    Connects via HTTP + SignalR
└────────────────┬────────────────────────┘
                 │  HTTP / SignalR
┌────────────────▼────────────────────────┐
│             Tanaste.Api                  │  ← Intelligence Engine (Headless API)
│    all logic, data, file operations     │    Runs independently; no UI required
└────────┬────────────────────────────────┘
         │
   ┌─────▼──────┐   ┌───────────────┐   ┌──────────────────┐
   │  Storage   │   │  Intelligence │   │    Ingestion     │
   │  (SQLite)  │   │ (Voter/Scorer)│   │  (Watch Folder)  │
   └────────────┘   └───────────────┘   └──────────────────┘
```

**Why the split matters:**
- The Engine can run silently as a background service — no browser, no interface, no overhead
- Any app that speaks HTTP can connect to the Engine directly — see [Arr Compatibility](#-arr-compatibility-radarrsonarr) below
- The Dashboard can be redesigned or replaced without touching the Engine or the database

**Internal Engine layers** (each depends only on the one above it):

```
Tanaste.Domain          ← Business rules and data shapes (zero dependencies)
  └─ Tanaste.Storage    ← Database reads and writes
      └─ Tanaste.Intelligence  ← Scoring, deduplication, conflict resolution
          └─ Tanaste.Processors  ← File-type readers (EPUB, video, comic)
              └─ Tanaste.Ingestion  ← Watch folder, file queue, background worker
                  └─ Tanaste.Api    ← HTTP endpoints and SignalR hub
```

---

## 🔌 Arr Compatibility (Radarr / Sonarr)

Tanaste's Engine exposes a standard HTTP API secured by an **`X-Api-Key` header** — the same authentication pattern used by Radarr, Sonarr, Lidarr, and the broader \*Arr ecosystem.

**To connect an external app:**

1. Open the Tanaste Swagger UI at `http://localhost:61495/swagger`
2. Use `POST /admin/api-keys` to create a named key for your app (e.g. `"Radarr integration"`)
3. Add the key as an `X-Api-Key` header in your external app's Tanaste connection settings
4. Revoke it any time with `DELETE /admin/api-keys/{id}` — other apps are unaffected

External apps can query Hubs, trigger library scans, and resolve metadata conflicts via the Engine's full REST API without ever opening the Dashboard.

---

## 🗺️ Project Roadmap

### ✅ Completed

| Phase | What was built |
|---|---|
| **Phase 1** | Macro-architecture, bounded contexts, and core design invariants |
| **Phase 2** | Domain model — Hub, Work, Edition, MediaAsset, and all contracts |
| **Phase 3** | Metadata provider contracts and claim structure |
| **Phase 4** | SQLite storage layer — ORM-less raw SQL, WAL mode, embedded schema |
| **Phase 5** | Media processors — EPUB, Video (stub), Comic (CBZ/CBR), Generic fallback |
| **Phase 6** | Intelligence Engine — Weighted Voter, Conflict Resolver, Identity Matcher, Hub Arbiter |
| **Phase 7** | Ingestion Engine — Watch Folder, debounce queue, content hasher, background worker |
| **UI Deliverable 1** | Dashboard shell — MudBlazor layout, dark mode, Bento Grid, Hub cards, Command Palette |
| **UI Deliverable 2** | State & real-time — UniverseViewModel, UniverseMapper, SignalR Intercom listener |

### 🔄 In Progress / Planned

| Milestone | Description |
|---|---|
| **UI Deliverable 3** | Full Hub detail pages, Works list, Edition drill-down |
| **UI Deliverable 4** | Live ingestion progress feed using Intercom SignalR events |
| **UI Deliverable 5** | Metadata conflict resolution UI — review and resolve flagged Claims |
| **Automotive Mode** | High-contrast, large-button display mode for TV / in-vehicle use |
| **Video metadata** | Replace stub video extractor with FFmpeg-based real extractor |
| **Metadata providers** | TMDB (movies/TV), Open Library (books), MusicBrainz (audiobooks) |
| **Mobile companion** | Read-only library browser via the existing Engine API |

---

## 🛠️ Tech Stack (Full Reference)

| What it does | Technology |
|---|---|
| Language & runtime | C# / .NET 10 |
| Database | SQLite via `Microsoft.Data.Sqlite` — raw SQL, no ORM |
| Engine API | ASP.NET Core minimal APIs |
| Real-time events | SignalR (`/hubs/intercom`) |
| Dashboard | Blazor Server |
| UI components | MudBlazor 9 |
| SignalR client | `Microsoft.AspNetCore.SignalR.Client` |
| EPUB parsing | VersOne.Epub |
| API docs | Swashbuckle (`/swagger`) |
| Tests | xUnit 2, coverlet |

---

## 📄 License

Tanaste is free and open-source software, licensed under the **GNU Affero General Public License v3.0 (AGPLv3)**.

> This means you are free to use, modify, and distribute Tanaste — but if you deploy a modified version as a network service, you must also make your modifications available under the same license.

See the [`LICENSE`](LICENSE) file for the full license text.

All dependencies are MIT or Apache 2.0 licensed and are compatible with AGPLv3.

---

<div align="center">

*Built with care for people who take their media library seriously.*

[Report a Bug](https://github.com/shyfaruqi/tanaste/issues) · [Request a Feature](https://github.com/shyfaruqi/tanaste/issues) · [View the Engine API](http://localhost:61495/swagger)

</div>
