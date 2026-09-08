$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$path=Join-Path $repo 'build/gamebar/RiftReady.Development.cer'
$certificate=New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($path)
if($certificate.Thumbprint -ne 'DAF444B739DC4F9A8651CCEA68D7A6ED3F0CAA65' -or $certificate.Subject -ne 'CN=RiftReady.Development'){throw 'Unexpected test certificate; nothing imported.'}
Import-Certificate -FilePath $path -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
