# Post-0.12.20 code audit

Local version: **0.12.21**, not published or installed over the user's app.
Single-agent review of first-party desktop code, data/parsing and helper lifecycles,
navigation/settings, release/update handling, tests and build workflow. The scope
included correctness, unnecessary work, resource retention and duplicate behavior.
This is not a claim that every mechanic, third-party runtime or live integration is verified.

## Fixed findings

- **Unreadable preferences overwritten at startup.** A failed read left defaults that
  migration immediately saved over the original. Active writes now use PreferencesStore,
  refuse unreadable existing files, atomically replace the file and retain a .bak recovery
  copy. Installer upgrade/rollback preserves that copy. No automatic recovery is attempted.
- **Retained native controls grew with repeated match clicks.** Detail routes now reuse
  match identity (game ID, then recorded date/champion/queue where available). Fresh
  records replace the old control tree. Back/Forward is capped at 64 entries and dynamic
  pages no longer reachable from history are disposed; review drafts remain retained.
- **Cached context went stale.** Champion summaries refresh when history changes;
  new reflections use current champion/focus without rewriting old drafts or saved notes.
  Matchup refresh detects local-player identity, mode, level and spell-rank changes.
  Account changes/disconnect clear last-match and account-bound pages; normal accountless
  postgame snapshots retain the preceding context.
- **Disposed client restarted its helper.** Transport requests now reject use after
  disposal and release exited Process objects before restart. Mobile sharing also rejects
  restart after disposal; its sender timer sleeps entirely while sharing is disabled.
- **Updater mixed releases during a concurrent check.** Prepare snapshots and verifies
  the signed bundle before downloading, then verifies against that same immutable release.
  Launch re-verifies the adjacent signed metadata and downloaded file, not mutable UI state.
  GameStart now shares the match-time review/update gate. Enumerated game Process objects
  are disposed; no League process is stopped by these checks.
- **Redundant rendering/data work.** Reuse the full-window backing bitmap and cached rank
  artwork; avoid invalidating unchanged scrollbar ranges and reassigning unchanged review
  controls. Reuse parsed summoner data. Active polling no longer constructs retired overlay
  payloads; the archived standalone experiment explicitly opts in without being activated.
- **Invalid recommendation values.** Reject non-finite timestamps and sample counts in
  retained compatibility code. This does not restore unavailable recommendations.

Per the user's clarification, Updates now opens as a fixed, close-only popup with the
same dimmed-owner treatment as Preferences. The obsolete HostedPage wrapper was removed.
Opening/closing Updates leaves the current page, navigation history and review draft alone;
dirty reviews and active matches still prevent installation.

## Evidence

Six initial regression cases failed against the pre-change 0.12.20 binary, covering
settings overwrite, stale champion summary, duplicate detail pages, stale reflection
champion, disconnect context and helper restart. The separate updater-race reproduction
also failed against that baseline. Both pass with the revised source.

Completed checks:

- 769 built-in app checks; 26 home-data, 14 rank-history and 132 dashboard checks.
- 39 page-navigation/retirement checks, including real modal ownership and disabled
  native owner, dirty-review gate, status refresh and unchanged underlying page/draft.
- 15 audit regressions plus the isolated signed-update race (temporary test key,
  offline downloader, no installer launch).
- Full Preferences/UI/review/loadout/postgame suites and all-monitor window chrome.
- Mobile privacy/lifecycle, 39 recommendation and 104 archived-overlay checks;
  5 Python transport, 3 mobile HTTP and 18 collector tests.
- Upgrade from 0.12.20 while the installer working directory is the installation folder;
  rollback and settings/review/rank/recovery preservation.
- Existing published manifest/installer signature, hash/size, tamper and HTTPS checks
  using the current verifier; public dist assets were not replaced.

Inspected overview, champion reference, Review at 1280 width, Updates and all three
Preferences sections. The native page harness renders at 1920x1040 (1080p with taskbar)
and 1280x950. Test-only sample data stays outside the product.

Same offscreen workload, 20 opens of one match:

| Measure | Baseline | Revised |
| --- | ---: | ---: |
| Retained match-detail pages | 20 | 1 |
| Additional native USER objects | 779 | 37–38 |
| 60 overview renders | 19,031 ms | 18,982–19,048 ms |

This demonstrates substantially lower repeated-page resource retention, **not** a
meaningful rendering-speed or in-game FPS improvement. Bitmap caching removes allocation
churn but this measurement does not establish lower overall CPU usage.

Evidence directories (ignored, local):

- build/audit-aefe4e2a1f1c4c31911047e9ea6a7c22
- build/home-418cfa7bceab4c699551d0610a738c23
- build/ui-7ab4da753b044186a202079cc95ed33e
- build/installer-test-2e14187406ed483f95047668624ecff0
- build/audit-package-0.12.21

## Remaining limits

No fresh League live-game session, mobile-browser reconnect exercise or real update
installation was performed. Network collector tests use fixtures; they do not establish
Riot access. The bundled patch remains 16.17.1 with patch matching unverified. Retired
overlay/Game Bar and legacy standalone dialogs remain compatibility/experimental code,
not active features. Broader removal of that archived code is separate from this audit.
No Riot/Overwolf approval, new gameplay assistance, website deployment, release signing
or public publishing is implied.
