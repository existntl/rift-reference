# Fullscreen overlay investigation

Update: the user chose an independent renderer. See [prototype status](independent-overlay.md)
for the implemented D3D11 renderer/Present adapter and owned-host verification. League
compatibility is still unverified. The earlier Overwolf proposal was submitted successfully
using riftready.gg, but this prototype does not use its runtime.

Latest test: user reports invisible overlay with0.12.13 in Practice Tool Fullscreen.
Verified WindowMode0,1920x1080, enabled overlay and fresh PRACTICETOOL data/known inventory
with10 players. The fixed mode gate is not the remaining cause. Native fullscreen
visibility failed on this setup; exact DXGI presentation mode was not measured.

Fullscreen support is a user priority (2026-09-07). Current installed0.12.13 uses
transparent nonactivating WinForms windows; no exclusive-fullscreen renderer is implemented.

## First validation: League Fullscreen with Windows optimizations

The previous Practice Tool test was invalidated by the CLASSIC-only gate, fixed in0.12.12.
Retest0.12.13 in a fresh Practice Tool game with League set to Fullscreen. Verify the
purchase panel is visible with League foreground, hold Tab for markers, and compare FPS
in the same stationary scene with the overlay disabled and enabled. Record the actual
result before deciding a new runtime is necessary. Borderless FPS was reported144→85;
this does not establish the behavior of Fullscreen with fullscreen optimizations.

Read-only machine inspection found no League.exe AppCompat Layers entry in HKCU/HKLM;
GameDVR_FSEBehaviorMode, HonorUserFSEBehaviorMode and DXGIHonorFSEWindowsCompatible were0.
This shows no explicit disable in those inspected settings, not proof of the active
presentation path. No Windows/game configuration has been changed for this investigation.

[Microsoft](https://devblogs.microsoft.com/directx/demystifying-full-screen-optimizations/)
explains that Windows10 fullscreen optimizations can retain the game's fullscreen setting
while presenting through optimized borderless composition, including support for overlays.
Visible overlays can still add composition cost. Do not promise144FPS or call this true FSE.

## If the existing path fails

True exclusive fullscreen requires integration with game rendering. An ordinary topmost
window or Game Bar widget is not proof of this capability. A candidate supported runtime is
[Overwolf's overlay package](https://dev.overwolf.com/ow-electron/reference/Overwolf-electron-APIs/overlay/interfaces/IOverwolfOverlayApi/),
which documents overlay injection into supported games. This is a substantial integration,
not a checkbox in WinForms. Keep the existing local Riot API data collector, transfer only
sanitized overlay values to a separate renderer, and retain pass-through/nonactivating behavior.
Do not transfer API keys, LCU tokens, player identifiers or raw API responses to that renderer.

[Overwolf onboarding](https://dev.overwolf.com/ow-electron/getting-started/project-roadmap/)
requires an approved app proposal for full package access and currently does not approve
private apps. Rift Ready has a public distribution, but there is no verified Overwolf approval
or application ID. Prepare a public application proposal for review before external submission.
No framework installed, account created, proposal sent, or unsupported injection implemented.

[Riot's Vanguard FAQ](https://www.riotgames.com/en/DevRel/vanguard-faq) says API-based overlays
should continue working and explicitly states there is no Vanguard allowlist. Overwolf app
approval is distinct from Riot registration and cannot be described as a Vanguard exemption.
