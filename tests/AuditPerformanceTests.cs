using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RiftReference;
// Same deterministic, offscreen workload for published and revised binaries. Not game FPS.
class AuditPerformanceTests {
 static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
 [DllImport("user32.dll")]static extern int GetGuiResources(IntPtr process,int flag);
 static void Call(Dashboard form,string method,params object[] args){typeof(Dashboard).GetMethod(method,Hidden).Invoke(form,args);Application.DoEvents();}
 [STAThread]static int Main(){Application.EnableVisualStyles();using(var form=new Dashboard(AppDomain.CurrentDomain.BaseDirectory,true))using(var process=Process.GetCurrentProcess()){
  ((Preferences)typeof(Dashboard).GetField("prefs",Hidden).GetValue(form)).SecondMonitor=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Size=new Size(1920,1040);form.Show();var profile=new HomeProfile{Name="Performance QA",Tier="EMERALD",Rank="Emerald II",LP=30};for(int i=0;i<100;i++)profile.Matches.Add(new HomeMatch{Champion="Vayne",Role="ADC",Queue=420,Result="Victory",Played=DateTime.UtcNow.AddHours(-i)});typeof(Dashboard).GetField("state",Hidden).SetValue(form,new Snapshot{Account=profile.Name,Phase="Lobby",Home=profile});Call(form,"RefreshPageContext");Call(form,"NavigatePage","overview",true);
  using(var target=new Bitmap(form.Width,form.Height)){for(int i=0;i<3;i++)form.DrawToBitmap(target,new Rectangle(Point.Empty,form.Size));var watch=Stopwatch.StartNew();for(int i=0;i<60;i++)form.DrawToBitmap(target,new Rectangle(Point.Empty,form.Size));watch.Stop();Console.WriteLine("60 offscreen overview renders (ms): "+watch.ElapsedMilliseconds);}
  int before=GetGuiResources(process.Handle,1);for(int i=0;i<20;i++){Call(form,"NavigatePage","overview",true);var bounds=(Rectangle)typeof(Dashboard).GetProperty("HomeBounds",Hidden).GetValue(form,null);var row=HomeDashboard.RowBounds(bounds,0);Call(form,"OnMouseClick",new MouseEventArgs(MouseButtons.Left,1,row.X+20,row.Y+95,0));}var host=(Panel)typeof(Dashboard).GetField("pageHost",Hidden).GetValue(form);Console.WriteLine("Retained detail pages after 20 opens: "+host.Controls.OfType<MatchDetailPage>().Count());Console.WriteLine("Native USER objects added: "+(GetGuiResources(process.Handle,1)-before));form.Close();
 }return 0;}
}
