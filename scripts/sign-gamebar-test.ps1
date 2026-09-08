param([Parameter(Mandatory=$true)][string]$PackagePath)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=[IO.Path]::GetFullPath((Join-Path $repo 'build/gamebar'))
$package=(Resolve-Path -LiteralPath $PackagePath).Path
if(!$package.StartsWith($output+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($package) -notin @('.appx','.msix')){throw 'Select a Game Bar package inside build/gamebar.'}
$signtool=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin/10.0.19041.0/x64/signtool.exe'
if(!(Test-Path -LiteralPath $signtool)){throw 'Windows SDK19041 signing tool is missing.'}
$subject='CN=RiftReady.Development'
$certificate=Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $subject -and $_.FriendlyName -eq 'Rift Ready local Game Bar test' -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date).AddDays(1) } | Sort-Object NotAfter -Descending | Select-Object -First 1
if(!$certificate){
 $certificate=New-SelfSignedCertificate -Type Custom -Subject $subject -FriendlyName 'Rift Ready local Game Bar test' -CertStoreLocation Cert:\CurrentUser\My -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyUsage DigitalSignature -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddMonths(3) -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}')
}
& $signtool sign /fd SHA256 /s My /sha1 $certificate.Thumbprint $package
if($LASTEXITCODE -ne 0){throw 'Package signing failed.'}
$publicCertificate=Join-Path $output 'RiftReady.Development.cer'
Export-Certificate -Cert $certificate -FilePath $publicCertificate -Force | Out-Null
Write-Output "Signed local test package: $package"
Write-Output "Public certificate: $publicCertificate"
Write-Output "Certificate thumbprint: $($certificate.Thumbprint)"
Write-Output 'Certificate trust and app installation were not changed. The private key remains non-exportable in the current-user certificate store.'
