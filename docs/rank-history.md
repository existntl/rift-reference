# Ranked profile and progression

The idle home screen displays the local account's Ranked Solo tier badge and LP. Iron through Diamond have a 100 LP division progress bar; apex tiers show uncapped LP.

Rank changes are recorded locally in rank-history.json next to preferences.json. Account keys are SHA256 hashes; raw identifiers and match history are not stored there. At most 500 snapshots per account and ten accounts are retained. Atomic writes preserve a .bak recovery copy; invalid files remain untouched. Both files survive upgrades and rollback and are excluded from source and packages.

The graph shows the latest 30 observed snapshots in the current tracking window. It cannot reconstruct earlier LP history. Queue seasonId is used when available; otherwise the window is the calendar year, not an inferred Riot season. Divisions use continuous 100 LP steps, with all apex tiers sharing the apex LP scale. Different windows are not connected.

Bootstrap the 11 emblems with cache/runtime/python.exe scripts/cache-rank-badges.py. Riot client assets come from the pinned CommunityDragon mirror: https://raw.communitydragon.org/16.17/plugins/rcp-fe-lol-shared-components/global/default/ . They are bundled in data/rank-badges; no runtime asset requests. Missing assets show a neutral fallback.

scripts/verify-home.ps1 covers parsing, persistence and preview renders. Live client integration remains unverified. Synthetic history exists only in the render fixture.
