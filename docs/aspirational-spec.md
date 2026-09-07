# League companion — agreed build specification

## Objective
A lightweight Windows desktop companion that trains recognition of punish windows and improves game-state decisions. Primary user: Emerald Vayne bot specialist with approximately four million mastery points. Support every champion and role, unfamiliar champions, and future sharing.

## Environment
Windows 10 Pro 22H2, Ryzen 5 3600, NVIDIA GTX 1070 Ti. Two landscape 1920x1080 displays; secondary display to the right. RAM not verified because CIM access was denied. Default region NA and language English. Automatically follow the currently logged-in League account.

## Interaction and presentation
Automatic champion-select, active-game and postgame transitions. No interaction required during play. Large readable text, minimal animation, no essential hover content. All ten champions have visible Q/W/E/R and summoner-spell reference rows. Give the active player and lane matchup greater prominence without hiding other champions. Silent by default. Configure coaching topics, density and optional alerts before games. Save preferences locally. Provide demo mode and disconnected/stale-data states.

## Pregame
Show draft strengths and weaknesses, lane outlook, early versus late strengths, and conditional teamfight advantages. Suggest multiple complementary picks for available roles with reasons: engage, peel, frontline, damage balance and champion fit. Provide lane plan, team win conditions, rune suggestions and item build alternatives. Never present uncalibrated scores as win probabilities. Incomplete drafts must remain visibly incomplete.

## In-game reference
Show one clearly labelled cooldown estimate per spell where justified, accounting for supported known item, level and rune effects. Opponents ability ranks, complete runes, stacks and temporary effects may be unknown: explicitly label assumptions and avoid false precision. Champion level is not ability rank. Distinguish basic ability, ultimate and summoner modifiers, special resets and charges. Unsupported mechanics need an unavailable or approximate state. No cast tracking, countdowns or claims that enemy spells are currently unavailable.

Show concise conditional punish guidance: what to bait, what to respect, and what opportunity follows a key spell being used. Highlight relevant level and item spikes. Advice must work for unfamiliar champions, with concise explanations rather than beginner-only or Vayne-only assumptions.

## Strategy and jungle
Default coaching priorities: punish opportunities, immediate threats and the next strategic priority. Offer preferences for wave management, positioning, purchases, objective setup, side lanes versus grouping and target selection. Update only when supported inputs materially change. Use conditional statements where vision, location or readiness is unknown.

Jungle panel: sourced common opening routes, clear-time ranges, gank windows and plausible route alternatives, with assumptions and patch/mode applicability. Never claim these reveal actual enemy location or observed route.

Team comparison: burst, sustained damage, durability, engage, peel, crowd control and conditions that favour each composition. No unsupported live winner prediction; potential damage alone cannot establish fight outcome.

## Builds and client integration
Recommend context-dependent purchases, defensive alternatives and runes, explaining tradeoffs. Optional user-enabled rune application and item-set export before games; keep these off by default and separate from reading game data. Validate available client endpoints and compatibility before promising operation. No automatic purchases or game inputs. Pro-derived builds require attributable, maintainable sources; do not label heuristic recommendations as pro builds.

## Postgame
Short recap based on available match evidence, with matchup objectives and reflection prompts. Do not claim to detect missed punish opportunities without cast/replay evidence. Replay-based coaching is a separate extension.

## Data and architecture direction
Local rules-and-data engine by default, without a required subscription, cloud AI or account. Separate live-data adapter, patch-versioned reference data, mechanics calculations, coaching rules and UI. Prefer a lightweight native Windows implementation; confirm installed SDK/build tools before choosing packaging. Official local APIs for supported data; static data requires validation and special-mechanic coverage. Review Riot requirements for the exact adaptive recommendations and distribution; API availability is not blanket product approval.

Do not send match data to cloud services by default. Gracefully handle unavailable League, unsupported modes, missing data, client updates, account changes and patch mismatches. Never substitute invented data during a real match.

## Distribution and performance
Provide a Windows installer and versioned update mechanism, applying updates outside matches. Public signing and hosting require an actual publisher/distribution setup; unsigned development builds must be identified honestly. Keep polling modest and UI updates event-driven where possible. Measure CPU and memory with League running before making resource guarantees. Performance profiles only if measurements justify them.

## Acceptance criteria
- Demo works without League and clearly identifies simulated data.
- Correctly follows current account, champion select, game, postgame and next game.
- All ten champion and summoner references fit readably on a 1080p secondary display.
- No clicks or hovers required during gameplay.
- Known modifiers produce validated results; uncertain inputs remain labelled.
- No countdowns, enemy readiness assertions or invented position awareness.
- Matchup, draft and strategy advice explains conditions and source limitations.
- Optional client writes remain disabled until enabled by the user.
- Failure/disconnection does not leave stale values presented as current.
- Validate calculations with meaningful mechanic fixtures and test live transitions in practice/custom games with the user before ranked use.
- Installer and update flow are tested; report actual measured performance and any coverage gaps.

## Remaining engineering validation
Installed SDK/toolchain, runtime memory, live API schemas, champion-select access, source coverage, special spell mechanics, update hosting and signing. These are implementation investigations, not unanswered user preferences.
