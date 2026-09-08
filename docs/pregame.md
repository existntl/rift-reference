# Pregame and loadout preparation (0.8.0)

Champion select now uses a dedicated draft brief on desktop and phone/tablet. It contains selected champion strengths/vulnerabilities, lane opponent kit considerations and conditional team composition plans. Cooldown tables and summoner durations are absent in this phase; live match references remain unchanged. Desktop roster headers still swap sides and persist the preference.

Guidance uses bundled Data Dragon 16.17.1 tips/tags, existing authored kit summaries and a Vayne-specific strengths/vulnerabilities summary. Composition suggestions are qualitative patterns, not predictions. Missing roles, teams or picks must remain unknown. These are not statistical counter rankings, and patch match is unverified.

## Runes / builds

The sidebar opens a native editor. Select a champion, open its Onetricks.gg or Probuilds page, and choose runes/items. Local 0.8.1 replaces dropdown runes and the item spreadsheet with tree/rune/shard icon rows and a searchable, category-filtered item catalog beside named shop sections. Rune controls validate four primary rows, two different secondary rows, distinct trees and three stat shards. Selecting a secondary rune replaces the choice in that row; selecting a third row replaces the oldest selected row. Clicking a selected secondary rune clears it. Changing trees clears incompatible choices. Rune descriptions appear on hover or keyboard focus.

Select a shop section with its circle button, then click catalog items to add them. Item names and gold costs are visible; repeat clicks add copies. Sections can be named, added, removed and reordered. Click a placed item for earlier/later/remove actions. Old saved plans using names or IDs are supported; items are resolved transactionally before replacing the plan. New plans retain the same file format and unambiguous IDs. The final review expands IDs to names.

`src/LoadoutEditor.cs` owns visual controls and their selection models. `src/BuildPlanner.cs` retains persistence, source links and the guarded apply workflow. Icons are bundled from Riot Data Dragon by `scripts/cache-loadout-icons.py`, with no runtime network access. Client reference screenshots were inspected from https://blog.loltheory.gg/how-to-get-more-rune-pages/ and https://esports.gg/news/league-of-legends/vi-guide-for-beginners/. They inform interaction/layout only; Rift Reference keeps its own branding and charcoal/teal styling. Data Dragon documentation: https://developer.riotgames.com/docs/lol#data-dragon. Reference screenshots are not packaged.

Save/load uses user-selected JSON files (`rift-loadout-1`) outside the app's managed state. Plans record a user-entered source/patch note plus bundled data version. Loading a plan never applies it. Files do not contain account data or credentials and are not bundled into installers. Loading revalidates at apply time, rather than trusting IDs from the file.

Two explicit actions create a new rune page (and select it) or a new champion-specific custom item set. Existing pages/sets are never deleted or replaced. Repeated imports can create duplicates. A full rune book must be managed by the user in League. Choose the RR set from the shop's Item Sets dropdown; this app does not buy items or force a build during play.

The private PC-only League transport allows POST only to `/lol-perks/v1/pages` and `/lol-item-sets/v1/item-sets/{summonerId}/sets`. Before writing, it reads the local account ID, current phase and current local champion selection. Wrong champion, unknown profile/slot or non-ChampSelect state aborts. Demo mode cannot write. Imports have no automatic retry; an uncertain result asks the user to check League before retrying. The last phase/pick read and subsequent write cannot be made atomic across the unofficial client API. No mobile route can initiate these operations.

## Sources and unfinished integration

Reviewed 2026-09-06:

- Riot policies and client API scope: https://developer.riotgames.com/docs/lol and https://developer.riotgames.com/policies/general. No hidden player analysis or enemy timer tracking added; registration/audit remains unverified.
- Source pages: https://www.onetricks.gg/champions/builds/Vayne and https://probuilds.net/champions/details/Vayne. The former returned a Vercel security checkpoint to an ordinary automated request. No bypass attempted. A supported public recommendation/export API was not found. Therefore **automatic recommendations from these sites are not implemented**; the UI explicitly says source feeds are not connected. No fabricated win rates, imported plans or provider partnership claims.
- Endpoint schema: https://github.com/KebsCS/lcu-and-riotclient-api/blob/main/lcu/swagger.json (GET/POST item-set collection, POST rune pages). Readable rune example: https://hextechdocs.dev/how-to-set-runes-using-lcu/. Its delete-current-page approach is deliberately not used.
- Item-set block semantics: https://github.com/CommunityDragon/HexDocs/blob/master/lol/misc/itemsets.md.

To finish the requested automatic provider recommendations, obtain a supported feed/export mechanism or provider access, then add a versioned adapter with champion/role/patch/source provenance and stale-data handling. Browser links and this manual editor must not be described as that completed integration.

## Verification

Run `scripts/verify-ui.ps1` (includes LoadoutTests), `scripts/verify-mobile.ps1`, `tests/transport_test.py`, and `tests/mobile-browser.cjs`. Loadout tests mock the client: valid/invalid rune IDs, secondary rows, item validation, custom names/order, phase/pick guards and append-only writes. Transport tests exercise the real pipe loop against a fake HTTP opener, including 204 responses and rejected arbitrary writes.

Native lobby/editor renders were inspected; mobile browser tests cover draft/live transitions. This does not verify real-client rune-page selection, page-capacity errors, item-set visibility inside a match, or provider feeds. A real champion-select smoke test is still required. Version 0.8.0 is published; the normal user installation was not changed during validation.

The 0.8.1 visual editor passes save/load compatibility, invalid-plan preservation, secondary-row/tree selection, actual rune/catalog button clicks, catalog search, duplicate items, item and section ordering/removal, and 1080p-fit renders. Existing 768 app checks and four transport tests pass. App updater and website were published as 0.8.1 on 2026-09-06, with public signature/download and older-client updater verification. Normal user installation was not modified.
