using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace RiftReference {
// The overview only summarizes identified local-client match records.
public static class HomeDashboard {
 public static int SidebarWidth(Rectangle bounds){return Math.Max(290,Math.Min(450,(int)(bounds.Width*.253)));}
 public static Rectangle SummaryBounds(Rectangle bounds){int left=SidebarWidth(bounds);return new Rectangle(bounds.X+left+20,bounds.Y,bounds.Width-left-20,180);}
 public static Rectangle TableBounds(Rectangle bounds){var summary=SummaryBounds(bounds);return new Rectangle(summary.X,summary.Bottom+16,summary.Width,Math.Max(100,bounds.Bottom-summary.Bottom-16));}
 public static int VisibleMatches(Rectangle bounds){return Math.Max(1,(TableBounds(bounds).Height-126)/94);}
 public static Rectangle HistoryScrollBounds(Rectangle bounds){var table=TableBounds(bounds);return new Rectangle(table.Right-17,table.Y+92,14,Math.Max(1,VisibleMatches(bounds)*94));}
 public static Rectangle RowBounds(Rectangle bounds,int row){var table=TableBounds(bounds);return new Rectangle(table.X+15,table.Y+92+row*94,table.Width-37,94);}
 static readonly Color Red=Color.FromArgb(245,75,101),Gold=Color.FromArgb(239,188,111);
 // A small, fixed set of UI fonts; avoid creating a native font for every table cell.
 static readonly Dictionary<int,Font> Fonts=new Dictionary<int,Font>();
 static readonly StringFormat CellFormat=new StringFormat(StringFormat.GenericTypographic){FormatFlags=StringFormatFlags.NoWrap,Trimming=StringTrimming.EllipsisCharacter,LineAlignment=StringAlignment.Center};
 static Font FontAt(float size,bool bold=false){int key=(int)(size*2)+(bold?1000:0);Font font;if(!Fonts.TryGetValue(key,out font)){font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular);Fonts.Add(key,font);}return font;}
 static void Text(Graphics g,string value,Rectangle r,float size,Color color,bool bold=false){
  if(r.Width<1||r.Height<1)return;
  // Draw directly into the retained bitmap. TextRenderer/GetHdc copies the bitmap
  // for every cell and made a scrollbar input spend hundreds of ms in rasterization.
  using(var brush=new SolidBrush(color))g.DrawString(value??"",FontAt(size,bold),brush,r,CellFormat);
 }
 static void Label(Graphics g,string value,int x,int y,int w,int h,float size,Color color,bool bold=false){Text(g,value,new Rectangle(x,y,w,h),size,color,bold);}
 static void Card(Graphics g,Rectangle r){Theme.Surface(g,r,Theme.Panel,Theme.Accent);}
 static void Line(Graphics g,int x,int y,int w){using(var p=new Pen(Theme.Border))g.DrawLine(p,x,y,x+w,y);}
 static string Count(int? n){return n.HasValue?n.Value.ToString():"—";}
 static string Rate(int? n,double seconds,int decimals){return n.HasValue&&seconds>0?(n.Value*60.0/seconds).ToString("F"+decimals):"—";}
 static string Percent(double? n){return n.HasValue?(n.Value*100).ToString("0")+"%":"—";}
 static bool Win(HomeMatch m){return String.Equals(m.Result,"Victory",StringComparison.OrdinalIgnoreCase);}
 static bool Loss(HomeMatch m){return String.Equals(m.Result,"Defeat",StringComparison.OrdinalIgnoreCase);}
 static string Queue(int q){return q==420?"Ranked solo":q==440?"Ranked flex":q==450?"ARAM":q==400||q==430||q==490?"Normal":"Queue "+q;}
 static string Age(DateTime? played){if(!played.HasValue)return "Unknown";var t=DateTime.UtcNow-played.Value.ToUniversalTime();return t.TotalMinutes<60?Math.Max(0,(int)t.TotalMinutes)+"m ago":t.TotalHours<24?(int)t.TotalHours+"h ago":(int)t.TotalDays+"d ago";}
 static string Champion(DataStore d,string key){try{return d.Name(key);}catch{return key??"Champion";}}
 static void Portrait(Graphics g,Rectangle r,string key,Func<string,Image> portrait){
  Image img=null;try{if(portrait!=null)img=portrait(key);}catch{}
  using(var b=new SolidBrush(Theme.Background))g.FillRectangle(b,r);if(img!=null)g.DrawImage(img,r);else Text(g,"?",r,18,Theme.Muted,true);
  using(var p=new Pen(Theme.Border))g.DrawRectangle(p,r);
 }
 static string Kda(IEnumerable<HomeMatch> matches){var rows=matches.Where(m=>m.Kills.HasValue&&m.Deaths.HasValue&&m.Assists.HasValue).ToList();if(rows.Count==0)return "—";long deaths=rows.Sum(m=>(long)m.Deaths.Value);return deaths==0?"Perfect":(rows.Sum(m=>(double)m.Kills.Value+m.Assists.Value)/deaths).ToString("0.00");}
 static string Average(List<HomeMatch> rows,Func<HomeMatch,int?> field,string format){var known=rows.Where(m=>field(m).HasValue).ToList();return known.Count==0?"—":known.Average(m=>(double)field(m).Value).ToString(format);}
 static string AverageRate(List<HomeMatch> rows,Func<HomeMatch,int?> field,string format){var known=rows.Where(m=>field(m).HasValue&&m.Duration>0).ToList();return known.Count==0?"—":known.Average(m=>field(m).Value*60.0/m.Duration).ToString(format);}
 static void RankBadge(Graphics g,Rectangle r,DataStore data,string tier,Func<string,Image> cached){
  if(cached!=null){var img=cached(tier);if(img!=null){float scale=Math.Min(r.Width/(float)img.Width,r.Height/(float)img.Height);int w=(int)(img.Width*scale),h=(int)(img.Height*scale);g.DrawImage(img,new Rectangle(r.X+(r.Width-w)/2,r.Y+(r.Height-h)/2,w,h));}else Text(g,"—",r,26,Theme.Muted,true);return;}
  string key=(tier??"").ToLowerInvariant();if(!new[]{"iron","bronze","silver","gold","platinum","emerald","diamond","master","grandmaster","challenger"}.Contains(key))key="unranked";
  try{string file=Path.Combine(data.Root,"rank-badges",key+".png");if(File.Exists(file)){using(var img=Image.FromFile(file)){float scale=Math.Min(r.Width/(float)img.Width,r.Height/(float)img.Height);int w=(int)(img.Width*scale),h=(int)(img.Height*scale);g.DrawImage(img,new Rectangle(r.X+(r.Width-w)/2,r.Y+(r.Height-h)/2,w,h));}return;}}catch{}
  Text(g,"—",r,26,Theme.Muted,true);
 }
 static string PointRank(RankPoint p){return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase((p.Tier??"").ToLowerInvariant())+(String.IsNullOrEmpty(p.Division)?"":" "+p.Division)+" · "+p.LP+" LP";}
 static void RankGraph(Graphics g,Rectangle r,HomeProfile profile){
  Label(g,"RANKED LADDER",r.X,r.Y,r.Width,22,9,Theme.Muted);
  var points=profile==null||profile.RankHistory==null?new List<RankPoint>():profile.RankHistory.Where(p=>p!=null&&p.Season==profile.Season).OrderBy(p=>p.At).ToList();
  if(points.Count<2){Label(g,points.Count==1?"First rank snapshot recorded":"No ranked snapshots yet",r.X,r.Y+32,r.Width,26,11,Theme.Ink);Label(g,"Progress appears as your rank changes.",r.X,r.Y+61,r.Width,24,9,Theme.Muted);return;}
  Label(g,PointRank(points[0])+" → "+PointRank(points[points.Count-1]),r.X,r.Y+22,r.Width,24,r.Width<340?9:10,Theme.Ink);
  var plot=new Rectangle(r.X+4,r.Y+59,r.Width-8,Math.Max(18,r.Height-98));double low=points.Min(p=>p.Ladder),high=points.Max(p=>p.Ladder),pad=Math.Max(10,(high-low)*.15);low-=pad;high+=pad;double span=(points[points.Count-1].At-points[0].At).TotalSeconds;var path=new List<PointF>();
  foreach(var p in points)path.Add(new PointF(plot.X+(float)(span>0?(p.At-points[0].At).TotalSeconds/span:0)*plot.Width,plot.Bottom-(float)((p.Ladder-low)/(high-low))*plot.Height));
  using(var grid=new Pen(Theme.Border))for(int i=0;i<3;i++){float y=plot.Y+plot.Height*i/2f;g.DrawLine(grid,plot.X,y,plot.Right,y);}
  using(var pen=new Pen(Theme.Accent,2))g.DrawLines(pen,path.ToArray());using(var dot=new SolidBrush(Theme.Accent))foreach(var p in path)g.FillEllipse(dot,p.X-3,p.Y-3,6,6);
  string format=points[0].At.Date==points[points.Count-1].At.Date?"HH:mm":"MMM d";
  Label(g,points[0].At.ToLocalTime().ToString(format),r.X,plot.Bottom+5,r.Width/2,18,8,Theme.Muted);Label(g,points[points.Count-1].At.ToLocalTime().ToString(format),r.Right-60,plot.Bottom+5,60,18,8,Theme.Muted);Label(g,"Recorded snapshots · rank + LP",r.X,r.Bottom-17,r.Width,18,8,Theme.Muted);
 }
 static void Profile(Graphics g,Rectangle bounds,DataStore data,HomeProfile profile,Func<string,Image> portrait,List<HomeMatch> matches,Func<string,Image> rankBadge){
  int left=SidebarWidth(bounds),rankH=Math.Min(450,Math.Max(430,bounds.Height-280));var rank=new Rectangle(bounds.X,bounds.Y,left,rankH);Card(g,rank);int x=rank.X+26,w=left-52;
  Label(g,"YOUR PROFILE",x,rank.Y+19,w,23,10,Theme.Muted);
  Label(g,profile==null||String.IsNullOrWhiteSpace(profile.Name)?"Welcome to Rift Ready":profile.Name,x,rank.Y+47,w,36,left<340?17:21,Theme.Ink,true);
  int badge=left<340?108:146;RankBadge(g,new Rectangle(rank.X+18,rank.Y+88,badge,badge),data,profile==null?null:profile.Tier,rankBadge);int rx=rank.X+badge+24,rw=rank.Right-rx-24;
  Label(g,profile==null||String.IsNullOrWhiteSpace(profile.Rank)?"Rank unavailable":profile.Rank,rx,rank.Y+116,rw,32,left<340?12:17,Theme.Accent,true);
  bool capped=profile!=null&&new[]{"IRON","BRONZE","SILVER","GOLD","PLATINUM","EMERALD","DIAMOND"}.Contains((profile.Tier??"").ToUpperInvariant());
  Label(g,profile!=null&&profile.LP.HasValue?profile.LP+(capped?" / 100 LP":" LP"):"— LP",rx,rank.Y+154,rw,27,11,Theme.Ink,true);
  if(capped&&profile.LP.HasValue){using(var brush=new SolidBrush(Theme.Border))g.FillRectangle(brush,rx,rank.Y+188,rw,5);using(var brush=new SolidBrush(Theme.Accent))g.FillRectangle(brush,rx,rank.Y+188,(float)(rw*Math.Max(0,Math.Min(100,profile.LP.Value))/100.0),5);}
  Label(g,"RANKED SOLO · SEASON",x,rank.Y+226,w,23,10,Theme.Muted);
  string record="Record unavailable";if(profile!=null&&profile.Wins.HasValue&&profile.Losses.HasValue){long total=(long)profile.Wins.Value+profile.Losses.Value;record=profile.Wins+"W   "+profile.Losses+"L"+(total>0?"   "+(100.0*profile.Wins.Value/total).ToString("0")+"% win rate":"");}Label(g,record,x,rank.Y+251,w,26,11,Theme.Ink,true);
  RankGraph(g,new Rectangle(x,rank.Y+292,w,rank.Height-307),profile);
  var performance=new Rectangle(bounds.X,rank.Bottom+16,left,bounds.Bottom-rank.Bottom-16);if(performance.Height<80)return;Card(g,performance);Label(g,"Champion performance",x,performance.Y+12,w,30,left<340?14:16,Theme.Ink,true);Label(g,"Recent matches · Current filters",x,performance.Y+42,w,24,9,Theme.Muted);
  var groups=matches.GroupBy(m=>m.Champion).OrderByDescending(a=>a.Count()).ToList();int py=performance.Y+78;
  foreach(var group in groups){if(py+58>performance.Bottom-10)break;Line(g,performance.X+1,py-4,performance.Width-2);Portrait(g,new Rectangle(x,py,48,48),group.Key,portrait);int wins=group.Count(Win),losses=group.Count(Loss),decided=wins+losses;Label(g,Champion(data,group.Key),x+64,py,w-146,23,11,Theme.Ink,true);Label(g,Kda(group)+" KDA",x+64,py+25,w-146,23,10,Gold);Label(g,decided>0?(100.0*wins/decided).ToString("0")+"%":"—",performance.Right-87,py,65,23,11,Theme.Accent,true);Label(g,wins+"W – "+losses+"L",performance.Right-100,py+25,80,23,9,Theme.Muted);py+=62;}
  if(groups.Count==0)Label(g,"Champion trends appear after matches load.",x,py,w,30,9,Theme.Muted);
 }
 static void Summary(Graphics g,Rectangle r,List<HomeMatch> matches,string scope,Func<string,Image> portrait){
  Card(g,r);int wins=matches.Count(Win),losses=matches.Count(Loss),decided=wins+losses;
  Label(g,"Last "+matches.Count,r.X+24,r.Y+25,155,36,20,Theme.Ink,true);Label(g,wins+"W – "+losses+"L",r.X+24,r.Y+71,145,26,11,Theme.Muted);Label(g,scope,r.X+24,r.Y+102,145,23,9,Theme.Muted);
  int diameter=120,ringX=r.X+180,ringY=r.Y+30;using(var pen=new Pen(Theme.Border,10))g.DrawArc(pen,ringX,ringY,diameter,diameter,0,360);if(decided>0)using(var pen=new Pen(Theme.Accent,10))g.DrawArc(pen,ringX,ringY,diameter,diameter,-90,360f*wins/decided);
  string rate=decided>0?(100.0*wins/decided).ToString("0")+"%":"—";using(var f=new Font("Segoe UI",20,FontStyle.Bold))TextRenderer.DrawText(g,rate,f,new Rectangle(ringX,ringY+29,diameter,39),Theme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);using(var f=new Font("Segoe UI",10))TextRenderer.DrawText(g,"Win rate",f,new Rectangle(ringX,ringY+70,diameter,24),Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
  int metricX=r.X+330,metricWidth=r.Width>=1160?128:Math.Max(93,(r.Width-350)/3);
  string[] values={Kda(matches),Average(matches,m=>m.CS,"0"),AverageRate(matches,m=>m.Damage,"0")},labels={"Avg KDA","Avg CS","Avg Dmg/min"},details={Average(matches,m=>m.Kills,"0.0")+" / "+Average(matches,m=>m.Deaths,"0.0")+" / "+Average(matches,m=>m.Assists,"0.0"),AverageRate(matches,m=>m.CS,"0.0")+" / min","Known match records"};
  for(int i=0;i<3;i++){int x=metricX+i*metricWidth;using(var pen=new Pen(Theme.Border))g.DrawLine(pen,x-14,r.Y+62,x-14,r.Bottom-36);Label(g,values[i],x,r.Y+59,metricWidth-18,33,18,Theme.Ink,true);Label(g,labels[i],x,r.Y+95,metricWidth-18,24,10,Theme.Muted);Label(g,details[i],x,r.Y+123,metricWidth-18,22,9,Theme.Muted);}
  int groupX=metricX+3*metricWidth+20,space=r.Right-groupX-20;if(space<260)return;Label(g,"Most played",groupX,r.Y+23,space,26,10,Theme.Muted);int count=Math.Min(3,space/143),width=space/count;
  foreach(var group in matches.GroupBy(m=>m.Champion).OrderByDescending(a=>a.Count()).Take(count)){int won=group.Count(Win),lost=group.Count(Loss),total=won+lost;Portrait(g,new Rectangle(groupX,r.Y+69,53,60),group.Key,portrait);Label(g,total==0?"—":(100.0*won/total).ToString("0")+"%",groupX+64,r.Y+69,width-64,25,11,Theme.Accent,true);Label(g,Kda(group)+" KDA",groupX+64,r.Y+97,width-64,23,9,Gold);Label(g,won+"W – "+lost+"L",groupX+64,r.Y+124,width-64,22,9,Theme.Muted);groupX+=width;}
 }
 public static string ProfileDescription(HomeProfile profile){return profile==null?"Your profile is unavailable. Open League of Legends to load your profile.":"Your profile: "+profile.Name+". "+profile.Rank+". "+Count(profile.LP)+" LP. Ranked solo season: "+Count(profile.Wins)+" wins, "+Count(profile.Losses)+" losses. Open LP history for recorded rank snapshots.";}
 public static string SummaryDescription(List<HomeMatch> rows){int wins=rows.Count(Win),losses=rows.Count(Loss);return "Current filters: "+rows.Count+" matches, "+wins+" wins, "+losses+" losses. Win rate "+(wins+losses==0?"unavailable":(100.0*wins/(wins+losses)).ToString("0")+" percent")+". Average KDA "+Kda(rows)+". Average CS "+Average(rows,m=>m.CS,"0")+". Average damage per minute "+AverageRate(rows,m=>m.Damage,"0")+".";}
 public static string MatchDescription(DataStore data,HomeMatch m){return Champion(data,m.Champion)+", "+m.Result+", "+Queue(m.Queue)+", "+(String.IsNullOrEmpty(m.Role)?"role unavailable":m.Role)+", "+(m.Played.HasValue?m.Played.Value.ToLocalTime().ToString("f"):"date unavailable")+". Kills "+Count(m.Kills)+", deaths "+Count(m.Deaths)+", assists "+Count(m.Assists)+". KDA "+Kda(new[]{m})+". CS "+Count(m.CS)+", "+Rate(m.CS,m.Duration,1)+" per minute. Vision per minute "+Rate(m.Vision,m.Duration,2)+". Damage per minute "+Rate(m.Damage,m.Duration,0)+". Team damage share "+Percent(m.DamageShare)+".";}
 public static void Draw(Graphics g,Rectangle bounds,DataStore data,HomeProfile profile,Func<string,Image> portrait,int offset=0,string search="",int queue=0,int limit=100,Func<int,Image> itemIcon=null,Func<string,Image> rankBadge=null,bool rowsOnly=false,List<HomeMatch> filtered=null){
  if(bounds.Width<600||bounds.Height<300)return;var saved=g.Save();g.SetClip(bounds);g.SmoothingMode=SmoothingMode.AntiAlias;
  try{
   var matches=filtered??HomeData.Filter(profile,search,queue,limit);var table=TableBounds(bounds);
   if(!rowsOnly){Profile(g,bounds,data,profile,portrait,matches,rankBadge);var summary=SummaryBounds(bounds);Summary(g,summary,matches,queue==0?"All queues":Queue(queue),portrait);Card(g,table);Label(g,"Recent matches",table.X+24,table.Y+16,300,35,18,Theme.Ink,true);}
   else {g.SetClip(new Rectangle(table.X+1,table.Y+92,table.Width-2,table.Height-93));using(var fill=new SolidBrush(Theme.Panel))g.FillRectangle(fill,table);}
   if(matches.Count==0){Label(g,profile==null?"Your next session starts here":"No matches match these filters",table.X+30,table.Y+117,table.Width-60,42,23,Theme.Ink,true);Label(g,profile==null?"Open League of Legends to load your profile.":"Try another champion, queue, or match range.",table.X+30,table.Y+174,table.Width-60,30,12,Theme.Accent);if(profile!=null)Label(g,profile.Notice,table.X+30,table.Y+220,table.Width-60,30,10,Theme.Muted);return;}
   bool items=table.Width>=1040;float[] fractions=items?new[]{0f,.108f,.210f,.291f,.387f,.501f,.602f,.707f,.799f}:new[]{0f,.13f,.25f,.35f,.46f,.63f,.76f,.88f};string[] headings=items?new[]{"RESULT","CHAMPION","ROLE","TIME AGO","KDA","VISION/MIN","CS/MIN","DMG/MIN","ITEMS"}:new[]{"RESULT","CHAMPION","ROLE","TIME AGO","KDA","VISION/MIN","CS/MIN","DMG/MIN"};
   var firstRow=RowBounds(bounds,0);for(int col=0;col<headings.Length;col++){int x=firstRow.X+12+(int)(fractions[col]*(firstRow.Width-24));Label(g,headings[col],x,table.Y+66,130,23,9,Theme.Muted);}Line(g,firstRow.X,table.Y+91,firstRow.Width);
   int visible=Math.Min(matches.Count,VisibleMatches(bounds));offset=Math.Max(0,Math.Min(offset,matches.Count-visible));
   for(int i=0;i<visible;i++){
    var m=matches[offset+i];var row=RowBounds(bounds,i);Color result=Win(m)?Theme.Accent:Loss(m)?Red:Theme.Muted;using(var fill=new SolidBrush(i%2==0?Color.FromArgb(10,26,34):Color.FromArgb(8,22,29)))g.FillRectangle(fill,row);
    using(var stripe=new SolidBrush(result))g.FillRectangle(stripe,row.X+4,row.Y+12,5,row.Height-24);
    int[] x=fractions.Select(f=>row.X+22+(int)(f*(row.Width-24))).ToArray();
    Label(g,m.Result,x[0],row.Y+18,x[1]-x[0]-8,30,14,result,true);Label(g,Queue(m.Queue),x[0],row.Y+50,x[1]-x[0]-8,23,9,Theme.Muted);
    Portrait(g,new Rectangle(x[1],row.Y+13,58,64),m.Champion,portrait);
    Label(g,String.IsNullOrEmpty(m.Role)?"—":m.Role,x[2],row.Y+24,x[3]-x[2]-8,32,11,Theme.Ink);
    Label(g,Age(m.Played),x[3],row.Y+25,x[4]-x[3]-8,30,10,Theme.Muted);
    Label(g,Count(m.Kills)+" / "+Count(m.Deaths)+" / "+Count(m.Assists),x[4],row.Y+16,x[5]-x[4]-8,30,12,Theme.Ink,true);string ratio=Kda(new[]{m});Label(g,ratio+" KDA",x[4],row.Y+48,x[5]-x[4]-8,24,10,ratio=="Perfect"?Gold:Theme.Muted);
    Label(g,Rate(m.Vision,m.Duration,2),x[5],row.Y+16,x[6]-x[5]-8,30,12,Theme.Ink,true);Label(g,"Vis/min",x[5],row.Y+48,x[6]-x[5]-8,24,9,Theme.Muted);
    Label(g,Rate(m.CS,m.Duration,1),x[6],row.Y+16,x[7]-x[6]-8,30,12,Theme.Ink,true);Label(g,Count(m.CS)+" CS",x[6],row.Y+48,x[7]-x[6]-8,24,9,Theme.Muted);
    int dw=items?x[8]-x[7]-8:row.Right-x[7]-10;Label(g,Rate(m.Damage,m.Duration,0),x[7],row.Y+16,dw,30,12,Theme.Ink,true);Label(g,Percent(m.DamageShare)+" of team",x[7],row.Y+48,dw,24,9,Theme.Muted);
    if(items){int size=Math.Min(34,(row.Right-x[8]-15)/7-3);for(int slot=0;slot<7;slot++){var box=new Rectangle(x[8]+slot*(size+3),row.Y+(row.Height-size)/2,size,size);using(var fill=new SolidBrush(Theme.Background))g.FillRectangle(fill,box);int? id=m.Items!=null&&slot<m.Items.Length?m.Items[slot]:null;if(id.HasValue&&id>0){Image icon=itemIcon==null?null:itemIcon(id.Value);if(icon!=null)g.DrawImage(icon,box);else Text(g,id.Value.ToString(),box,7,Theme.Muted);}else if(!id.HasValue)Text(g,"—",box,9,Theme.Muted);using(var border=new Pen(Theme.Border))g.DrawRectangle(border,box);}}
    Line(g,row.X,row.Bottom-1,row.Width);
   }
   Label(g,"Matches "+(offset+1)+"–"+(offset+visible)+" of "+matches.Count+"  ·  Select a row for details  ·  ↑ ↓ browse / Enter open",table.X+24,table.Bottom-34,table.Width-48,25,9,Theme.Muted);
  }finally{g.Restore(saved);}
 }
}
}
