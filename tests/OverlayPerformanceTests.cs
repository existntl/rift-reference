using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using RiftReference;

class OverlayPerformanceTests {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static object Field(object target,string name){return target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 [STAThread]static int Main(string[] args){try{
  Application.EnableVisualStyles();var data=new DataStore(args[0]);var opt=new OverlayOptions{Stats=true,Purchase=false,Gold=false,Buffs=false};
  using(var overlay=new GameOverlay(data,()=>opt)){
   ((Timer)Field(overlay,"timer")).Stop();
   var state=CompactOverlayVisuals.Demo();state.Overlay.Stats=StatsPanel.Sample();
   typeof(GameOverlay).GetField("state",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(overlay,state);
   var refresh=typeof(GameOverlay).GetMethod("RefreshForGame",BindingFlags.Instance|BindingFlags.NonPublic);
   var stats=(OverlayWindow)Field(overlay,"stats");int invalidations=0;stats.Invalidated+=(s,e)=>invalidations++;
   var game=new Rectangle(-30000,-30000,1920,1080);refresh.Invoke(overlay,new object[]{opt,game});Application.DoEvents();invalidations=0;
   for(int i=0;i<100;i++)refresh.Invoke(overlay,new object[]{opt,game});
   Check(invalidations==0,"Unchanged key polls repaint stats");
   state.Overlay=new OverlaySnapshot{Stats=StatsPanel.Sample()};refresh.Invoke(overlay,new object[]{opt,game});
   Check(invalidations>0,"New live sample does not repaint stats");invalidations=0;
   var moved=new Rectangle(game.X+20,game.Y+10,game.Width,game.Height);var old=stats.Location;refresh.Invoke(overlay,new object[]{opt,moved});
   Check(stats.Location==new Point(old.X+20,old.Y+10),"Game-window movement loses panel alignment");
   var previous=stats.Size;var resized=new Rectangle(game.X,game.Y,120,60);refresh.Invoke(overlay,new object[]{opt,resized});
   Check(stats.Size!=previous&&stats.Width<=120&&stats.Height<=60,"Viewport resize does not fit stats");
   var changed=opt.Copy();changed.StatsCs=true;refresh.Invoke(overlay,new object[]{changed,game});Check(stats.Height>previous.Height,"New settings do not update stats rows");
   overlay.Suspend();Check(!stats.Visible,"Suspend does not hide stats");refresh.Invoke(overlay,new object[]{changed,game});Check(stats.Visible,"Fresh foreground panel fails to reappear");
  }
  // Bounded offscreen GDI work comparison, not a measurement of League FPS or DWM.
  var natural=StatsPanel.Size(opt);var target=PanelSizing.Fit(natural,0,0,new Size(1920,1080));var sample=StatsPanel.Sample();
  using(var bitmap=new Bitmap(target.Width,target.Height))using(var graphics=Graphics.FromImage(bitmap)){
   Action paint=()=>PanelSizing.Draw(graphics,target,natural,g=>StatsPanel.Draw(g,sample,opt));for(int i=0;i<20;i++)paint();
   var watch=Stopwatch.StartNew();for(int i=0;i<1000;i++)paint();watch.Stop();double before=watch.Elapsed.TotalMilliseconds;
   watch.Restart();for(int i=0;i<100;i++)paint();watch.Stop();
   Console.WriteLine("Offscreen stats GDI workload for 100 seconds at 1Hz live input: former 1000 draws {0:0.0}ms; current 100 draws {1:0.0}ms. Not a game FPS benchmark.",before,watch.Elapsed.TotalMilliseconds);
  }
  Console.WriteLine(checks+" overlay refresh checks passed");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
