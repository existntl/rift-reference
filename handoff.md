# Rift Reference handoff

## Local 0.12.20 page navigation and feature retirement (2026-09-08)

Analyzed both supplied Blitz videos at one-second intervals with selected detailed
frames. See docs/navigation-flow.md for timecoded findings, implemented mappings,
data limits and future feature candidates. The user wants many of these features
eventually; unavailable integrations are deferred, not visible placeholder controls.

New DashboardNavigation.cs, Pages.cs and ReviewPage.cs implement main-window pages
for champion pool/catalog/reference, LP history, match details, Matchups, Review and
Updates. Back/Forward preserves page state and overview filters/row scroll. Review
drafts survive navigation; leaving a dirty note or closing requires an inline decision.
The history graph uses only recorded current-season rank snapshots with time filters.
Match details use known final self stats and inventory, preserving unknown vs zero.
No global tiers, public-profile backend, event timelines or predictions were invented.
The overview retains its existing custom row scroll instead of copying whole-document
scroll literally. Original logo and most-played-champion hero remain unchanged.

Dashboard no longer creates either overlay runtime, starts a Game Bar feed or widget,
or uses saved overlay settings to force 1s polling. Removed Game overlay from Preferences
and Builds / Runes from active navigation. Archived source and compatibility tests remain;
Preferences Save no longer touches overlay.json. Active settings has three sections.
No gameplay logic, audio schedule, mobile sharing authority or policy scope was expanded.

At explicit user request, uninstalled current-user package
RiftReady.GameBarPrototype_1.1.1.0_x64__q4vf8r75vcnhg. Windows Game Bar remains installed
with Status Ok; no Rift Ready widget/bridge process remains. Did not remove shared
dependencies/certificates or touch League/Vanguard. Normal installation overlay.json
was backed up to overlay.before-retirement-20260908.json and Enabled/GameBar set false
for its next startup. The main installed app was not force-closed or replaced.

Local source version 0.12.20 supersedes the unpublished 0.12.19 build below. Public
remains 0.12.18, website unchanged; nothing was pushed, signed for release, published,
or installed. Rebuild and sign a matching manifest before any future publication;
the existing dist/latest.json is NOT valid for the new local installer.

Validation passed: 769 app checks, 26 home-data / 14 rank-history checks, 132 dashboard
checks, 35 new navigation/retirement checks, all-monitor chrome, full Preferences/UI/
loadout/postgame suites, mobile privacy/lifecycle and 3 HTTP tests, and upgrade from
public 0.12.18 with rollback from the installation working directory. New tests cover
dirty-review close/install blocking, background-update refresh and honest empty states.
Inspected 1920×1040 pages and 1280×950 variants, plus Preferences button hit targets.
No fresh League live-game validation or claimed Riot/Overwolf approval.
Evidence: build/home-af589a228d2c43eeb48bd5e6c91fbea9,
build/ui-e700d5415df54bafbee8ef29cd42608e, final page renders/tests in build/app,
and build/installer-test-86a65c991cdc4e97a7ccb56dfb81d24e.
Final local installer: dist/RiftReference-Setup.exe, 23,647,744 bytes,
SHA-256 9E96DB669C71B90A93B1241972AEA9D3440199AC8DE7D55F433CCF0C3272172F.
Logo hash remains 200DC01A04D66023308E3D61D7D314725B9231907F33229BCBA36EC0F62A7C92.

## Local 0.12.19 demo removal and Preferences fixes (2026-09-08)

The user requested removal of all demo modes, reported clipped Cancel/Save controls on
every Preferences page, and requested the shorter "Minutes and seconds" dropdown label.
Removed the session selector, hidden legacy demo/connect buttons, simulated startup,
--demo launch handling, dashboard/postgame sample factories, and demo-specific desktop
copy. Normal startup still polls League automatically. Match/draft/results fixtures now
live in tests/DashboardFixtures.cs, compiled only into standalone UI/mobile test tools.
Render tools start with empty state and no automatic polling/audio; --render writes
overview.png, not fake matches. Keep overlay-alignment/audio previews and internal
Snapshot.Demo safety guards; these are not user-selectable game sessions.

Preferences title/description child windows overlapped Cancel at its left and bottom edges
(DrawToBitmap alone hid this native-window occlusion). Header geometry now reserves space
for both action buttons, constrains labels and keeps buttons in front. Tests check native
child hit targets at three points per button, label overlap and text fit on all four pages
at 1040 and 980 px widths. Escape cancels. Dropdown text is "Minutes and seconds", with
"Seconds only" retained; existing whole-second/minute-boundary calculations are unchanged.

Passed: 769 app checks; 26 home-data/14 rank-history checks; 154 dashboard checks; full UI
save/cancel/reopen, lane drag, postgame, loadout, toggle/dropdown suites; mobile privacy/
helper lifecycle and 3 HTTP tests; 104 overlay checks; 0.12.18 installer upgrade and rollback
from the installation working directory, preserving preferences/reviews/overlay/rank history.
Local --probe detected the connected League lobby and account, with no roster (0 players).
No fresh live-game test. Final overview/waiting and Preferences renders were inspected for
the user's 1920x1080 display. Evidence:
build/home-7d6ca2da99374337ad58ac43f39876f6,
build/ui-f6999a2fcfbc47b1ae2844d6c04b0ded,
build/overlay-edd479bebec246c2aa1a1826b67d3f8b,
build/installer-test-3c843bf354904269a158d7b84dece550 and build/app.

Local installer is dist/RiftReference-Setup.exe (23,636,992 bytes), SHA-256
208E3F257250B311AB5DC274FB7AA4F3ABB97935D227A562F0D7885F13537094.
This change is NOT published, signed for release, pushed, or installed into the normal
user installation. Public remains 0.12.18; website unchanged. The older dist/latest.json
must not be paired with this installer; create a matching 0.12.19 signed manifest when
publication is requested. Existing backups and generated older demo screenshots remain
untouched and excluded from the package.

## 0.12.18 player-overview redesign (2026-09-08)

The user requested the supplied overview mockup, explicitly retaining the current logo,
and then specified that the background must be the player's most-played champion.
Implemented horizontal navigation, blue-black panels and the existing teal accent,
profile/rank/performance sidebar, win-rate ring and match summary, and a dense history
table with real item slots. Champions opens a local-history browser; selection filters
Overview. Search filters local champion/role history, with all/solo/flex/ARAM queues and
100/50/20 match ranges. No public summoner search or invented statistics was added.

ChampionArtwork chooses the most frequent champion across the loaded last 100 all-queue
matches, with most-recent appearance breaking ties. It is independent of table filters.
The previous profile is retained in memory only while the same account remains connected.
No history, missing assets or offline requests show a neutral hero. Public Riot splash
images are fetched asynchronously over HTTPS and cached in data/splashes; account details
and match records are not uploaded. Original logo SHA-256 remains
200DC01A04D66023308E3D61D7D314725B9231907F33229BCBA36EC0F62A7C92.

Version 0.12.18 is published as the latest release:
https://github.com/existntl/rift-reference/releases/tag/v0.12.18.
The public tag points to source commit `2789ca2479b6dfafca79da5217b49bfde35f1406`;
tested local source is commit `26c9e2a836ddb01dd7963d3f2552572651c14f7f`, equivalent
apart from line endings and the ReleaseSecurity.cs UTF-8 BOM. The matching installer is
23,881,728 bytes, SHA-256
`B124178B1F9D642C940DB225629DE1479BA61442B3379C78595DDE3C010B6EC5`.
Public assets passed publisher-signature, size/hash, tampered-metadata, wrong-installer
and HTTPS checks. The compiled 0.12.15 updater discovered, downloaded and verified
0.12.18 without installing it. Evidence is in build/release-0.12.18 and build/public-0.12.18.

Passed: 769 app checks; 26 home-data and 14 rank-history checks; 149 dashboard
control/artwork checks; all-monitor chrome; full UI settings/toggle/dropdown/loadout/
postgame suite; 39 recommendation checks, 18 collector tests and 104 overlay checks.
Home evidence: build/home-7ea7bc33fa0b4fd4b378b9e2b2272295. UI evidence:
build/ui-6c7dc69859fa433b8a8cac6e05d187b0. Overlay evidence:
build/overlay-1ed4912192f0464d99d2805ec76b1f4d. Final 1920x1080 overview/live/draft/
settings and 1280-wide overview renders were inspected. The inherited-working-directory
upgrade from 0.12.17 and rollback passed, preserving preferences, reviews, overlay settings,
rank history and recovery copies; evidence: build/installer-test-53f0c1f75be648e2b84c290971db0c3e.
Website content remains 0.12.16; its latest-download link serves 0.12.18. The normal
installation has not been replaced. Live-game integration was not revalidated this turn.

## Single-agent work preference (2026-09-08)

The user withdrew the standing permission for sub-agents. Keep future Rift Ready work
single-agent and do not spawn or delegate to sub-agents unless the user explicitly changes
this preference again. This supersedes the historical 2026-09-06 parallel-work preference.

## Published 0.12.17 Blitz-reference UI pass (2026-09-08)

The user supplied three current Blitz dashboard screenshots and requested UI/feature
adjustments plus Blitz-style settings checkboxes in Rift Ready's palette. The screenshots
were treated only as visual references. Version 0.12.17 is now published at
https://github.com/existntl/rift-reference/releases/tag/v0.12.17. The public tag points to
source commit `40b1bf1e637ab0c846db3dcbd64a011d065ea900`, verified against local source
commit `3e9d428` apart from line endings. Public assets are `RiftReference-Setup.exe`
(23,669,248 bytes) and the signed `latest.json`. Installer SHA-256 is
`99FAA49CFC4BCCB3066F27F93FFEBD68258BA9F83F76E8AB40E29C51B4EDAF76`.

The public download passed publisher signature, installer size/hash, tampered-metadata and
wrong-file rejection checks. A compiled 0.12.15 updater discovered, downloaded and verified
0.12.17 without installing it. The final installer passed the inherited-working-directory
upgrade from 0.12.16 and rollback checks, preserving preferences, reviews, overlay settings,
rank history and recovery copies. Evidence is in `build/public-0.12.17`,
`build/release-0.12.17` and `build/installer-test-62c3bb56ec21403491706c50be091246`.
The website content remains 0.12.16 and its latest-download link serves the new installer.
The normal installation was not replaced by publication.

Added reusable `RiftToggle : CheckBox` controls with a compact pill track, charcoal/off and
Rift Ready teal/on states, round thumb, hover/focus/disabled styling and a SystemColors
high-contrast branch. Existing `Checked` behavior, Space-key activation and accessibility
semantics remain. All active General, Game overlay, Audio, Updates, stats-panel and
scoreboard-alignment checkboxes use the new control. Obsolete/unreachable settings and the
installer were not redesigned.

