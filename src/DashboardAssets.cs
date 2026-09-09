using System;
using System.IO;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RiftReference {
public sealed class MinimalDialog : MinimalWindow {
 public MinimalDialog(string title,Size size){Text="Rift Ready · "+title;Size=size;BackColor=Theme.Background;ForeColor=Theme.Ink;Icon=Brand.Icon;StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;DoubleBuffered=true;UseFixedDialogChrome();}
}
public sealed class ChampionHistoryButton : Button {
 public Image Portrait;public int MatchCount,Wins,Losses;
 public ChampionHistoryButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Theme.Background);Theme.Surface(g,ClientRectangle,Theme.Panel,Theme.Accent);if(Portrait!=null)g.DrawImage(Portrait,12,10,48,48);TextRenderer.DrawText(g,Text,Font,new Rectangle(80,6,270,30),Theme.Ink,TextFormatFlags.VerticalCenter);using(var detail=new Font("Segoe UI",10)){TextRenderer.DrawText(g,MatchCount+" matches",detail,new Rectangle(80,35,260,25),Theme.Muted);int decided=Wins+Losses;TextRenderer.DrawText(g,decided==0?"— win rate":(100.0*Wins/decided).ToString("0")+"% win rate",detail,new Rectangle(370,12,190,25),Theme.Accent);TextRenderer.DrawText(g,Wins+"W – "+Losses+"L",detail,new Rectangle(370,36,190,25),Theme.Muted);}if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(4,4,Width-8,Height-8),Theme.Accent,Theme.Panel);}
}
// Only a public champion key is sent to Riot's static CDN. No account information leaves the PC.
public sealed class ChampionArtwork : IDisposable {
 readonly DataStore data;readonly Control owner;readonly HttpClient http=new HttpClient{Timeout=TimeSpan.FromSeconds(12)};
 string requested="";bool disposed;DateTime attempted;Image image;
 public string Champion{get;private set;}
 public Image Image{get{return image;}}
 public ChampionArtwork(DataStore data,Control owner){this.data=data;this.owner=owner;Champion="";}
 public void SetProfile(HomeProfile profile){
  string key=HomeData.MostPlayedChampion(profile);
  if(key==requested&&(image!=null||key==""||(DateTime.UtcNow-attempted).TotalMinutes<5))return;
  requested=key;attempted=DateTime.UtcNow;Champion="";if(image!=null){image.Dispose();image=null;}
  if(key!=""&&System.Text.RegularExpressions.Regex.IsMatch(key,"^[A-Za-z0-9]+$")&&data.Champion(key)!=null)Load(key);
 }
 async void Load(string key){
  Image loaded=null;
  try{
   string folder=Path.Combine(data.Root,"splashes"),path=Path.Combine(folder,key+".jpg");
   byte[] bytes=File.Exists(path)?await Task.Run(()=>File.ReadAllBytes(path)):null;
   if(bytes==null){bytes=await http.GetByteArrayAsync("https://ddragon.leagueoflegends.com/cdn/img/champion/splash/"+key+"_0.jpg");if(bytes.Length>8*1024*1024)return;}
   using(var stream=new MemoryStream(bytes))using(var source=System.Drawing.Image.FromStream(stream))loaded=new Bitmap(source);
   if(disposed||owner.IsDisposed||requested!=key){loaded.Dispose();return;}
   image=loaded;loaded=null;Champion=key;owner.Invalidate();
   if(!File.Exists(path))try{await Task.Run(()=>{Directory.CreateDirectory(folder);File.WriteAllBytes(path,bytes);});}catch(IOException){}catch(UnauthorizedAccessException){}
  }catch(Exception){if(loaded!=null)loaded.Dispose();/* Offline or missing artwork leaves the neutral hero intact. */}
 }
 public void Dispose(){disposed=true;http.Dispose();if(image!=null){image.Dispose();image=null;}}
}

public sealed class MatchSearch : UserControl {
 readonly TextBox input=new TextBox{BorderStyle=BorderStyle.None,BackColor=Theme.Panel,ForeColor=Theme.Ink,Font=new Font("Segoe UI",10),AccessibleName="Search recent matches"};
 readonly Label hint=new Label{Text="Search matches…",ForeColor=Theme.Muted,BackColor=Theme.Panel,Font=new Font("Segoe UI",10),AutoSize=false,Cursor=Cursors.IBeam};
 public override string Text{get{return input.Text;}set{input.Text=value;}}
 public MatchSearch(){BackColor=Theme.TitleSurface;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Controls.Add(input);Controls.Add(hint);hint.Click+=(s,e)=>input.Focus();input.TextChanged+=(s,e)=>{hint.Visible=input.Text.Length==0&&!input.Focused;OnTextChanged(e);};input.GotFocus+=(s,e)=>{hint.Visible=false;Invalidate();};input.LostFocus+=(s,e)=>{hint.Visible=input.Text.Length==0;Invalidate();};Resize+=(s,e)=>{input.SetBounds(40,10,Math.Max(1,Width-54),22);hint.Bounds=input.Bounds;};AccessibleName="Filter local matches by champion or role";}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Theme.Surface(e.Graphics,ClientRectangle,Theme.Panel,Theme.Accent);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var pen=new Pen(input.Focused?Theme.Accent:Theme.Muted,1.5f)){e.Graphics.DrawEllipse(pen,15,12,11,11);e.Graphics.DrawLine(pen,25,22,30,27);}}
}
}
