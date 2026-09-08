using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using RiftReference;
using RiftReady.GameBarBridge;
class GameBarIntegrationTests {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static void Set(object target,string name,object value){target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
 static void Publish(GameBarIntegration publisher){typeof(GameBarIntegration).GetMethod("Publish",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(publisher,new object[]{false});}
 [STAThread]static int Main(){try{
  string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"gamebar-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  var options=new OverlayOptions{Enabled=true,GameBar=false,Champion="Vayne",Target=3031,BoardY=100,RowStart=244,RowGap=75};
  var saved=new JavaScriptSerializer().Deserialize<OverlayOptions>(new JavaScriptSerializer().Serialize(options));Check(saved.Enabled&&!saved.GameBar&&saved.RowStart==244&&saved.RowGap==75,"Existing options changed during serialization");
  options.GameBar=true;OverlayStorage.Save(folder,options);var restored=new JavaScriptSerializer().Deserialize<OverlayOptions>(File.ReadAllText(Path.Combine(folder,"overlay.json")));Check(restored.GameBar&&restored.Champion=="Vayne"&&restored.Target==3031&&restored.BoardY==100,"Fullscreen option lost other saved values");
  Check(!new JavaScriptSerializer().Deserialize<OverlayOptions>("{\"Enabled\":true}").GameBar,"Legacy settings implicitly enabled fullscreen mode");
  foreach(var viewport in new[]{new Rectangle(0,0,1920,1080),new Rectangle(-1920,0,1920,1080),new Rectangle(1920,-200,2560,1440)}){
   var frame=new DisplaySnapshot();DisplaySnapshotProducer.SetGeometry(frame,options,viewport);
   Check(frame.viewportX==viewport.X&&frame.viewportY==viewport.Y&&frame.viewportWidth==viewport.Width&&frame.viewportHeight==viewport.Height,"Monitor origin was lost");
   Check(frame.boardX>=0&&frame.boardY>=0&&frame.boardX+frame.boardWidth<=viewport.Width&&frame.boardY+frame.boardHeight<=viewport.Height,"Board escaped viewport");
   Check(frame.rowStart>=frame.boardY&&frame.rowStart+4*frame.rowGap<=frame.boardY+frame.boardHeight,"Gold rows escaped board");
   Check(frame.purchaseX>=0&&frame.purchaseY>=0&&frame.purchaseX+300<=viewport.Width&&frame.purchaseY+90<=viewport.Height,"Purchase anchor escaped viewport");
  }
  options.BoardX=100000;options.BoardY=-100000;options.BoardScale=100000;options.RowStart=100000;options.RowGap=-100000;options.BuildOpacity=0;options.StatsOpacity=200;var clamped=new DisplaySnapshot();DisplaySnapshotProducer.SetGeometry(clamped,options,new Rectangle(-1920,0,1920,1080));Check(clamped.boardX>=0&&clamped.boardY>=0&&clamped.boardX+clamped.boardWidth<=1920&&clamped.boardY+clamped.boardHeight<=1080,"Invalid alignment not clamped");Check(clamped.purchaseOpacity==.2&&clamped.statsOpacity==1,"Opacity not bounded");
  options.Enabled=false;string output=Path.Combine(folder,"display.json");
  using(var publisher=new GameBarIntegration(null,()=>options)){
   Set(publisher,"path",output);Set(publisher,"temp",output+".tmp");
   publisher.Update(new Snapshot{Account="SECRET-ACCOUNT",Notice="SECRET-TOKEN",Overlay=new OverlaySnapshot{Opponent="SECRET-NAME"}});
   Publish(publisher);string json=File.ReadAllText(output);var envelope=J.Parse(json);var frame=J.Get(envelope,"frame");
   Check(J.Get(frame,"visible") is bool&&!(bool)J.Get(frame,"visible")&&J.Get(frame,"targetName")==null,"Disabled publisher leaked display");
   Check(!json.Contains("SECRET")&&Encoding.UTF8.GetByteCount(json)<=16384,"Envelope exposed identities or exceeded bounds");
   Check(J.N(envelope,"utcTicks")>0&&J.N(frame,"viewportWidth")>0,"Hidden frame omitted timestamp or setup geometry");
   Check(!File.Exists(output+".tmp"),"Atomic publication left temporary content");
   Publish(publisher);Check(File.ReadAllText(output)==json,"Disabled publisher needlessly rewrote heartbeat");
   // Verify shutdown clears a previously visible frame without touching the game.
   File.WriteAllText(output,"{\"frame\":{\"visible\":true}}");Set(publisher,"wroteHidden",false);options.Enabled=true;publisher.Dispose();
   Check(!(bool)J.Get(J.Get(J.Parse(File.ReadAllText(output)),"frame"),"visible"),"Shutdown left visible frame");
  }
  var recent=typeof(GameBarIntegration).GetMethod("HasRecentConnection",BindingFlags.Public|BindingFlags.Static);
  if(recent!=null){string connection=Path.Combine(folder,"connected.txt");var now=DateTime.UtcNow;Func<bool> check=()=> (bool)recent.Invoke(null,new object[]{connection,now});
   Check(!check(),"Absent widget heartbeat counted as connected");File.WriteAllText(connection,now.AddSeconds(-5).Ticks.ToString());Check(check(),"Five-second heartbeat boundary rejected");File.WriteAllText(connection,now.AddSeconds(-6).Ticks.ToString());Check(!check(),"Stale widget heartbeat counted as connected");File.WriteAllText(connection,now.AddSeconds(1).Ticks.ToString());Check(!check(),"Future widget heartbeat counted as connected");File.WriteAllText(connection,new string('9',100));Check(!check(),"Oversized widget heartbeat accepted");
  }else throw new Exception("Rebuild main assembly with connection freshness helper before running tests.");
  Console.WriteLine(checks+" Game Bar main integration checks passed.");return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