A later settings screenshot clarified the requested dropdown treatment. Added reusable
`RiftComboBox` controls with a rounded charcoal trigger, compact chevron, detached rounded
dark menu, lighter neutral selected row, Rift Ready teal check/focus treatment and a custom
dark scrollbar for long champion/item lists. Mouse selection, arrow/Home/End navigation,
F4/Alt+Down/Enter/Space opening and accessible expanded/collapsed/value semantics remain.
Preferences, Builds & runes, loadout editing, mobile address selection, reflection review and
the retained legacy settings paths now use the same control; no saved values or schemas changed.

Champion select now presents the ten known picks as compact cards instead of floating
portraits, keeps enemy-left/ally-right swapping and persistence, and emphasizes the local
player. Each team header shows counts of Data Dragon Tank, Mage and Support tags; these are
explicit tag counts, not damage shares, synergy scores or predicted win rates. Live roster
rows use separated rounded cards and retain the same reference cooldown semantics. Stale
draft/empty-state copy now accurately describes the guarded build save/import flow.

Builds & runes now uses the slim Rift Ready title chrome, dark owner-drawn champion/role
selectors, stronger segmented Common/Win rate and Paths/Options states, and rounded build
cards with clearer teal selection. It still selects coherent complete rune/core cohorts,
does not infer situational items, and never applies or locks anything automatically.

Validation passed 769 application checks, the full UI suite including dedicated toggle and
dropdown keyboard/accessibility/activation/render checks, 39 recommendation checks and 18
collector tests, 104 overlay checks, 14 home-data checks, 14 rank-history checks, and window
chrome/all-monitor maximize checks. Fresh 1920x1080 app, draft and build renders, every
settings page, and an opened long dropdown were inspected. Current dropdown evidence is in
`build/ui-51f85681b67c4699a9e5c70f868808ae` (including `dropdown-open.png`),
`build/overlay-09017178942c440f952fd1a7d5835a69`
and `build/app`; earlier home/rank/chrome evidence remains valid.

Do not add the reference's personalized pick/tier/synergy statistics, player mastery/rank/KDA,
team damage percentages, matchup-filtered builds, pro/OTP identities or click-to-lock-in from
the current data. Those features need a verified source, production/API scope and a fresh Riot
policy review; the current public recommendation feed and RSO profile backend remain unconnected.

## Published 0.12.16 title chrome, Preferences modal and click level (2026-09-08)

The current source follows the user's Blitz references with a slim charcoal title strip,
Rift Ready/version text at the left and compact Windows-ordered minimize, maximize/restore
and close glyphs at the right. Preferences uses the same strip with only close visible,
cannot be dragged or edge-resized, has rounded corners, and opens over a 58% black owner
backdrop. Save and Cancel remain explicit inside the dialog. The one-second click PCM level
increases from 3500 to 5000 while other cues, master-volume scaling and its off-by-default
preference remain unchanged. Source version is 0.12.16 and publication is complete.
The locally tested source is commit `6107d624fe091a2eb4111220627e3ec307725f86`.
GitHub tag v0.12.16 points to public source commit
`ded9a499667abed20c6af62db483a30432fb5c3d`. The public installer is 23,662,080
bytes with SHA-256
`C3A772BF33A89E291EA1BECEC0B9979A6A3404375140E1D9E4E3FFE2EB8FFFB0`.

The 769-check app suite, home/rank history, Preferences save/reopen, postgame, playbook,
loadout, 102 overlay checks, installer upgrade/rollback preservation, manifest tamper
rejection, and public download signature verification passed. A compiled 0.12.15 updater
probe discovered, downloaded and verified 0.12.16 without installing it. Public website
runtime source `bb6eb12099b5e26ccceba25db839daeabff91790` is deployed as Sites version 16.
The website keeps the Preferences/settings consolidation out of its feature showcase at
the user's request.

## Published 0.12.15 and website continuation (2026-09-08)

The user requested one Preferences area for all settings, a separate Updates area,
and styling consistent with Rift Ready. The published0.12.15 release replaces the
native TabControl flow with a minimal-window Preferences shell and an internal left
rail for General, Game overlay, Audio & reminders, and Phone / tablet. Game overlay
and Phone / tablet were removed from the main feature navigation; Preferences and
Updates are bottom utility actions. Updates now opens its own themed window and
retains automatic checks, match/champion-select update gating, HTTPS download,
publisher verification, release notes, and install behavior.

The Preferences pages use charcoal cards, teal section markers and active states,
dark owner-drawn combo boxes, consistent spacing, and the existing top-right
yellow/green/red title controls. Overlay options are edited as a draft; Cancel leaves
`overlay.json` untouched and Save retains the separate `preferences.json` and
`overlay.json` schemas. Phone sharing still starts/stops immediately and remains
embedded in Preferences. UI, mobile, server, postgame, home/rank history, window
chrome, app self-check, and overlay tests pass. Fresh render evidence is in
`build/app/*settings.png` and `build/app/overlay-preferences.png`; UI evidence is
`build/ui-6528b17ec1ed465f8f99f39235b96548` and home evidence is
`build/home-fae708783413450692aa195bf1fdf500`.

Version0.12.15 is now the public latest release:
https://github.com/existntl/rift-reference/releases/tag/v0.12.15. GitHub main and the
tag point to public release commit `4a6a6e827df428b7fc8900e2b5e7df1e0106d58e`;
the exact locally tested source commit is
`6ee20b20e9e9a61ef47493704e7021322dea3ceb`. The workflow used to transfer that
source removed its staging archive and workflow afterward. Public assets are
`RiftReference-Setup.exe` and signed `latest.json`. Installer SHA-256 is
`55152F26810794583FC2A0793A9AD3D91461F6719CA50C09E72C9B3443A3C68C`.
The normal user installation was not replaced by the publication workflow.

The matching website is live at `https://riftready.gg`. Runtime source commit
`7df2339f736ddfcb0bfa856efb89e738e87041c5` was confirmed at the Sites remote,
saved as Sites version14, and deployed successfully. Production HTTP verification found
the new0.12.15, Preferences, Updates, and installer content. Site source has a later
documentation-only commit `d492ac1217aad6a7b7c7fbcd06f7d3c30fb4630e`; it does
not require another runtime deployment. Direct Sites Git access worked, so the approved
GitHub Actions fallback was not used and no GitHub secret was created. Never copy a Sites
credential or release signing key into either repository.

## Supported Windows widget candidate (2026-09-07)

User said proceed with a different compatible route. Investigated Game Bar widgets
instead of retrying blocked module loading. InstalledGameBar7.326.8061.0 statusOk;
LeagueWindowMode0/1920x1080. Askeduser Win+G whilegamefocused: does it appearoverLeague
or switch/minimize? Answer pending. See docs/gamebar-overlay.md. No widgetbuilt,
SDKinstalled or securitysettingschanged. Need actualGameBarvisibility before choosing
this substantial UWP/XAML port; no claimfullscreenorFPSfixed.

## League native loader blocked (2026-09-07)

User opened Practice Tool for independent overlaytest. PID25256 was League executable
C:\Riot Games\League of Legends\Game\League of Legends.exe, verified via limited
QueryFullProcessImageName rights (.NET Process.Path was blank). Launcher now uses that
read-only fallback. Actual ordinary loader failed `target loader module unavailable`
Windows error5, during module enumeration BEFORE remote allocation/writing/LoadLibrary.
No native overlay DLL loaded. Producer stopped, installedapp untouched. Do not claim
gamefullscreenworks or attribute error specificallytoVanguard without evidence.
No elevated/evasive retry. Nativeapp host tests remainvalid but League pathblocked.
Fixed launcher misleading unconditional loadedDLLcleanuptext and preserve loader stderr
in attach-errors.txt via separate subprocess. No second League attempt was made.

## Independent renderer prototype (2026-09-07)

User explicitly chose building own integration after Overwolf submission. Implemented
native D3D11 renderer, explicit normal LoadLibrary/MinHook Present adapter and isolated
API/pixel producer. Read docs/independent-overlay.md and integration/README.md. No League
process running at inspection; asked user to open Practice Tool Fullscreen for live QA.
Installed0.12.13/public0.12.0 remain unchanged. No League attach performed yet.
Complete ordinary DLL load/Present hook test PASSED in owned fullscreen host with
SEQUENTIAL swap chain: DXGIfullscreenTRUE1920x1080;82,089 nonbackground pixels on,
zero afterdisable. Duplicate attach rejected. Evidence build/native-overlay/
verify-3b3c6a7439a346ee87dba5bef3bb8e1f; repeat integration/verify.ps1.
13bridge +26lifecycle +102overlay checks, app selftests,10GPU checks passed.
Own1920x1080 DXGI fullscreenTRUE test:567frames/4000ms, CPU draw mean.0354ms; this is
not a League benchmark. scripts/start-native-overlay-test.ps1 prepares a180sec opt-in
test after build; fails closed if normalprocess loading denied. No anti-cheat changes.
Overwolf submission succeeded with riftready.gg, ow-electron, no monetization;
older unsubmitted notes below are historical. Independent route requires no OW runtime.

## Fullscreen retest failed on current renderer (2026-09-07)

User reports no overlay in fresh Practice Tool fullscreen. Verified running0.12.13,
game.cfg WindowMode0 at1920x1080, overlay enabled/Purchase true, live probe reports
PRACTICETOOL/10players/Fresh=true/InventoryKnown=true. Therefore previous mode gate
is ruled out. This does not prove exact DXGI presentation path, but current native
renderer does not meet user's fullscreen requirement on this setup. Proceed toward
supported renderer integration; Overwolf access/approval remains external prerequisite.
Existing proposal docs/fullscreen-framework-proposal-draft.md is not submitted.

## Fullscreen priority: validation and integration prerequisites (2026-09-07)

User explicitly requests fullscreen overlay support as very important. Current0.12.13
unchanged; do not call fullscreen feature implemented. Inspected League compatibility
registry: no matching HKCU/HKLM Layers entry; three GameConfigStore FSE settings0.
No game running at inspection, saved WindowMode2. No system/game settings changed.
Asked user to retest fresh Practice Tool in Fullscreen now mode gate is repaired;
result pending. Existing native overlay may work with Windows FSO, which is not true
exclusive fullscreen. Need actual visibility + same-scene FPS before larger migration.

Primary research confirmed Overwolf Electron Overlay gaming package requires developer
credentials even local dev; approved developer key or Console API credentials. Public
proposal approval/full access needed, private-only apps not approved. No credentials/appID
available, no new runtime installed or custom injection. Drafted docs/fullscreen-overlay.md
and docs/fullscreen-framework-proposal-draft.md for concrete integration/access steps;
proposal NOT submitted. GameBar is not proof of true FSE support. Overwolf app approval
is not a Vanguard allowlist (Riot states no such list). Public release still0.12.0.

