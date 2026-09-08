$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$baseFolder=Join-Path $repo 'build/private-launcher'
$source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'start-private-recommendations.ps1'))
$code=[regex]::Match($source,"(?s)-TypeDefinition @'\r?\n(.*?)\r?\n'@").Groups[1].Value
if(!$code){throw 'Private launcher source missing.'}
$code+=@'
public static class PrivateBuildsEntry {
 [System.STAThread] public static void Main(string[] args) {
  System.Windows.Forms.Application.EnableVisualStyles();
  if(args.Length!=2)return;
  string root=args[0];int budget;
  if(!int.TryParse(args[1],out budget)||budget<150||budget>5000)return;
  byte[] identity=System.Text.Encoding.UTF8.GetBytes(System.IO.Path.GetFullPath(root).TrimEnd('\\').ToUpperInvariant());
  string name;
  using(var hash=System.Security.Cryptography.SHA256.Create())name="Local\\RiftReadyPrivateCollector-"+System.BitConverter.ToString(hash.ComputeHash(identity)).Replace("-","");
  bool created;
  using(var instance=new System.Threading.Mutex(true,name,out created)) {
   if(!created)return;
   try { System.Windows.Forms.Application.Run(new PrivateRecommendationWindow(System.IO.Path.Combine(root,"cache/runtime/python.exe"),System.IO.Path.Combine(root,"services/recommendations/pipeline.py"),System.IO.Path.Combine(root,"cache/data"),System.IO.Path.Combine(root,"services/recommendations/output"),budget)); }
   finally { instance.ReleaseMutex(); }
  }
 }
}
'@
# Each source revision has its own executable, so a running version is never
# overwritten. Identical rebuilds reuse the executable even while it is open.
$hash=[Security.Cryptography.SHA256]::Create()
try {$revision=([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes($code)))).Replace('-','').ToLowerInvariant()}
finally {$hash.Dispose()}
$folder=Join-Path $baseFolder $revision
[void](New-Item -ItemType Directory -Force -Path $folder)
$inputFile=Join-Path $folder 'PrivateBuilds.cs'
$exe=Join-Path $folder 'PrivateBuilds.exe'
if(Test-Path -LiteralPath $exe){Write-Output $exe;return}
[IO.File]::WriteAllText($inputFile,$code,[Text.UTF8Encoding]::new($false))
$temporary=Join-Path $folder ('PrivateBuilds-'+[guid]::NewGuid().ToString('N')+'.exe')
$compilerOutput=& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe "/out:$temporary" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $inputFile
if($LASTEXITCODE -ne 0){throw 'Private launcher compilation failed.'}
# Another build can finish first; its complete identical revision is reusable.
try {[IO.File]::Move($temporary,$exe)}
catch {if(!(Test-Path -LiteralPath $exe)){throw};Remove-Item -LiteralPath $temporary}
Write-Output $exe
