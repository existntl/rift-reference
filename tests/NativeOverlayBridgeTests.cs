using System;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Windows.Forms;
using RiftReference;
class NativeOverlayBridgeTests {
 static int checks;
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
 [STAThread]static int Main(string[] args){try{
  Application.EnableVisualStyles();
  string session=Guid.NewGuid().ToString("N");
  using(var bridge=new NativeOverlayBridge(session))using(var map=MemoryMappedFile.OpenExisting(NativeOverlayBridge.MapName(session),MemoryMappedFileRights.Read))using(var view=map.CreateViewAccessor(0,NativeOverlayBridge.Capacity,MemoryMappedFileAccess.Read)){
   var pixels=new byte[]{1,2,3,255,4,5,6,128};Check(bridge.Publish(pixels,2,1,true),"publish");
   Check(view.ReadUInt32(0)==NativeOverlayBridge.Magic&&view.ReadUInt32(4)==1,"protocol");
   Check(view.ReadUInt32(12)==2&&view.ReadUInt32(20)==8&&view.ReadUInt32(24)==8,"dimensions");
   Check(view.ReadByte(64)==1&&view.ReadByte(71)==128,"BGRA payload");
   bridge.Heartbeat(false);Check(view.ReadUInt32(28)==0,"hide");
   uint seq=view.ReadUInt32(8);bridge.Heartbeat(true);Check(view.ReadUInt32(8)==seq&&view.ReadUInt32(28)==1,"heartbeat avoids upload");
   bool invalid=false;try{bridge.Publish(new byte[4],2,1,true);}catch(ArgumentException){invalid=true;}Check(invalid,"reject wrong payload");
   bool collision=false;try{using(var duplicate=new NativeOverlayBridge(session)){} }catch(InvalidOperationException){collision=true;}Check(collision,"reject session collision");
   using(var gate=Mutex.OpenExisting(NativeOverlayBridge.MutexName(session))){var ready=new ManualResetEvent(false);var release=new ManualResetEvent(false);var t=new Thread(()=>{gate.WaitOne();ready.Set();release.WaitOne();gate.ReleaseMutex();});t.Start();ready.WaitOne();try{Check(!bridge.Publish(pixels,2,1,true),"busy reader must not block producer");}finally{release.Set();t.Join();ready.Dispose();release.Dispose();}}
  }
  using(var panel=new OverlayWindow()){panel.Bounds=new Rectangle(101,201,2,2);panel.Opacity=.5;panel.TransparencyKey=Color.Magenta;panel.Painter=g=>{g.Clear(Color.Magenta);g.FillRectangle(Brushes.Red,0,0,1,1);};
   using(var image=NativeOverlayBridge.Compose(new Size(4,4),new[]{panel},new[]{true},new Point(100,200))){Check(image.GetPixel(0,0).A==0,"background transparent");Check(image.GetPixel(1,1).R>240&&Math.Abs(image.GetPixel(1,1).A-128)<=2,"straight alpha and opacity");Check(image.GetPixel(2,2).A==0,"chroma key transparent");}
   using(var image=NativeOverlayBridge.Compose(new Size(4,4),new[]{panel},new[]{false})){Check(image.GetPixel(1,1).A==0,"disabled panels omitted");}
  }
  var root=AppDomain.CurrentDomain.BaseDirectory;var data=new DataStore(Path.Combine(root,"data"));var state=CompactOverlayVisuals.Demo();var options=new OverlayOptions{Target=3153,Champion="Vayne",BoardY=100,RowStart=244,RowGap=75};
  using(var visual=new CompactOverlayVisuals(data))using(var board=new OverlayWindow())using(var build=new OverlayWindow())using(var buffs=new OverlayWindow()){
   var size=new Size(1920,1080);visual.Configure(state,options,size,board,build,buffs);board.Bounds=ScoreboardLayout.Bounds(new Rectangle(Point.Empty,size),options);board.TransparencyKey=Color.Magenta;board.Opacity=1;
   build.Location=PanelPositions.Build(new Rectangle(Point.Empty,size),build.Size,options);buffs.Location=new Point((size.Width-buffs.Width)/2,24);buffs.TransparencyKey=Theme.Background;
   using(var frame=NativeOverlayBridge.Compose(size,new[]{board,build,buffs},new[]{true,true,true})){
    frame.Save(Path.Combine(root,"native-overlay-sample.png"));var pixels=NativeOverlayBridge.Pixels(frame);File.WriteAllBytes(Path.Combine(root,"native-overlay-sample.bgra"),pixels);
    if(args.Length==2&&args[0]=="--serve")using(var writer=new NativeOverlayBridge(args[1])){writer.Publish(pixels,size.Width,size.Height,true);Console.WriteLine("Sample frame ready");for(int i=0;i<600;i++){writer.Heartbeat(true);Thread.Sleep(100);}}
   }
  }
  Console.WriteLine(checks+" native bridge checks passed");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
