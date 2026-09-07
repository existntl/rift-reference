# Native game overlay (local 0.10.0)

## Panel gear controls (shared menu removed)

The layout preview no longer has a shared toolbar. Each editable panel's gear contains its size/position reset, transparency, full panel reset, next preview screen, and Use/Cancel layout actions. Stats also has its metric selector. Ctrl+Enter accepts, Esc cancels, and F6 cycles preview screens, including when every panel is hidden. Re-enable closed panels using Game overlay settings visibility switches; then reopen the editor. Save overlay settings after accepting a layout. Fixed objective cards have no controls. This supersedes earlier Show panel and Reset defaults toolbar instructions below.

## Fixed Baron and Elder cards

The buff UI now uses fixed top-center cards inspired by the supplied reference, with original code-drawn emblems. Purple Baron and mint Elder cards show only objective name, m:ss duration and a progress bar. No resize, move or close controls; old buff layout dimensions/position are ignored. Existing enable preference and opacity remain. Build and stats keep their editing controls. Only active kill-derived windows appear during gameplay, regardless of capturing team. Editor cards are sample data. Baron lasts 180 seconds; Elder lasts 150 seconds. These are estimated objective windows and can outlast individual holders who die. Elemental drakes do not start a duration card.

Duration sources checked 2026-09-06: [Riot 9.23](https://www.leagueoflegends.com/en-us/news/game-updates/patch-9-23-notes/) sets Baron to 180 seconds; the later [Riot 9.24b](https://www.leagueoflegends.com/en-au/news/game-updates/patch-9-24b-notes/) changes Elder to 150 seconds. This supersedes earlier documentation describing both as 180 seconds and a draggable buff panel.

## Compact size and transparency

Build, buffs and stats default to 50% of their previous width and height. Saved custom dimensions shrink once on migration, with a 120x40 minimum for resizing. Use the gear's **Transparency…** slider to choose 20–100% opacity for that panel; lower values are more transparent. The preview updates immediately, **Cancel** restores the old value, and **Use opacity** accepts it into the layout draft. Finish with **Use layout** and **Save overlay settings**. Text, icons and backgrounds share the live opacity; editor controls stay opaque. Default opacity is 96%. Reset size returns to compact automatic sizing; Reset defaults also restores opacity.

## Resizing panels

Open **Move / resize panels…**, unlock the desired panel, and drag an edge or corner. Drag inside to move. Relock to prevent both actions. Choose **Use layout**, then **Save overlay settings** to persist independent build, buff and stats dimensions. Gear > Reset size restores automatic sizing; Reset defaults restores every panel's default size and position. Resizing has minimum dimensions and stays inside the selected screen. Saved sizes fit smaller game windows; text/icons scale proportionally without stretching. Live overlays remain click-through; editing happens in the layout editor.

## Optional live stats

Game overlay > Stats panel configures visibility and metrics; Show stats in the layout
editor enables it as well. Its gear opens Choose displayed stats. CS/min and kill
participation are selected by default; total CS, K/D/A and vision score are optional.
The panel is off by default, independent of other widgets, and shares their explicit
editor-only drag/lock/close workflow. Position and metrics persist in overlay.json.

StatsPanel.cs reads scores.creepScore, kills, deaths, assists and wardScore from the active
player's roster entry. CS/min is exposed CS divided by elapsed game minutes. KP is
(kills + assists) / sum of five uniquely identified allies' kills. Missing inputs, zero
game time, zero team kills, incomplete roster or a numerator exceeding team kills display
unknown. The existing live-only and four-second stale gates apply. No gold/min or rank
benchmarks are fabricated. Data contract checked 2026-09-06:
https://developer.riotgames.com/docs/lol#game-client-api_live-client-data-api

## Move, lock and reset panels

Updated controls: each layout-editor panel has its own square lock, gear and close buttons
at the top right. The shared toggle is removed. Gear resets only that panel's position.
Close hides only that panel and updates its independent visibility preference on save;
Show build / Show buffs restores it. Use layout and then Save overlay settings to commit.
Gameplay overlays retain click-through behavior; these buttons belong to the layout editor.

Gear research: https://porofessor.gg/support and https://porofessor.gg/faq were reviewed
2026-09-06 alongside the official download page and the supplied image. Public official
documentation did not establish the exact menu behind the pictured panel gear. A per-panel
position reset is useful in our layout editor and was added as our own feature, not asserted
to duplicate Porofessor's undocumented menu.

Game overlay > Move / lock panels opens a full-screen sample layout. Choose Unlock panels,
drag the build or buff panel independently, then Lock panels or Use layout & lock. Save the
parent overlay settings to persist; Cancel discards editor changes. Screen cycles through
connected monitors. Reset defaults restores the build immediately left of the default-size
bottom-right minimap and the buffs at top-center. A larger/custom minimap may need manual
adjustment. The preview uses sample data, so timers can be positioned outside an active game.

Gameplay panels remain noninteractive/click-through at all times. The lock switch controls
the layout editor; it does not intercept game clicks. Positions are stored independently as
normalized offsets within available game-window space and clamped on resize. Custom legacy
X/Y build positions are migrated; untouched legacy defaults adopt the new requested defaults.
overlay.json and its existing recovery/installer preservation continue to cover layout data.

Game overlay in the desktop navigation opens independent enable, lane gold, objective and
purchase options. The master switch defaults off. Windows are topmost, non-activating,
layered and click-through. They display only over the foreground `League of Legends`
process, with a fresh live CLASSIC / Summoner's Rift snapshot. Borderless/windowed is
the supported target; exclusive fullscreen is not implemented or claimed. No injection,
memory reading, input interception, clicks or other game inputs are used.

Hold Tab (or the configured scoreboard key) to show team totals above the scoreboard and
five narrow directional gold markers along its center, in Top/Jungle/Mid/Bot/Support order.
Blue indicates the left side leads; red indicates the right. Allies-left is configurable
independently of the desktop dashboard. The label remains item-value estimate. Team totals
require five uniquely identified players and complete item values; missing values never
produce partial sums. Keep the game scoreboard in the displayed role order.
This detects the held key, not the game's actual scoreboard state; toggle scoreboard mode
is not supported. Each row compares the unique ally and enemy in the same exposed role.
Ambiguous/missing roles or inventories/prices stay unavailable. It sums bundled item
total values and explicitly excludes unspent gold. It is not total earned/spent gold;
consumables, free/transformed items and patch changes limit its interpretation.

Baron and Elder use event timestamps plus a 180-second baseline to show estimated team
buff windows. Elemental dragons never start temporary timers. Killer identity resolves
team only when unambiguous; unknown owners remain unknown. These are not per-champion
buff observations, and deaths can end individual buffs earlier. Rebuild from each current
event history, expire at the game-clock boundary and clear outside live games. No wall
clock interpolation: pause behavior follows the sampled game clock.

Purchase progress uses the active player's currentGold and inventory counts. Choose a
champion and target item or load a saved `rift-loadout-1` plan, then select one of its items.
Wrong champions show target selection guidance. Recursive recipe credit consumes each
owned copy once, preserves combine cost and never treats unrelated inventory as credit.
Show missing gold for the target and up to three distinct immediate components as
alternatives, not simultaneous affordability. Prices/recipes come from the bundled patch;
discounts, special champion mechanics, inventory slots and shop restrictions are not
modeled. 'Enough gold' is not a guarantee that the shop permits the purchase.

Automatic Probuilds recommendations remain unfinished. The source page was reviewed on
2026-09-06 but no documented supported feed was identified. Existing browser links and
saved manual plans are available. Do not label these as pulled Probuilds recommendations.

Overlay uses one-second full snapshots when enabled, reusing the existing local transport;
four-second stale timeout hides both windows. Foreground/held-key checks run at 100 ms
without network requests. Existing dashboard refresh resumes when disabled. Settings are
stored atomically in overlay.json, with overlay.json.bak recovery, preserved by the installer
and excluded from packages/source. No new fields are added to the mobile projection.

## Reference redesign and research (2026-09-06)

The user supplied two Blitz/Porofessor screenshots. They are visual references only and
are not shipped as assets. OverlayVisuals.cs implements a navy/gold HUD with blue/red
scoreboard markers, a horizontal icon recipe and target progress badge, and a separate
compact buff panel with remaining-duration bars. Icons reuse the bundled Riot assets.
The main desktop palette is unchanged. Component badges are affordability alternatives;
Ready means enough gold under the bundled recipe, not guaranteed shop eligibility.

Game overlay > Align scoreboard controls horizontal offset, top, scale, first-row offset,
row spacing and left/right team order. Values scale from 1080p and clamp to the game
viewport, including monitors with negative coordinates. Build/buff panels remain together
within the game bounds. Old overlay.json files get alignment defaults without losing the
existing enable switches or target. The preview is a schematic, not live League validation.

Primary-source findings:
- https://porofessor.gg/download offers both Overwolf and standalone downloads and describes
  in-game overlays. It does not disclose the standalone implementation.
- https://dev.overwolf.com/ow-native/live-game-data-gep/supported-games/league-of-legends/
  documents live_client_data, gold, events and panel_location. The latter reports scoreboard
  coordinates. This demonstrates a supported platform mechanism for alignment; it does
  not establish the private implementation of either screenshot.
- https://dev.overwolf.com/ow-electron/reference/Overwolf-electron-APIs/overlay/interfaces/IOverwolfOverlayApi/
  documents tracked games, overlay injection, window positioning and input options.
- https://dev.overwolf.com/ow-native/reference/manifest/manifest-json/ documents input pass-through.
- https://blitz.gg/ advertises overlays and match analysis but no public rendering or
  gold-formula specification was found. Do not invent claims about its proprietary code.

Rift Ready retains its lightweight native window implementation. Overwolf is not a
drop-in API for this standalone C# executable: adoption would require a separate runtime,
integration and distribution workflow. We did not install it or implement custom injection.
Riot's documented local API supplies inventory/current-player gold/events but not scoreboard
panel geometry, so this implementation uses adjustable alignment. Exact automatic alignment,
automatic row reordering detection and exclusive-fullscreen support are not implemented.
No new jungle-camp or enemy ability tracking was added from the additional reference imagery.

## Sources and review boundaries

Checked 2026-09-06:
- https://developer.riotgames.com/policies/general (registration/audit and game integrity)
- https://developer.riotgames.com/docs/lol (integrity and Live Client Data API)
- https://static.developer.riotgames.com/docs/lol/liveclientdata_events.json (kill event schema)
- https://www.leagueoflegends.com/en-us/news/game-updates/patch-9-23-notes/
  (both buff baselines standardized to 180 seconds)
- https://www.leagueoflegends.com/en-us/news/game-updates/patch-26-1-notes/
  (current-season objective changes; no duration change identified)
- https://probuilds.net/ (no documented feed established)

Riot requires registration and auditing of products and new features. Rift Ready's audit
status is unverified; another app's overlay is not proof of approval for this app. No enemy
ability/summoner timers are introduced. This is a local implementation, not a published
release or a claim of Riot approval. Run OverlayTests and the existing --test/UI checks;
actual League focus, hold/release, click-through, multi-monitor/DPI and game-event checks
remain necessary. Render overlay-preview.png at 1920x1080 and overlay-settings.png.
