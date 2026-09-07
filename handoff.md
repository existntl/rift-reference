# Rift Reference handoff

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

## Parallel work preference (2026-09-06)

User explicitly approved a lead agent coordinating specialist agents for substantial tasks. Use independent assignments, shared project context, clear file ownership and coordinated database changes. Lead integrates and reviews all contributions and runs appropriate combined checks before reporting completion. Available concurrency is currently four including the lead; do not spawn agents just to fill slots or invent work. This supersedes the earlier no-subagents preference. No application/release changes are implied by this workflow update.

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