## Cleaner gold markers0.12.13 (2026-09-07)

User photo969f5e1c confirms calibrated overlay visible; requested no role indicators
and better visual quality. Removed labels, reduced marker height34→24, centered single
line bold11pt values (was9pt regular), enabled ClearType grid fit on opaque scoreboard
panels, antialias only arrow triangles, use1px borders. Editor instructions updated;
role comparison semantics unchanged and calibration preserved.102 overlay and768 app
checks passed;1920 preview visually inspected. Installed hash-verified0.12.13 after
graceful close; no dialogs open. Only app restarted, League left alone. Previous0.12.12
backup retained, overlay.json hash unchanged, private feed enabled. Public0.12.0.

## User photo confirms overlay; local scoreboard calibration (2026-09-07)

Phone photo in current task attachment21add473 confirms0.12.12 Practice Tool overlay
visible: team strip overlaps scoreboard header, first marker low, SUP below bottom.
Calibrated installed overlay.json only from observed marker/row positions: BoardY100
(was210),RowStart244 (was160),RowGap75 (was88),BoardScale100 andBoardX0 unchanged.
First row moves370→344 reference pixels; last722→644; header lifted110. Approximate
photo-based fit needs user visual check, not claimed pixel-perfect. No dialogs open;
gracefully restarted Rift Ready only, preserved other settings/privatefeed and old alignment
in overlay.json.before-photo-alignment.bak. No binary/version/public changes.

## Practice Tool overlay mode gate fixed0.12.12 (2026-09-07)

User still saw no overlay after switching borderless (game.cfg WindowMode2 verified).
Live local probe revealed In game / PRACTICETOOL /1 player, Fresh=false and inventory
unknown because both overlay parser and freshness accepted only CLASSIC. Added shared
SupportedMode gate accepting CLASSIC or PRACTICETOOL. Existing map11, stale/demo and
data completeness rules remain. Live probe against updated binary returned Fresh=true,
InventoryKnown=true. Added four regression checks;102 overlay +768 app checks pass.
Waited until user closed Preferences before restart to preserve edits. Installed verified
0.12.12, backup previous-0.12.11, settings/private feed retained, League left running.
User subsequently supplied a photo confirming visible Practice Tool overlay. Public0.12.0.

## Secondary-monitor blank/inaccessible window fixed0.12.11 (2026-09-07)

User reported blank app and unable to reopen after minimize. Initial process absent,
reopened0.12.10 responded but content remained inaccessible. Native screenshots unavailable
on this Windows build (SetIsBorderRequired E_NOINTERFACE), accessibility buttons visible.
Reproduced exact geometry using transparent test form on both real monitors: secondary
screen startsX1920 but maximized app landedX3840. Primary startup also omitted taskbar bounds.
Root cause MinimalWindow assigned absolute Screen.WorkingArea to MaximizedBounds, which
Windows interprets relative to monitor origin. Fixed relative work-area coordinates and
initialization on handle creation, retained refresh on normal move/maximize button.
Extended WindowChromeTests verifies actual Bounds equals each connected WorkingArea for
startup and button maximize. Passes on both monitors;768 app checks pass.
Installed hash-verified0.12.11 with previous-0.12.10 backup, only exe replaced; settings and
private feed preserved. Restarted Rift Ready only to repair user-requested inaccessible app;
League game left running. User confirmed app is visible and usable after fix. Public remains0.12.0.

## Overlay size and performance investigation0.12.10 (2026-09-07)

User played with overlay:144FPS fullscreen vs~85 borderless; no other overlay enabled.
Read-only system check: Windows10 build19045, GTX1070Ti driver32.0.15.8183,1920x1080
144Hz; saved League config WindowMode0,VSync0,GlobalScale0,minimap1.65. No game active
during investigation, so actual frame-time comparison unavailable. Do not change driver,
graphics/security settings or promise recovery from unmeasured hypotheses.

Changed gold markers80x53 to64x34, team strip440x72 to320x64. Preserved role labels,
direction semantics and saved row centers/spacing; exact alignment needs held-Tab screenshot.
Alignment editor now previews1920x1080 instead of incorrect square1080x1080 reference.
GameOverlay avoids repeated stats invalidation and geometry work on unchanged100ms polls;
keeps focus/key response. Current stats disabled, so this does not explain reportedFPSloss.
7 refresh tests,98 overlay checks and768 app checks pass. Offscreen stats benchmark
1000draws126.8ms vs100draws15ms; explicitly not gameFPS evidence. Renders reviewed at1920.
Evidence build/overlay-01210. Installed hash-verified0.12.10 after graceful app close, no game;
exe.previous-0.12.9 backup and user JSON hashes preserved. Relaunched with private feed.
Public remains0.12.0. No collector interruption or public deployment.

Fullscreen investigation: native transparent topmost windows do not implement exclusive
fullscreen composition. Overwolf documents graphics overlay injection and requires app
proposal approval/full-feature access; distinct from Riot API access or Vanguard allowlisting
(Riot explicitly says no Vanguard allowlist). Porofessor offers Overwolf and standalone;
Blitz internal rendering not verified. Next diagnosis: same Practice Tool scene, borderless
overlay disabled/enabled, fullscreen baseline, recordFPS. Need screenshot for exact row fit.

## Private Riot access repaired and real collection started (2026-09-07)

Same key returned200 in Riot portal NA status tester and403 with Python defaults.
Explicit RiftReady-PrivateCollector/0.12.9 User-Agent and application/json Accept
headers resolve it: local diagnostic status and ranked checks both passed, followed
by real Match-v5/timeline collection. Key transferred through browser clipboard to
masked native entry; clipboard cleared, no credential file or raw header logging.
Current collector is legacy build/private-desktop/PrivateBuilds.exe, budget1500;
do not interrupt it to replace launcher. Output private.sqlite is committed during
collection. Interim local recommendations.json was exported from read-only SQLite;
app Fetch/Bundles successfully validates its format, but initially zero qualifying
builds. Need check final sample/eligible counts before claiming populated choices.
Installed Builds & runes UI was also opened through native keyboard navigation;
it loads the private feed and reports the sample minimum, with apply disabled.
Latest observed collection: NA1 1377 player samples/165 matches (finished region),
EUW1 475/72 (in progress); first run remains active. No qualifying bundle yet.

Added scripts/build-private-launcher.ps1: maintained standalone themed GUI instead
of hidden PowerShell dialog, source-hashed immutable exe, atomic compile and per-repo
mutex. start-private-builds preserves running private session before app restart.
Launcher --seeds200 lets pipeline budget cap seeds instead of always stopping at12;
1500-budget behavior remains12 per region. Collector18 tests, masked launcher tests
and two relaunch checks pass. Masked launcher tests require Windows PowerShell5.1;
PowerShell Core Add-Type lacks matching desktop framework references.
Future launcher builds display allowlisted region progress and final qualifying
build/rune counts. Raw output remains discarded; regression covers secret-appended
fake progress lines. Running older launcher is intentionally left undisturbed.

LiveHomeProbe against installed local client returned100 matches and1 actual rank
snapshot. LiveFeedProbe in ignored build/app validates the real local feed using the
native parser and plan generation without applying any client changes. Local app
remains0.12.9, public0.12.0. No public/private-feed publishing or support message sent.

## Renewed key still HTTP403; access diagnostic added (2026-09-07)

User screenshot confirms 403 after renewal. Corrected UI claim that renewal is always
the fix: Riot docs say invalid path and authorization can both yield403. Added Test
API access button and --check-access mode: checks NA status-v4 then Diamond league-v4,
stops on failure, no matches/DB collection. Writes only fixed region/service/outcome
fields to ignored output/access-check.json; never key/body/player identifiers. Test
verifies denial stops immediately and diagnostics are sanitized. Opened updated
visible launcher PID15836; user needs to paste key and select Test API access. Actual
denied service remains unknown until then. No further renewal recommended yet.

## Collector stopped; diagnostics improved (2026-09-07)

User reported collection did not finish. No pipeline.py process was running; only
app transport remained. Private SQLite was created but no feed exists. Original
launcher discarded stderr, so root cause of that attempt is unknown. Added fixed,
allowlisted UI diagnostics for HTTP status, network, throttling, budget, schema and
module failures; no arbitrary stderr/keys displayed. Test confirms HTTP403 category
is shown while key appended to fake stderr is discarded. Existing launcher checks
passed. Opened updated visible launcher (PID18648); user must re-enter key and retry
to obtain actual error. Do not claim the prior failure was HTTP403 without evidence.

## Private development-key setup and longer history 0.12.9 (2026-09-07)

User requested private development-key builds/runes plus older matches and LP graph.
Added local file feed opt-in with 2MB bound and unchanged downstream validation.
scripts/start-private-builds.ps1 starts installed app with RIFT_RECOMMENDATIONS_FILE
and opens separate existing masked collector (1500-request budget). Key stays with
collector only; no public feed/server or RSO service. First run may lack enough
qualifying matches; minimum 30 games/10 players unchanged. Actual collection awaits
user key entry; do not claim builds are populated or live API integration succeeded.

Local League history requests 0..99 with 0..19 fallback; parser/display/scroll cap
100. Existing server availability may limit actual records. LP graph removes 30-point
display cap, showing retained current-window snapshots; no inferred historical LP.
14 home,14 rank,36 recommendation,17 collector,5 transport and window/app checks pass.
Masked launcher secret-handling tests pass. Installed exe plus updated transport.py,
backed up both 0.12.8 files, preserved user data. Started private launcher. Public
release remains 0.12.0. Evidence build/home-55c1f03e757944ae9770afb0740d5ede and build/app.

## Windows button order 0.12.8 (2026-09-07)

Reordered top-right circles to minimize, maximize/restore, close (yellow/green/red).
Build and existing home/rank/app/window checks passed; compact render inspected.
Installed hash-verified executable after graceful close, no game active, with 0.12.7
backup and user data preserved. Evidence: build/home-9177f25a612f443b8b639602db01c64f.
Public release remains 0.12.0.

## Right-side window buttons 0.12.7 (2026-09-07)

Moved traffic-light group to top right with 8px outer margin and right anchoring;
updated title drag region to exclude right buttons. Existing home/rank/app/window
checks passed; added resize alignment and left title drag coverage. Inspected compact
render. Installed hash-verified executable after graceful close, no game active;
0.12.6 backup retained, data preserved. Evidence:
build/home-34e1900f0ee9474f91d5150490df4a65. Public remains 0.12.0.

