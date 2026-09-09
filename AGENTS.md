# Rift Ready working instructions

- Published 0.12.17 follows the user's Blitz dashboard and settings references without copying
  ads, branding or unsupported statistics. Preferences, Updates, stats and scoreboard-alignment
  settings use compact charcoal/off and Rift Ready teal/on pill toggles with keyboard,
  accessibility, focus, hover, disabled and high-contrast behavior. All selection dropdowns
  use a rounded charcoal trigger and a detached dark menu with a neutral selected row, teal
  check/focus treatment, keyboard navigation, accessible expanded state and a dark long-list
  scrollbar. Champion select uses
  ten compact roster cards, preserves enemies-left/allies-right and drag-to-swap, highlights
  the local player, and shows only Data Dragon Tank/Mage/Support tag counts. The live roster
  uses separated cards. Builds & runes now uses the slim Rift Ready title chrome, stronger
  Common/Win rate and Paths/Options states, rounded selected build cards, and dark selectors.
  Guarded save/apply semantics and complete-bundle identity are unchanged. Do not add the
  reference's personalized picks, tier/synergy statistics, player mastery/rank/KDA, damage
  percentages, matchup-conditioned builds, pro identities or click-to-lock-in without a
  verified compliant data source and fresh scope review. Public source tag v0.12.17 points
  to 40b1bf1e637ab0c846db3dcbd64a011d065ea900. Matching installer and signed manifest are
  published and verified, including discovery/download by the 0.12.15 updater. Installer
  SHA-256: 99FAA49CFC4BCCB3066F27F93FFEBD68258BA9F83F76E8AB40E29C51B4EDAF76.
  Website content remains 0.12.16; its latest-installer link serves 0.12.17. Normal
  installation was not replaced during publication.

- Public 0.12.16 follows the user's Blitz titlebar references: a slim charcoal header with
  left Rift Ready/version text and small Windows-ordered glyph controls on the right. The
  Preferences window is a fixed, centered close-only modal with rounded corners and a
  dimmed owner backdrop. Its Save and Cancel actions remain inside the dialog. The optional
  one-second click is about 43% stronger at the same master volume; other cues and its
  off-by-default setting are unchanged. The matching source, installer and signed manifest
  are published at GitHub tag v0.12.16. Installer SHA-256 is
  C3A772BF33A89E291EA1BECEC0B9979A6A3404375140E1D9E4E3FFE2EB8FFFB0. The matching
  website runtime is source `bb6eb12099b5e26ccceba25db839daeabff91790`, deployed as
  Sites version 16 at `https://riftready.gg`.

- Version 0.12.15 introduced one themed Preferences window with
  General, Game overlay, Audio & reminders, and Phone / tablet sections. Updates is
  a separate main-navigation utility and window. Preserve independent
  `preferences.json`/`overlay.json` schemas, overlay draft/cancel behavior, the
  update match gate and signature checks, Game Bar launch behavior, and immediate
  phone sharing controls. The matching source, installer and signed update manifest are
  published at GitHub tag v0.12.15. Installer SHA-256 is
  55152F26810794583FC2A0793A9AD3D91461F6719CA50C09E72C9B3443A3C68C. The matching
  matching website deployment is historical; read the first handoff section for the
  current release and production verification.

- Game Bar local widget1.0.3.0 installed with same-package AppService desktop
  live helper (experiments/gamebar). User photo confirmed pinned display-test
  counter visible over fullscreen Practice Tool. Helper activation-argument bug
  fixed; actual AppService open/send Success verified. Current game EndOfGame;
  fresh-match visible data/FPS validation still pending. User accepted Game Bar
  dependency for now. See docs/gamebar-overlay.md. This separately installed test
  component is not bundled with the public0.12.15 installer.
  Use build-gamebar.ps1, sign-gamebar-test.ps1 and install-gamebar-test.ps1;
  reuse approved development certificate. No game-process injection retry.

