using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RiftReference {
public sealed class PageHistory {
 readonly List<string> entries=new List<string>{"overview"};int index;
 public string Current{get{return entries[index];}}
 public bool CanBack{get{return index>0;}}public bool CanForward{get{return index+1<entries.Count;}}
 public void Navigate(string page){if(page==Current)return;entries.RemoveRange(index+1,entries.Count-index-1);entries.Add(page);index++;}
 public string Back(){if(CanBack)index--;return Current;}public string Forward(){if(CanForward)index++;return Current;}
 public void Reset(){entries.Clear();entries.Add("overview");index=0;}
}
public static class PageControls {
 public static Label Label(Control parent,string text,int x,int y,int width,int height,float size=11,Color? color=null){var label=new Label{Text=text,Bounds=new Rectangle(x,y,width,height),Font=new Font("Segoe UI",size),ForeColor=color??Theme.Ink,AutoEllipsis=true,UseMnemonic=false};parent.Controls.Add(label);return label;}
 public static Button Button(Control parent,string text,int x,int y,int width,Action action){var button=new Button{Text=text,Bounds=new Rectangle(x,y,width,36)};button.Click+=(s,e)=>action();parent.Controls.Add(button);Theme.Apply(button);return button;}
 public static TextBox Body(Control parent,string text,Rectangle bounds){var body=new TextBox{Text=text.Replace("\n",Environment.NewLine),ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Bounds=bounds,BorderStyle=BorderStyle.None,BackColor=Theme.Panel,ForeColor=Theme.Ink,Font=new Font("Segoe UI",11)};parent.Controls.Add(body);return body;}
 public static string Kda(IEnumerable<HomeMatch> matches){var known=matches.Where(m=>m.Kills.HasValue&&m.Deaths.HasValue&&m.Assists.HasValue).ToArray();if(known.Length==0)return "—";long deaths=known.Sum(m=>(long)m.Deaths.Value);return deaths==0?"Perfect":(known.Sum(m=>(double)m.Kills+m.Assists.Value)/deaths).ToString("0.00");}
}
// Preserve the updater's integrity checks while hosting it in the main navigation.
public sealed class HostedPage : Panel {
 public readonly MinimalWindow Content;
 public HostedPage(MinimalWindow content){Content=content;Name=content.GetType().Name+"Page";AutoScroll=true;BackColor=Theme.Background;content.UsePageLayout();Controls.Add(content);content.Location=Point.Empty;content.Show();Resize+=(s,e)=>Arrange();content.SizeChanged+=(s,e)=>Arrange();}
 void Arrange(){Content.Left=Math.Max(0,(ClientSize.Width-Content.Width)/2);AutoScrollMinSize=new Size(Content.Width,Content.Height);}
 protected override void Dispose(bool disposing){if(disposing)Content.Dispose();base.Dispose(disposing);}
}
public sealed class ChampionsPage : UserControl {
 readonly DataStore data;readonly Func<string,Image> portrait;readonly Action<string> open;HomeProfile profile;
 readonly TextBox search=new TextBox{Name="ChampionSearch"};readonly RiftComboBox queue=new RiftComboBox{Name="ChampionQueue"},sort=new RiftComboBox{Name="ChampionSort"},role=new RiftComboBox{Name="ChampionRole"};
 readonly FlowLayoutPanel rows=new FlowLayoutPanel{Name="ChampionRows",AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false};
 readonly Label notice;bool catalog,loaded;
 public ChampionsPage(DataStore data,Func<string,Image> portrait,Action<string> open){this.data=data;this.portrait=portrait;this.open=open;Name="ChampionsPage";BackColor=Theme.Background;
  PageControls.Label(this,"Champions",24,14,700,45,25);PageControls.Label(this,"Your loaded match history and the bundled champion catalog. No global tier ratings are inferred.",26,62,1100,28,10,Theme.Muted);
  PageControls.Button(this,"Your champion pool",24,104,180,()=>{catalog=false;RefreshRows();});PageControls.Button(this,"Champion catalog",216,104,170,()=>{catalog=true;RefreshRows();});
  search.SetBounds(24,157,340,30);search.AccessibleName="Search champions";Controls.Add(search);queue.SetBounds(380,157,190,30);queue.Items.AddRange(new object[]{"All queues","Ranked solo","Ranked flex","ARAM"});queue.SelectedIndex=0;Controls.Add(queue);sort.SetBounds(586,157,190,30);sort.Items.AddRange(new object[]{"Most played","Win rate","Name"});sort.SelectedIndex=0;Controls.Add(sort);
  role.SetBounds(792,157,190,30);role.Items.AddRange(new object[]{"All roles","TOP","JUNGLE","MID","ADC","SUPPORT"});role.SelectedIndex=0;Controls.Add(role);role.SelectedIndexChanged+=(s,e)=>RefreshRows();
  notice=PageControls.Label(this,"",24,203,1120,26,10,Theme.Muted);Controls.Add(rows);search.TextChanged+=(s,e)=>RefreshRows();queue.SelectedIndexChanged+=(s,e)=>RefreshRows();sort.SelectedIndexChanged+=(s,e)=>RefreshRows();Resize+=(s,e)=>LayoutRows();Theme.Apply(this);
 }
 public void UpdateProfile(HomeProfile value){if(loaded&&ReferenceEquals(profile,value))return;loaded=true;profile=value;RefreshRows();}
 void LayoutRows(){rows.SetBounds(24,240,Math.Max(200,Width-48),Math.Max(100,Height-260));foreach(Control row in rows.Controls)row.Width=Math.Max(240,rows.ClientSize.Width-24);}
 void RefreshRows(){rows.SuspendLayout();foreach(Control old in rows.Controls.Cast<Control>().ToArray())old.Dispose();
  var matches=HomeData.Filter(profile,"",new[]{0,420,440,450}[Math.Max(0,queue.SelectedIndex)],100);if(role.SelectedIndex>0)matches=matches.Where(m=>m.Role==Convert.ToString(role.SelectedItem)).ToList();var keys=catalog?data.Champions.Keys.AsEnumerable():matches.Select(m=>m.Champion).Distinct();
  keys=keys.Where(k=>data.Name(k).IndexOf(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0);
  if(sort.SelectedIndex==2||catalog)keys=keys.OrderBy(k=>data.Name(k));else if(sort.SelectedIndex==1)keys=keys.OrderByDescending(k=>{var r=matches.Where(m=>m.Champion==k&&(m.Result=="Victory"||m.Result=="Defeat")).ToArray();return r.Length==0?-1:r.Count(m=>m.Result=="Victory")/(double)r.Length;}).ThenBy(k=>data.Name(k));else keys=keys.OrderByDescending(k=>matches.Count(m=>m.Champion==k)).ThenBy(k=>data.Name(k));
  foreach(var key in keys){string selected=key;var records=matches.Where(m=>m.Champion==key).ToArray();var button=new ChampionHistoryButton{Text=data.Name(key),Portrait=portrait(key),MatchCount=records.Length,Wins=records.Count(m=>m.Result=="Victory"),Losses=records.Count(m=>m.Result=="Defeat"),Height=72,Margin=new Padding(0,0,0,8),AccessibleName=data.Name(key)+" · Open champion page"};button.Click+=(s,e)=>open(selected);rows.Controls.Add(button);}
  notice.Text=catalog?"Champion catalog · Bundled data "+data.Version+" · Statistics are your loaded matches only":matches.Count+" local matches · Select a champion to view its reference and your history";if(rows.Controls.Count==0)rows.Controls.Add(new Label{Text=profile==null?"Open League to load your champion pool, or browse the champion catalog.":"No champions match these filters.",Height=70,ForeColor=Theme.Muted});LayoutRows();rows.ResumeLayout();
 }
}
public sealed class ChampionPage : UserControl {
 public ChampionPage(DataStore data,string key,HomeProfile profile,Func<string,Image> portrait,Action matches){Name="ChampionPage";Size=new Size(1200,800);BackColor=Theme.Background;AutoScroll=true;var image=portrait(key);if(image!=null)Controls.Add(new PictureBox{Image=image,Bounds=new Rectangle(24,24,80,80),SizeMode=PictureBoxSizeMode.Zoom});PageControls.Label(this,data.Name(key),126,22,850,45,27);PageControls.Label(this,String.Join(" / ",data.Tags(key))+" · Bundled data "+data.Version,128,72,850,27,10,Theme.Muted);
  PageControls.Button(this,"View recent matches",24,128,200,matches);
  var rows=HomeData.Filter(profile,"",0,100).Where(m=>m.Champion==key).ToArray();int wins=rows.Count(m=>m.Result=="Victory"),losses=rows.Count(m=>m.Result=="Defeat");PageControls.Label(this,rows.Length+" loaded matches   ·   "+wins+"W – "+losses+"L   ·   "+PageControls.Kda(rows)+" KDA",24,184,1080,32,13,Theme.Accent);
  PageControls.Label(this,"Champion reference",24,244,900,35,20);int top=298;foreach(var spell in J.A(J.Get(data.Champion(key),"spells")).Select((value,index)=>new{value,index})){PageControls.Label(this,"QWER"[spell.index]+" · "+J.S(spell.value,"name"),24,top,1100,30,15,Theme.Accent);var body=PageControls.Body(this,J.Clean(J.S(spell.value,"description")),new Rectangle(24,top+40,1100,94));body.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;top+=162;}AutoScrollMinSize=new Size(0,top+24);
 }
}
public sealed class RankHistoryPage : UserControl {
 HomeProfile profile;readonly FlowLayoutPanel rows=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};readonly RankHistoryPlot plot=new RankHistoryPlot();readonly RiftComboBox range=new RiftComboBox{Name="RankHistoryRange"};
 public RankHistoryPage(){Name="RankHistoryPage";BackColor=Theme.Background;PageControls.Label(this,"LP history",24,14,900,45,25);PageControls.Label(this,"Locally recorded rank snapshots. Earlier LP is never reconstructed from match results.",26,66,1100,30,11,Theme.Muted);range.SetBounds(24,110,220,34);range.Items.AddRange(new object[]{"Current season","Last 30 days","Last 7 days"});range.SelectedIndex=0;range.SelectedIndexChanged+=(s,e)=>RefreshPoints();Controls.Add(range);Controls.Add(plot);Controls.Add(rows);Resize+=(s,e)=>{plot.SetBounds(24,168,Math.Max(250,Width-48),Math.Max(240,Height/2));rows.SetBounds(24,plot.Bottom+18,Math.Max(250,Width-48),Math.Max(80,Height-plot.Bottom-42));foreach(Control row in rows.Controls)row.Width=Math.Max(250,rows.Width-24);};Theme.Apply(range);}
 public void UpdateProfile(HomeProfile value){if(ReferenceEquals(profile,value)&&rows.Controls.Count>0)return;profile=value;RefreshPoints();}
 void RefreshPoints(){foreach(Control old in rows.Controls.Cast<Control>().ToArray())old.Dispose();DateTime since=range.SelectedIndex==1?DateTime.UtcNow.AddDays(-30):range.SelectedIndex==2?DateTime.UtcNow.AddDays(-7):DateTime.MinValue;var points=profile==null?new List<RankPoint>():profile.RankHistory.Where(p=>p!=null&&p.Season==profile.Season&&p.At>=since).OrderBy(p=>p.At).ToList();plot.SetPoints(points);foreach(var point in points.AsEnumerable().Reverse())rows.Controls.Add(new Label{Text=point.At.ToLocalTime().ToString("MMM d, yyyy  HH:mm")+"     "+point.Tier+" "+point.Division+"     "+point.LP+" LP",Width=Math.Max(250,rows.Width-24),Height=54,BackColor=Theme.Panel,ForeColor=Theme.Ink,Padding=new Padding(16),Font=new Font("Segoe UI",12),Margin=new Padding(0,0,0,8)});if(points.Count==0)rows.Controls.Add(new Label{Text="No recorded rank history in this period. Keep Rift Ready open while using the League client.",Width=Math.Max(250,rows.Width-24),Height=80,ForeColor=Theme.Muted});}
}
public sealed class RankHistoryPlot : Control {
 List<RankPoint> points=new List<RankPoint>();PointF[] positions=new PointF[0];int selected=-1;
 public int PointCount{get{return points.Count;}}
 public RankHistoryPlot(){Name="RankHistoryPlot";BackColor=Theme.Panel;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.ResizeRedraw,true);AccessibleName="Recorded ranked ladder history";}
 public void SetPoints(List<RankPoint> value){points=value;selected=-1;Invalidate();}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);selected=positions.Length==0?-1:Enumerable.Range(0,positions.Length).OrderBy(i=>Math.Abs(positions[i].X-e.X)).First();Invalidate();}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);selected=-1;Invalidate();}
 void TextAt(Graphics g,string text,Rectangle bounds,Color color){TextRenderer.DrawText(g,text,Font,bounds,color,TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;Theme.Surface(g,ClientRectangle,Theme.Panel,Theme.Accent);positions=new PointF[points.Count];
  if(points.Count==0){TextAt(g,"Your first recorded rank snapshot will appear here.",new Rectangle(30,35,Width-60,40),Theme.Muted);return;}
  var point=points[selected>=0?Math.Min(selected,points.Count-1):points.Count-1];TextAt(g,point.At.ToLocalTime().ToString("MMM d, yyyy  HH:mm")+"   ·   "+point.Tier+" "+point.Division+"   ·   "+point.LP+" LP",new Rectangle(30,22,Width-60,36),Theme.Accent);
  var plot=new Rectangle(70,82,Math.Max(20,Width-110),Math.Max(50,Height-135));double low=points.Min(p=>p.Ladder),high=points.Max(p=>p.Ladder),padding=Math.Max(10,(high-low)*.15);low-=padding;high+=padding;double seconds=(points.Last().At-points[0].At).TotalSeconds;
  using(var grid=new Pen(Theme.Border))for(int i=0;i<5;i++){int y=plot.Y+plot.Height*i/4;g.DrawLine(grid,plot.X,y,plot.Right,y);}
  for(int i=0;i<points.Count;i++)positions[i]=new PointF(plot.X+(float)(seconds>0?(points[i].At-points[0].At).TotalSeconds/seconds:.5)*plot.Width,plot.Bottom-(float)((points[i].Ladder-low)/(high-low))*plot.Height);
  if(points.Count>1)using(var line=new Pen(Theme.Accent,2))g.DrawLines(line,positions);using(var dot=new SolidBrush(Theme.Accent))foreach(var position in positions)g.FillEllipse(dot,position.X-4,position.Y-4,8,8);
  if(selected>=0&&selected<positions.Length)using(var line=new Pen(Theme.Muted))g.DrawLine(line,positions[selected].X,plot.Top,positions[selected].X,plot.Bottom);
  TextAt(g,points[0].At.ToLocalTime().ToString("MMM d HH:mm"),new Rectangle(plot.X,plot.Bottom+12,200,28),Theme.Muted);if(points.Count>1)TextAt(g,points.Last().At.ToLocalTime().ToString("MMM d HH:mm"),new Rectangle(plot.Right-150,plot.Bottom+12,150,28),Theme.Muted);
 }
}
public sealed class LessonsPage : UserControl {
 readonly DataStore data;readonly ListBox list=new ListBox{BorderStyle=BorderStyle.None,IntegralHeight=false};readonly TextBox body;string fingerprint="";
 List<CoachingCard> lessons=new List<CoachingCard>();
 public LessonsPage(DataStore data){this.data=data;Name="LessonsPage";BackColor=Theme.Background;PageControls.Label(this,"Matchups & playbook",24,14,1000,44,25);PageControls.Label(this,"Conditional reference lessons for the current or last observed lineup—not a measured outcome or prediction.",26,64,1160,32,10,Theme.Muted);Controls.Add(list);body=PageControls.Body(this,"",Rectangle.Empty);list.SelectedIndexChanged+=(s,e)=>{if(list.SelectedIndex>=0){body.Text=lessons[list.SelectedIndex].Body.Replace("\n",Environment.NewLine);body.SelectionStart=0;body.ScrollToCaret();}};Resize+=(s,e)=>{list.SetBounds(24,126,258,Math.Max(100,Height-150));body.SetBounds(308,126,Math.Max(200,Width-338),Math.Max(100,Height-150));};Theme.Apply(this);}
 public void UpdateContext(Snapshot state){string next=state.Phase+"|"+String.Join("|",state.Players.Select(p=>p.Champion+":"+p.Role+":"+p.Team));if(next==fingerprint)return;fingerprint=next;int chosen=Math.Max(0,list.SelectedIndex);list.Items.Clear();lessons=Coaching.Lessons(data,state).ToList();foreach(var lesson in lessons)list.Items.Add(lesson.Title);if(list.Items.Count>0)list.SelectedIndex=Math.Min(chosen,list.Items.Count-1);}
}
public sealed class MatchDetailPage : UserControl {
 public MatchDetailPage(DataStore data,HomeMatch match,Action champion){Name="MatchDetailPage";BackColor=Theme.Background;Size=new Size(1200,800);AutoScroll=true;PageControls.Label(this,data.Name(match.Champion)+" · "+match.Result,24,20,1100,45,25);PageControls.Label(this,(match.Played.HasValue?match.Played.Value.ToLocalTime().ToString("f"):"Date unavailable")+" · "+Postgame.Duration(match.Duration)+" · "+(match.Role==""?"Role unavailable":match.Role),26,76,1100,30,11,Theme.Muted);PageControls.Button(this,"Champion details",24,130,190,champion);
  var grid=new TableLayoutPanel{Bounds=new Rectangle(24,204,Width-48,334),ColumnCount=3,RowCount=2,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};for(int i=0;i<3;i++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));for(int i=0;i<2;i++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,50));Controls.Add(grid);
  string[] titles={"K / D / A","CS","CHAMPION DAMAGE","KDA","VISION SCORE","KILL PARTICIPATION"},values={Number(match.Kills)+" / "+Number(match.Deaths)+" / "+Number(match.Assists),Number(match.CS),Number(match.Damage),PageControls.Kda(new[]{match}),Number(match.Vision),match.KillParticipation.HasValue?(match.KillParticipation.Value*100).ToString("0")+"%":"—"},details={"Final recorded totals",Rate(match.CS,match.Duration)+" CS / min",Rate(match.Damage,match.Duration)+" damage / min","Known kills, deaths and assists",Rate(match.Vision,match.Duration)+" vision / min",match.KillParticipation.HasValue?"From complete team totals":"Complete team totals unavailable"};
  for(int i=0;i<titles.Length;i++){var card=new SettingsCard("MatchMetric"+i,0,150,Theme.Accent){Dock=DockStyle.Fill,Margin=new Padding(0,0,12,12)};grid.Controls.Add(card);PageControls.Label(card,titles[i],20,18,310,28,10,Theme.Muted);PageControls.Label(card,values[i],20,51,310,46,23,Theme.Ink);PageControls.Label(card,details[i],20,105,310,30,10,Theme.Muted);}
  var note=PageControls.Label(this,"FINAL INVENTORY · Empty slots and unavailable slots remain distinct",24,568,1100,28,11,Theme.Muted);var itemRow=new FlowLayoutPanel{Bounds=new Rectangle(24,608,Width-48,98),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};Controls.Add(itemRow);for(int i=0;i<7;i++){int? id=match.Items!=null&&i<match.Items.Length?match.Items[i]:null;string text=!id.HasValue?"Unavailable":id==0?"Empty slot":data.Items.ContainsKey(id.Value.ToString())?J.S(data.Items[id.Value.ToString()],"name"):"Item "+id;itemRow.Controls.Add(new Label{Text=text,ForeColor=Theme.Ink,BackColor=Theme.Panel,Size=new Size(143,75),Padding=new Padding(10),Margin=new Padding(0,0,10,0)});}PageControls.Label(this,"Local-client final record · Detailed event timelines and other players' profiles are not loaded.",24,732,1100,44,10,Theme.Muted);AutoScrollMinSize=new Size(0,800);
 }
 static string Rate(int? value,double duration){return value.HasValue&&duration>0?(value.Value*60/duration).ToString("0.0"):"—";}
 static string Number(int? value){return value.HasValue?value.Value.ToString("N0"):"—";}
}
}
