$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$cache = Join-Path $root 'cache\minhook'
$commit = 'c3fcafdc10146beb5919319d0683e44e3c30d537' # Official v1.3.4 tag
$archive = Join-Path $cache 'source.zip'
New-Item -ItemType Directory -Force $cache | Out-Null
if (!(Test-Path -LiteralPath $archive)) { Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/TsudaKageyu/minhook/archive/$commit.zip" -OutFile $archive }
# Reproducibility pin measured on first download from official upstream, not a publisher signature.
if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne 'CDCB160F734D81BD4D235DFEA79E3F5A661C8EF0AB74FA814272AA5449069034') { throw 'MinHook source hash mismatch' }
$source = Join-Path $cache "minhook-$commit"
if (!(Test-Path (Join-Path $source 'include\MinHook.h'))) { Expand-Archive $archive $cache -Force }
Write-Output $source
