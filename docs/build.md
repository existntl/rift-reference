# Building on Windows 10

Requires Windows .NET Framework 4.8 and PowerShell. No .NET SDK is required.
Run `powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Installer` from the repository.
The app goes into ignored `build/app`; the installer goes into ignored `dist`.
Use `-OutputDirectory ABSOLUTE_PATH` for a separate validation build.
Use `-InstallerOutputDirectory ABSOLUTE_PATH` with `-Installer` to keep a local test
installer separate from an already-published `dist` installer/signed-manifest pair.
For example: `-Installer -InstallerOutputDirectory build/audit-package-0.12.21`.

## Local cache bootstrap

This checkout has an independent copy of the verified 0.5.1 cache. For a new checkout,
obtain the 0.5.1 installer and signed latest.json from the v0.5.1 GitHub release.
Compile tests/ReleaseTests.cs with src/ReleaseSecurity.cs, embedding
publisher/update-public-key.xml as resource update-public-key.xml, and reference
System.Web.Extensions.dll. Run that verifier with the manifest and installer paths
before extracting. Run the verified installer with `--extract-test ABSOLUTE_EMPTY_FOLDER`.
Copy only its `data` and `runtime` folders into `cache/`. Preserve the runtime license.
The baseline cache is Data Dragon 16.17.1 and Python 3.13.15. No automatic patch match
is claimed. This explicit bootstrap does not depend on the original workspace.

For the visual loadout editor, run `cache/runtime/python.exe scripts/cache-loadout-icons.py`
after bootstrapping (and after intentionally replacing patch data). This caches 328 PNGs
for the current 16.17.1 data: tree, rune, stat-shard and purchasable SR item icons from Riot
Data Dragon. Builds bundle this ignored cache; the editor never downloads images during
use. A missing/corrupt image falls back to its visible name. The downloader uses eight
workers, validates PNG signatures and reuses existing files.

## Validation and publishing

The optional data/splashes cache holds original Riot Data Dragon hero artwork. Missing
champion artwork is fetched asynchronously during use; it is not required for compilation.
For offline Vayne preview renders, seed cache/data/splashes/Vayne.jpg from Riot's public
Data Dragon splash endpoint. DashboardUiTests uses isolated synthetic images to verify
champion switching, neutral fallback and filter independence without network access.

`build/app/RiftReference.exe --test` writes test-results.txt beside the executable.
`--render` writes a neutral, empty overview to overview.png; `--render-settings` writes
preferences PNGs. Neither creates a simulated match. There is no demo launch mode.
Simulated match/draft/results fixtures live in tests/DashboardFixtures.cs and are compiled
only into standalone test executables, not the app or installer. Snapshot.Demo remains an
internal safety marker for test fixtures and overlay-alignment previews, not a session mode.
Run tests in isolated output folders; layout tests intentionally change preferences.
`scripts/verify-home.ps1` includes the native page-navigation harness, which renders
the active pages at 1920×1040 and 1280×950 and verifies navigation, retained drafts,
account clearing and retired-overlay startup isolation. See docs/navigation-flow.md.
`scripts/verify-installer.ps1` verifies inherited-working-directory upgrades and rollback,
including preferences.json.bak alongside the existing settings/review/rank recovery files.
Use `-BaselineInstallerPath ABSOLUTE_OLD_INSTALLER` to exercise an older release upgrade.
`scripts/verify-ui.ps1` builds an isolated app, tests settings and review controls and saves
screenshots of dashboard variants and all playbook pages. `--render-practice` renders the
playbook and empty review window. No real account is needed for these offline checks.

`scripts/verify-audit.ps1` builds an isolated app and checks preference data-loss
regressions, page freshness/reuse/history limits, helper shutdown, asset reuse,
match-time gates and updater races. It also runs mobile/recommendation/archived-overlay
and Python helper/collector suites. Its signed-update race uses a disposable test key
and offline fixture downloader, never the publisher key or an installer launch.
The performance output is a deterministic offscreen allocation/resource comparison,
not an in-game FPS benchmark. Keep fixture data/tools confined to tests and ignored
build directories; they must not enter the production app or installer.

`scripts/verify-mobile.ps1` builds and checks sanitized mobile snapshots, the owned helper
lifecycle, token rotation and HTTP access controls. `tests/mobile-browser.cjs` uses
Playwright with installed Edge for phone/tablet viewport, pairing and reconnect checks;
set PLAYWRIGHT_MODULE to the installed Playwright module directory if needed. Outputs
stay under ignored build/app. The mobile helper uses only bundled Python standard-library
modules and the included MIT QR generator. No mobile package install is required.

The release private key remains outside this checkout in the original workspace.
Do not copy it here. Use src/ReleaseTool.cs with its configured external path only when
signing a release. Keep the embedded public key unchanged. Publisher signatures are
not Windows Authenticode signatures. A local installer does not publish an update.
