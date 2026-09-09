using System;using System.IO;using System.Drawing;using System.Reflection;using System.Windows.Forms;using RiftReference;
class HomeRender {
 static void Input(HistoryScrollBar scroll,string method,object args){typeof(HistoryScrollBar).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scroll,new[]{args});}
 static void CheckScroll(HistoryScrollBar scroll){
  scroll.Value=0;Input(scroll,"OnKeyDown",new KeyEventArgs(Keys.End));if(scroll.Value!=scroll.Limit)throw new Exception("End must reach final page");
  Input(scroll,"OnKeyDown",new KeyEventArgs(Keys.Home));Input(scroll,"OnKeyDown",new KeyEventArgs(Keys.PageDown));if(scroll.Value!=Math.Min(scroll.LargeChange,scroll.Limit))throw new Exception("Page navigation failed");
  scroll.Value=0;Input(scroll,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,10,10,-120));if(scroll.Value!=1)throw new Exception("Wheel navigation failed");
  scroll.Value=0;Input(scroll,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,10,8,0));Input(scroll,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,10,scroll.Height+50,0));Input(scroll,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,10,scroll.Height+50,0));if(scroll.Value!=scroll.Limit)throw new Exception("Drag must clamp to final page");scroll.Value=0;
 }
 [STAThread]static void Main(){Application.EnableVisualStyles();string root=AppDomain.CurrentDomain.BaseDirectory;var p=new HomeProfile{Name="Sample player",Rank="Emerald II",LP=64,Wins=42,Losses=38};for(int i=0;i<10;i++)p.Matches.Add(new HomeMatch{Champion=i%3==0?"Ashe":"Vayne",Result=i%3==0?"Defeat":"Victory",Queue=420,Role="ADC",Duration=1800+i*60,Played=DateTime.UtcNow.AddHours(-i-1),Kills=8+i,Deaths=3,Assists=6,CS=210+i*8,Vision=18,Damage=22000+i*900,KillParticipation=.58,DamageShare=.27});
  p.Tier="EMERALD";p.Division="II";p.Season="year:2026";int[] values={12,28,46,37,53,70,82,76,94,112,128,147,164};for(int i=0;i<values.Length;i++)p.RankHistory.Add(new RankPoint{At=DateTime.UtcNow.AddDays(i-values.Length),Tier="EMERALD",Division=values[i]>=100?"II":"III",LP=values[i]%100,Ladder=2100+values[i],Season=p.Season});
  foreach(var match in p.Matches)match.Items=new int?[]{3153,3006,3124,3031,0,0,3363};
  foreach(int width in new[]{1920,1280})foreach(bool empty in new[]{false,true})using(var form=new Dashboard(root,true)){
   form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Size=new Size(width,width==1920?1040:950);form.Text="Rift Ready · SAMPLE DATA · Home preview";
   ((Preferences)typeof(Dashboard).GetField("prefs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form)).SecondMonitor=false;form.Show();
   form.WindowState=FormWindowState.Normal;form.Size=new Size(width,width==1920?1040:950);form.Location=new Point(-30000,-30000);
   typeof(Dashboard).GetField("state",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,new Snapshot{Phase="Lobby",Demo=true,Account="Sample player",Home=empty?null:p});Application.DoEvents();
   bool cachedArt=File.Exists(Path.Combine(root,"data","splashes","Vayne.jpg"));using(var firstFrame=new Bitmap(form.Width,form.Height))form.DrawToBitmap(firstFrame,new Rectangle(Point.Empty,form.Size));var ready=DateTime.UtcNow.AddSeconds(2);while(DateTime.UtcNow<ready){Application.DoEvents();System.Threading.Thread.Sleep(10);}
   var art=(ChampionArtwork)typeof(Dashboard).GetField("artwork",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);if(!empty&&cachedArt&&(art.Champion!="Vayne"||art.Image==null))throw new Exception("Most-played cached artwork must load");if(empty&&art.Image!=null)throw new Exception("No history must use a neutral background");
   var layout=typeof(Dashboard).GetMethod("LayoutHistoryScroll",BindingFlags.Instance|BindingFlags.NonPublic);layout.Invoke(form,null);
   var scroll=(HistoryScrollBar)typeof(Dashboard).GetField("historyScroll",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
   if(!empty)CheckScroll(scroll);
   if(!empty){int last=scroll.Maximum-scroll.LargeChange+1;if(last<=0)throw new Exception("History must overflow");scroll.Value=last;layout.Invoke(form,null);if((int)typeof(Dashboard).GetField("homeOffset",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form)!=last)throw new Exception("Scrollbar must reach last match");scroll.Value=0;}
   using(var image=new Bitmap(width,1080))using(var shot=new Bitmap(form.Width,form.Height))using(var g=Graphics.FromImage(image)){g.Clear(Theme.Background);form.DrawToBitmap(shot,new Rectangle(Point.Empty,form.Size));g.DrawImageUnscaled(shot,0,0);image.Save(Path.Combine(root,"home-"+width+(empty?"-empty":"-sample")+".png"));}
  }
 }
}
