using System;using System.IO;using System.Drawing;using System.Reflection;using System.Windows.Forms;using RiftReference;
class HomeRender {
 [STAThread]static void Main(){Application.EnableVisualStyles();string root=AppDomain.CurrentDomain.BaseDirectory;var p=new HomeProfile{Name="Sample player",Rank="Emerald II",LP=64,Wins=42,Losses=38};for(int i=0;i<10;i++)p.Matches.Add(new HomeMatch{Champion=i%3==0?"Ashe":"Vayne",Result=i%3==0?"Defeat":"Victory",Queue=420,Role="ADC",Duration=1800+i*60,Played=DateTime.UtcNow.AddHours(-i-1),Kills=8+i,Deaths=3,Assists=6,CS=210+i*8,Vision=18,Damage=22000+i*900,KillParticipation=.58,DamageShare=.27});
  p.Tier="EMERALD";p.Division="II";p.Season="year:2026";int[] values={12,28,46,37,53,70,82,76,94,112,128,147,164};for(int i=0;i<values.Length;i++)p.RankHistory.Add(new RankPoint{At=DateTime.UtcNow.AddDays(i-values.Length),Tier="EMERALD",Division=values[i]>=100?"II":"III",LP=values[i]%100,Ladder=2100+values[i],Season=p.Season});
  foreach(int width in new[]{1920,1280})foreach(bool empty in new[]{false,true})using(var form=new Dashboard(root,true)){
   form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Size=new Size(width,width==1920?1040:950);form.Text="Rift Ready · SAMPLE DATA · Home preview";
   form.WindowState=FormWindowState.Normal;form.Size=new Size(width,width==1920?1040:950);form.Location=new Point(-30000,-30000);
   typeof(Dashboard).GetField("state",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,new Snapshot{Phase="Lobby",Demo=true,Account="Sample player",Home=empty?null:p});Application.DoEvents();
   using(var image=new Bitmap(width,1080))using(var shot=new Bitmap(form.Width,form.Height))using(var g=Graphics.FromImage(image)){g.Clear(Theme.Background);form.DrawToBitmap(shot,new Rectangle(Point.Empty,form.Size));g.DrawImageUnscaled(shot,0,0);image.Save(Path.Combine(root,"home-"+width+(empty?"-empty":"-sample")+".png"));}
  }
 }
}