## Minimal main-window title bar 0.12.6 (2026-09-07)

User requested Apple-like window buttons and minimal title bar. Main Dashboard now
inherits MinimalWindow: 32px charcoal title region without title text, three left
12px red/yellow/green dots in 24px hit targets, hover glyphs, accessible names/focus.
Red closes, yellow minimizes, green maximizes/restores. WM_NCHITTEST supports dragging
and edge/corner resizing; maximize uses screen working area. Other dialogs unchanged.
ContentHeight and TitleHeight account for custom title area in drawings, controls,
scrollbar and lane hit tests. Draw navigation into bitmap so GDI text offsets correctly.
WindowChromeTests verifies controls and drag/resize hit tests. Existing home/rank/app,
scrollbar and UI tests passed; 1920 and 1280 renders inspected. Evidence:
build/home-fd14315b24754232b2f3b01cad7b3a84 and build/ui-79fe593020d24a71bf7a4b7e1aeac235.
Installed hash-verified executable after graceful close without active League game;
0.12.5 backup and all data preserved. Public remains 0.12.0. Full Windows snap/multiple
monitor interaction is not exhaustively verified.

## Scrollbar border removed 0.12.5 (2026-09-07)

Removed scrollbar DrawFocusRectangle; retained focused thumb highlight and keyboard
navigation. Existing home/rank/app/scroll interaction checks passed and focused 1920
render confirmed border absent. Installed hash-verified executable after graceful
close with no active game; previous 0.12.4 backed up, data preserved. Evidence:
build/home-76cce25e788145c4aa358604e736abae. Public remains 0.12.0.

## Theme color correction 0.12.4 (2026-09-07)

User requested scrollbar match app colors. Uses Theme.Accent teal with lightly
brightened hover/focus and translucent Theme.Background bands. Geometry and behavior
unchanged. Build, home/rank/app and existing scrollbar checks passed; 1920 render
inspected. Installed hash-verified executable after graceful close, no active game;
0.12.3 backup retained and user data preserved. Evidence:
build/home-c4ddb83538a34b06a20ce4bcf7ede54c. Public release remains 0.12.0.

## Reference scrollbar 0.12.3 (2026-09-07)

User supplied image and selected far-right green scrollbar. Updated thumb to green
with subtle horizontal bands and near-black track, retaining slim rounded geometry,
hover/focus feedback and existing interaction/accessibility. Build, home/rank/app and
scrollbar interaction checks passed; inspected 1920 sample. Evidence:
build/home-ee2eb6d9fc4f4f0d90d6652562eb0a26. Installed verified 0.12.3 executable after
graceful close with no game active; backed up 0.12.2, preserved data. Public still 0.12.0.

## Themed history scrollbar 0.12.2 (2026-09-07)

Replaced native light scrollbar with HistoryScrollBar in Theme.cs: slim rounded muted
teal thumb, charcoal track, teal hover/focus/drag, wider hit target, keyboard focus,
accessible scrollbar role/value and high contrast system colors. Track ends at match
rows above footer. Wheel, drag, page clicks, arrows/Home/End/Page keys retain bounded
row navigation. HomeRender tests keyboard, wheel, drag end clamping and layout.
Home/rank/app checks passed; inspected 1920 and 1280 sample layouts. Evidence:
build/home-37669a9faa8c4e7b8f2a4a4c632c1edf. Installed verified executable after graceful
close with no active League game; previous 0.12.1 executable backed up, data untouched.
Public remains 0.12.0; this is a local installed update, not a published release.

## Local scrollbar update 0.12.1 (2026-09-07)

Added a native vertical scrollbar beside match history, including draggable thumb,
arrow/page/keyboard navigation and synchronized wheel scrolling with bounded offsets.
Hidden outside the profile history view; disabled when all matches fit. Verified home
and rank checks, app checks, scrollbar end range, and sample render at 1920 and 1280.
Installed the verified executable into the user's existing installation after graceful
app close (no League game process active). Preserved all data and saved the previous
executable as RiftReference.exe.previous-0.12.0. Public release remains 0.12.0;
0.12.1 is local only. Source changes: App.cs, HomeDashboard.cs, ReleaseSecurity.cs,
tests/HomeRender.cs. Evidence: build/home-c9a93a0ab5454e93a8ef3510ce5b40d8.

Riot Chrome retry also failed. Developer support ticket 138375537 was successfully
submitted and is Open: https://support-developer.riotgames.com/hc/en-us/requests/138375537.

## Fresh-login retry also failed (2026-09-07)

Signed out of Riot portal; user completed fresh authentication and confirmed done. Retried
production registration with the saved full description, Default Group, League of Legends,
tournaments No and published website/policy links. Same error: "Failed to create application!
Selected app type is not available". This third submission attempt used a fresh authenticated
session, making an old-session timeout less likely. No successful registration/application ID.
Do not repeat the same flow without new evidence; manual browser retry or Riot support is next.

## Riot submission attempted; portal rejected creation (2026-09-07)

User explicitly confirmed accepting Riot terms and continuing. Accepted the displayed general
and tournament policy acknowledgments, selected production registration, filled Rift Ready,
Default Group, League of Legends, tournaments No, https://riftready.gg/ and the reviewed full
scope/contact/policy links. Submit returned: "Failed to create application! Selected app type
is not available". Repeated once through a fresh production-selection/agreement/form flow;
same error. No application ID, successful creation, review status or verification token was
issued. Do not claim submitted. Public policies/site remain published. No keys accessed.
Terms acceptance confirmation is no longer pending; the portal error is the current blocker.
Application text remains in continuation outputs/riot-submission-text.md; portal tab retained.

## Public policies and pending Riot agreement (2026-09-07)

User requested publish and submit. Published https://riftready.gg/terms and /privacy through
the separate website checkout; current homepage announcement now0.12.0. Sites version13
deployment succeeded, public pages HTTPS200. Public policies omit private application/editorial
notes and do not present proposed backend retention controls as deployed. Native unchanged.

Riot production registration reached I AGREE gate on /app-type. Browser rules require
action-time confirmation for legal acceptance; asked user, awaiting response. No submission.
Private portal-ready text: continuation outputs/riot-submission-text.md. Keep private draft
ignored; next step is accept only after confirmation, fill actual portal fields, submit and
verify outcome/website ownership requirements. Signed-in account MRSPOOKY; development-key
page exists but keys were not revealed or regenerated.

## Province confirmed (2026-09-07)

User confirmed Vancouver, BC. Private application and review outputs updated; Terms propose
BC/applicable federal law while preserving mandatory user rights, and Privacy adds BC OIPC
complaint guidance. No street address inferred. Earlier province-unknown notes are historical.
Policies remain drafts; nothing published or submitted.

## Operator/audience draft update (2026-09-07)

User supplied operator/contact details in the private application and policy drafts and
delegated audience/age selection. Draft initial market: Canadian League players of all ranks,
18+ and local age of majority (19 where applicable), no upper limit. No minor onboarding or
international expansion initially. This is not a current enforced download restriction;
do not assume collector participants are adults. Province, required address, effective date,
providers, retention and eligibility implementation remain unresolved. Review output copies
updated; no publication, email, submission or app change.

## Private application and policy review (2026-09-07)

User approved correcting the private application and preparing Terms/Privacy drafts, not
submission/publication. Updated docs/riot-production-application.md to 0.12.0, verified apex
website status, full local endpoint/mobile disclosures, pseudonymous collector storage,
unverified live flows and separate production/RSO approval. Added explicit .gitignore rule.

Accepted scope: own linked all-rank Solo/Duo API profile; proposed RSO verification; official
LP snapshots remain local initially; separate NA/EUW/KR Diamond+ aggregate feed. No public
profile lookup or cloud LP archive in proposed v1. Backend/RSO are not implemented. Profile
regions remain unresolved (NA1 suggested for first validation only). Retention targets in
Privacy draft are proposals, not approved commitments or deployed controls.

Review deliverables are in C:/Users/fuck/Documents/Codex/2026-09-07/continue-rift-ready-development-from-the/outputs:
riot-production-application-review.md (private copy), terms-of-service-draft.md,
privacy-policy-draft.md and policy-review-decisions.md. Resolve operator/contact, effective
date, markets/age eligibility, providers, retention and request handling before finalizing.
Website still lacks Terms/Privacy pages and advertises 0.10.0; no site change in this task.
No app code, release, install, credentials, portal submission or acceptance changed.

## Published0.12.0 (2026-09-07)

Published all pending implemented native changes to Latest release https://github.com/existntl/rift-reference/releases/tag/v0.12.0. Includes direct recommendation screen, Blitz-style idle player overview, actual rank badge/LP bar and locally recorded rank progression. Riot API-backed profile and live Diamond+ feed remain pending; private application draft excluded.

All application checks,13 home parsing,14 rank-history,33 recommendation,17 collector and5 transport checks passed; installer upgrade/rollback from0.11.0 preserved preferences and rank history. Downloaded public manifest/installer verified signature/hash/size and tamper rejection; isolated older updater discovered/downloaded/verified0.12.0 without installing. No normal installation performed. Existing latest download URL follows this release. Evidence build/public-0.12.0. Installer SHA256 b19f973ba7b3fd5ff3c8505df82c3feb229476f55610e231c90620dd6940dc05. Exact83-file public source snapshot verified at commit d74c297d75bff74f706677da06f4add3545d9e74 (release tag), tree99c11761c369ca8e417311ef9a357cefcc821713. Release-state docs updated locally after publication.

## API application scope extension (2026-09-07)

User wants rank badge/LP/progression powered by Riot API and added to the started request. Updated private docs/riot-production-application.md with ACCOUNT-V1, LEAGUE-V4 and MATCH-V5 personal-profile scope, official rank snapshots, no invented historical/per-match LP, server-side key and separate profile access/retention requirements. All-rank personal profiles remain separate from Diamond+ recommendations. Draft only; no portal submission, API backend or release performed. Existing local-client implementation remains in place.

## Rank badge and progression (local, 2026-09-07)

Added actual Riot tier badges, divisional LP progress bar and recorded ranked ladder graph to idle home. See docs/rank-history.md for asset bootstrap and local persistence. Graph starts from observed rank snapshots, with no invented earlier history. Preserves rank-history.json and .bak on upgrade/rollback; excludes them from packages/source.13 home parsing and14 rank history checks plus app --test passed.1920x1080 and1280 sample renders inspected; installer upgrade/rollback passed. Live-client integration remains unverified. Local only; public remains0.11.0.

## Blitz-style home polish (local, 2026-09-07)

