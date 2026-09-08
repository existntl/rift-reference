param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$cache = Join-Path $repo 'cache/gamebar-tools'
$installer = Join-Path $cache 'vs_BuildTools.exe'
$expectedHash = '236367B68BA9A51708263AB10A1C85546CC4A8ECA78B365168811D19C4FB2F29'
# Official VS2022 17.14.39 bootstrapper, verified against Microsoft's winget manifest.
$uri = 'https://download.visualstudio.microsoft.com/download/pr/fa619120-9c0e-47e6-bfe0-3ee96fb671b2/236367b68ba9a51708263ab10a1c85546cc4a8eca78b365168811d19c4fb2f29/vs_BuildTools.exe'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $existing = & $vswhere -latest -version '[17.0,18.0)' -products Microsoft.VisualStudio.Product.BuildTools -requires Microsoft.VisualStudio.Workload.UniversalBuildTools Microsoft.VisualStudio.Component.Windows10SDK.19041 -property installationPath
    if ($existing) { Write-Output "UWP build tools are installed: $existing"; return }
}
New-Item -ItemType Directory -Path $cache -Force | Out-Null
if (!(Test-Path -LiteralPath $installer)) { Invoke-WebRequest -Uri $uri -OutFile $installer }
if ((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash -ne $expectedHash) { throw 'Build Tools installer checksum mismatch. No installer was started.' }
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'CN=Microsoft Corporation,') { throw 'Build Tools installer Microsoft signature validation failed.' }
Write-Output 'Installing official Microsoft UWP build tools and Windows SDK19041. Approve the Windows installer prompt if shown. No automatic restart is allowed.'
$installArgs = '--quiet --wait --norestart --nocache --add Microsoft.VisualStudio.Workload.UniversalBuildTools --add Microsoft.VisualStudio.Component.Windows10SDK.19041 --includeRecommended'
$process = Start-Process -FilePath $installer -ArgumentList $installArgs -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -notin @(0,3010)) { throw "Build Tools installation failed with exit code $($process.ExitCode)." }
if ($process.ExitCode -eq 3010) { Write-Output 'Installation succeeded; Windows reports a restart is required. No restart was performed.' }
else { Write-Output 'Build Tools installation completed.' }
