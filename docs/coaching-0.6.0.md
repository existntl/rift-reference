# Decision practice — 0.6.0

The dashboard adds a lane plan, enemy kit references and one training focus. The previous
matchup cards remain selectable in Preferences. Full lessons are available through Playbook;
Review saves self-assessment locally after a game, with no automatic scoring or cast analysis.

## Coverage and sources

Champion tools were checked against the bundled Riot Data Dragon 16.17.1 descriptions.
The Vayne, Lulu, Caitlyn and Nautilus official champion pages were additionally consulted
on 2026-09-06. The rules summarize mechanics; tactical options are authored interpretations,
not verified Challenger advice or predicted matchup outcomes.

- https://www.leagueoflegends.com/en-us/champions/vayne/
- https://www.leagueoflegends.com/en-us/champions/lulu/
- https://www.leagueoflegends.com/en-us/champions/caitlyn/
- https://www.leagueoflegends.com/en-us/champions/nautilus/
- https://ddragon.leagueoflegends.com/cdn/16.17.1/data/en_US/champion.json

Only Vayne + Lulu versus Caitlyn + Nautilus has a specifically authored four-champion plan.
Other plans combine ADC pattern, partner protection and both opponents' tool summaries.
Threat references cover 28 champions; other champions have an explicit unreviewed state.
Summaries are not exhaustive. R mentions describe capabilities, not learned rank or readiness.

General lessons cover wave control, recalls, teamfight access, advantage conversion and
practice habits. No observed wave, location, vision, casts or live opportunity is inferred.
Static kit references are separate from the cooldown-duration table. Patch match remains
unverified, and non-CLASSIC lane plans use a clearly labeled unsupported state.

## Policy boundary

Reread https://developer.riotgames.com/docs/lol and
https://developer.riotgames.com/policies/general on 2026-09-06. The design highlights
conditional choices and known champion mechanics. No enemy countdowns, hidden information,
automatic actions or dictated rotations were introduced. Riot registration/audit is still
unverified; implementation and publication do not establish Riot approval.

## User data

Preferences migration preserves existing layout, audio and timing choices. Reviews are
stored in reviews.json beside preferences, with reviews.json.bak retained on subsequent
atomic writes. Installer upgrades preserve both review files. Corrupt notes are not overwritten.
The package excludes settings, notes, generated screenshots and test data.

## Validation

768 application checks pass, including partner/opponent changes, incomplete and ambiguous
roles, unsupported mode, focus migration, Unicode notes, edit preservation and corrupt-file
rejection. UI tests exercise real WinForms controls for preferences save/reopen, review
save/reopen/edit, old/new card modes and playbook navigation. Layout screenshots were
inspected at 1920x1040 (1080p monitor working area). Installer regression includes updates
started from the installed folder and rollback with preference/review preservation.

Live game integration, normal Windows shortcut creation and live audio playback have not
been newly verified. Version 0.6.0 was published on 2026-09-06 to the latest GitHub update
channel. The public installer and signed manifest passed verification, including discovery
and verified download through the 0.5.1 updater. Normal user installation was not performed.
