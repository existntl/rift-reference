using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace RiftReference {
// Post-game profile presentation. Unknown fields remain unknown, never synthetic scores.
public static class HomeDashboard {
 public static Rectangle HistoryScrollBounds(Rectangle bounds){return new Rectangle(bounds.Right-20,bounds.Y+116,20,Math.Max(0,VisibleMatches(bounds)*(bounds.Width-Math.Min(440,Math.Max(278,(int)(bounds.Width*.30)))-20<800?132:148)));}
 public static int VisibleMatches(Rectangle bounds){int leftW=Math.Min(440,Math.Max(278,(int)(bounds.Width*.30)));int rightW=bounds.Width-leftW-20;return Math.Max(1,(bounds.Height-148)/(rightW<800?132:148));}
 static readonly Color Red=Color.FromArgb(238,100,113);
 static void Text(Graphics g,string value,Rectangle r,float size,Color color,bool bold=false){
  if(r.Width<=0||r.Height<=0)return;
  using(var f=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular))
   TextRenderer.DrawText(g,value??"",f,r,color,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter);
 }
 static void Label(Graphics g,string value,int x,int y,int w,int h,float size,Color color,bool bold=false){Text(g,value,new Rectangle(x,y,w,h),size,color,bold);}
 static readonly Color Surface=Color.FromArgb(25,27,33);
 static readonly Color Divider=Color.FromArgb(17,19,24);
 static readonly Color Gold=Color.FromArgb(213,181,130);
 static void Card(Graphics g,Rectangle r){using(var b=new SolidBrush(Surface))g.FillRectangle(b,r);}
 static void Line(Graphics g,int x,int y,int w){using(var p=new Pen(Divider,2))g.DrawLine(p,x,y,x+w,y);}
 static string Count(int? n){return n.HasValue?n.Value.ToString():"—";}
 static string Rate(int? n,double seconds,int decimals){return n.HasValue&&seconds>0?(n.Value*60.0/seconds).ToString("F"+decimals):"—";}
 static string Percent(double? n){return n.HasValue?(n.Value*100).ToString("0")+"%":"—";}
 static bool Win(HomeMatch m){return string.Equals(m.Result,"Victory",StringComparison.OrdinalIgnoreCase);}
 static bool Loss(HomeMatch m){return string.Equals(m.Result,"Defeat",StringComparison.OrdinalIgnoreCase);}
 static string Queue(int q){return q==420?"Ranked solo":q==440?"Ranked flex":q==450?"ARAM":q==400||q==430||q==490?"Normal":"Queue "+q;}
 static string Age(DateTime? played){if(!played.HasValue)return "Time unavailable";var t=DateTime.UtcNow-played.Value.ToUniversalTime();if(t.TotalMinutes<60)return Math.Max(0,(int)t.TotalMinutes)+"m ago";if(t.TotalHours<24)return (int)t.TotalHours+"h ago";return (int)t.TotalDays+"d ago";}
 static string Champion(DataStore d,string key){try{return d.Name(key);}catch{return key??"Champion";}}
 static void Portrait(Graphics g,Rectangle r,string key,Func<string,Image> portrait){
  Image img=null;try{if(portrait!=null)img=portrait(key);}catch{}
  if(img!=null)g.DrawImage(img,r);else{using(var b=new SolidBrush(Theme.Border))g.FillRectangle(b,r);Text(g,"?",r,22,Theme.Muted,true);}
 }
 static void Metric(Graphics g,string value,string label,Rectangle r,Color color){
  Label(g,value,r.X,r.Y,r.Width,27,r.Width<133?10:12,color,true);Label(g,label,r.X,r.Y+29,r.Width,22,10,Theme.Muted);
 }
 static string GroupKda(IEnumerable<HomeMatch> matches){var rows=matches.Where(m=>m.Kills.HasValue&&m.Deaths.HasValue&&m.Assists.HasValue).ToList();if(rows.Count==0)return "— KDA";int deaths=rows.Sum(m=>m.Deaths.Value);return deaths==0?"Perfect KDA":((rows.Sum(m=>m.Kills.Value+m.Assists.Value))/(double)deaths).ToString("0.00")+" KDA";}
 static void RankBadge(Graphics g,Rectangle r,DataStore data,string tier){
  string key=(tier??"").ToLowerInvariant();
  if(!new[]{"iron","bronze","silver","gold","platinum","emerald","diamond","master","grandmaster","challenger"}.Contains(key))key="unranked";
  try{string path=Path.Combine(data.Root,"rank-badges",key+".png");if(File.Exists(path)){using(var img=Image.FromFile(path)){float scale=Math.Min(r.Width/(float)img.Width,r.Height/(float)img.Height);int w=(int)(img.Width*scale),h=(int)(img.Height*scale);g.DrawImage(img,new Rectangle(r.X+(r.Width-w)/2,r.Y+(r.Height-h)/2,w,h));}return;}}catch{}
  using(var p=new Pen(Theme.Border,2))g.DrawEllipse(p,r);
  Text(g,"—",r,26,Theme.Muted,true);
 }
 static string PointRank(RankPoint p){return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase((p.Tier??"").ToLowerInvariant())+(string.IsNullOrEmpty(p.Division)?"":" "+p.Division)+" · "+p.LP+" LP";}
 static void RankGraph(Graphics g,Rectangle r,HomeProfile profile){
  Label(g,"RANKED LADDER",r.X,r.Y,r.Width,20,9,Theme.Muted,true);
  var points=profile==null||profile.RankHistory==null?new List<RankPoint>():profile.RankHistory.Where(p=>p!=null&&p.Season==profile.Season).OrderBy(p=>p.At).ToList();
  if(points.Count<2){Label(g,points.Count==1?"First rank snapshot recorded":"No ranked snapshots yet",r.X,r.Y+36,r.Width,26,11,Theme.Ink,true);Label(g,"Progress appears as your rank changes.",r.X,r.Y+65,r.Width,24,9,Theme.Muted);return;}
  var plot=new Rectangle(r.X+5,r.Y+48,r.Width-10,r.Height-84);
  double low=points.Min(p=>p.Ladder),high=points.Max(p=>p.Ladder);double pad=Math.Max(10,(high-low)*.15);low-=pad;high+=pad;
  double span=(points[points.Count-1].At-points[0].At).TotalSeconds;
  var path=new List<PointF>();
  foreach(var p in points){float x=plot.X+(float)(span>0?(p.At-points[0].At).TotalSeconds/span:0)*plot.Width;float y=plot.Bottom-(float)((p.Ladder-low)/(high-low))*plot.Height;path.Add(new PointF(x,y));}
  using(var grid=new Pen(Color.FromArgb(45,49,57),1)){grid.DashStyle=DashStyle.Dot;for(int i=0;i<3;i++){float y=plot.Y+plot.Height*i/2f;g.DrawLine(grid,plot.X,y,plot.Right,y);}}
  using(var line=new Pen(Color.FromArgb(92,171,170),2))g.DrawLines(line,path.ToArray());
  using(var dot=new SolidBrush(Theme.Accent))foreach(var p in path)g.FillEllipse(dot,p.X-3,p.Y-3,6,6);
  Label(g,PointRank(points[0])+" → "+PointRank(points[points.Count-1]),r.X,r.Y+21,r.Width,22,r.Width<320?8:9,Theme.Ink);
  string format=points[0].At.Date==points[points.Count-1].At.Date?"HH:mm":"MMM d";
  Label(g,points[0].At.ToLocalTime().ToString(format),r.X,plot.Bottom+5,r.Width/2,18,8,Theme.Muted);
  Label(g,points[points.Count-1].At.ToLocalTime().ToString(format),r.Right-62,plot.Bottom+5,62,18,8,Theme.Muted);
  Label(g,"Recorded snapshots · rank + LP",r.X,r.Bottom-17,r.Width,18,8,Theme.Muted);
 }
 public static void Draw(Graphics g,Rectangle bounds,DataStore data,HomeProfile profile,Func<string,Image> portrait,int offset=0){
  if(bounds.Width<500||bounds.Height<200)return;
  var saved=g.Save();g.SetClip(bounds);g.SmoothingMode=SmoothingMode.AntiAlias;
  try{
   var matches=profile==null||profile.Matches==null?new List<HomeMatch>():profile.Matches.Where(m=>m!=null).Take(100).ToList();
   int gap=20,leftW=Math.Min(440,Math.Max(278,(int)(bounds.Width*.30)));int rightX=bounds.X+leftW+gap,rightW=bounds.Right-rightX;
   var rank=new Rectangle(bounds.X,bounds.Y,leftW,430);Card(g,rank);
   Label(g,"YOUR PROFILE",rank.X+26,rank.Y+19,leftW-52,22,9,Theme.Muted,true);
   Label(g,profile==null||string.IsNullOrWhiteSpace(profile.Name)?"Welcome to Rift Ready":profile.Name,rank.X+26,rank.Y+48,leftW-52,34,20,Theme.Ink,true);
   int cy=rank.Y+107,emblem=leftW<340?86:110,rx=rank.X+26+emblem+22,rw=rank.Right-rx-20;
   RankBadge(g,new Rectangle(rank.X+20,cy-15,emblem+12,emblem+12),data,profile==null?null:profile.Tier);
   Label(g,profile==null||string.IsNullOrWhiteSpace(profile.Rank)?"Rank unavailable":profile.Rank,rx,cy+13,rw,29,leftW<340?12:16,Theme.Accent,true);
   bool capped=profile!=null&&new[]{"IRON","BRONZE","SILVER","GOLD","PLATINUM","EMERALD","DIAMOND"}.Contains((profile.Tier??"").ToUpperInvariant());
   Label(g,profile!=null&&profile.LP.HasValue?profile.LP+(capped?" / 100 LP":" LP"):"— LP",rx,cy+48,rw,24,12,Theme.Ink,true);
   if(capped&&profile.LP.HasValue){using(var b=new SolidBrush(Theme.Border))g.FillRectangle(b,rx,cy+83,rw,5);using(var b=new SolidBrush(Theme.Accent))g.FillRectangle(b,rx,cy+83,(float)(rw*Math.Max(0,Math.Min(100,profile.LP.Value))/100.0),5);}
   Label(g,"RANKED SOLO · SEASON",rank.X+26,rank.Y+219,leftW-52,20,9,Theme.Muted,true);
   string record="Record unavailable";
   if(profile!=null&&profile.Wins.HasValue&&profile.Losses.HasValue){int total=profile.Wins.Value+profile.Losses.Value;record=profile.Wins+"W  "+profile.Losses+"L"+(total>0?"   "+(100.0*profile.Wins.Value/total).ToString("0")+"% wins":"");}
   Label(g,record,rank.X+26,rank.Y+240,leftW-52,27,12,Theme.Ink,true);
   RankGraph(g,new Rectangle(rank.X+26,rank.Y+279,leftW-52,135),profile);
   var performance=new Rectangle(bounds.X,rank.Bottom+gap,leftW,Math.Max(20,bounds.Bottom-rank.Bottom-gap));Card(g,performance);
   Label(g,"Performance",performance.X+26,performance.Y+16,leftW-52,29,15,Theme.Ink,true);
   Label(g,"Recent matches · All queues",performance.X+26,performance.Y+47,leftW-52,22,9,Theme.Muted);
   Line(g,performance.X,performance.Y+82,performance.Width);
   var groups=matches.GroupBy(m=>m.Champion).OrderByDescending(a=>a.Count()).ToList();int py=performance.Y+103;
   foreach(var group in groups){if(py+68>performance.Bottom-16)break;Portrait(g,new Rectangle(performance.X+26,py,54,54),group.Key,portrait);int wins=group.Count(Win);
    Label(g,Champion(data,group.Key),performance.X+94,py,leftW-(leftW>=380?180:117),25,12,Theme.Ink,true);
    int losses=group.Count(Loss),decided=wins+losses;
    Label(g,GroupKda(group),performance.X+94,py+28,leftW-180,22,10,Gold,true);
    if(leftW>=380){Label(g,decided>0?(100.0*wins/decided).ToString("0")+"%":"—",performance.Right-80,py,60,25,11,Theme.Accent,true);Label(g,wins+"W–"+losses+"L",performance.Right-80,py+28,60,22,9,Theme.Muted);}
    else Label(g,wins+"W–"+losses+"L",performance.Right-72,py+28,54,22,9,Theme.Muted);
    py+=84;
   }
   if(groups.Count==0){Label(g,"Your champion trends",performance.X+22,py,leftW-44,28,12,Theme.Ink,true);Label(g,"appear after match history loads.",performance.X+22,py+32,leftW-44,28,10,Theme.Muted);}
   var summary=new Rectangle(rightX,bounds.Y,rightW,100);Card(g,summary);
   Label(g,"Last "+matches.Count,rightX+24,summary.Y+19,130,29,15,Theme.Ink,true);
   if(matches.Count>0){int wins=matches.Count(Win),losses=matches.Count(Loss);Label(g,wins+"W–"+losses+"L · All queues",rightX+24,summary.Y+51,210,23,10,Theme.Muted,true);}
   else Label(g,"All queues",rightX+24,summary.Y+51,200,23,10,Theme.Muted);
   int count=Math.Min(3,Math.Max(0,(rightW-230)/185)),sx=summary.Right-24-count*185;
   foreach(var group in groups.Take(count)){int wins=group.Count(Win),losses=group.Count(Loss),decided=wins+losses;Portrait(g,new Rectangle(sx,summary.Y+24,50,50),group.Key,portrait);Label(g,decided>0?(100.0*wins/decided).ToString("0")+"%":"—",sx+61,summary.Y+25,54,23,11,Theme.Accent,true);Label(g,wins+"W–"+losses+"L",sx+115,summary.Y+25,64,23,9,Theme.Muted,true);Label(g,GroupKda(group),sx+61,summary.Y+52,118,23,10,Gold);sx+=185;}
   int rowY=summary.Bottom+16;
   if(matches.Count==0){var empty=new Rectangle(rightX,rowY,rightW,bounds.Bottom-rowY);Card(g,empty);int x=empty.X+34,y=empty.Y+50;
    Label(g,"Your next session starts here",x,y,rightW-68,40,22,Theme.Ink,true);
    Label(g,"Rank, recent results and champion performance in one place.",x,y+53,rightW-68,31,12,Theme.Muted);
    Label(g,"Open League of Legends to load your profile.",x,y+107,rightW-68,31,13,Theme.Accent,true);
    if(profile!=null&&!string.IsNullOrWhiteSpace(profile.Notice))Label(g,profile.Notice,x,y+155,rightW-68,48,10,Theme.Muted);
    return;
   }
   int rowH=rightW<800?132:148,visible=Math.Min(matches.Count,VisibleMatches(bounds));offset=Math.Max(0,Math.Min(offset,matches.Count-visible));
   rightW-=28;
   for(int i=0;i<visible;i++){
    HomeMatch m=matches[i+offset];var row=new Rectangle(rightX,rowY,rightW,rowH);Card(g,row);Color result=Win(m)?Theme.Accent:m.Result=="Defeat"?Red:Theme.Muted;
    int portraitSize=rightW<800?64:76,mx=row.X+portraitSize+44,mw=(row.Right-mx-20)/4;
    Portrait(g,new Rectangle(row.X+24,row.Y+24,portraitSize,portraitSize),m.Champion,portrait);
    Label(g,m.Result,mx,row.Y+21,140,30,17,result,true);
    string meta=m.Role+"  ·  "+Queue(m.Queue)+"  ·  "+Age(m.Played);
    Label(g,meta,mx+145,row.Y+24,row.Right-mx-165,25,rightW<800?9:10,Theme.Muted);
    string kda=m.Kills.HasValue&&m.Deaths.HasValue&&m.Assists.HasValue?(m.Deaths==0?"Perfect":((m.Kills.Value+m.Assists.Value)/(double)m.Deaths.Value).ToString("0.00")):"—";
    Metric(g,kda+" KDA",Count(m.Kills)+" / "+Count(m.Deaths)+" / "+Count(m.Assists),new Rectangle(mx,row.Y+64,mw,54),Gold);
    Metric(g,Rate(m.Vision,m.Duration,2)+" Vis/min",Percent(m.KillParticipation)+" KP",new Rectangle(mx+mw,row.Y+64,mw,54),Theme.Ink);
    Metric(g,Rate(m.CS,m.Duration,1)+" CS/min",Count(m.CS)+" CS",new Rectangle(mx+mw*2,row.Y+64,mw,54),Theme.Ink);
    Metric(g,Rate(m.Damage,m.Duration,0)+" Dmg/min",Percent(m.DamageShare)+" of team",new Rectangle(mx+mw*3,row.Y+64,mw,54),Theme.Ink);
    Line(g,row.X,row.Bottom-1,row.Width);rowY+=rowH;
   }
   string footer="Matches "+(offset+1)+"–"+(offset+visible)+" of "+matches.Count+" · Scroll to see more · Completed games";
   if(profile!=null&&!string.IsNullOrWhiteSpace(profile.Notice))footer+=" · "+profile.Notice;
   Label(g,footer,rightX+3,rowY,rightW-6,24,9,Theme.Muted);
  }finally{g.Restore(saved);}
 }
}
}