User requested closer match to Blitz UI. HomeDashboard now uses flat charcoal continuous match rows with dividers, larger portraits, gold KDA text, screenshot-order metrics, wider rank/performance column with RR logo ring, and compact Last10 champion summary. Unknown data/result semantics preserved; footer keeps scroll hint even with notices. Idle heading is Player overview. No synthetic LP history, grades or copied rank emblems.

13 home parsing checks and application tests pass. Full1920 preview inspected (build/home-7d9f9b5b019d441491464aadc1eb89d5/home-1920-sample.png); compact1280x950 content verified via updated offline harness build/home-3dc6ea92d76d48f79831c9e6c5c1b64d. Harness avoids second-monitor Shown maximization when testing small widths. Local only, public remains0.11.0.

## Profile home screen (local, 2026-09-07)

User supplied Blitz-style idle dashboard reference. Added HomeDashboard with Solo rank/LP/season record left, recent champion records, recent10 all-queue summary and scrollable match rows with result/KDA/CS/min/vision/min/damage/min/KP/team damage share. Missing values remain unknown; no invented LP trends, grades or placement scores. Main idle view uses it; draft, live-data-loss and postgame retain their existing screens. History uses current-summoner identity and bounded local ranked/history endpoints through restricted read-only transport.60s memory cache per account/session, cleared disconnect and refreshed after live/postgame. No raw history written to disk or added to mobile payloads.

13 parsing checks and5 transport tests passed; application checks pass.1920x1080 sample and1280 empty layouts inspected in ignored build/home-*; placeholders in standalone renders do not replace app's portrait loader. Actual local client history schema/availability still needs live validation. Direct-build-screen change and this home dashboard are local only; public remains0.11.0.

## Direct builds screen (local, 2026-09-07)

User no longer wants original editor. Main Runes / builds opens RecommendationPicker standalone directly; save plan and guarded preview/apply runes/items moved into dashboard. Overlay still uses picker mode. Legacy BuildPlanner retained only as compatibility/test code, not app navigation. Empty feed disables save/apply; no synthetic production data. Applying captures validated plan, reviews, checks demo and client, disables edits while pending and keeps existing phase/champion guards. Feed refresh now consistently updates action availability.

17 collector tests,33 recommendation checks and application --test passed; direct populated synthetic preview inspected at1920x1080 (build/app/builds-direct.png). User installation/public release remains0.11.0; this follow-up has not been published.

## Published0.11.0 (2026-09-07)

User authorized publishing all pending changes. Published https://github.com/existntl/rift-reference/releases/tag/v0.11.0 as Latest with matching installer and signed latest.json. Includes connected dashboard/path browsing, direct overlay build selection, native dark title bars and explicit shortcut icons. Live Diamond+ feed still unavailable; no collector, key, database or private application draft is packaged. Existing website latest-download link automatically follows this release; website content unchanged.

17 collector tests,29 native recommendation checks,98 overlay checks, application tests and installer upgrade/rollback from0.9.3 passed. Downloaded public files passed publisher signature/hash/size and tamper rejection. Isolated0.9.3 updater discovered, downloaded and verified0.11.0 without installing. Evidence build/public-0.11.0. Installer SHA25670c5f38eeebd0287da563040d4d9ff241de501922111dd8d506c80950cd6e69a. No normal installation performed.

Source published through connector with non-force fast-forward to decdceede30f48b2099ffc2eafee8dfa55c523dc, tree984af63cacce14fe6bcf3d2e7b1ea0a5ee6a63f9; release tag points there. Private docs/riot-production-application.md excluded. Earlier local/unpublished notes are historical. Release-state documentation was updated after publication locally.

## OneTricks-style path browsing (local, 2026-09-07)

User asked to inspect OneTricks in Brave and implement its build-path approach. Brave was not exposed by browser inventory; public https://www.onetricks.gg/champions/builds/Vayne inspected instead, disclosed to user. Recommendation dashboard now has Paths / Options, first-core-item option cards with aggregate games/wins and explicitly limited shares of listed builds, and All paths reset. Clicking an option filters whole bundles and preserves valid selection or selects the first matching bundle. Champion/role/feed changes reset the filter. No OneTricks data feed/scraping, expert identities or unsupported matchup inference added.

17 collector tests,29 native recommendation checks and app --test pass. Lead reviewed options/filtered renders at1920x1080 in build/app/recommendation-path-options.png and recommendation-path-filtered.png. Synthetic fixtures only. No release, normal installation or live feed activation; public remains0.10.0.

## Dark native title bars (local, 2026-09-07)

User rejected the light Windows title strip. Added Theme.TitleBar, called for main dashboard, recommendation dashboard and Theme.Apply dialogs. DWM dark frame with legacy attribute fallback; exact charcoal caption/light text/subtle border on supporting Windows versions. Native window controls and geometry retained. High contrast uses system colors at application. Handle recreation reapplies styling. Microsoft reference: https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute.

UI suite and app --test passed. Native DWM readback in isolated test returned success and dark=1. Inspected1920x1040 dashboard render for layout regressions; DrawToBitmap does not capture compositor dark chrome, so actual title appearance still needs onscreen confirmation. Evidence build/ui-6a8f1ff26b24473f9c7e8e95c0c4c070. Local source/build only; no installation or public release.

## Connected visual build dashboard (local, 2026-09-07)

Implemented the user's Blitz screenshot direction with a three-column native dashboard: frequency/win-rate build cards, full rune choices, observed skill upgrade sequence, summoner spells and item sections. `RecommendationDashboard.cs` owns the picker; `Recommendations.cs` validates v2 paired rune/core groups and detail sample bounds. `OverlaySettings` can open the same selector and select the first core target; applying runes/items still uses the existing review flow. Spell icons are cached with Data Dragon assets.

Collector exports `rift-diamond-2`, grouping complete rune pages with observed three-item cores; detail modes are independently supported within each group, not necessarily observed together. Nullable SQLite details migration preserves rows and refetches legacy details. No fabricated pro identities, situational advice or skill timing. Read docs/recommendations.md. Synthetic preview data is test-only. No normal installation, release, website deployment or live collection performed for this change.

Verification: 17 collector tests, 26 native recommendation checks, 98 overlay checks and application --test passed. Lead inspected sample dashboard, unavailable state and overlay settings at1920x1080. Tests cover coherent alternative selection, role changes, invalid feeds/details, saved-plan transfer and settings isolation. Legacy enrichment preserves known core data if a timeline is absent; an accepted ambiguous-undo timeline still invalidates it. Evidence is ignored build/app/recommendation-*.png and build/overlay-bf64adb62ea540e0bbba865d215ab84d. Source remains local; public remains0.10.0.

## Windows icon repair (2026-09-07)

User reported a generic taskbar icon. Inspection verified correct white/teal RR icons in installed0.10.0 executable, current build, and running app window. Desktop and Start Menu shortcuts had unspecified IconLocation. Backed them up under ignored build/icon-repair-* and set their explicit icon to the accepted logo copied as installed rift-ready.ico; targets unchanged. Refreshed Windows icon display with ie4uinit -show, without restarting Explorer or the app. Taskbar visual refresh may require reopening the app. Installer.cs now explicitly sets future shortcuts to executable icon index0; targeted compiled shortcut regression passed. No binaries installed/released, preferences unchanged. Source change remains local for the next release.

## Riot development access setup (2026-09-06)

User signed into Riot portal and generated a development key; portal confirms expiry September7 at23:25 Pacific. Key was redacted from browser output, never stored in source/chat. Private collector launcher now supports masked user entry and child-process-only key environment. First live collection still awaits local key entry; do not infer successful API calls from portal status. Production registration reaches an I AGREE terms gate, left untouched for owner review. Application text prepared in docs/riot-production-application.md, not submitted; no production key or public hosting. See docs/recommendations.md for private launcher and feed setup.

Parallel agents implemented/tested the launcher and reviewed the collector; lead reran both suites. Twelve collector tests and launcher credential/lifecycle checks pass; launcher rendering inspected at1920x1080. Redirects cannot forward keys; omitted false match wins count as losses; malformed schemas are excluded. Masked launcher opened (process26344 at launch) for user entry. Do not start a second collection while it is running. No live result verified yet.

## Historical parallel work preference (2026-09-06; superseded 2026-09-08)

The user previously approved a lead agent coordinating specialist agents for substantial tasks. That authorization was withdrawn on 2026-09-08; follow the current single-agent preference at the top of this handoff. No application or release changes are implied by either workflow update.

## Diamond+ recommendations (local, 2026-09-06)

User accepted Riot-match aggregation and selected NA / EUW / Korea; no API key yet. Added Python/SQLite collector and restricted loopback feed preview under services/recommendations, plus Diamond+ choices in Runes / builds. Current patch, ranked solo, seven-day window; complete rune pages and timeline-derived first-three-core paths, minimum30 games/10 players, frequency ordering with samples/wins. Rank is observed at collection, not match time. Ambiguous undos exclude item samples. Native validation preserves manual plans when unavailable and uses existing explicit apply. Saved plans feed overlay target selection; no automatic advancement. Read docs/recommendations.md for setup, sampling and activation limitations.

Verification: 6 Python test cases, 10 native recommendation checks and 768 application checks passed; 1920x1080 fixture/unavailable picker and editor renders inspected. Synthetic choices are test-only. No key, live collection, deployment, public release, website changes or normal installation. Feed currently uses RIFT_RECOMMENDATIONS_URL developer override; public endpoint and production access still required. Public remains0.10.0 / website12. Source changes are local and uncommitted.

## Published 0.10.0 and website version 12 (2026-09-06)

Public release https://github.com/existntl/rift-reference/releases/tag/v0.10.0 includes the matching installer and signed latest.json. Public download signature/hash/size verified; the extracted 0.9.3 app discovered, downloaded and verified 0.10.0 without installing it. Upgrade from the old installation working directory and rollback preserve preferences, reviews and overlay.json/recovery. 98 overlay plus 768 application checks pass. Normal installation unchanged; live League integration remains unverified.

Native release source published to main via GitHub connector at ea211099a755f57d013b59f33f6ea1e5f5648890; tree c8ed137d5eb5900d4d62898f1be85f55e777c214 exactly matches local release commit 54d8b63. CLI GitHub authentication is not configured; release upload used the signed-in in-app browser. Do not try repeated CLI login prompts. Site version 12 is public at https://rift-reference.reid-hill.chatgpt.site; source 760b400124e6aedc07b1e4e9f655a5cc6938c721 adds overlay tab, real schematic demo and setup/limitations FAQ. Website build and TypeScript passed; browser QA was not requested. Domain purchase remains paused.

## Removed shared layout toolbar (local 0.10.0)

