using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using RiftReference.Release;
namespace RiftReference {
public class UpdateChannel {public string manifestUrl="";}
public class UpdateManager {
 readonly string home,key;public string Status="Not checked yet.";public Manifest Available;string signedManifest;
 public UpdateManager(string root){home=root;key=ReleaseInfo.PublicKey();}
 public static bool GameRunning(){var games=Process.GetProcessesByName("League of Legends");try{return games.Length>0;}finally{foreach(var game in games)game.Dispose();}}
 public async Task Check(){Available=null;string channelFile=Path.Combine(home,"update-channel.json");
  if(!File.Exists(channelFile)){Status="Update service is not configured.";return;}
  var channel=new JavaScriptSerializer().Deserialize<UpdateChannel>(File.ReadAllText(channelFile));
  if(channel==null||string.IsNullOrWhiteSpace(channel.manifestUrl)){Status="Update service is not configured.";return;}
  ReleaseInfo.Https(channel.manifestUrl);Status="Checking for updates…";
  string temp=Path.Combine(Path.GetTempPath(),"RiftReference-manifest-"+Guid.NewGuid().ToString("N")+".json");
  try{await Download(channel.manifestUrl,temp,131072);signedManifest=File.ReadAllText(temp);var m=ReleaseInfo.Verify(signedManifest,key);if(new Version(m.version)>new Version(ReleaseInfo.Version)){Available=m;Status="Version "+m.version+" is available.";}else Status="You are up to date ("+ReleaseInfo.Version+").";}finally{if(File.Exists(temp))File.Delete(temp);}
 }
 async Task Download(string url,string target,long limit){ReleaseInfo.Https(url);await Task.Run(()=>{
  var info=new ProcessStartInfo{FileName=Path.Combine(home,"runtime","python.exe"),Arguments="-I \""+Path.Combine(home,"update_fetch.py")+"\" "+Convert.ToBase64String(Encoding.UTF8.GetBytes(url))+" \""+target+"\" "+limit,WorkingDirectory=Path.GetTempPath(),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
  using(var p=Process.Start(info)){var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(120000)){p.Kill();throw new IOException("Download timed out. Try again later.");}if(p.ExitCode!=0)throw new IOException("Download failed. Check your connection and try again.");}
 });}
 public async Task<string> Prepare(){if(Available==null)throw new InvalidOperationException("No update is available.");if(GameRunning())throw new InvalidOperationException("Finish your League match before updating.");
  // A background check can replace Available while this download is in flight.
  // Capture and re-verify the signed bundle before yielding; never mix two releases.
  string envelope=signedManifest;var release=ReleaseInfo.Verify(envelope,key);
  if(new Version(release.version)<=new Version(ReleaseInfo.Version))throw new InvalidOperationException("No newer update is available.");
  string folder=Path.Combine(Path.GetTempPath(),"RiftReferenceUpdate-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string installer=Path.Combine(folder,"RiftReference-Setup.exe");
  await Download(release.installerUrl,installer,release.sizeBytes);ReleaseInfo.VerifyFile(installer,release);File.WriteAllText(Path.Combine(folder,"release.json"),envelope);return installer;
 }
 public void Launch(string installer){if(GameRunning())throw new InvalidOperationException("A League match is running. The update will wait until you finish.");
  var release=ReleaseInfo.Verify(File.ReadAllText(Path.Combine(Path.GetDirectoryName(installer),"release.json")),key);
  if(new Version(release.version)<=new Version(ReleaseInfo.Version))throw new InvalidOperationException("No newer update is available.");
  ReleaseInfo.VerifyFile(installer,release);
  Process.Start(new ProcessStartInfo{FileName=installer,Arguments="--apply \""+home.TrimEnd('\\')+"\" "+Process.GetCurrentProcess().Id+" \""+Path.Combine(Path.GetDirectoryName(installer),"release.json")+"\"",WorkingDirectory=Path.GetTempPath(),UseShellExecute=false,CreateNoWindow=true});
 }
}
}