- User chose an independent fullscreen integration. Experimental C++ D3D11 renderer,
  normal user-mode Present adapter and isolated C# frame producer live under
  experiments/native-overlay. Read docs/independent-overlay.md. The experiment remains
  unbundled from public0.12.15. The own fullscreen host works; League compatibility
  is unverified until an actual Practice Tool test. Do not claim production readiness.
  Use scripts/verify-native-overlay.ps1; ordinary access denial ends the test, never
  elevate or change anti-cheat/security protections to work around it.

- Local0.12.13 removes role labels from gold markers at user request (supersedes
  earlier visible-role-label requirement). Single-line64x24 markers,11pt bold values,
  ClearType grid fit, antialiased arrows and1px panel borders. Preserve calibrated
  BoardY100/RowStart244/RowGap75. Row semantics still Top/JG/Mid/Bot/Sup.

- Local0.12.12 permits PRACTICETOOL as well as CLASSIC in overlay parser/freshness.
  User's solo custom test reports PRACTICETOOL; prior CLASSIC-only gate hid everything.
  Live read verified fresh=true and inventory known with fix. Map11 parsing and stale/demo
  guards retained; incomplete enemy/team comparisons remain unavailable.

- Local0.12.11 fixes blank/inaccessible main window on secondary monitor. WinForms
  MaximizedBounds needs monitor-relative coordinates, not absolute WorkingArea.
  Previous code doubled secondary X (1920 to3840). Initialize bounds on handle creation
  and refresh on normal moves/maximize. All-monitor bounds tests cover startup and button.

- Local0.12.10 installed: compact gold markers64x34 (was80x53), narrower team totals,
  true16:9 alignment preview; saved row centers/spacing preserved pending real scoreboard
  screenshot. Redundant100ms stats/geometry updates removed. User reports144FPS fullscreen
  versus85 borderless with only Rift Ready overlay enabled; cause unresolved, require
  same-scene borderless overlay-off/on comparison. True exclusive fullscreen still unsupported.
  See docs/overlay.md fullscreen research. Do not claim FPS recovery or exact alignment.

- 2026-09-07 private Riot HTTP403 resolved: same portal-tested key passes NA status
  and ranked endpoints after explicit collector User-Agent and JSON Accept headers.
  Real collection now saves match samples. Minimum30 games/10 players remains;
  do not equate successful access with populated recommendations. Standalone private
  launcher builds are immutable and guard duplicate sessions. Live local history
  returned100 matches and one recorded LP snapshot. Public remains0.12.0.

- Local installed 0.12.9 supports private builds via RIFT_RECOMMENDATIONS_FILE and
  scripts/start-private-builds.ps1 (masked separate collector). History requests up
  to 100 matches with 20-match fallback; graph shows all retained current-window
  snapshots. Never fabricate old LP. Public release remains 0.12.0.

- Local installed 0.12.8 uses Windows button order at top right: yellow minimize,
  green maximize/restore, red close. Retain Apple-style circles.

- Local installed 0.12.7 moves traffic-light window buttons to upper right, retaining
  red/yellow/green order and right-edge anchoring. Supersedes left-side placement.

- Local installed 0.12.6: user requested minimal Apple-style title bar on main window.
  MinimalWindow provides a 32px title region and left red/yellow/green controls;
  keep drag/resize hit testing, taskbar-aware maximize and keyboard-accessible buttons.
  Dashboard content uses ContentHeight and TitleHeight offsets. Other dialogs retain
  native title bars. Public release is still 0.12.0.

- Local installed 0.12.5 removes the scrollbar dotted focus border at user request;
  teal thumb brightness remains the focus cue. Public release remains 0.12.0.