User requested removing the shared resize/layout menu and placing relevant functions in each gear. Removed toolbar and instruction banner. Build/stats gears now include reset only that panel, existing size/position/transparency controls, next preview screen, Use layout and Cancel layout. Ctrl+Enter/Esc/F6 work when all panels are hidden. Parent settings explains shortcuts, sample data and restoring closed panels through existing visibility switches. Fixed buff cards unchanged. 98 overlay plus 768 application checks passed; 1920x1080 editor and settings previews inspected. Local build only; public release unchanged.

## Fixed objective cards (local 0.10.0)

Replaced buff panel with code-drawn rounded Baron/Elder cards based on user's reference: purple/mint monster emblems, title, m:ss countdown and progress track. Fixed top-center 200x106 cards with 8px gap; only active cards occupy space. No team label or editor chrome/move/resize/close. Prior buff position/size ignored; enable switch and saved opacity retained. Layout editor uses sample data. Live kill events drive automatic visibility, independently of owner. Corrected Elder from 180 to 150 seconds (Riot 9.24b notes); Baron remains 180 (9.23 notes). Ordinary elemental dragons do not start cards. 95 overlay checks, 768 application checks, 1920x1080 preview inspected. No live game validation or publication; public remains 0.9.3.

## Compact panels and transparency (local 0.10.0)

User requested at least 50% smaller overlay boxes and independent gear transparency. Default width and height are halved; PanelSizeVersion migrates custom dimensions once (zero still automatic). Minimum resize is 120x40. Gear > Transparency offers 20–100% whole-panel opacity, preview, Use opacity, and Cancel rollback; each value persists independently with the parent settings. Layout editor chrome remains opaque and compact. Live windows apply native Form.Opacity and stay click-through. Build remains beside minimap, buffs top center. 90 overlay checks and 768 application checks; compact 1920x1080 and opacity dialog previews inspected. Local build only; public 0.9.3 unchanged, live League integration unverified.

## Resizable overlay panels (local 0.10.0)

Build, buff and stats panels now resize from all edges/corners in the layout editor when unlocked. Lock prevents moving/resizing. Six independent dimensions persist through Use layout and Save overlay settings; zero retains automatic sizing for older preferences. Sizes fit the current viewport; content scales proportionally. Gear offers Reset size; Reset defaults restores all positions and sizes. Defaults beside minimap/top center unchanged. 81 overlay checks and 768 application checks passed; inspected 1920x1080 resize preview. Live game integration remains unverified. Public 0.9.3 unchanged; no publication or normal install performed.

## Optional stats panel (local 0.10.0)

Implemented the accepted optional stats panel. CS/min and kill participation are the default
selections; total CS, K/D/A and vision score are optional. Enable via Game overlay > Stats
panel or Show stats in the layout editor. Its gear selects displayed metrics; it also has
independent drag/lock/close/reset and normalized saved placement. Default position is left
side, below the editor toolbar. Uses sanitized current-player scores and complete same-team
kill totals from the existing local snapshot. Missing scores, zero game time, incomplete team
data and zero team kills remain unknown rather than guessed. No gold/min or rank benchmarks.
Riot Live Client scores schema checked 2026-09-06. 71 overlay checks plus 768 app checks pass;
1920x1080 stats preview inspected. Real-game validation remains pending. Not published or
installed normally; public app remains 0.9.3 and website version 11.

## Individual panel controls (local 0.10.0)

Replaced the layout editor's general lock toggle with three small controls on each panel:
independent lock/unlock, settings gear (reset only this position), and close. Closing a panel
updates its own overlay visibility option when the editor and parent settings are saved;
Show build/Show buffs restore it. No inactive fake buttons are drawn over gameplay. The
explicit editor remains the interaction surface; live overlays remain click-through.
Researched Porofessor's official support/FAQ/download pages on 2026-09-06; its exact panel
gear actions were not documented, so the reset action is our design choice rather than a
claim of matching hidden Porofessor behavior. 63 overlay checks and the 768 app checks pass,
including independent lock, close and restore. Local only, publication unchanged.

## Movable overlay panels (local 0.10.0)

User accepted the compact redesign and requested independent drag/drop with lock/unlock.
Added Game overlay > Move / lock panels: full-screen preview using real panel renderers,
Unlock/Lock, drag either panel, Reset defaults, screen switch, cancel and Use layout & lock.
The parent Save overlay settings commits the result atomically. Gameplay windows remain
locked and click-through; editing happens in the explicit layout preview. Build defaults
bottom-right, to the left of a default-size minimap; buffs default top-middle. Existing custom
legacy X/Y build positions migrate, normalized custom positions clamp across window sizes
and monitors, and moving one widget does not affect the other. Local compilation and 59
overlay checks pass, including lock/unlock drag behavior and migration. Public release and
normal installation unchanged; real-game testing is still pending.

## Overlay reference redesign (local 0.10.0)

User supplied Blitz/Porofessor images and explicitly asked for research and matching overlay
behavior. Primary sources reviewed: Porofessor download page, Overwolf League game-events,
overlay/window API and pass-through manifest documentation, Blitz homepage and current Riot
general policy. Public documentation establishes platform capabilities, not the proprietary
implementation of either app. Findings and links are in docs/overlay.md.

Implemented five role-aligned blue/red gold-difference markers and complete team item-value
totals, compact horizontal component/target icons with gold-needed badges and target progress,
and separate Baron/Elder bars. Added alignment controls with live schematic preview, scale,
offsets, row spacing and team-side order. Transparent scoreboard windows expose only the
small HUD elements. Main app palette is unchanged; no Overwolf runtime was added.
Local compilation, 768 app checks and 50 overlay checks pass. Inspected 1920x1080 preview
and alignment/settings renders in original workspace overlay-change/evidence. Source remains
0.10.0 unpublished; public 0.9.3/site version 11 and normal installation unchanged. Real-game
alignment/focus/click-through, exclusive fullscreen and automatic Probuilds ingestion remain
unverified or unimplemented as detailed in docs/overlay.md.

## Local 0.10.0 game overlay (not published)

User requested Tab lane gold comparison, Baron/dragon buff duration and gold-to-item
progress based on pro recommendations. Implemented an optional native overlay with
independent switches, a held-scoreboard-key lane item-value comparison, estimated
Baron/Elder team windows and target/component purchase progress. Separate desktop
settings allow choosing a champion/item or loading the existing saved manual plan format.
Automatic Probuilds recommendation ingestion is still unfinished; no supported feed was
established. Do not describe saved manual plans as automatic recommendations.

Read docs/overlay.md for data limits and policy sources checked 2026-09-06. Source 0.10.0
is local only. Public app 0.9.3, website version 11 and paused domain decision are unchanged.
No normal user installation changed. Authoritative build, 768 app checks, 35 overlay checks,
existing layout/practice/loadout/postgame UI regressions, and installed-folder CWD upgrade
from 0.9.3 plus rollback pass. Overlay settings and recovery are preserved. Installer evidence:
build/installer-test-e9447ddf572b4a8498a1d413fcd3f013. Inspected 1920x1080 overlay preview,
overlay settings and affected desktop navigation; visual/test artifacts are staged in the
original workspace at overlay-change/evidence. Public dist installer/manifest are untouched;
unsigned local test installer is under overlay-change/package-8e5600eb636f498684ce094d32051620.
Real League focus/Tab/click-through, events, DPI and device smoke testing remain pending.

Updated 2026-09-06. Read with AGENTS.md.

New sessions: read START-HERE.md for the concise current state, including website palette
publication (Sites version 11), paused domain decision and landscape support/validation limits.

## 0.9.3 companion logo correction

Final phone screenshot inspection found the embedded logo was blocked by the mobile
CSP (also affected earlier rebrand builds). Added only img-src data:; external images
remain blocked. Browser regression now waits for successful logo decoding. Mobile server
and phone/tablet browser tests plus 768 app checks pass; inspected phone logo successfully.
Native fix commit 149f372. No installer behavior or desktop styling changes.

Published latest v0.9.3, matching signed manifest and 21,521,920-byte installer.
SHA256 `7a4cb9e131a52f8000a0a6a101fc1487a26edeac0050a6716a6c9ac949915711`.
Public/local match, signature/hash/size/tamper and isolated 0.9.1 updater verification
pass. Evidence build/public-0.9.3, updater probe in build/public-0.9.2/old-client-probe.
No normal installation changed. This supersedes 0.9.2 for downloads.

Website source `688733a754ced77ba710e4dac3afc8df2d3b96d2`, Sites version 10
`appgprj_6a9d95f2d8c481919181cda785d25520~appgver_17c0dc5467e08191aefae0f59d142915`,
deployment `appgdep_6a9dc679c1f081919aacd52397bc5dde` succeeded. Production build/types
and public HTTP/version announcement verification pass. Public access and URL unchanged.

## 0.9.2 white-and-teal logo

Implemented the user's second logo board across the embedded desktop logo, app and
installer icons and phone companion; website gets cache-safe v2 logo/icon URLs and
refreshed active previews. Preserved the previous charcoal UI palette and all upgrade
identities. UI, branding, mobile server checks and installed-folder upgrade/rollback
pass. Evidence build/ui-0b52ec0343cd47efbbe5a4b3ba44fe9f and
build/installer-test-4fa18b2ff0334a949408061760a004de. Phone/tablet browser checks pass.

Published latest v0.9.2 with matching signed manifest and 21,521,920-byte installer.
SHA256 `5e5c7576454a3be797a02ec1b6859a9a7d064276d2f8b8309835dd5c18291b2a`.
Public/local hash match, signature/hash/size/tamper checks and isolated 0.9.1 updater
discovery/download verification pass (build/public-0.9.2). Native source commit 617d79c;
normal user installation was not changed.

Website version 9 published successfully with existing public access. Source
`b6b9a1e1c9003d36e8807ffe4f2bc3236c6de3c6`; Sites version
`appgprj_6a9d95f2d8c481919181cda785d25520~appgver_54a386235e648191a26009f4051ea90f`,
deployment `appgdep_6a9dc59afd4c819188179ff7cc256204`. Build/types, local HTTP and public
page/logo/favicon checks pass. No website browser UI QA requested.

## 0.9.1 palette correction

Published latest app release v0.9.1 with matching installer/signed manifest. Public installer
21,715,456 bytes; SHA256 `d78fee90f71d77c883ec59ab2649d0ddb67e8ff15c2f80c46d24d1f3ff5fe3e8`.
Signature/hash/size/tamper and old 0.9.0 updater download checks pass (build/public-0.9.1).
UI harness, native render inspection and phone/tablet browser tests pass. Installer CWD
upgrade/rollback evidence build/installer-test-ccb5880873164619a691ba98e338f3dc.
Native feature commit 84ce646; normal installation unchanged.

