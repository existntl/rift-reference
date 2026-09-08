# Windows Game Bar live prototype

## App integration in progress

Local main app0.12.14 installed through the existing installer with preserved
preference/review/rank-history hashes and a backup before enabling GameBar mode.
User supplied photos confirmed the compact live prototype; reported no noticeable
FPS drop (photo130FPS), not a controlled performance measurement.

Main app now owns the only League API poller. GameBarIntegration publishes its
bounded sanitized DTO atomically to %LOCALAPPDATA%/RiftReady/GameBar/display.json.
The packaged relay reads that feed, rejects stale/future/oversize/corrupt envelopes,
and sends it through same-package AppService. No Python/API reader bundled into
the relay. Settings control fullscreen/desktop mode, scoreboard calibration,
panel positions/sizes/transparency and display options. Desktop overlays are
suppressed in GameBar mode. Widget opens automatically on main startup or via
Game overlay / Open Game Bar; Game Bar owns pin/click-through.

Verification:768 app checks,28 integration checks,8 relay checks and34 widget
parser/geometry checks passed; installer upgrade/rollback passed; settings rendered
and inspected at1920x1080. The full-viewport1.1.0 widget exposed a cross-view UI
thread lifecycle crash during real startup; fixing before final validation.
Do not claim the aligned integrated version works until runtime check passes.

## Current local build

User supplied a photo showing the pinned counter over Practice Tool with Game Bar
closed. The display test passed on this PC with League's fullscreen setting.
This does not establish FPS impact or true exclusive-swapchain behavior.

Local widget1.0.3.0 is built, signed and installed (Appx Status Ok). The packaged
desktop helper starts with the widget, reads the existing installed data/settings,
and sends display-only JSON through the same-package RiftReadyDisplayFeed AppService.
Its runFullTrust capability is for the desktop API reader; no elevated access,
game-process loading, network exemption or broad IPC ACL is used.

The helper polls once per second and sends at most four times per second. Identical
frames do not rebuild UI controls. The widget hides invalid or four-second-old data.
Gold follows the saved Tab shortcut; stats, selected item and buffs follow saved
options. No role labels or invented unknown values. This first live version uses
one movable compact panel, not the old scoreboard-positioned WinForms windows.
The main installed app remains0.12.13; public releases are unchanged.

Verification: package builds;29 producer checks,26 exact-parser checks,768 existing
app self-checks pass. Fixed helper startup rejecting Windows' two activation
arguments: ignore OS metadata only when the current package identity matches ours.
Actual packaged activation now logs AppService open/send Success and stays alive.
The current API snapshot reports EndOfGame/PRACTICETOOL; a fresh Practice Tool
session is needed for visible gold and FPS validation. User accepted continuing
the Game Bar approach after clarification that it is a required dependency.
Log (sanitized and size-bounded):
%LOCALAPPDATA%/RiftReady/GameBar/bridge.log (packaged redirection may apply).

Build: scripts/build-gamebar.ps1, then scripts/sign-gamebar-test.ps1 with the
generated versioned MSIX, then scripts/install-gamebar-test.ps1. Existing approved
certificate trust is reused. Parser checks: experiments/gamebar/Widget/tests/verify-parser.ps1.

## Initial display-test history

After ordinary DLL loading was denied by League's process protection, the next
candidate is a supported Game Bar widget. This does not require access to League's
modules. No loading retry, bypass or elevated process access is planned.

Read-only inspection found Microsoft.XboxGamingOverlay7.326.8061.0 installed with
status Ok. League remains configured WindowMode0,1920x1080. User was asked to press
Win+G while League is focused and report whether Game Bar appears over the game or
minimizes/switches it. User confirmed Game Bar appears. The Rift Ready pinned-widget
test is still pending; Game Bar visibility alone does not prove that test passes.
FPS still needs a separate same-scene measurement.

Microsoft's supported widget is a UWP XAML application. The current WinForms window
cannot be registered directly. The minimal first test should be a pinned transparent
widget with a small text/counter, before porting the full HUD. Development requires
Visual Studio2022 with UWP workload and Windows SDK19041 (now installed; exit0,
no reboot required). Bootstrap: scripts/bootstrap-gamebar-tools.ps1.
The existing portable LLVM-MinGW compiler is not a replacement for that toolchain.

For production IPC, packaging a small desktop companion with the widget permits
AppService messages and binary blobs. An unpackaged desktop application can use
named pipes/RPC, but Microsoft documents additional Store-policy exceptions and
package-specific ACL requirements. Do not broaden the existing mapping ACL to all
app containers or add loopback/network exemptions as a shortcut. Use bounded,
sanitized display content, stale/disconnect hiding and user-controlled pinning.

The display-test source is in experiments/gamebar/Widget; build it with
scripts/build-gamebar.ps1 once tools are installed. Its one-second counter is
explicitly a display test, with no match data or network capabilities. Output is
confined to build/gamebar. The sanitized future live-data contract is in
experiments/gamebar/Bridge/DisplaySnapshot.cs and passes29 Framework checks.

The widget compiled and packaged successfully. The signed local test package is
build/gamebar/packages/RiftReady.GameBar_1.0.0.0_x64_Debug_Test/RiftReady.GameBar_1.0.0.0_x64_Debug.msix.
Its manifest has no capabilities; the package includes the matching Microsoft
runtime dependencies. Signing uses scripts/sign-gamebar-test.ps1; the non-exportable
private key stays in CurrentUser/My, with only the public certificate exported.
Certificate subject CN=RiftReady.Development, thumbprint
DAF444B739DC4F9A8651CCEA68D7A6ED3F0CAA65, expires in three months.

CurrentUser/TrustedPeople import succeeded, but Add-AppxPackage still failed with
0x800B0109 (untrusted signing certificate), surfaced as0x80073CF0. Activity ID:
970abbbe-2c29-000a-3543-5c97292cdd01. User subsequently approved PC-level trust;
scripts/trust-gamebar-test.ps1 imported the exact certificate into
LocalMachine/TrustedPeople through the Windows administrator prompt. Retried
installation succeeded: RiftReady.GameBarPrototype_1.0.0.0_x64__q4vf8r75vcnhg,
Status Ok. Do not use
Trusted Root or disable signature enforcement. The failed deployment command is
scripts/install-gamebar-test.ps1 (now succeeds). No Developer Mode, firewall, League or Vanguard
changes were made. Pinned-widget visibility and FPS remain untested.

Sources:
- https://learn.microsoft.com/en-us/gaming/game-bar/overview
- https://learn.microsoft.com/en-us/gaming/game-bar/guide/visual-studio
- https://learn.microsoft.com/en-us/xbox/game-bar/guide/communicating-apps
- https://learn.microsoft.com/en-us/gaming/game-bar/api/xgb-widget
- https://devblogs.microsoft.com/directx/demystifying-full-screen-optimizations/
