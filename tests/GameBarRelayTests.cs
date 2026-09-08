using System;using System.IO;using System.Web.Script.Serialization;using RiftReady.GameBarBridge;
class RelayTests {
 static int n;static void Check(bool v){if(!v)throw new Exception("Relay check "+n);n++;}
 static int Main(){string dir=Path.Combine(Path.GetTempPath(),"RiftReadyRelay-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"display.json");var now=DateTime.UtcNow;var frame=new DisplaySnapshot{fresh=true};var json=new JavaScriptSerializer();
 Check(!LiveProducer.ReadFrame(path,now).fresh);
 File.WriteAllText(path,json.Serialize(new{utcTicks=now.Ticks,frame=frame}));Check(LiveProducer.ReadFrame(path,now).fresh);
 Check(!LiveProducer.ReadFrame(path,now.AddSeconds(5)).fresh);Check(!LiveProducer.ReadFrame(path,now.AddSeconds(-1)).fresh);
 File.WriteAllText(path,"{");Check(!LiveProducer.ReadFrame(path,now).fresh);
 File.WriteAllText(path,new string('x',16385));Check(!LiveProducer.ReadFrame(path,now).fresh);
 File.WriteAllText(path,json.Serialize(new{utcTicks=now.Ticks,frame=(object)null}));Check(!LiveProducer.ReadFrame(path,now).fresh);
 frame.differences=new double?[1];File.WriteAllText(path,json.Serialize(new{utcTicks=now.Ticks,frame=frame}));Check(!LiveProducer.ReadFrame(path,now).fresh);
 Console.WriteLine(n+" relay checks passed. Evidence: "+dir);return 0;
 }
}