Website palette/screenshots published successfully with existing public access. Source
`0e524685d5382f6325e2efd5b946e7b8e8914451`; Sites version 8
`appgprj_6a9d95f2d8c481919181cda785d25520~appgver_0f03191f116481919d71921e012104ab`,
deployment `appgdep_6a9dc0e44dd4819181b983b8006dbc11`. Build/types and public HTTP check pass.

User asked to restore previous colors. Restored the pre-rebrand desktop Theme tokens and
navigation background, and the previous website/mobile CSS palette. Rift Ready name,
monogram, tagline, icons and upgrade identities remain unchanged. No behavior changes.

## Rift Ready 0.9.0 rebrand

Published and verified: https://github.com/existntl/rift-reference/releases/tag/v0.9.0 is latest, with matching signed latest.json and 21,715,456-byte RiftReference-Setup.exe. SHA256 `ff35831d863ea948e742b0d2dad49856bbcdf2571f07e2c39940fbbb77649ba4`. Public installer matches local package and passes signature/hash/size/tamper tests; isolated 0.8.1 updater discovered/downloaded/verified 0.9.0 without installation. Evidence build/public-0.9.0. Native source commit 576bf03; normal installation unchanged.

Website rebrand published successfully with existing public access and URL. Source `e3a72a8213862999a2e2c77d591dafc4e4361c94`; Sites version 7 `appgprj_6a9d95f2d8c481919181cda785d25520~appgver_94c54475fb48819182efe8a7bc88bd0e`, deployment `appgdep_6a9dbfa3600c8191846cfc5386fcdbfb`. Site metadata title is Rift Ready. Production build/types and public page/logo/favicon HTTP checks pass. Browser UI QA was not requested. User-visible brand is new; retained old URL/file identifiers preserve continuity.

User supplied a Rift Ready brand board and requested implementation in app/webpage. Display copy, desktop navigation/logo, app and installer icons, Windows display/shortcut names, phone companion, website branding/metadata/favicon and active screenshots now use Rift Ready. Palette follows supplied mint #5FE1C2/deep green #0F3D36/charcoal #0B1211/off-white #E9F1EE; small text uses a lighter muted green for legibility. See docs/branding.md for asset provenance and prompt. Internal executable, product ID, registry/install paths, existing GitHub release filenames, website URL, source namespace and signing key are deliberately stable for upgrades.

Added Brand.cs, committed logo asset and repeatable icon builder; mobile logo is embedded at build time, no new route. Installer migrates the old shortcut only when its target matches this installation. Tests: 768 checks, complete UI harness, mobile privacy/server/browser checks, upgrade from 0.8.1 with installed-folder CWD and rollback (build/installer-test-ee5e772da72145f5903a198741f78973), shortcut ownership and embedded-logo checks pass. UI evidence build/ui-67ed29228dca408fba51fc03d1649e32; fresh default renders build/app. Normal user installation unchanged. Publication status will be recorded after verification.

## Published 0.8.1 app and website

User explicitly requested publishing all local changes. Released https://github.com/existntl/rift-reference/releases/tag/v0.8.1 as latest, with matching RiftReference-Setup.exe and signed latest.json. Public installer is 20,918,784 bytes, SHA256 `7ed747e47605f8abac9c511bf05fed3e86aee32831621f3a1c1d144232aba16f`. Public/local installers match; signature/hash/size/tamper checks pass. An isolated 0.8.0 updater discovered, downloaded and verified 0.8.1 without installation (build/public-0.8.1). Upgrade from 0.8.0 while installer CWD is the installation folder, and rollback/prefs/review recovery checks pass (build/installer-test-a609aaa262c5426daa3648d928b51bb2). Source feature commits 0d536ab and f9c8b96 remain in the authoritative local checkout; release tag targets the public README branch. No normal user installation was changed.

Website publication succeeded at https://rift-reference.reid-hill.chatgpt.site with the 0.8.1 announcement, current rune editor/postgame portrait screenshots and an added item-editor feature tab/preview. Source `a2a61c76168d32074c1b4862119859b029908648`, Sites version 6 `appgprj_6a9d95f2d8c481919181cda785d25520~appgver_a2a20b86c36481919e95ddb41c347952`, deployment `appgdep_6a9dbc6f13688191ab20d9772703e6c4` succeeded with existing public access. Production build/types and public page/assets HTTP checks pass. No browser UI QA was requested. Dist now contains the published 0.8.1 installer/manifest; private key stayed external. Existing provider feed and real-client validation limits remain.

## 0.8.1 implementation history (now published)

Follow-up: added bundled champion portraits beside all ten names in the desktop postgame scoreboard, with a placeholder for unavailable artwork. Champion columns reserve space for portraits; role labels fit compact rows. Inspected 1920x1040 and 1280x950 renders; existing PostgameTests passes (also renders 1720). Evidence build/app/demo-postgame.png and postgame-1280.png. Still local 0.8.1, not published; mobile summary unchanged.

User requested rune pages/items that look familiar from the League client. Inspected actual rune and item-set reference screenshots (sources in docs/pregame.md). Replaced the rune dropdowns and item spreadsheet with primary/secondary tree and rune icon rows, stat shard icons, selection highlights/descriptions, searchable/category-filtered item icons with prices, and named shop sections with add/reorder/remove actions. Preserved Rift Reference branding/charcoal/teal styling, explicit previews and guarded append-only imports. No provider feeds or new gameplay assistance added.

New src/LoadoutEditor.cs separates selection models and visual controls from BuildPlanner persistence/import flow. Existing rift-loadout-1 plans remain compatible, including item names; invalid saved item sections cannot partially replace the current plan. New scripts/cache-loadout-icons.py caches 328 official Data Dragon PNGs for bundled 16.17.1. Assets stay ignored in cache/data/loadout-icons and are copied by normal builds, with no runtime network requests. Source imports BuildPlanner + LoadoutEditor; no new runtime dependency.

Version source is local 0.8.1. Public app/site remain 0.8.0; dist still contains the verified 0.8.0 published installer/manifest. No signing, release publication, website changes or normal installation in this task. Validation: 768 checks, UI harness including actual icon clicks/search, rune row/tree rules, save/load, invalid-plan preservation, duplicate items and ordering/removal; four transport tests. Inspected rune/item renders in build/ui-2d3e31b4e1a14cd3b8a2c4307b86d88c at 1296x899 (fits 1920x1080). Existing live-client validation limits still apply.

## Published 0.8.0 and website update

User explicitly requested publication after the postgame work. Released https://github.com/existntl/rift-reference/releases/tag/v0.8.0 with RiftReference-Setup.exe and matching signed latest.json, marked latest. Public installer: 16,992,256 bytes; SHA256 `3bfe5dca8b2ec3edfceb03a2893ebb20908840e62fccdc0c64f4d71d402e62dc`. Public signature/hash/size/tamper checks pass. An isolated 0.7.0 updater discovered, downloaded and verified 0.8.0 without installation; evidence build/public-0.8.0. Upgrade/rollback regression also passed (build/installer-test-21bf2dd370c34f3199fbe4ca6776a1b4). Native feature source is local commit b41ece5; GitHub release tag targets the public README branch, not the maintained source checkout. Normal user installation was not modified.

Website https://rift-reference.reid-hill.chatgpt.site was updated with the 0.8.0 announcement, current dashboard plus pregame/postgame/loadout screenshots, phase-based feature tabs and honest integration limits. Website source f6b132c29956be98ddcc66ca108eed0481107d9d; Sites version 5 (`appgprj_6a9d95f2d8c481919181cda785d25520~appgver_d9f7ab1206e481918dac8794340ebd7e`), deployment `appgdep_6a9db6d1996481919cbaf8f1a73740ba`, succeeded with existing public access. Production build and types pass; public page and new postgame asset return HTTP 200. No website browser UI QA requested or performed. Remaining provider-feed and real-client validation limitations below still apply.

## Postgame statistics (0.8.0)

User requested stats instead of cooldowns after a game. Implemented desktop and mobile match summaries for PreEndOfGame, WaitingForStats and EndOfGame. Live GameEnd events also switch out of the cooldown screen. Reads `/lol-end-of-game/v1/eog-stats-block` only for postgame; checks its gameId against `/lol-gameflow/v1/session` gameData.gameId before displaying it. No arbitrary latest-history fallback or stale live-sample totals. Missing/unready results display a waiting state and retry with ordinary polling.

Shows outcome, duration, personal K/D/A and ratio, CS including neutral minions, CS/min, champion damage and damage/min, gold, vision, final items, and two-team scoreboards. Unknown fields render as dashes, not zero. Missing either minion component makes total CS unknown. Raw result account/chat fields are discarded; mobile receives only sanitized summary strings and champion stats. Review remains for manual notes. No persistent match history added.

`src/Postgame.cs` owns parsing/formatting and the sanitized mobile projection. `--render` now emits demo-postgame.png; Demo results is available in the header. `tests/PostgameTests.cs` tests parsing, stale/wrong-phase rejection, unknown and zero handling, privacy and 1920/1720/1280 desktop renders. Existing 768 checks, UI harness, four transport tests and mobile checks pass; browser tests cover result/live transitions. See docs/postgame.md. Completed-game testing against an actual League client remains pending. App and website are now published as 0.8.0; normal installation was not modified.

## Pregame and loadout tools (0.8.0)

New user request: replace draft cooldown tables with strengths/weaknesses, counters and comp plans; add source recommendations and client rune/custom-shop imports. Implemented dedicated draft brief (desktop + mobile), kit-based matchup considerations, qualitative team plans, native rune/item editor, save/load plans, direct source champion-page links and explicit guarded append-only client imports. New files: src/Pregame.cs, src/BuildPlanner.cs, tests/LoadoutTests.cs, tests/transport_test.py. See docs/pregame.md for behavior and source research.

Automatic Probuilds/Onetricks recommendation feeds are **unfinished**: no documented integration found, and Onetricks ordinary automated request hit a security checkpoint. No bypass attempted. Current UI honestly provides browser links and manual choices, not imported source recommendations or statistical counter rankings. Finish through a supported provider feed/export mechanism; do not silently call this complete.

Validation: 768 existing app checks pass; UI harness passes including new loadout validation and draft header persistence. Three transport mock tests and three mobile server tests pass. Phone/tablet browser checks include draft/live switching. Installer upgrade from public 0.7.0 with installation folder as working directory and rollback pass (build/installer-test-0081068701234846a50674ce703c396c). UI editor evidence: build/ui-a53fda92ed3c4b58829f8ba35bf60a20; draft render: build/app/demo-draft.png. These checks do not substitute for the pending real League smoke test.

Compiler, published app and website now describe 0.8.0. Signed dist/latest.json matches the published 0.8.0 installer. No normal installation was performed. Mock transfer/transport tests cannot establish real League page selection or in-game item-set visibility. Real-client smoke validation remains pending. No local user build files are automatically stored in the installation folder; saved plans use user-chosen paths.

