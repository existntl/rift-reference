# Rift Ready Game Bar widget

UWP widget hosted by Game Bar over the user's fullscreen League session. The live
page accepts the bounded version-one display DTO through `ApplyLiveJsonAsync`.
It renders selected purchase progress, enabled statistics, active Baron/Elder
estimates, and scoreboard item-value comparisons. Unknown values remain dashes.
No role labels are displayed; the five comparisons retain producer row order.

Gold is visible only when the producer reports `goldVisible`. Invalid/disconnected
frames and four-second receive timeouts clear every live panel. Foreground Game Bar
shows connection help while idle; pinned gameplay stays transparent. Identical JSON
refreshes freshness without rebuilding controls. The widget never accesses the game
process or receives Riot credentials; transport is managed by the app-service host.

Build with Visual Studio 2022 MSBuild, the UWP C# tools and Windows SDK 10.0.19041:

    MSBuild.exe RiftReady.GameBar.csproj /restore /p:Configuration=Debug /p:Platform=x64

Outputs are under the repository's ignored `build/gamebar` directory. Debug uses
the managed UWP runtime, not .NET Native. Package signing is disabled. Installation
and any development certificate are managed separately; no private key belongs here.

Identity: `RiftReady.GameBarPrototype`, publisher `CN=RiftReady.Development`.
Application ID: `App`. Game Bar extension ID: `RiftReadyDisplay`.

After development registration, press Win+G, choose Rift Ready in the Widgets menu,
pin it, and close Game Bar. Enable Game Bar click-through so mouse input reaches the
game. Its opacity control adjusts the panel background while text remains readable.
The freshness timer stops when the widget is not visible or the app suspends,
rechecks on resume, and is detached on close.
Opening the app normally shows setup instructions instead of implying it is an overlay.

Run `tests/verify-parser.ps1` for the receive-boundary checks. It compiles the exact
parser source against Windows SDK metadata; no package installation is needed.

SDK activation and manifest proxy declarations follow Microsoft's MIT-licensed
[XboxGameBarSamples](https://github.com/microsoft/XboxGameBarSamples/tree/master/Samples/WidgetSampleCS).
The pinned SDK version `7.2.240903001` matches that sample's proxy declarations.
The MIT notice is included in `THIRD-PARTY-NOTICES.txt`. Package logos are size variants
of Rift Ready's existing `assets/branding/rift-ready.png`.
