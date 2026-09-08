# Draft: Rift Ready fullscreen overlay integration

Prepared for user review; not submitted. This is a proposal for Overwolf developer access,
not an assertion that Overwolf or Riot has approved Rift Ready.

## Public application

Rift Ready is a publicly distributed Windows League of Legends companion with a native
second-monitor dashboard. Public product page: https://rift-reference.reid-hill.chatgpt.site/
The requested addition is a supported fullscreen overlay renderer while retaining the
existing native application and its local data processing.

## Overlay features

- Scoreboard inventory-value comparisons using exposed item inventories and bundled item
  prices. These exclude unspent gold and are labeled estimates. Unknown comparisons remain
  unavailable; the feature does not infer hidden gold or opponent activity.
- Current-player purchase progress toward a user-selected item or saved build, based on
  exposed inventory/current gold. Purchases are never automated.
- Baron and Elder duration cards based on exposed objective events, labeled estimates.
- Optional current-player CS, K/D/A, vision and team kill-participation metrics where available.

The overlay is noninteractive during play, hides when League loses focus or data is stale,
and uses separately editable positions/sizes. No enemy cast tracking, input automation,
memory reading or anti-cheat exemption is requested. Practice Tool support is included.

## Proposed technical integration

Use an approved Overwolf Electron overlay component alongside the C# WinForms application.
The native app continues to read the documented local League APIs and computes display
values. The overlay component receives only a bounded, versioned local display snapshot;
authentication tokens, API keys, account identifiers and raw API responses remain excluded.

Implementation stages after access is available:
1. Verify supported League rendering modes with a minimal noninteractive window and record
   injection success/failure through the documented runtime events.
2. Implement authenticated local IPC with a session-scoped secret, bounded messages and
   stale/disconnect hiding. Restrict the channel to this computer.
3. Port the current visual elements, preserving calibrated geometry and click-through behavior.
4. Test visibility, input pass-through, focus transitions, multi-monitor placement and FPS
   on the user's Windows10/GTX1070Ti/1080p144Hz system in Practice Tool.
5. Integrate optional runtime installation, update integrity, rollback and clear failure status.

Performance acceptance requires measured fullscreen overlay-off/on frame times in the same
scene. A desktop-render benchmark is insufficient. The user's reported144FPS fullscreen
versus85FPS borderless establishes why the presentation path must be tested carefully.

## Distribution and data scope

The application is public. Private development-key recommendation experiments remain private
and are not proposed as a publicly distributed feed. Current production API access and
provider approval must be represented accurately during review. Initial public release
pricing/monetization and any additional provider commitments require the owner's review;
this draft makes none.

## Access needed

Overwolf developer console approval, approved app identity and development credentials.
Credentials must be entered through the provider's supported local configuration, never
included in source or sent in chat. No account creation or proposal submission has occurred.

Sources:
- https://dev.overwolf.com/ow-electron/getting-started/project-roadmap/
- https://dev.overwolf.com/ow-electron/guides/dev-tools/dev-mode/
- https://dev.overwolf.com/ow-electron/reference/Overwolf-electron-APIs/overlay/interfaces/IOverwolfOverlayApi/
- https://www.riotgames.com/en/DevRel/vanguard-faq

## 2026-09-07 Overwolf proposal submitted

User explicitly authorized submission and acceptance of Developer/Monetization Terms.
Submitted the Rift Ready app idea through the signed-in Overwolf account using
https://riftready.gg, ow-electron, business model None, categories Stats and Guides & Trainers,
and League of Legends only. Proposal includes fullscreen/Practice Tool integration and
accurate local-API/estimate scope. Success page verified:
https://dev.overwolf.com/app-idea-form/success/ (We got you! / Success!).
Provider says it will contact the account by email; check spam if no email within two days.
Submission is not approval or developer credentials. Fullscreen integration remains pending.