## Consolidation

This checkout is maintained, cloned from https://github.com/existntl/rift-reference.git
on branch `consolidate-local-source`, preserving the initial README commit and release tags.
The app source matched the original distributable mirror byte for byte. Standalone harnesses
were copied from the maintained source; obsolete Setup.cs was omitted. Original workspace,
installers, preferences, backups and private signing key remain intact. Do not independently
maintain the old work/src or source mirror.

Structure: src (C#), helpers (Python), tests (standalone harnesses), scripts (build/validation),
docs (build and aspirational specification), config (update feed), publisher (public key only).
Ignored cache contains an independent copy of the verified data/runtime bootstrap.
No private key, local preferences or generated package belongs in Git.

Build: `powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Installer`.
See docs/build.md for fresh-checkout bootstrap from the signed 0.5.1 release.
Migration baseline: 745 app checks pass; installer inherited-working-directory upgrade
and rollback preserve preferences and executable. Output: build/app and dist.

## Product and continuity

Windows 10 native second-monitor League companion, primarily Vayne ADC in Emerald solo queue.
Published version is 0.8.0. Remote currently hosts releases, not maintained app source.
Report local work separately from public publishing.
Feed: https://github.com/existntl/rift-reference/releases/latest/download/latest.json

Keep enemy-left/allies-right defaults, drag-to-swap persistence, whole-second cooldowns,
minutes-and-seconds formatting, colored spell labels and names, all-enemy respawn notices,
and brief "Jungle check." speech. First jungle cue stays 120 seconds, every 90 through 480.
One-second click stays off by default; each audio interval is independently selectable.

0.5.1 fixes updating when installer working directory is the installation folder. Preserve
that regression and recovery folders. Never force-kill League or unrelated processes.
User confirmation that their original failed updater retry succeeded is still unavailable.

Private release key stays outside checkout in the original workspace. Keep the embedded
public key compatible; verify signed metadata and installer hash/size when releasing.
Publisher signatures are separate from Authenticode; installer is not Authenticode-signed.

## Published 0.6.0 feature build

Implemented on `feature/decision-practice`, based on migration commit `597e600`:
- Dashboard lane plan, five-enemy kit reference and persistent one-focus sentence.
- Specifically authored Vayne/Lulu versus Caitlyn/Nautilus plan. Other quartets combine
  authored ADC patterns and partner/enemy tools; not all pairings have researched plans.
- 28 champion threat summaries; unknown kits are shown as unreviewed. Missing or ambiguous
  bot roles produce an incomplete state. Unbalanced lane data uses a roster layout.
- Playbook: matchup references, teamfight plan with ally protection, wave/recall scenarios,
  advantage conversion, training instructions, sources and coverage.
- Review: local self-assessment, previous-note selection and editing, atomic save with a
  recovery copy. Available after matches (and in demo), no automatic popup or diagnosis.
- Preferences v2 maps old focus options and preserves previously migrated settings. Users
  can restore the original matchup cards. Existing audio options and timing remain intact.
- Installer preserves preferences.json, reviews.json and reviews.json.bak across upgrades.

New source: src/Coaching.cs and src/Practice.cs. Tests: tests/CoachingTests.cs (included in
--test), tests/PracticeUiTests.cs and scripts/verify-ui.ps1. Installer package uses intentional
inputs, excluding notes, preferences and test artifacts. Version 0.6.0 was published on
2026-09-06 as the latest regular release, with RiftReference-Setup.exe and signed latest.json.
It was not installed into the user's normal installation. Source remains local at feature
commit f054264; the public release tag points to the existing release-channel README commit.

Release: https://github.com/existntl/rift-reference/releases/tag/v0.6.0
Installer SHA-256: 2ddb430f97e515e9375dbf0f7c420a47134b1bcc7a25a79c25681f1f9034918a
Publisher files are in ignored dist. Public verification evidence is in build/public-0.6.0.
The latest-feed manifest and public installer passed signature, hash/size and tamper checks.
A copied 0.5.1 updater discovered, downloaded and verified 0.6.0 without applying it.
Signing used the existing external private key without copying it into the checkout.

Validation: 768 app checks; UI harness verifies preferences save/reopen, drag persistence,
original-card option, alternate/partial/solo layouts, all playbook pages and review save/edit.
Inspected 1920x1040 windows for a 1920x1080 monitor with taskbar. Upgrade/rollback checks
preserve notes and their recovery copy. Public signing key continuity is unchanged.
Live gameplay and normal installer registration/shortcut checks remain unverified.

## Published 0.7.0 phone/tablet and visual refresh

User authorized phone/tablet as an alternative to a second monitor, then requested a
Blitz-inspired desktop visual refresh. Both were published as 0.7.0 on 2026-09-06.
The normal installation was not modified during validation.

Preferences > Phone / tablet selects a private IPv4 interface and starts a bundled Python
HTTP helper. Local QR and copyable link contain a per-session 256-bit secret. Explicit stop
and app shutdown terminate the owned helper. Sharing is off on every launch; no preference
auto-enables it and no firewall or router settings are changed. Fresh sessions revoke old
codes. See docs/mobile.md for pairing, Private firewall guidance and trusted-LAN limits.

Mobile.cs serializes a whitelist of champions, roles, levels, reference spells, kit summaries,
focus and lane plan. No account names, review notes, client credentials, raw endpoints or
game inputs are served. One selected RFC1918 IPv4 listener, exact Host/Origin validation,
Bearer-authenticated state, no CORS/cache, fixed routes, bounded workers/socket timeouts.
Local HTTP is unencrypted. UTF-8 Python mode is required for correct reference text.
Mobile refresh is every 2s over the existing desktop 3–30s data polling, default 5s.
Stale/lost data is hidden and reconnects automatically. Audio remains on PC. Normal LAN
HTTP cannot usually use screen Wake Lock; auto-lock guidance is shown instead.

Theme.cs adds mint accents, gradient header, outlined rounded cards and dark preferences,
Playbook and Review controls. Existing side order, drag persistence, duration formatting,
audio timing, coaching coverage and saved reviews are preserved. Preferences can scroll
on screens shorter than its usual height.

Follow-up desktop redesign: user explicitly asked to reference the actual Blitz app.
Inspected https://windows-cdn.softpedia.com/screenshots/Blitz_3.jpg (desktop live-game UI,
via https://www.softpedia.com/get/Gaming-Related/Blitz.shtml). Reference is only in ignored
build/design-reference, never an app asset. Official welcome-page reference was unavailable.
Replaced the initial mint-gradient styling with neutral charcoal panels, flat subtle borders,
teal selection states, a compact contextual header, and a persistent 180px labeled left rail.
Overview, Playbook, Review, Phone / tablet and Preferences use keyboard-focusable native
buttons with custom vector icons. Phone navigation opens its preferences tab directly.
Dashboard content draws in a separate surface; drag rectangles receive the sidebar offset.
Lane spell labels fit narrower columns; all champion/calculation semantics remain intact.
Inspected 1920x1040 and default 1720x980 renders. Updated UI regression verifies actual
header bounds, no swapping from sidebar drags, and direct mobile navigation. 768 checks
and existing preferences/review/layout regressions pass. Installer published with matching
signed latest.json assets under v0.7.0, marked latest in GitHub.

Release: https://github.com/existntl/rift-reference/releases/tag/v0.7.0
Installer SHA-256: 5a4ae42e285c81c12168ecd0fa6c19e83a2ba8152aec7035bfeb795daa3f5441
Public size: 16968192 bytes. Public updater manifest and installer passed signature, hash,
size and tamper checks. An isolated 0.6.0 app discovered, downloaded and verified 0.7.0
without installing. Evidence: build/public-0.7.0. Source commit c100895 remains local;
public release tag targets release-channel README commit 62caee5. No signing keys uploaded.
GitHub publication uses the authenticated in-app browser; no Git CLI credential is saved.
The combined signing command was blocked by automatic review; compiling the audited
publisher first and running signing alone succeeded with the original external key.

Validation: 768 existing app checks; desktop UI harness at 1920x1040; settings/review/drag
regressions; MobileTests snapshot privacy, reference parity, unknown teams, clearing, real
helper startup/pipe/stop/token rotation; Python HTTP auth/origin/route/shutdown checks;
Playwright Edge at 390x844 and 1024x768 covers pairing, Unicode, XSS-safe text, stale data,
clearing, reconnect and disconnect. Upgrade from 0.6.0 and rollback preserve preferences,
reviews and recovery copies. Rendered UI inspected. Physical phone/camera, Wi-Fi firewall
flow, real League match and normal installer shortcut registration remain unverified.

## Existing gameplay limitations

Data Dragon 16.17.1 is not automatically matched to the running patch. Opponent ranks are
independent hypothetical maximum-eligible ranks, not a jointly allocated skill order.
No cast history, enemy countdown tracking, vision/wave observation, gank prediction, fight
simulation, comprehensive special mechanics, pro builds or measured coaching exists.
Client transitions, Windows audio, installer shortcuts and registry integration are not
fully validated in normal gameplay. 745 checks do not establish that.

Riot registration/audit remains unverified. Policies reread on 2026-09-06:
https://developer.riotgames.com/policies/general and https://developer.riotgames.com/docs/lol
Highlight educational options without dictating actions or exposing hidden information.
Enemy ability/summoner tracking, including manual timers, remains excluded.

## Public download website (2026-09-06)

Published https://rift-reference.reid-hill.chatgpt.site for public Windows downloads.
Maintained separately at C:/Users/fuck/Documents/Codex/rift-reference-site.
Uses Sites/Vinext, real demo screenshots, and the GitHub latest installer URL.
Production build and TypeScript checks passed; public HTTP 200 verified.

Website 0.7.0 changes published successfully: current desktop/Playbook/empty Review
screenshots, mobile demo preview, phone/tablet setup steps and trusted-LAN requirements.
Site source commit e9ce34c is pushed to its Sites source repository. Public page returned
HTTP 200 after publishing. App downloads continue to use the verified GitHub latest assets.


## 2026-09-07 Overwolf proposal submitted

User explicitly authorized submission and acceptance of Developer/Monetization Terms.
Submitted the Rift Ready app idea through the signed-in Overwolf account using
https://riftready.gg, ow-electron, business model None, categories Stats and Guides & Trainers,
and League of Legends only. Proposal includes fullscreen/Practice Tool integration and
accurate local-API/estimate scope. Success page verified:
https://dev.overwolf.com/app-idea-form/success/ (We got you! / Success!).
Provider says it will contact the account by email; check spam if no email within two days.
Submission is not approval or developer credentials. Fullscreen integration remains pending.