- Local installed 0.12.4 corrects scrollbar color to Theme.Accent teal (#42cdc6),
  retaining the reference's rounded banded design. Supersedes green preference below.

- Local installed 0.12.3 follows the user's far-right scrollbar image: slim rounded
  green thumb with subtle horizontal bands, near-black track; rest of theme unchanged.

- Local installed 0.12.2 styles match history with a charcoal track and rounded teal
  thumb, brighter hover/focus, 20px interaction width and row-aligned height. Public
  remains 0.12.0. Keep wheel, drag, page clicks and keyboard accessibility.

- Local installed version 0.12.1 adds a native match-history scrollbar with bounded
  wheel/drag/page navigation. Public release remains 0.12.0. See newest handoff entry.

- 2026-09-07 API scope review: user accepted own linked all-rank Solo/Duo profiles with
  proposed RSO linking, local-only LP history initially, and a separate public Diamond+
  aggregate feed. Backend/RSO remain unimplemented. Private application draft is now
  explicitly Git-ignored; never force-add/publish it. Terms/Privacy review drafts remain
  unpublished in the continuation task's outputs. Owner/contact, markets/age eligibility,
  profile regions/providers and proposed retention periods require review; they are not
  implemented service guarantees. See newest handoff entry.

- Latest public release is0.12.0 (2026-09-07): direct builds screen, player overview, actual rank badges, LP bar and local rank history. Public signed installer and older updater download verified. Source tag d74c297d75bff74f706677da06f4add3545d9e74. This supersedes older local-only/release entries. Riot API profile backend and live recommendation feed remain pending.

- User requested Riot API-backed personal rank/LP/progression on2026-09-07. Added to private production-application draft; not submitted or implemented as a backend. Personal profiles are all ranks, separate from Diamond+ recommendation aggregates. Historical graph must record official snapshots, never infer LP from wins/losses.

- Idle home includes actual tier badges, divisional LP bar and locally recorded rank progression. See docs/rank-history.md. Preserve rank-history.json and .bak across updates; exclude from source/packages. Never invent earlier LP. Bootstrap badges with scripts/cache-rank-badges.py. Local only; public remains0.11.0.

- Idle home visual preference: closer to Blitz reference, flat continuous match rows/dividers, larger portraits, compact Last10 champion summary, rank/performance left column. Keep RR branding and charcoal/teal; never invent LP trends or grades to fill reference visuals. Home preview harness renders full application with cached portraits and sample-data title.

- No-game overview uses HomeDashboard: local own-account Solo rank and recent10 all-queue matches/champion summaries, scroll rows with mouse wheel. HomeData parses only identified self; nullable stats and full-team-only KP/damage share.60s account/session cache is memory-only, cleared disconnect; no external Riot key needed. No LP delta graphs, grades or placement estimates. Local client history support/live validation unverified.

- User removed the original manual editor from the active app flow. Runes / builds opens RecommendationPicker directly with save and guarded preview/apply controls; overlay retains chooser mode. Legacy BuildPlanner source remains for compatibility/tests only, not navigation. No sample recommendations in production when feed unavailable.

- Latest public release is **0.11.0**, published2026-09-07: visual recommendation/path browser, direct overlay selection, dark native titlebars and shortcut icon fix. Signed public download and0.9.3 updater verified. Live Diamond+ feed remains unconnected; collector/key/database are not installed. Source tag decdceede30f48b2099ffc2eafee8dfa55c523dc. This supersedes prior local/unpublished and0.10.0 release-state entries below.

- Build-path browsing follows the public OneTricks reference: Paths / Options, first-core item filtering and All paths reset. Option shares use only listed qualifying bundles, never implied whole-population pick rates. Keep rune/core bundle identity when filtering; no OneTricks scraping/feed or expert roster is connected.

- Native title bars should blend with charcoal theme. Theme.TitleBar applies DWM dark frame (20 with legacy19 fallback) and supported caption/text/border colors; keeps native controls/snap/resize and respects high contrast at application. Apply on handle creation; do not replace with borderless chrome casually.

- Local build dashboard follows the supplied Blitz reference: connected rune/core alternatives, Common / Win rate sorting, rune trees, observed skill upgrade sequence, spells and item sections. `rift-diamond-2` details are qualifying modes within the selected rune/core cohort, not one jointly observed complete loadout. No invented pro identities or situational recommendations. Overlay settings can choose the same saved build directly; target advancement remains manual. Public release unchanged; real collector output and hosting remain unverified.

- Local Diamond+ recommendation collector and native picker: read docs/recommendations.md. Accepted NA1/EUW1/KR ranked solo, current patch, 7 days, minimum30 games/10 players. User generated a development key expiring September7,2026 at23:25 Pacific; it is not stored in source. Masked private launcher accepts local user entry. Real collection, production access, hosting and endpoint configuration remain pending; never claim the feed is live. Keep key in the separate collector environment, never the native app. Preserve explicit apply/review and saved-plan overlay target selection.

- Current public release is 0.10.0, with matching installer and signed manifest; website version 12 describes overlays. Public signature/hash and the 0.9.3 updater download verified. 866 app/overlay checks and installer upgrade/rollback passed. No normal installation or live-game validation performed. GitHub source main matches the verified release tree. CLI GitHub credentials are absent: publish binaries through the signed-in in-app browser and source through the GitHub connector; never assume CLI authentication.

- Layout editor has no shared toolbar/menu. Build/stats gears contain per-panel reset, transparency, next preview screen, and Use/Cancel layout. Ctrl+Enter accepts, Esc cancels, F6 cycles screens even when all panels are hidden. Restore closed panels in Preferences > Game overlay, then reopen editor. Fixed buff cards remain without controls. Keep the parent settings shortcut/help text discoverable.

- Buff UI supersedes the draggable buff panel: fixed top-center Baron/Elder cards, 200x106 each with 8px gap, no chrome/team labels/move/resize/close. Ignore legacy buff size/position; keep existing enable switch and opacity. Only active kill-derived windows appear in game (Baron 180s, Elder 150s); ordinary dragons excluded. Layout editor shows sample cards. These remain estimated objective windows, not individual holder tracking.

- Overlay panels default to half the previous width/height (build 215x101, two buffs 150x62, default stats 145x69). PanelSizeVersion migrates saved dimensions once; zero means automatic compact size. Independent gear > Transparency sets whole-panel opacity 20–100% (default 96%), with cancelable preview; editor controls remain opaque. Resize, positions and opacity persist in overlay.json. Live overlays remain click-through.

- Optional stats overlay: enable via Game overlay > Stats panel or Show stats in the layout editor. CS/min and kill participation are selected by default; CS, K/D/A and vision score are selectable through its gear. No gold/min or rank benchmarks. Stats have independent position, lock/close and visibility. Missing inputs and zero team kills yield unknown KP. Read docs/overlay.md.

- Layout editor panels now have individual square lock/unlock, gear and close controls. No shared lock toggle. Gear resets only that panel's position; close updates only its visibility switch on save. Show build/Show buffs restores hidden panels. These controls are in the explicit layout editor; live overlays remain click-through. Porofessor's exact per-panel gear menu could not be verified from public official docs.

- Overlay build defaults bottom-right immediately left of the default minimap; buff timers default top-center. Game overlay > Move / lock panels opens a full-screen layout preview with independent drag/drop, Unlock/Lock, Reset defaults, screen selection and Use layout & lock; save the parent settings to persist. Gameplay windows remain locked/click-through. Custom normalized positions adapt to resolution; preserve legacy custom X/Y placement. See src/PanelLayout.cs.

- Overlay design now follows the user's Blitz/Porofessor references: five role-aligned directional gold markers and complete team totals, compact item/component icons and target progress, separate Baron/Elder bars. Navy/gold overlay colors are specific to this requested HUD; retain the existing main-app palette. Scoreboard alignment is configurable, not automatically detected; role order must match Top/Jungle/Mid/Bot/Support. See docs/overlay.md for research and limitations.

- Local **0.10.0** adds an opt-in native game overlay; public remains **0.9.3** and website version 11. Read docs/overlay.md. Gold is lane inventory-value difference, buffs are estimated Baron/Elder team windows, and purchase progress uses a selected item or saved manual plan. Automatic Probuilds feed remains unfinished. Borderless/windowed is the target; exclusive fullscreen is unsupported. Preserve overlay.json and overlay.json.bak during upgrades; keep them out of source/packages. Do not claim real-game validation or Riot audit approval.

- Latest published patch is **0.9.3**: mobile CSP permits embedded data-image logos, and browser QA checks successful image decoding. This supersedes the 0.9.2 release entry below; keep external image loads blocked.

- Current logo (0.9.2) uses the second board's white lower-left/inner diagonal and teal upper bowl/right leg. Preserve the accepted charcoal interface palette. See docs/branding.md for asset provenance.

- Latest color preference: retain Rift Ready branding/logo but use the pre-rebrand charcoal/teal desktop theme and previous website/mobile colors. Do not reapply the brand board's green-tinted UI surfaces.

- Display brand is now **Rift Ready** (0.9.0), using the user-supplied RR monogram and mint/deep-green/charcoal palette. Read docs/branding.md. Keep legacy `RiftReference` executable/product/install/updater identities and repository URLs for compatibility; these are not visible-brand strings to bulk rename. Saved plans/preferences/reviews and signing-key continuity must remain intact. Build embeds assets/branding/rift-ready.png, generates app/installer icons and embeds the phone logo without adding a server route. Run scripts/verify-branding.ps1 for legacy-shortcut ownership and mobile embedding checks.

Read `handoff.md` before making changes. It records the release state, user preferences, known limitations, and the next-session migration plan. Keep both documents current when architecture, release workflow, or accepted requirements change.

## Project and user

- Native, lightweight Windows 10 League of Legends companion for a landscape second monitor. The user primarily plays Vayne ADC in Emerald ranked solo, but features should support other champions and roles.
- Prefer automatic local client detection and information readable without hovering or clicking during games.
- Cooldown values are reference durations, with estimates clearly identified. Do not imply observed casts, remaining cooldowns, or exact enemy spell ranks.
- Current accepted preferences: enemies left, allies right; draggable lane headers swap sides and persist; whole-second cooldowns; minutes plus seconds at one minute or more (seconds-only optional); distinct colored Q/W/E/R labels and ability names beneath cooldowns; all-enemy respawn notices; short “Jungle check.” speech.
- First jungle reminder remains **120 seconds**, repeating every 90 seconds through 480 seconds. The user corrected their earlier observation and requested no timing change. Every audio interval has an independent option; the 1-second click is off by default.
- Accepted coaching: lane plan, enemy kit reference and one-focus sentence on the dashboard; deeper lessons in Playbook; saved self-assessment in Review. Keep unknown roles and unreviewed kits explicit. Only one quartet has a specifically authored plan; other pairings are pattern guides.
- Accepted mobile option: opt-in phone/tablet browser display on a trusted home LAN, paired by local QR/link. Off on every app start; fresh secret on every sharing session; explicit Stop. Share only sanitized reference/coaching data, never credentials, accounts or review notes. Native desktop/mobile styling uses dark cards and mint accents.
- Desktop visual direction was refined from an inspected Blitz desktop screenshot: 180px labeled navigation rail, neutral charcoal surfaces, teal active states, compact header and flat cards. Keep original Rift Reference branding and all reference semantics. Header drag hit areas must include the navigation offset; test displayed bounds rather than old absolute coordinates.

## Scope and communication

- Carry authorized work through implementation and appropriate verification. Give concise progress updates. Do not repeatedly ask for permission already supplied.
- Do not claim features in the aspirational specification are implemented without checking the code.
- The user withdrew the standing permission for sub-agents on 2026-09-08. Keep Rift Ready work single-agent and do not spawn or delegate to sub-agents unless the user explicitly changes this preference again. This supersedes the 2026-09-06 parallel-work authorization.
- Do not send messages to third parties without explicit authorization. Drafting release notes or a support request does not authorize sending a support message.
- Verify current Riot rules before introducing new gameplay assistance. Enemy summoner/ability countdown tracking, including manually started timers, was excluded after reviewing current compliance guidance. Do not represent the app as Riot-approved; registration/audit is not verified.

## Source and builds

- Published **0.8.1** refreshes the pregame rune/item editor with icon-based tree rows, stat shards and an item catalog beside named sections, plus desktop postgame champion portraits. App updater and website were published and verified on 2026-09-06. `src/LoadoutEditor.cs` owns visual selection; preserve `rift-loadout-1` saved-plan compatibility and guarded preview/apply semantics. Run `cache/runtime/python.exe scripts/cache-loadout-icons.py` before building from a fresh cache; bundle its ignored Riot Data Dragon assets without runtime icon downloads.

- **0.8.0** adds a dedicated pregame brief and manual rune/custom-item-set editor. Read docs/pregame.md. External source feeds are NOT connected; do not claim automatic Probuilds/Onetricks recommendations. Client writes are explicit new-page/new-set POST operations, guarded by current phase and local pick; never delete user pages to free capacity or expose writes through mobile. Real-client import validation remains pending.
- **0.8.0** also has a dedicated postgame scoreboard on desktop/mobile. Read docs/postgame.md. Use final client results only after checking gameflow match ID; missing values stay unknown. Never show cooldown references in postgame, infer final totals from a stale live sample, or send raw end-of-game responses (which may include credentials/identifiers) to mobile. Completed-game live capture validation is pending.

- `src` is authoritative after consolidation; `helpers` contains Python code and `tests` the standalone harnesses. The original workspace and source mirror are retained backups; do not maintain them independently.
- Use `Installer.cs`. `Setup.cs` is obsolete.
- Published release is **0.9.2 (Rift Ready)**, released 2026-09-06 with matching installer and signed manifest. Public download/signature and the 0.9.1 updater were verified. This applies the updated white-and-teal logo while retaining the previous interface colors. Build with `scripts/build.ps1 -Installer`; see `docs/build.md` for cache bootstrap. No .NET SDK or Electron runtime is required.
- **0.7.0** adds mobile sharing and refreshed styling; published, but not installed into the user's normal installation during validation. See docs/mobile.md and run scripts/verify-mobile.ps1 for helper/privacy/lifecycle checks. Browser QA is tests/mobile-browser.cjs with Playwright/Edge. Bundle mobile.html, mobile_server.py and MIT-licensed qrcodegen.py; use UTF-8 mode for the Python pipe.
- Use explicit UTF-8 encoding for scripts that read/write source. Avoid one-off search/replace scripts for ongoing maintenance; prior edits exposed encoding issues. Prefer reviewable patches.
- Keep generated packages, downloaded runtimes, test installations, screenshots, preferences and logs out of source commits. Add ignore rules before staging files.

## Validation

- Run the app's `--test` checks for code changes affecting calculations or behavior. The 768 checks are not proof that all champion mechanics or live integration are correct. Use `scripts/verify-ui.ps1` for settings, review editing and layout checks.
- Render and inspect affected UI at the user's 1920x1080 monitor size. Rendering is available through `--render` and `--render-settings`.
- Test meaningful behavior: layout persistence, settings migration, update integrity, installer upgrades and rollback as appropriate. Do not merely mirror implementation in tests.
- Installer regression: launch the installer with the existing installation folder as its working directory, then update that folder. 0.5.0 failed with a sharing violation; 0.5.1 fixes this. Preserve this regression when refactoring.
- Never force-kill League or unrelated processes. Helpers owned by the app may be stopped during its shutdown. Windows filesystem operations must use checked, absolute targets inside the intended test workspace.

## Release and secrets

- Public repository/update channel: `https://github.com/existntl/rift-reference`.
- Private signing key: `work/publisher/release-key.private.xml` in the original workspace. It is unencrypted. Never read it into tool output, commit it, upload it, copy it into an app package, or rotate it casually. Keep it securely backed up outside the checkout. The embedded public key must remain compatible with existing recipients.
- Public-key file `update-public-key.xml` may be committed. Publisher signatures are separate from Windows Authenticode; the installer is not Authenticode-signed.
- Increment the release version for changed published binaries. Publish matching `RiftReference-Setup.exe` and signed `latest.json` assets under the matching version tag. Do not overwrite an existing published version.
- Verify the public updater download and signature after publishing. A local build does not update recipients until its release is published.
- Preserve user preferences and recovery copies during upgrades. Do not delete old workspace or installer backups as part of consolidation without a separate, explicit cleanup decision.
- Reviews live beside preferences in `reviews.json`; its recovery copy is `reviews.json.bak`. Preserve both during upgrades. Never package user notes or silently overwrite a corrupt review file.

- Public download website: https://rift-reference.reid-hill.chatgpt.site. Its separate authoritative checkout is C:/Users/fuck/Documents/Codex/rift-reference-site; follow its AGENTS.md and Sites skills for website changes.

