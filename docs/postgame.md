# Postgame summary

Version 0.8.0 replaces the cooldown dashboard in PreEndOfGame, WaitingForStats and EndOfGame with a results view. A Live Client Data `GameEnd` event also ends the live reference display while final results are pending. An unavailable endpoint shows a waiting message, not estimated final totals. Exiting postgame returns to the ordinary client-phase view; results are not stored as match history.

Displayed when provided by the client:

- Victory/defeat and match duration.
- Kills, deaths, assists and KDA ratio. Zero deaths is shown as deathless.
- Total CS (lane plus neutral minions) and CS/min.
- Damage to champions and damage/min.
- Gold earned and vision score.
- Both team scoreboards and the local player's final items.

Unknown fields stay nullable and render as `—`. A missing neutral-minion or lane-minion total means total CS is unknown. A missing/zero duration disables per-minute calculations. The view does not infer rank changes, performance grades, missed opportunities or why a game was won/lost. Review can still save the user's own reflection.

## Data and privacy

GET `/lol-end-of-game/v1/eog-stats-block` is allowed through the private local helper; writes and other end-of-game routes remain blocked. Parse only during postgame and require its positive gameId to equal the current `/lol-gameflow/v1/session` gameData.gameId. A stale response, invalid result or absent roster cannot populate the scoreboard. No match-history endpoint is queried as a fallback.

The raw endpoint can include summoner IDs, PUUIDs and chat credentials. Only champion IDs, team IDs, local-player flag, roles, selected numeric stats, outcome, duration and item IDs enter the display model. Account identifiers may be compared transiently to identify self; they are not retained in the parsed snapshot. Mobile has an explicit projection containing display strings and no cooldown, account or credential fields. No raw results are logged or saved.

Sources checked 2026-09-06:

- Riot client API and policies: https://developer.riotgames.com/docs/lol
- End-of-game and gameflow schemas: https://github.com/KebsCS/lcu-and-riotclient-api/blob/main/lcu/swagger.json
- Example typed EOG stat names: https://git.vhaudiquet.fr/vhaudiquet/leaguerecorder/src/commit/7aa4bfbf646a82ac1c4f7c6734fd932b496ed646/record-daemon/src/lqp/api_types.rs

Riot registration/audit remains unverified. The unofficial client schema may change; failures must remain honest unavailable states.

## Validation

`scripts/verify-ui.ps1` includes PostgameTests: final-result parsing, CS arithmetic, zero-death/zero-duration handling, missing data, stale game IDs, wrong phases, GameEnd event detection, mobile privacy and renders at 1920x1040, 1720x980 and 1280x950. `scripts/verify-mobile.ps1` generates real serialized fixtures for `tests/mobile-browser.cjs`, which exercises live/draft/postgame transitions, phone/tablet overflow and disconnect behavior. `tests/transport_test.py` checks the exact read-only EOG route. Existing `--test` checks remain required.

Desktop renders were inspected. Testing uses synthetic EOG fixtures, not a captured completed League match. Actual result availability, schema behavior across queues, and final-item accuracy need a live completed-game smoke test before claiming those verified. Version 0.8.0 is published, with these validation limits stated in its release notes.
