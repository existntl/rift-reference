using System;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Windows.Forms;
using RiftReference;

// Own-process lifecycle checks. No key injection, League access, or visible windows.
class NativeOverlayLifecycleTests {
 static int checks;
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static object Field(object owner,string name){return owner.GetType().GetField(name,Hidden).GetValue(owner);}
 static void Set(object owner,string name,object value){owner.GetType().GetField(name,Hidden).SetValue(owner,value);}
 static void Refresh(GameOverlay overlay,OverlayOptions options,Rectangle game){typeof(GameOverlay).GetMethod("RefreshForGame",Hidden).Invoke(overlay,new object[]{options,game});}
 static void HiddenWindows(GameOverlay overlay){foreach(string name in new[]{"gold","details","buffs","stats"})Check(!((OverlayWindow)Field(overlay,name)).Visible,"Native-only mode showed "+name);}
 [STAThread]static int Main(string[] args){
  string prior=Environment.GetEnvironmentVariable("RIFT_READY_RENDER_SESSION");
  try{
   Application.EnableVisualStyles();
   var data=new DataStore(args.Length>0?args[0]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data"));
   string session=Guid.NewGuid().ToString("N");Environment.SetEnvironmentVariable("RIFT_READY_RENDER_SESSION",session);
   var opt=new OverlayOptions{Enabled=true,Purchase=true,Gold=false,Buffs=false,Stats=false,Key=0,Champion="Vayne",Target=3153};
   var overlay=new GameOverlay(data,()=>opt,false);
   try{
    ((Timer)Field(overlay,"timer")).Stop();
    Check(Field(overlay,"nativeBridge")!=null,"Opt-in bridge was not created");
    using(var map=MemoryMappedFile.OpenExisting(NativeOverlayBridge.MapName(session),MemoryMappedFileRights.Read))
    using(var view=map.CreateViewAccessor(0,NativeOverlayBridge.Capacity,MemoryMappedFileAccess.Read)){
     var state=CompactOverlayVisuals.Demo();state.Demo=false;Set(overlay,"state",state);Set(overlay,"received",DateTime.UtcNow);
     var game=new Rectangle(1920,0,1920,1080);
     Refresh(overlay,opt,game);
     Check(view.ReadUInt32(28)==1&&view.ReadUInt32(8)>0,"Initial visible frame was not published");
     Check(view.ReadUInt32(12)==1920&&view.ReadUInt32(16)==1080,"Viewport dimensions are wrong");
     HiddenWindows(overlay);
     uint seq=view.ReadUInt32(8);
     for(int i=0;i<10;i++)Refresh(overlay,opt,game);
     Check(view.ReadUInt32(8)==seq,"Unchanged key polls reuploaded texture");
     var sample=CompactOverlayVisuals.Demo();sample.Overlay.Gold=1234;state.Overlay=sample.Overlay;
     Refresh(overlay,opt,game);Check(view.ReadUInt32(8)==seq+1,"New live sample did not publish");seq=view.ReadUInt32(8);
     // Model the cached prior key-down state; Key=0 deterministically observes
     // key-up without synthesizing keyboard events on the user's computer.
     Set(overlay,"nativeHeld",true);Refresh(overlay,opt,game);
     Check(view.ReadUInt32(8)==seq+1,"Cached key transition did not publish");seq=view.ReadUInt32(8);
     overlay.Suspend();Check(view.ReadUInt32(28)==0,"Suspend left native frame visible");HiddenWindows(overlay);
     Refresh(overlay,opt,game);Check(view.ReadUInt32(28)==1&&view.ReadUInt32(8)==seq+1,"Resume did not republish");
     var hidden=opt.Copy();hidden.Purchase=false;hidden.Gold=true;hidden.Key=0;seq=view.ReadUInt32(8);
     Refresh(overlay,hidden,game);
     Check(view.ReadUInt32(28)==0,"Gold-only mode kept an empty frame visible with key up");
     Check(view.ReadUInt32(8)==seq,"Hidden-only poll unnecessarily uploaded pixels");
     Refresh(overlay,opt,game);Check(view.ReadUInt32(28)==1&&view.ReadUInt32(8)>seq,"Restoring enabled panel did not publish");
     Refresh(overlay,opt,new Rectangle(-1600,40,1600,900));
     Check(view.ReadUInt32(12)==1600&&view.ReadUInt32(16)==900,"Resize did not update transport dimensions");HiddenWindows(overlay);
     Set(overlay,"received",DateTime.UtcNow.AddSeconds(-5));
     typeof(GameOverlay).GetMethod("Refresh",Hidden).Invoke(overlay,null);
     Check(view.ReadUInt32(28)==0,"Stale data left native frame visible");
     Refresh(overlay,opt,game);overlay.Dispose();overlay=null;
     Check(view.ReadUInt32(28)==0,"Disposal left native frame visible");
    }
   }finally{if(overlay!=null)overlay.Dispose();}
   Console.WriteLine(checks+" native overlay lifecycle checks passed");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
  finally{Environment.SetEnvironmentVariable("RIFT_READY_RENDER_SESSION",prior);}
 }
}
