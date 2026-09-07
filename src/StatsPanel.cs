using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RiftReference {
public sealed class LiveOverlayStats {
 public double? Cs,CsPerMinute,Kills,Deaths,Assists,KillParticipation,Vision;
 public static LiveOverlayStats Parse(object self,object[] players,double time){var raw=J.Get(self,"scores");var s=new LiveOverlayStats{Cs=OverlayData.Number(raw,"creepScore"),Kills=OverlayData.Number(raw,"kills"),Deaths=OverlayData.Number(raw,"deaths"),Assists=OverlayData.Number(raw,"assists"),Vision=OverlayData.Number(raw,"wardScore")};
  if(time>0&&!Double.IsInfinity(time)&&!Double.IsNaN(time))s.CsPerMinute=s.Cs*60/time;
  string team=J.S(self,"team");var allies=players.Where(p=>J.S(p,"team")==team).ToArray();var identities=allies.Select(p=>J.S(p,"riotId")!=""?J.S(p,"riotId"):J.S(p,"summonerName")).ToArray();var kills=allies.Select(p=>OverlayData.Number(J.Get(p,"scores"),"kills")).ToArray();
  if(team!=""&&allies.Length==5&&identities.All(id=>id!="")&&identities.Distinct().Count()==5&&kills.All(k=>k.HasValue)&&s.Kills.HasValue&&s.Assists.HasValue){double total=kills.Sum(k=>k.Value),part=s.Kills.Value+s.Assists.Value;if(total>0&&part<=total)s.KillParticipation=part/total*100;}
  return s;
 }
}
public static class StatsPanel {
 public static LiveOverlayStats Sample(){return new LiveOverlayStats{Cs=168,CsPerMinute=7.0,Kills=6,Deaths=2,Assists=4,KillParticipation=62.5,Vision=18};}
 static string F(double? value,string format){return value.HasValue?value.Value.ToString(format,System.Globalization.CultureInfo.InvariantCulture):"—";}
 public static List<KeyValuePair<string,string>> Rows(LiveOverlayStats stats,OverlayOptions opt){var s=stats??new LiveOverlayStats();var rows=new List<KeyValuePair<string,string>>();if(opt.StatsCs)rows.Add(new KeyValuePair<string,string>("CS",F(s.Cs,"0")));if(opt.StatsCspm)rows.Add(new KeyValuePair<string,string>("CS / min",F(s.CsPerMinute,"0.0")));if(opt.StatsKda)rows.Add(new KeyValuePair<string,string>("K / D / A",F(s.Kills,"0")+" / "+F(s.Deaths,"0")+" / "+F(s.Assists,"0")));if(opt.StatsKp)rows.Add(new KeyValuePair<string,string>("Kill participation",F(s.KillParticipation,"0.0")+(s.KillParticipation.HasValue?"%":"")));if(opt.StatsVision)rows.Add(new KeyValuePair<string,string>("Vision score",F(s.Vision,"0")));return rows;}
 public static Size Size(OverlayOptions opt){return new Size(290,70+Math.Max(1,Rows(null,opt).Count)*34);}
 public static Point Position(Rectangle game,Size size,OverlayOptions opt){double u=Double.IsNaN(opt.StatsU)||Double.IsInfinity(opt.StatsU)?0:Math.Max(0,Math.Min(1,opt.StatsU)),v=Double.IsNaN(opt.StatsV)||Double.IsInfinity(opt.StatsV)?0:Math.Max(0,Math.Min(1,opt.StatsV));return new Point(game.Left+(opt.StatsPlaced?(int)Math.Round(u*Math.Max(0,game.Width-size.Width)):Math.Min(24,Math.Max(0,game.Width-size.Width))),game.Top+(opt.StatsPlaced?(int)Math.Round(v*Math.Max(0,game.Height-size.Height)):Math.Min(170,Math.Max(0,game.Height-size.Height))));}
 public static void Store(Rectangle game,Rectangle panel,OverlayOptions opt){opt.StatsPlaced=true;opt.StatsU=Math.Max(0,Math.Min(1,(panel.Left-game.Left)/(double)Math.Max(1,game.Width-panel.Width)));opt.StatsV=Math.Max(0,Math.Min(1,(panel.Top-game.Top)/(double)Math.Max(1,game.Height-panel.Height)));}
 public static void Draw(Graphics g,LiveOverlayStats stats,OverlayOptions opt){var size=Size(opt);g.Clear(Color.FromArgb(19,29,35));using(var border=new Pen(Color.FromArgb(168,146,88)))g.DrawRectangle(border,1,1,size.Width-2,size.Height-2);using(var small=new Font("Segoe UI",9))using(var label=new Font("Segoe UI",11))using(var number=new Font("Segoe UI",12,FontStyle.Bold)){g.DrawString("YOUR STATS",small,Brushes.LightGray,12,12);int y=42;var rows=Rows(stats,opt);foreach(var row in rows){g.DrawString(row.Key,label,Brushes.LightGray,12,y);using(var format=new StringFormat{Alignment=StringAlignment.Far})g.DrawString(row.Value,number,Brushes.White,new RectangleF(150,y,126,28),format);y+=34;}if(rows.Count==0)g.DrawString("Choose stats using the gear",small,Brushes.LightGray,12,y);g.DrawString("This match · live client data",small,Brushes.Gray,12,size.Height-23);}}
 public static bool Settings(IWin32Window owner,OverlayOptions opt){using(var form=new Form{Text="Stats panel settings",ClientSize=new Size(365,325),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){string[] labels={"Show stats panel","CS / min","Kill participation","Total CS","K / D / A","Vision score"};bool[] values={opt.Stats,opt.StatsCspm,opt.StatsKp,opt.StatsCs,opt.StatsKda,opt.StatsVision};var boxes=new List<CheckBox>();for(int i=0;i<labels.Length;i++){var c=new CheckBox{Text=labels[i],Checked=values[i],Left=20,Top=20+i*35,Width=310};boxes.Add(c);form.Controls.Add(c);}var save=new Button{Text="Use stats settings",Left=170,Top=268,Width=170,Height=34,DialogResult=DialogResult.OK};form.Controls.Add(save);Theme.Apply(form);if(form.ShowDialog(owner)!=DialogResult.OK)return false;opt.Stats=boxes[0].Checked;opt.StatsCspm=boxes[1].Checked;opt.StatsKp=boxes[2].Checked;opt.StatsCs=boxes[3].Checked;opt.StatsKda=boxes[4].Checked;opt.StatsVision=boxes[5].Checked;return true;}}
 public static void Copy(OverlayOptions from,OverlayOptions to){to.Stats=from.Stats;to.StatsCspm=from.StatsCspm;to.StatsKp=from.StatsKp;to.StatsCs=from.StatsCs;to.StatsKda=from.StatsKda;to.StatsVision=from.StatsVision;to.StatsPlaced=from.StatsPlaced;to.StatsU=from.StatsU;to.StatsV=from.StatsV;}
}
}
