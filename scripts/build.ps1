param([string]$OutputDirectory, [switch]$Installer)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'build/app' }
$appRoot = [IO.Path]::GetFullPath($OutputDirectory)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$publicKey = Join-Path $projectRoot 'publisher/update-public-key.xml'
$brandImage = Join-Path $projectRoot 'assets/branding/rift-ready.png'
foreach ($folder in @('data','runtime')) {
    $cached = Join-Path $projectRoot "cache/$folder"
    if (!(Test-Path -LiteralPath $cached)) { throw "Missing $cached. See docs/build.md for cache bootstrap." }
}
if (!(Test-Path -LiteralPath (Join-Path $projectRoot 'cache/data/loadout-icons/runes/5011.png'))) {
    throw 'Missing loadout icon cache. Run cache/runtime/python.exe scripts/cache-loadout-icons.py before building.'
}
New-Item -ItemType Directory -Path $appRoot -Force | Out-Null
$brandIcon = Join-Path $appRoot 'rift-ready.ico'
& (Join-Path $PSScriptRoot 'build-brand-icon.ps1') -OutputPath $brandIcon
foreach ($folder in @('data','runtime')) { Copy-Item -LiteralPath (Join-Path $projectRoot "cache/$folder") -Destination $appRoot -Recurse -Force }
Copy-Item -LiteralPath (Join-Path $projectRoot 'helpers/transport.py'),(Join-Path $projectRoot 'helpers/update_fetch.py'),(Join-Path $projectRoot 'helpers/mobile_server.py'),(Join-Path $projectRoot 'helpers/mobile.html'),(Join-Path $projectRoot 'helpers/qrcodegen.py'),(Join-Path $projectRoot 'config/update-channel.json') -Destination $appRoot -Force
$mobileTemplate = [IO.File]::ReadAllText((Join-Path $projectRoot 'helpers/mobile.html'),[Text.Encoding]::UTF8)
$mobileLogo = [Convert]::ToBase64String([IO.File]::ReadAllBytes([IO.Path]::ChangeExtension($brandIcon,'.png')))
[IO.File]::WriteAllText((Join-Path $appRoot 'mobile.html'),$mobileTemplate.Replace('__RIFT_READY_LOGO__',$mobileLogo),[Text.Encoding]::UTF8)
$sourceNames = @('Core','App','Matchup','AudioCues','Updates','ReleaseSecurity','Mobile','Theme','Brand','Pregame','BuildPlanner','LoadoutEditor','Postgame','Overlay','OverlayVisuals','PanelLayout','StatsPanel')
if (Test-Path -LiteralPath (Join-Path $projectRoot 'src/Coaching.cs')) { $sourceNames += 'Coaching' }
if (Test-Path -LiteralPath (Join-Path $projectRoot 'src/Practice.cs')) { $sourceNames += 'Practice' }
$sources = $sourceNames | ForEach-Object { Join-Path $projectRoot "src/$_.cs" }
if (Test-Path -LiteralPath (Join-Path $projectRoot 'tests/CoachingTests.cs')) { $sources += Join-Path $projectRoot 'tests/CoachingTests.cs' }
$refs = @('/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll','/reference:System.Net.Http.dll',"/reference:$env:WINDIR/Microsoft.NET/assembly/GAC_MSIL/System.Speech/v4.0_4.0.0.0__31bf3856ad364e35/System.Speech.dll")
& $compiler /nologo /target:winexe "/out:$appRoot/RiftReference.exe" "/win32icon:$brandIcon" "/resource:$brandImage,rift-ready.png" "/resource:$publicKey,update-public-key.xml" @refs @sources
if ($LASTEXITCODE -ne 0) { throw 'App compilation failed' }
if ($Installer) {
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $packageRoot = Join-Path $projectRoot ('build/package-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $packageRoot | Out-Null
    $payload = Join-Path $packageRoot 'payload.zip'
    $zip = [IO.Compression.ZipFile]::Open($payload, [IO.Compression.ZipArchiveMode]::Create)
    try {
        # Intentional package inputs. No preferences, reviews, test outputs or publisher files.
        $inputs = @('RiftReference.exe','transport.py','update_fetch.py','mobile_server.py','mobile.html','qrcodegen.py','update-channel.json') | ForEach-Object { Get-Item -LiteralPath (Join-Path $appRoot $_) }
        $inputs += foreach ($folder in @('data','runtime')) { Get-ChildItem -LiteralPath (Join-Path $appRoot $folder) -Recurse -File }
        foreach ($file in $inputs) {
            $relative = $file.FullName.Substring($appRoot.TrimEnd('\').Length + 1).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $zip.Dispose() }
    $dist = Join-Path $projectRoot 'dist'
    New-Item -ItemType Directory -Path $dist -Force | Out-Null
    & $compiler /nologo /target:winexe "/out:$dist/RiftReference-Setup.exe" "/resource:$publicKey,update-public-key.xml" "/resource:$payload,payload.zip" "/win32icon:$brandIcon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $projectRoot 'src/Installer.cs') (Join-Path $projectRoot 'src/ReleaseSecurity.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
    Write-Output "Built $dist/RiftReference-Setup.exe"
}
Write-Output "Built $appRoot/RiftReference.exe"
