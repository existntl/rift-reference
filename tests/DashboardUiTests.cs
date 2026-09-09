using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using RiftReference;
class DashboardUiTests {
 static int checks;
 static object Get(Dashboard form,string name){return typeof(Dashboard).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);}
 static void Set(Dashboard form,string name,object value){typeof(Dashboard).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,value);}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static void Paint(Dashboard form){using(var image=new Bitmap(form.Width,form.Height))form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));}
 static void WaitFor(Func<bool> ready){var end=DateTime.UtcNow.AddSeconds(3);while(!ready()&&DateTime.UtcNow<end){Application.DoEvents();System.Threading.Thread.Sleep(10);}Check(ready(),"Artwork completion timed out");}
 [STAThread]static int Main(){try{
  Application.EnableVisualStyles();string root=AppDomain.CurrentDomain.BaseDirectory;string folder=Path.Combine(root,"data","splashes");Directory.CreateDirectory(folder);
  // Deterministic offline assets, confined to the isolated test installation.
  foreach(string key in new[]{"Ahri","Ashe"})using(var image=new Bitmap(key=="Ahri"?20:30,20))using(var g=Graphics.FromImage(image)){g.Clear(key=="Ahri"?Color.Red:Color.Blue);image.Save(Path.Combine(folder,key+".jpg"),System.Drawing.Imaging.ImageFormat.Jpeg);}
  var profile=new HomeProfile{Name="UI test player"};for(int i=0;i<100;i++)profile.Matches.Add(new HomeMatch{Champion=i<70?"Ahri":"Ashe",Queue=i<70?420:450,Role=i<70?"MID":"ADC",Result=i%2==0?"Victory":"Defeat"});
  using(var form=new Dashboard(root,true)){
   var initial=(Snapshot)Get(form,"state");Check(initial.Players.Count==0&&initial.Home==null&&!initial.Demo,"Offline startup fabricated a session");
   Check(!((Timer)Get(form,"timer")).Enabled&&!((Timer)Get(form,"audioTimer")).Enabled,"Offline tools started live polling or audio");
   Check(typeof(Dashboard).GetMethod("Demo")==null&&typeof(Postgame).GetMethod("Demo")==null,"Sample session factory is still shipped");
   Check(typeof(Dashboard).GetConstructors().Single().GetParameters()[1].DefaultValue.Equals(false),"Normal launch no longer connects automatically");
   Check(!form.Controls.Cast<Control>().Any(c=>c.Text.IndexOf("demo",StringComparison.OrdinalIgnoreCase)>=0||c is RiftComboBox&&((RiftComboBox)c).Items.Cast<object>().Any(i=>i.ToString().IndexOf("demo",StringComparison.OrdinalIgnoreCase)>=0)),"Demo control remains in the app");
   ((Preferences)Get(form,"prefs")).SecondMonitor=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Size=new Size(1280,950);form.Show();Set(form,"state",new Snapshot{Phase="Lobby",Account=profile.Name,Home=profile});Paint(form);
   var art=(ChampionArtwork)Get(form,"artwork");WaitFor(()=>art.Champion=="Ahri");Check(art.Image.Width==20,"Banner did not use the most-played cached champion");
   var search=(MatchSearch)Get(form,"matchSearch");var queue=(RiftComboBox)Get(form,"historyQueue");var range=(RiftComboBox)Get(form,"historyRange");var scroll=(HistoryScrollBar)Get(form,"historyScroll");
   Check(scroll.Maximum==99,"Initial history did not include 100 rows");scroll.Value=scroll.Limit;range.SelectedIndex=2;Check(scroll.Maximum==19&&scroll.Value==0,"Range change must reset and bound the scrollbar");
   range.SelectedIndex=0;queue.SelectedIndex=3;Check(scroll.Maximum==29&&scroll.Value==0,"Queue control is not wired to history");search.Text="Ashe";Paint(form);Check(scroll.Maximum==29&&art.Champion=="Ahri","Searching the table changed the profile artwork");
   search.Text="No such champion";Paint(form);Check(!scroll.Visible&&queue.Visible&&range.Visible,"Empty search left stale rows/scrollbar or hid filters");
   search.Text="";queue.SelectedIndex=0;Paint(form);Check(scroll.Visible&&scroll.Maximum==99,"Clearing filters did not restore history");
   foreach(int width in new[]{1280,1920}){form.Width=width;Paint(form);var controls=form.Controls.Cast<Control>().Where(c=>c.Visible&&c.Top<70).ToArray();for(int i=0;i<controls.Length;i++)for(int j=i+1;j<controls.Length;j++)Check(!controls[i].Bounds.IntersectsWith(controls[j].Bounds),"Header controls overlap: "+controls[i].Text+" / "+controls[j].Text);Check(queue.Right<range.Left,"Match filters overlap");}
   form.Controls.OfType<Button>().Single(b=>b.Text=="Live game").PerformClick();Paint(form);Check(!queue.Visible&&!range.Visible&&!scroll.Visible,"History controls leaked onto live view");form.Controls.OfType<Button>().Single(b=>b.Text=="Overview").PerformClick();Paint(form);Check(queue.Visible,"Overview navigation did not return to history");
   form.Controls.OfType<Button>().Single(b=>b.Text=="Champions").PerformClick();var host=(Panel)Get(form,"pageHost");var pool=host.Controls.OfType<ChampionsPage>().Single();var list=pool.Controls.OfType<FlowLayoutPanel>().Single();var champion=list.Controls.OfType<ChampionHistoryButton>().Single(b=>b.Text=="Ashe");Check(champion.MatchCount==30,"Champion browser counts are not real history");champion.PerformClick();var detail=host.Controls.OfType<ChampionPage>().Single();detail.Controls.OfType<Button>().Single(b=>b.Text=="View recent matches").PerformClick();Check(search.Text=="Ashe","Champion page did not apply its history filter");
   var changed=new HomeProfile{Name=profile.Name};changed.Matches.Add(new HomeMatch{Champion="Ashe"});Set(form,"state",new Snapshot{Phase="Lobby",Account=changed.Name,Home=changed});Paint(form);WaitFor(()=>art.Champion=="Ashe");Check(art.Image.Width==30,"Changed profile reused previous champion art");
   Set(form,"state",new Snapshot());Paint(form);Check(art.Image==null&&art.Champion=="","Disconnect leaked the previous player's artwork");
   form.Render(Path.Combine(root,"overview-no-demo.png"));form.Show();form.Controls.OfType<Button>().Single(b=>b.Text=="Live game").PerformClick();form.Render(Path.Combine(root,"waiting-no-demo.png"));form.Close();
  }
  Console.WriteLine(checks+" dashboard control/artwork checks passed");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
