# Diamond+ recommendations

Local implementation; **not live or published**. User selected NA1, EUW1 and KR and generated
a development key in the Riot portal, expiring September 7, 2026 at 23:25 Pacific. Public app
remains 0.10.0. No product registration/audit approval is claimed. The key is not stored in source.

## Behavior and sampling

The separate Python collector uses League-v4 ranked listings and Match-v5 histories and
timelines. Only qualifying roster participants count, not all their teammates. Queue420,
map11, CLASSIC, current Data Dragon major/minor patch, trailing seven days, at least ten
minutes, no early surrender and known role are required. Rank is Diamond+ **at collection**;
Match-v5 does not provide historical rank. Match/player pairs are deduplicated.

Roster: first configurable pages of each Diamond division plus Master/Grandmaster/Challenger.
Accounts are randomly sampled from this roster. This is bounded, not exhaustive or population
representative. Default500-call budget caps seeds at three per region to allow all three
regions to be visited; `--budget`, `--pages` and `--seeds` control coverage. Run only one
collector per key. 401/403 stop; 429 honors Retry-After; rate-limit headers can extend pacing;
server retries and total requests are bounded. Failed runs keep committed private samples
without replacing the public feed. Repeated successful runs grow the sample.

Dashboard builds group a complete nine-choice rune page and the first three distinct core
item purchases **from the same match participants**. Core items are completed timeline
purchases, excluding boots/consumables/trinkets, upgrade components and items below2000
gold. Minimum30 games and10 distinct players apply to each connected build. The feed retains
the20 most common qualifying builds per champion/role. The dashboard can sort these by
frequency or observed win rate; its win-rate tab is not a search across every rare build.
Win rates have completion bias: only games reaching three qualifying core purchases count.
They describe these samples, not causal item strength or a prediction of the user's outcome.

Each build can include the most common qualifying spell pair, starting purchases, component
purchase sequence, match-end inventory and skill-upgrade sequence **within that build's
cohort**. Each detail independently needs30 games and10 players, and shows its own sample
count. These detail modes need not all have occurred together in a single match; the common
link is the selected rune/core combination. Missing or insufficient observations stay empty.

- Starting items mean purchases before90 seconds, not a snapshot of inventory at the fountain.
  Duplicate purchases are preserved; sequences exceeding20 items are excluded.
- Purchase order is the first20 eligible purchasable Summoner's Rift purchases after undo
  correction, including components and repeat purchases. Sales do not become purchases.
  Ambiguous undo IDs exclude core and purchase-derived observations for that participant.
- Match-end items are observed inventory, excluding empty slots, trinkets and non-SR items.
  They are sorted for comparison and never interpreted as purchase order or a guaranteed
  six-item target. Independently observed inventory can survive a malformed purchase timeline.
- Skill order uses only observed normal Q/W/E/R upgrade events, up to18 points. The grid
  numbers are **upgrade number, not champion level**; unobserved upgrades remain blank.
- Spells are an unordered observed pair. Situational recommendations, inferred playstyle
  labels and verified professional-player builds are not implemented.

Runes / builds opens the three-column visual dashboard directly. Champion and role
selectors accompany the fixed Diamond+ / NA-EUW-KR / patch filters. Selecting a build updates
the rune visualization, skill grid and item/spell sections together. Save and preview/apply
controls live on this screen; the original manual editor is no longer in navigation. Applying uses
explicit review and champion-select checks. Game overlay > Choose build opens the same
selector and selects its first core item as the target; save overlay settings to persist it.
Saved plans can also be loaded manually. Existing component-cost calculations apply;
targets do not advance automatically.
The app rejects wrong patches/filters, feeds older than24 hours and inadequate/invalid choices.
Unavailable data never becomes synthetic recommendations or overwrites manual plans.

## Feed format and migration

### Build-path reference

