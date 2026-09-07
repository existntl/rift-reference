# Rift Ready — new session handoff

Local update: native source 0.10.0 implements an opt-in borderless/windowed game overlay.
See the newest handoff entry and docs/overlay.md. Public app is still 0.9.3; website version
11 and paused domain decision are unchanged. Automatic Probuilds ingestion and real-game
overlay smoke validation remain unfinished. Do not resume publication automatically.

Prepared 2026-09-06. Read this first, then AGENTS.md and handoff.md in this checkout.
Historical entries in handoff.md describe earlier states; this summary identifies the current state.

## Where the work lives

- Authoritative native app: C:/Users/fuck/Documents/Codex/rift-reference
- Authoritative website: C:/Users/fuck/Documents/Codex/rift-reference-site
- The saved Codex project “League App” still opens the old workspace at
  C:/Users/fuck/Documents/Codex/2026-09-05/i-am-2. Its work/src and outputs source are backups.
  Always change to the authoritative checkouts before editing. Do not maintain backup copies.
- Native branch: feature/decision-practice. Website branch: main. Native maintained source
  is local; public GitHub release tags target the separate public README branch.

## Current product and publication

Rift Ready is a lightweight native Windows League of Legends companion. Desktop is the
main app; phone/tablet support is an opt-in browser companion over a trusted local network.
Minimum advertised setup: Windows 10 or later, 64-bit Intel/AMD, .NET Framework 4.8 or
later; local League client for detection. A second monitor is optional, not a minimum.

- Published latest app: 0.9.3, https://github.com/existntl/rift-reference/releases/tag/v0.9.3
- Public installer/update filenames remain RiftReference-Setup.exe and latest.json.
- Installer: 21,521,920 bytes; SHA256
  7a4cb9e131a52f8000a0a6a101fc1487a26edeac0050a6716a6c9ac949915711.
- Public signature/hash/size/tamper checks and older-client updater download verification pass.
  No normal user installation was changed during validation.
- Website: https://rift-reference.reid-hill.chatgpt.site — public, latest Sites version 11.
- Website published source: 5c5837e989690e5713cb159e51a2f31d0cb56363.
- Project: appgprj_6a9d95f2d8c481919181cda785d25520.
- Saved version: appgprj_6a9d95f2d8c481919181cda785d25520~appgver_0a2c575ebcc08191adbf5994d2599695.
- Deployment: appgdep_6a9dc75a5ab481918df6511c58ea1d73, succeeded. Website build/types
  and public HTTP check passed. No website browser UI QA was requested.

## Accepted design and behavior

- Name: Rift Ready. Tagline: Analyze · Plan · Climb.
- Latest logo: white lower-left/inner diagonal, teal upper bowl/right leg from the second
  supplied board. assets/branding/rift-ready.png; provenance in docs/branding.md.
- Desktop AND website palette: background #0f1014, cards #191b21, borders #2b2e36,
  teal #42cdc6, text #eef0f4, muted #949aa7. Do not restore green-tinted UI surfaces.
- Logo is embedded in desktop, Windows/installer icons and mobile HTML. Website uses
  rift-ready-v2.png / rift-ready-v2.ico. Mobile CSP allows data images; browser QA verifies decoding.
- Pregame shows champion strengths/weaknesses, kit-based matchup considerations and comp
  plans instead of cooldowns. Rune/item editor uses client-inspired tree/icon layouts and
  named custom shop sections, saved plans and guarded explicit client imports.
- In-game cooldowns are reference durations, never observed casts or countdowns.
- Postgame shows final stats and desktop champion portraits, not cooldown references.
- Enemies left/allies right by default; draggable lane headers persist swaps.
- First jungle reminder 120 seconds; repeats every 90 seconds through 480. Do not change it.
- Mobile sharing is off each launch; session secret, QR/link pairing, stop control, sanitized
  data only. No credentials, account details, review notes or client writes exposed.
- Landscape works through responsive layout. Side-by-side teams above 700 CSS pixels;
  narrower widths stack. Tablet tested at 1024x768; physical phone rotation not verified.

## Domain decision — explicitly paused

User wants to hold off on buying a domain and is leaning toward riftready.gg.
Do not buy, connect, migrate hosting or modify DNS unless asked to resume.
Prices/availability below were checked 2026-09-06 and must be rechecked before purchase:

- Porkbun riftready.gg available: US$51.80 registration and annual renewal.
- GoDaddy exact .gg available: C$138.41/year; separate renewal not confirmed.
- Porkbun riftready.ca available: US$8.92 sale, renewal US$9.18 sale / US$11.38 regular.
  Canadian presence requirements apply.
- riftready.com aftermarket listing: US$3,795 plus transfer fee, not standard registration.
- riftready.app available: US$8.75 first year / US$14.93 renewal; offered as budget alternative.
- Custom-domain support/connection for this Sites host has NOT been verified. Do not promise
  that changing DNS alone can connect it. Check supported hosting/domain workflow first.

## Remaining limitations — do not overclaim

- Automatic Probuilds/Onetricks feeds are NOT connected. Current tools are browser source
  links and manual selections. No bypassing provider security checkpoints.
- Real League-client rune/item import smoke test and completed-game stats validation pending.
- Physical phone/tablet Wi-Fi, camera pairing and rotation testing pending; browser checks
  are not proof of these device flows.
- Riot registration/audit unverified. No Riot-approved claims or enemy cooldown tracking.

## Development and release safeguards

Follow authoritative AGENTS.md, docs/build.md, docs/mobile.md and handoff.md. No subagents
unless user explicitly asks. Build with scripts/build.ps1 -Installer; use Installer.cs.
Relevant checks: --test (768), verify-ui.ps1, verify-mobile.ps1, tests/mobile-browser.cjs,
verify-branding.ps1 and installed-folder upgrade/rollback regression when appropriate.
Keep legacy executable/product/registry/install/updater identities for compatibility.
Preserve preferences, plans, reviews and recovery copies. Keep generated packages and
runtime/cache/test evidence out of source commits.

Release signing key stays external in the original workspace's work/publisher directory.
Never read it into output, commit, copy into packages, upload or rotate it. Keep the embedded
public key compatible. Changed published binaries require a NEW version and matching signed
manifest; never overwrite existing releases. Verify public download/signature after publishing.
Follow Sites skills for website work and reuse its existing project/access/URL.

## Start of the next chat

No new feature request is pending. The user asked to prepare this handoff and start a fresh
session. Read the authoritative instructions, briefly confirm the current state, then wait
for the user's next request. Do not resume the paused domain purchase or speculative features.
