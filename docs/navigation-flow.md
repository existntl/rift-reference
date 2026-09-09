# Page navigation and deferred integrations

## Reference analysis (2026-09-08)

Reviewed both user-provided captures as visual references, sampling once per second
and inspecting the important navigation states. Ads, premium prompts and Blitz
branding are not app requirements. No audio analysis is claimed.

| Capture / time | Observed flow | Rift Ready implementation |
| --- | --- | --- |
| Synq profile, 0–9s | Persistent navigation, profile and match history | Existing own-account overview, queue/range/search and bounded history scroll retained |
| Synq profile, 10–20s | Champion catalog / tier-list filters | Champion pool and bundled catalog pages, champion search, queue/role filters and sorting; no unsupported global tiers |
| Synq profile, 21–35s | Settings modal over the current page | Preferences remains the modal: General, Audio, Phone/tablet; no overlay section |
| Synq profile, 37–44s | Search and another player's profile | Not enabled: public player lookup requires a supported data integration |
| existntl LP history, 0–10s and 19–22s | Overview / LP history navigation and graph inspection | Dedicated rank-history page, current-season/30-day/7-day filters, actual snapshot plot and dated values |
| existntl LP history, 11–18s and 31–37s | Match list → detail page → return | Click a match row for final recorded statistics and inventory; Back/Forward returns without losing overview filters/scroll |
| existntl LP history, 23–26s | Role-based champion pool | Own loaded history filtered by role; no fabricated LP contributions |
| existntl LP history, 38–56s | Graph, event timeline and win-probability tabs | Deferred: the local history model does not contain the required event/roster/prediction data |

Champions, champion reference, rank history, match details, Matchups and Review are
main-window pages. Alt+Left/Right and visible arrows support navigation. Per the user's
later clarification, Updates is a popup like Preferences (local 0.12.21), superseding
the Updates page in published 0.12.20. Both use fixed close-only chrome and a dimmed
owner. Updates preserves the underlying page/history/draft and blocks installation
during a match or with unsaved Review edits. Preferences preserves Save/Cancel behavior.
Navigation retains up to 64 entries, reuses a match's detail page and releases dynamic
pages removed from history. Cached champion summaries refresh with loaded history.
Review drafts remain in
memory across page changes; switching notes, starting a new note or closing with
unsaved edits uses an inline Save/Discard/Keep editing prompt. Match-time review
gating remains, and saved review storage/recovery semantics are unchanged.

The overview retains its bounded, custom match-list scrollbar, rather than copying
Blitz's whole-document scroll literally. Missing client data is still blank; no
demo data, user-selectable fake sessions or nonfunctional placeholder tabs are added.

## Temporarily removed from the active app

User requested removal of broken or permission-dependent features, explicitly
including the Win+G experiment. The dashboard no longer creates GameOverlay or
GameBarIntegration, launches a widget, publishes a Game Bar feed, or switches to
one-second polling because of saved overlay settings. Overlay preferences and the
unavailable Builds / Runes recommendation entry are removed from active navigation.
Legacy source, saved-plan compatibility, overlay.json and recovery copies remain.
The experimental widget is not an installer dependency or bundled payload.

On this PC, uninstalled the exact current-user package
`RiftReady.GameBarPrototype_1.1.1.0_x64__q4vf8r75vcnhg`. Confirmed it is absent,
its app/bridge processes are gone, and `Microsoft.XboxGamingOverlay` remains installed
with Status Ok. No Windows Game Bar, runtime dependencies, certificates, League or
Vanguard components were removed. Source and built test packages remain recoverable.
The normal installation's overlay.json was backed up as
`overlay.before-retirement-20260908.json`; only Enabled/GameBar were changed to false,
preventing old installed builds from launching the removed widget on their next start.
The running main installation was not force-closed, replaced or upgraded.

## Future feature direction (not implemented or approved)

The user would eventually like many features shown in these captures. Keep them as
future candidates, not abandoned requirements and not visible promises:

- Supported fullscreen overlays after Riot/Overwolf integration and permission review.
- Public player lookup / linked profile navigation with an authorized data source.
- Populated build/rune recommendations and verified explicit imports.
- Real match roster/stat graphs and timelines when complete source data is available.
- Tier/synergy/role-population statistics with a documented, adequate dataset.
- Prediction features only after a separate policy, data and product-scope review.

Submission to Overwolf is not approval. Do not claim Riot approval or assume that
access alone supplies historical data. No enemy cast/countdown tracking is authorized.

## Verification

`verify-home.ps1` includes PageNavigationTests. It exercises actual native button
actions, page visibility without top-level dialogs except Preferences/Updates,
Updates modal ownership/native disabled owner/dirty-review gate, Back/Forward, retained filters/scroll,
match row hit targets, saved/unsaved reviews, LP point counts, account clearing and
old enabled-overlay settings. Renders cover 1920×1040 (1080p with taskbar) and 1280×950.
`verify-ui.ps1` keeps preferences migration/hit-target/save/cancel and archived
component tests. Old modal helper methods remain for isolated rendering/compatibility;
they are not reachable from active navigation. AuditRegressionTests additionally covers
stale page context, bounded history, repeated match detail reuse and helper shutdown.