The public [OneTricks Vayne build page](https://www.onetricks.gg/champions/builds/Vayne)
was inspected on September7,2026. It exposes Paths / Options, first-item filtering,
starting sets, popular items and rune/skill sections. Brave is not available through
the connected browser tools, so the user's exact active page was not inspected.
Our path browser uses our qualifying Diamond+ bundles, not OneTricks' Masters+ expert
roster or its statistics. Option percentages describe only the listed qualifying
builds; they are not overall champion pick rates. A path selection retains its paired
rune page. Unavailable matchup and player data are not inferred.

The anonymous feed is now `rift-diamond-2`. Existing top-level filters, timestamps and
champion/role rows remain. Rows retain the independent `runes` and `items` arrays (top five
each) for compatibility; the dashboard uses only connected `builds` entries:

```text
builds[] = {
  value: { primary, secondary, perks: [9 IDs], coreItems: [3 IDs] },
  games, wins, players,
  details: {
    summonerSpells: choice | null,
    startingItems: choice | null,
    purchaseOrder: choice | null,
    finalItems: choice | null,
    skillOrder: choice | null
  },
  situationalItems: []
}
choice = { value: [integer IDs], games, wins, players }
```

`players` is an anonymous distinct-player count. No player identifiers, match IDs or API keys
are exported. Each detail's counts are bounded by its parent cohort. Item and rune identifiers
are validated against the app's current catalog before selection can replace the manual plan.

Opening an older private SQLite database adds a nullable `details` column without deleting
samples. Legacy rune/core observations still contribute to aggregates, but unknown details
are not filled synthetically. Collection revisits eligible existing records whose details are
NULL, then enriches the same match/player record. A processed details object, even with null
optional fields, prevents repeated refetches solely because those observations are missing.
Old v1 feeds cannot produce connected dashboard builds and require collector re-export.

## Private setup when a key is available

On Windows, `scripts/start-private-recommendations.ps1` provides a masked paste field and
starts one bounded private collector. The key is passed only in the child environment and
is cleared from the field after launch; it is not saved to disk, command arguments or logs.
The window displays completion and permits stopping its own worker. This does not start a
public service or connect the released app. Close other collectors before starting a run.

Sign into [Riot Developer Portal](https://developer.riotgames.com/) and configure the collector
process environment `RIOT_API_KEY` privately, using a secret manager/local environment tool.
Never paste it in chat or put it in source, command arguments, native app settings or packages.
Development keys expire after24 hours and are for private development.

From the repository:

```powershell
./cache/runtime/python.exe services/recommendations/pipeline.py --data cache/data
./cache/runtime/python.exe services/recommendations/serve.py
```

Collection may take several minutes. The second command serves only the feed on loopback;
it does not expose the SQLite database or arbitrary files. Set the **app process** environment
`RIFT_RECOMMENDATIONS_URL` to `http://127.0.0.1:8769/recommendations.json` and launch the
local build. This URL contains no Riot key. Use `--export-only` on the first command to
export already committed samples after an interrupted run. Empty database means empty feed.

## Public activation still required

Register/update the product and feature with Riot and obtain a production key before public
service. Host the collector privately with one worker, secret storage and scheduled refresh.
Serve **only recommendations.json** over HTTPS in a separate public document root/object.
Never serve the output directory; private.sqlite contains pseudonymous sample records.
Raw API responses are not stored. Older-than-seven-day records are pruned after successful
collection; failed runs may retain them until the next successful run. Restrict private store
access and apply retention to backups.

Choose and verify hosting, configure the released app's endpoint (currently developer
environment override only), collect enough samples, and test actual Riot responses before
publishing. No hosted endpoint, scheduler, paid service or production access was created.
The public website/download have not changed.

## Verification and research

Run `scripts/verify-recommendations.ps1` for collector/privacy tests, native validation,
saved-plan integration,1920x1080 UI renders and existing application checks. Fixture choices
are synthetic test inputs and are not bundled or served as recommendations.

Checked2026-09-06: [Riot keys and limits](https://developer.riotgames.com/docs/portal),
[general policy](https://developer.riotgames.com/policies/general),
[API reference](https://developer.riotgames.com/apis),
[League docs](https://developer.riotgames.com/docs/lol),
[known timeline undo issue](https://github.com/RiotGames/developer-relations/issues/1174).
This is our aggregation implementation; Blitz's current proprietary ranking formula is unknown.
