using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
namespace RiftReady.GameBarBridge {
sealed class FeedEnvelope { public long utcTicks; public DisplaySnapshot frame; }
sealed class LiveProducer : ApplicationContext {
 const string Family="RiftReady.GameBarPrototype_q4vf8r75vcnhg";
 readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=250};
 readonly string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RiftReady","GameBar");
 AppServiceConnection connection;bool busy,closing;DateTime nextConnect,disconnected=DateTime.UtcNow,lastHealth;
 readonly DateTime started=DateTime.UtcNow;string lastStatus="";
 static void Log(string message){try{string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RiftReady","GameBar");Directory.CreateDirectory(dir);string path=Path.Combine(dir,"bridge.log");if(File.Exists(path)&&new FileInfo(path).Length>65536)File.WriteAllText(path,"");File.AppendAllText(path,DateTime.UtcNow.ToString("o")+" "+message+Environment.NewLine);}catch{}}
 LiveProducer(){timer.Tick+=Tick;timer.Start();Log("Integrated bridge started; main app feed only");}
 internal static DisplaySnapshot ReadFrame(string path,DateTime now){
  try{var file=new FileInfo(path);if(!file.Exists||file.Length>16384)return new DisplaySnapshot();
   var envelope=new JavaScriptSerializer{MaxJsonLength=16384}.Deserialize<FeedEnvelope>(File.ReadAllText(path));
   if(envelope==null||envelope.frame==null||envelope.utcTicks<=0||envelope.utcTicks>now.Ticks||(now.Ticks-envelope.utcTicks)>TimeSpan.FromSeconds(4).Ticks)return new DisplaySnapshot();
   DisplaySnapshotProducer.Serialize(envelope.frame);return envelope.frame;
  }catch{return new DisplaySnapshot();}
 }
 void Disconnect(){if(connection!=null){connection.Dispose();connection=null;}disconnected=DateTime.UtcNow;nextConnect=disconnected.AddSeconds(2);}
 async Task<bool> Connect(){if(DateTime.UtcNow<nextConnect)return false;nextConnect=DateTime.UtcNow.AddSeconds(2);var candidate=new AppServiceConnection{AppServiceName="RiftReadyDisplayFeed",PackageFamilyName=Family};try{var operation=candidate.OpenAsync();var open=operation.AsTask();if(await Task.WhenAny(open,Task.Delay(5000))!=open){operation.Cancel();candidate.Dispose();return false;}var status=await open;Log("AppService open: "+status);if(status!=AppServiceConnectionStatus.Success){candidate.Dispose();return false;}connection=candidate;return true;}catch{candidate.Dispose();return false;}}
 async void Tick(object sender,EventArgs args){if(busy||closing)return;busy=true;try{
  if(connection==null&&!await Connect()){if((DateTime.UtcNow-disconnected).TotalSeconds>=60)ExitThread();return;}
  var now=DateTime.UtcNow;var dto=ReadFrame(Path.Combine(folder,"display.json"),now);
  var operation=connection.SendMessageAsync(new ValueSet{{"json",DisplaySnapshotProducer.Serialize(dto)}});var send=operation.AsTask();
  if(await Task.WhenAny(send,Task.Delay(5000))!=send){operation.Cancel();Disconnect();return;}
  var response=await send;string status="Send="+response.Status+" Fresh="+dto.fresh+" Visible="+dto.visible;
  if(status!=lastStatus){Log(status);lastStatus=status;}
  if(response.Status!=AppServiceResponseStatus.Success){Disconnect();return;}
  if((now-lastHealth).TotalSeconds>=2){lastHealth=now;try{Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"connected.txt"),now.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));}catch{}}
  if(response.Message.ContainsKey("continue")&&response.Message["continue"] is bool&&!(bool)response.Message["continue"]&&(now-started).TotalSeconds>10)ExitThread();
 }catch{Disconnect();}finally{busy=false;}}
 protected override void ExitThreadCore(){if(closing)return;closing=true;timer.Stop();if(connection!=null)connection.Dispose();Log("Integrated bridge stopped");base.ExitThreadCore();}
 [STAThread]static int Main(string[] args){try{
  if(args.Length>0){bool own=false;try{own=Windows.ApplicationModel.Package.Current.Id.FamilyName==Family;}catch{}if(!own)return 2;}
  bool created;using(var mutex=new Mutex(true,"Local\\RiftReady.GameBarBridge",out created)){if(!created)return 0;try{Application.EnableVisualStyles();using(var producer=new LiveProducer())Application.Run(producer);}finally{mutex.ReleaseMutex();}}return 0;
 }catch(Exception error){Log("Bridge startup failed: "+error.GetType().Name);return 1;}}
}
}
