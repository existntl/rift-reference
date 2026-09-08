using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using RiftReady.GameBarBridge;

namespace RiftReference {
// The main application owns the API sample. This publishes only the display DTO;
// the packaged Game Bar helper does not need a second League connection.
public sealed class GameBarIntegration : IDisposable {
 public const string PackageFamily="RiftReady.GameBarPrototype_q4vf8r75vcnhg";
 public const string LaunchUri="ms-gamebar://launch/activate/"+PackageFamily+"_App_RiftReadyDisplay";
 const int MaxEnvelopeBytes=16384;
 readonly DataStore data;readonly Func<OverlayOptions> options;readonly Timer timer=new Timer{Interval=250};
 readonly string path,temp;Snapshot snapshot;DateTime received;bool disposed,wroteHidden;Rectangle lastGame=Screen.PrimaryScreen.Bounds;
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern int GetPackagesByPackageFamily(string family,ref uint count,IntPtr names,ref uint length,IntPtr buffer);
 public string Status {get;private set;}
 public static bool Installed {get{try{uint count=0,length=0;int result=GetPackagesByPackageFamily(PackageFamily,ref count,IntPtr.Zero,ref length,IntPtr.Zero);return result==122&&count>0;}catch{return false;}}}
 public static bool HasRecentConnection(string path,DateTime now){try{var file=new FileInfo(path);if(!file.Exists||file.Length>64)return false;long ticks;if(!Int64.TryParse(File.ReadAllText(path).Trim(),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out ticks)||ticks<DateTime.MinValue.Ticks||ticks>DateTime.MaxValue.Ticks)return false;double age=(now-new DateTime(ticks,DateTimeKind.Utc)).TotalSeconds;return age>=0&&age<=5;}catch{return false;}}
 public static string SetupStatus {get{
  var connection=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RiftReady","GameBar","connected.txt");
  if(HasRecentConnection(connection,DateTime.UtcNow))return "Connected to Game Bar. Pin Rift Ready, then return to League.";
  return Installed?"Pin Rift Ready in Game Bar (Win + G), then return to League.":"Install the Rift Ready Game Bar component to use fullscreen overlays.";
 }}
 public static void Open(){Process.Start(new ProcessStartInfo{FileName=LaunchUri,UseShellExecute=true});}
 public GameBarIntegration(DataStore store,Func<OverlayOptions> getOptions){
  data=store;options=getOptions;
  path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RiftReady","GameBar","display.json");
  temp=path+"."+Process.GetCurrentProcess().Id+".tmp";
  timer.Tick+=(s,e)=>Publish(false);timer.Start();
 }
 public void Update(Snapshot value){if(!Object.ReferenceEquals(snapshot,value)){snapshot=value;received=DateTime.UtcNow;}}
 void Publish(bool shuttingDown){
  if(disposed&&!shuttingDown)return;
  var opt=options();bool enabled=!shuttingDown&&opt!=null&&opt.Enabled&&opt.GameBar;
  if(!enabled&&wroteHidden)return;
  try{
   var now=DateTime.UtcNow;Rectangle game=Rectangle.Empty;
   bool focused=enabled&&GameOverlay.GameBounds(out game);
   bool held=focused&&opt.Key>0&&opt.Key<256&&(GetAsyncKeyState(opt.Key)&0x8000)!=0;
   var frame=enabled?DisplaySnapshotProducer.Create(data,snapshot,opt,now,received,held,focused):new DisplaySnapshot();
   if(focused)lastGame=game;
   DisplaySnapshotProducer.SetGeometry(frame,opt??new OverlayOptions(),lastGame);
   // Validate the DTO before placing it in the bounded envelope.
   string json="{\"utcTicks\":"+now.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"frame\":"+DisplaySnapshotProducer.Serialize(frame)+"}";
   if(Encoding.UTF8.GetByteCount(json)>MaxEnvelopeBytes)throw new InvalidDataException();
   Directory.CreateDirectory(Path.GetDirectoryName(path));
   File.WriteAllText(temp,json,new UTF8Encoding(false));
   if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
   wroteHidden=!enabled;Status=enabled?"Game Bar display updated":"Game Bar display off";
  }catch{Status="Game Bar display unavailable";}
 }
 public void Dispose(){if(disposed)return;timer.Stop();Publish(true);disposed=true;timer.Dispose();try{if(File.Exists(temp))File.Delete(temp);}catch{}}
}
}
