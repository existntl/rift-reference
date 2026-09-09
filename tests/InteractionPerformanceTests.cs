using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using RiftReference;
// Identical offscreen input/paint workload for baseline and revised builds. Not live FPS.
class InteractionPerformanceTests {
 static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(Dashboard f,string key){return typeof(Dashboard).GetField(key,Hidden).GetValue(f);}
 static void Paint(Control c,Graphics g){c.GetType().GetMethod("OnPaint",Hidden).Invoke(c,new object[]{new PaintEventArgs(g,c.ClientRectangle)});}
 [STAThread]static int Main(){Application.EnableVisualStyles();using(var f=new Dashboard(AppDomain.CurrentDomain.BaseDirectory,true)){
  ((Preferences)Get(f,"prefs")).SecondMonitor=false;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-30000,-30000);f.Size=new Size(1920,1040);f.Show();var p=new HomeProfile{Name="SAMPLE DATA · Interaction QA",Tier="EMERALD",Rank="Emerald II",LP=30};for(int i=0;i<100;i++)p.Matches.Add(new HomeMatch{Champion=i%3==0?"Ashe":"Vayne",Role="ADC",Queue=420,Result=i%2==0?"Victory":"Defeat",Played=DateTime.UtcNow.AddHours(-i),Kills=8,Deaths=4,Assists=6,CS=210,Vision=19,Damage=24000,Duration=1800,Items=new int?[]{3153,3006,3124,0,null,0,3363}});typeof(Dashboard).GetField("state",Hidden).SetValue(f,new Snapshot{Account=p.Name,Phase="Lobby",Home=p});
  using(var b=new Bitmap(f.Width,f.Height))using(var g=Graphics.FromImage(b)){
   Action paint=()=>{Paint(f,g);var field=typeof(Dashboard).GetField("homeOverview",Hidden);if(field!=null){var c=(Control)field.GetValue(f);Paint(c,g);}};paint();Application.DoEvents();paint();var watch=Stopwatch.StartNew();for(int i=0;i<60;i++){f.Location=new Point(-30000+i,-30000+i);paint();}watch.Stop();Console.WriteLine("60 move/repaint cycles (ms): "+watch.ElapsedMilliseconds);
   var scroll=(HistoryScrollBar)Get(f,"historyScroll");watch.Restart();for(int i=1;i<=60;i++){scroll.Value=i;paint();}watch.Stop();Console.WriteLine("60 history scroll/repaint cycles (ms): "+watch.ElapsedMilliseconds);
  }f.Close();
 }return 0;}
}
