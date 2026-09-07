using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace RiftReference {
public sealed class OverlayOptions {
 public bool Enabled,Gold=true,Buffs=true,Purchase=true;
 public int Key=9,X=24,Y=160,Target;
 public int BoardX=0,BoardY=210,BoardScale=100,RowStart=160,RowGap=88;
 public bool AlliesLeft=true;
 public bool Stats,StatsCspm=true,StatsKp=true,StatsCs,StatsKda,StatsVision,StatsPlaced;
 public double StatsU,StatsV;
 public int PanelLayoutVersion;
 public bool BuildPlaced,BuffsPlaced;
 public double BuildU,BuildV,BuffsU,BuffsV;
 public int PanelSizeVersion;public int BuildOpacity=96,BuffsOpacity=96,StatsOpacity=96;
 public int BuildWidth,BuildHeight,BuffsWidth,BuffsHeight,StatsWidth,StatsHeight;
 public string Champion="",PlanPatch="",Source="Manual selection";
 public OverlayOptions Copy(){var result=(OverlayOptions)MemberwiseClone();PanelSizing.Migrate(result);return result;}
}
public static class OverlayStorage {
 public static void Save(string home,OverlayOptions options){PanelSizing.Migrate(options);string path=Path.Combine(home,"overlay.json"),temp=path+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(options),System.Text.Encoding.UTF8);if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
}
public sealed class BuffWindow { public string Name,Team;public double Ends;public static int Duration(string name){return name=="Elder"?150:180;} }
public sealed class OverlaySnapshot {
 public LiveOverlayStats Stats=new LiveOverlayStats();
 public double? AllyTotal,EnemyTotal;public List<GoldPair> Pairs=new List<GoldPair>();
 public double? Gold,SelfValue,EnemyValue;public string Opponent="",Champion="",Patch="";
 public bool InventoryKnown;public List<int> Inventory=new List<int>();public List<BuffWindow> Buffs=new List<BuffWindow>();
}
public sealed class PurchaseCost { public double Remaining;public bool Owned;public List<int> Missing=new List<int>(); }
public static class OverlayData {
 public static double? Number(object raw,string key){double value;return J.Get(raw,key)!=null&&Double.TryParse(J.S(raw,key),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value)&&!Double.IsNaN(value)&&!Double.IsInfinity(value)&&value>=0?(double?)value:null;}
 static string Identity(object raw){string id=J.S(raw,"riotId");return id!=""?id:J.S(raw,"summonerName");}
 public static List<int> Inventory(object raw){
  var items=J.Get(raw,"items") as object[];if(items==null)return null;var result=new List<int>();
  foreach(var item in items){var id=Number(item,"itemID");var count=Number(item,"count");if(!id.HasValue||!count.HasValue||id!=Math.Floor(id.Value)||count!=Math.Floor(count.Value)||count>20||id>Int32.MaxValue)return null;if(id==0)continue;for(int n=0;n<(int)count.Value;n++)result.Add((int)id.Value);}
  return result;
 }
 public static double? Value(DataStore data,List<int> inventory){if(inventory==null)return null;double total=0;foreach(int id in inventory){object item;if(!data.Items.TryGetValue(id.ToString(),out item))return null;var gold=Number(J.Get(item,"gold"),"total");if(!gold.HasValue)return null;total+=gold.Value;}return total;}
 public static OverlaySnapshot Parse(DataStore data,object root,Snapshot state){
  var result=new OverlaySnapshot();if(state.Phase!="In game"||state.Mode!="CLASSIC"||J.N(J.Get(root,"gameData"),"mapNumber")!=11)return result;
  var active=J.Get(root,"activePlayer");string id=Identity(active);if(id=="")return result;
  var players=J.A(J.Get(root,"allPlayers"));var selves=players.Where(p=>Identity(p)==id).ToArray();if(selves.Length!=1)return result;var self=selves[0];string team=J.S(self,"team");if(team!="ORDER"&&team!="CHAOS")return result;
  result.Champion=data.Resolve(J.S(self,"championName"));result.Patch=J.S(J.Get(root,"gameData"),"gameVersion");result.Gold=Number(active,"currentGold");
  var inventory=Inventory(self);result.InventoryKnown=inventory!=null;if(inventory!=null)result.Inventory=inventory;result.SelfValue=Value(data,inventory);
  string role=LaneView.Role(J.S(self,"position"));var enemies=players.Where(p=>J.S(p,"team")==(team=="ORDER"?"CHAOS":"ORDER")&&role!=""&&LaneView.Role(J.S(p,"position"))==role).ToArray();
  if(enemies.Length==1){result.Opponent=data.Name(J.S(enemies[0],"championName"));result.EnemyValue=Value(data,Inventory(enemies[0]));}
  ScoreboardData.Populate(data,players,team,result);
  result.Stats=LiveOverlayStats.Parse(self,players,state.Time);
  // Rebuild from the current event history: no state or timestamps survive a new match.
  foreach(string kind in new[]{"Baron","Elder"}){
   var evt=J.A(J.Get(J.Get(root,"events"),"Events")).Where(e=>(kind=="Baron"?J.S(e,"EventName")=="BaronKill":J.S(e,"EventName")=="DragonKill"&&J.S(e,"DragonType")=="Elder")&&Number(e,"EventTime").HasValue&&J.N(e,"EventTime")<=state.Time).OrderByDescending(e=>J.N(e,"EventTime")).FirstOrDefault();
   if(evt==null||state.Time>=J.N(evt,"EventTime")+BuffWindow.Duration(kind))continue;
   string killer=J.S(evt,"KillerName");var owners=players.Where(p=>killer!=""&&(Identity(p)==killer||J.S(p,"summonerName")==killer)).ToArray();string owner=owners.Length==1?J.S(owners[0],"team"):"";
   result.Buffs.Add(new BuffWindow{Name=kind,Team=owner==team?"Allies":owner==(team=="ORDER"?"CHAOS":"ORDER")?"Enemies":"Team unknown",Ends=J.N(evt,"EventTime")+BuffWindow.Duration(kind)});
  }
  return result;
 }
 public static PurchaseCost Cost(DataStore data,int target,List<int> owned){
  if(owned==null)throw new ArgumentException("Inventory unavailable.");var pool=new List<int>(owned);var result=new PurchaseCost();result.Owned=pool.Contains(target);
  result.Remaining=Remaining(data,target,pool,new HashSet<int>(),result.Missing);return result;
 }
 static double Remaining(DataStore data,int id,List<int> pool,HashSet<int> path,List<int> missing){
  if(pool.Remove(id))return 0;object item;if(!path.Add(id)||path.Count>12||!data.Items.TryGetValue(id.ToString(),out item))throw new ArgumentException("Recipe unavailable.");
  var total=Number(J.Get(item,"gold"),"total");if(!total.HasValue)throw new ArgumentException("Price unavailable.");
  double credit=0;var children=J.A(J.Get(item,"from"));
  foreach(var raw in children){int child;if(!Int32.TryParse(Convert.ToString(raw),out child))throw new ArgumentException("Recipe unavailable.");object childItem;if(!data.Items.TryGetValue(child.ToString(),out childItem))throw new ArgumentException("Recipe unavailable.");var price=Number(J.Get(childItem,"gold"),"total");if(!price.HasValue)throw new ArgumentException("Price unavailable.");var leaves=new List<int>();double cost=Remaining(data,child,pool,path,leaves);credit+=price.Value-cost;if(cost>0)missing.Add(child);}
  path.Remove(id);return Math.Max(0,total.Value-credit);
 }
 public static bool Purchasable(DataStore data,int id){object item;return data.Items.TryGetValue(id.ToString(),out item)&&J.Get(J.Get(item,"gold"),"purchasable") is bool&&(bool)J.Get(J.Get(item,"gold"),"purchasable")&&J.Get(J.Get(item,"maps"),"11") is bool&&(bool)J.Get(J.Get(item,"maps"),"11");}
 public static string GoldText(OverlaySnapshot s){if(!s.SelfValue.HasValue||!s.EnemyValue.HasValue)return "Lane comparison unavailable";double diff=s.SelfValue.Value-s.EnemyValue.Value;return (diff>0?"+":"")+diff.ToString("0")+"g vs "+s.Opponent;}
 public static string Duration(double seconds){int n=(int)Math.Ceiling(Math.Max(0,seconds));return (n/60)+"m "+(n%60).ToString("00")+"s";}
 public static bool Fresh(Snapshot s,DateTime now,DateTime received){return !s.Demo&&s.Phase=="In game"&&s.Mode=="CLASSIC"&&s.Overlay!=null&&(now-received).TotalSeconds>=0&&(now-received).TotalSeconds<=4;}
}

// Desktop windows only. No injection, memory reads, input interception or game writes.
public sealed class OverlayWindow:Form {
 public Action<Graphics> Painter;
 public string Heading="",Main="",Detail="";public List<string> Rows=new List<string>();
 readonly Font heading=new Font("Segoe UI",9,FontStyle.Bold),main=new Font("Segoe UI",17,FontStyle.Bold),body=new Font("Segoe UI",10);
 public OverlayWindow(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Theme.Panel;ForeColor=Theme.Ink;DoubleBuffered=true;AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(340,250);Opacity=0.96;}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ExStyle|=0x08000000|0x00000020|0x00080000|0x00000080;return cp;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x0084){m.Result=new IntPtr(-1);return;}if(m.Msg==0x0021){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Draw(e.Graphics);}
 public void Draw(Graphics g){if(Painter!=null){Painter(g);return;}g.Clear(Theme.Panel);using(var p=new Pen(Theme.Border))g.DrawRectangle(p,0,0,ClientSize.Width-1,ClientSize.Height-1);using(var b=new SolidBrush(Theme.Accent))g.FillRectangle(b,0,0,3,ClientSize.Height);DrawText(g,Heading,heading,Theme.Accent,14,12,25);DrawText(g,Main,main,Theme.Ink,14,40,58);int y=104;foreach(var row in Rows){DrawText(g,row,body,Theme.Ink,14,y,40);y+=43;}DrawText(g,Detail,body,Theme.Muted,14,ClientSize.Height-55,47);}
 void DrawText(Graphics g,string text,Font font,Color color,int x,int y,int height){TextRenderer.DrawText(g,text,font,new Rectangle(x,y,ClientSize.Width-28,height),color,TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);}
 protected override void Dispose(bool disposing){if(disposing){heading.Dispose();main.Dispose();body.Dispose();}base.Dispose(disposing);}
}
public sealed class GameOverlay:IDisposable {
 [StructLayout(LayoutKind.Sequential)]struct Rect {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct NativePoint {public int X,Y;}
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr window,out Rect rect);
 [DllImport("user32.dll")]static extern bool ClientToScreen(IntPtr window,ref NativePoint point);
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
 readonly DataStore data;readonly Func<OverlayOptions> options;readonly Timer timer=new Timer{Interval=100};
 readonly OverlayWindow gold=new OverlayWindow(),details=new OverlayWindow(),buffs=new OverlayWindow(),stats=new OverlayWindow();readonly CompactOverlayVisuals visuals;Snapshot state=new Snapshot();DateTime received;OverlayOptions lastOptions;OverlaySnapshot lastData;Size lastGameSize;
 public GameOverlay(DataStore d,Func<OverlayOptions> get){data=d;options=get;visuals=new CompactOverlayVisuals(d);gold.TransparencyKey=Color.Magenta;gold.BackColor=Color.Magenta;gold.Opacity=1;buffs.TransparencyKey=Theme.Background;timer.Tick+=(s,e)=>Refresh();timer.Start();}
 public void Update(Snapshot value){if(Object.ReferenceEquals(state,value))return;state=value;received=DateTime.UtcNow;lastData=null;Refresh();}
 public void Suspend(){gold.Hide();details.Hide();buffs.Hide();stats.Hide();}
 static bool GameBounds(out Rectangle bounds){bounds=Rectangle.Empty;try{IntPtr window=GetForegroundWindow();uint id;GetWindowThreadProcessId(window,out id);using(var p=Process.GetProcessById((int)id)){if(!p.ProcessName.Equals("League of Legends",StringComparison.OrdinalIgnoreCase))return false;}Rect r;var origin=new NativePoint();if(!GetClientRect(window,out r)||!ClientToScreen(window,ref origin))return false;bounds=new Rectangle(origin.X,origin.Y,r.Right-r.Left,r.Bottom-r.Top);return bounds.Width>=800&&bounds.Height>=600;}catch{return false;}}
 void Refresh(){var opt=options();Rectangle game;if(!opt.Enabled||!OverlayData.Fresh(state,DateTime.UtcNow,received)||!GameBounds(out game)){Suspend();return;}
  PanelSizing.Migrate(opt);details.Opacity=PanelTransparency.Clamp(opt.BuildOpacity)/100.0;buffs.Opacity=PanelTransparency.Clamp(opt.BuffsOpacity)/100.0;stats.Opacity=PanelTransparency.Clamp(opt.StatsOpacity)/100.0;
  if(lastData!=state.Overlay||lastOptions!=opt||lastGameSize!=game.Size){visuals.Configure(state,opt,game.Size,gold,details,buffs);lastData=state.Overlay;lastOptions=opt;lastGameSize=game.Size;}
  gold.Bounds=ScoreboardLayout.Bounds(game,opt);
  details.Location=PanelPositions.Build(game,details.Size,opt);
  bool held=opt.Key>0&&opt.Key<256&&(GetAsyncKeyState(opt.Key)&0x8000)!=0;
  if(opt.Gold&&held){if(!gold.Visible)gold.Show();}else gold.Hide();
  if(opt.Purchase){if(!details.Visible)details.Show();}else details.Hide();
  buffs.Location=new Point(game.Left+Math.Max(0,(game.Width-buffs.Width)/2),game.Top+24);
  if(opt.Buffs&&BuffCards.Active(state).Count>0){if(!buffs.Visible)buffs.Show();}else buffs.Hide();
  stats.ClientSize=PanelSizing.Fit(StatsPanel.Size(opt),opt.StatsWidth,opt.StatsHeight,game.Size);stats.Painter=g=>PanelSizing.Draw(g,stats.ClientSize,StatsPanel.Size(opt),p=>StatsPanel.Draw(p,state.Overlay.Stats,opt));stats.Location=StatsPanel.Position(game,stats.Size,opt);if(opt.Stats){if(!stats.Visible)stats.Show();stats.Invalidate();}else stats.Hide();
 }
 public static void Fill(DataStore d,Snapshot state,OverlayOptions opt,OverlayWindow gold,OverlayWindow details){
  var s=state.Overlay??new OverlaySnapshot();gold.ClientSize=new Size(420,156);gold.Heading="RIFT READY  /  LANE ITEM VALUE";gold.Main=OverlayData.GoldText(s);gold.Rows.Clear();gold.Detail="Inventory value estimate · excludes unspent gold\nData "+d.Version+" · patch match unverified";
  details.Heading="RIFT READY  /  "+(opt.Purchase?"PURCHASE PLAN":"OBJECTIVE BUFFS");details.Rows.Clear();details.Main="";
  if(opt.Buffs)foreach(var buff in s.Buffs)details.Rows.Add(buff.Name+" · "+buff.Team+" · "+OverlayData.Duration(buff.Ends-state.Time)+" estimated");
  if(opt.Purchase){object item;bool valid=opt.Target!=0&&d.Items.TryGetValue(opt.Target.ToString(),out item)&&opt.Champion==s.Champion;
   if(!valid){details.Main="Choose an item target";details.Rows.Add("Overlay settings → select your champion and item, or load a saved plan.");}
   else {item=d.Items[opt.Target.ToString()];details.Main=J.S(item,"name");try{if(!s.InventoryKnown||!s.Gold.HasValue)throw new ArgumentException();var cost=OverlayData.Cost(d,opt.Target,s.Inventory);double need=Math.Max(0,Math.Ceiling(cost.Remaining-s.Gold.Value));details.Rows.Add(cost.Owned?"Already owned":need==0?"Enough gold · check shop availability":need.ToString("0")+"g to go · "+Math.Floor(s.Gold.Value).ToString("0")+"g on hand");
    foreach(int component in cost.Missing.Distinct().Take(3)){var c=OverlayData.Cost(d,component,s.Inventory);double gap=Math.Max(0,Math.Ceiling(c.Remaining-s.Gold.Value));details.Rows.Add(J.S(d.Items[component.ToString()],"name")+" · "+(gap==0?"enough gold":gap.ToString("0")+"g to go"));}
   }catch(ArgumentException){details.Rows.Add("Cost unavailable · waiting for inventory / price data");}}
  }else details.Main="Team buff windows";
  details.Detail=opt.Purchase?"Estimates · data "+d.Version+" · patch unverified\nManual plan; Probuilds feed not connected":"Estimated expiry from objective kill time.\nIndividual buffs can end earlier on death.";
  if(opt.Purchase&&opt.Buffs&&s.Buffs.Count>0)details.Rows.Add("Buffs: team windows; holders may die earlier.");
  details.ClientSize=new Size(365,170+43*details.Rows.Count);gold.Invalidate();details.Invalidate();
 }
 public static void Preview(DataStore d,string path){
  CompactOverlayVisuals.RenderPreview(d,path);
 }
 public void Dispose(){timer.Dispose();gold.Dispose();details.Dispose();buffs.Dispose();stats.Dispose();visuals.Dispose();}
}

public sealed class OverlaySettings:Form {
 readonly DataStore data;readonly OverlayOptions draft;readonly ComboBox champions=new ComboBox(),items=new ComboBox(),keys=new ComboBox();readonly Label status=new Label();readonly TextBox search=new TextBox();
 readonly CheckBox enabled=new CheckBox(),gold=new CheckBox(),buffs=new CheckBox(),purchase=new CheckBox();readonly NumericUpDown x=new NumericUpDown(),y=new NumericUpDown();HashSet<int> planIds;
 public OverlayOptions Result;
 public OverlaySettings(DataStore d,OverlayOptions original){data=d;draft=original.Copy();Text="Rift Ready · Game overlay";ClientSize=new Size(680,705);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.None;
  Label("GAME OVERLAY",24,18,620,30);Label("Borderless / Windowed League · hides when you switch apps",24,52,620,26);
  var statsSettings=new Button{Text="Stats panel…",Left=24,Top=650,Width=170,Height=34};statsSettings.Click+=(s,e)=>StatsPanel.Settings(this,draft);Controls.Add(statsSettings);
  var align=new Button{Text="Align scoreboard…",Left=424,Top=278,Width=204,Height=32};align.Click+=(s,e)=>{using(var form=new ScoreboardAlignment(data,draft))if(form.ShowDialog(this)==DialogResult.OK){draft.BoardX=form.Result.BoardX;draft.BoardY=form.Result.BoardY;draft.BoardScale=form.Result.BoardScale;draft.RowStart=form.Result.RowStart;draft.RowGap=form.Result.RowGap;draft.AlliesLeft=form.Result.AlliesLeft;}};Controls.Add(align);
  Check(enabled,"Enable overlay",original.Enabled,90);Check(gold,"Lane item-value difference while scoreboard key is held",original.Gold,125);Check(buffs,"Baron and Elder buff windows",original.Buffs,160);Check(purchase,"Item and component purchase progress",original.Purchase,195);
  Label("Scoreboard key",24,238,150,24);keys.SetBounds(180,234,120,28);keys.DropDownStyle=ComboBoxStyle.DropDownList;foreach(Keys key in new[]{Keys.Tab,Keys.Space,Keys.Oemtilde,Keys.F1,Keys.F2,Keys.F3,Keys.F4})keys.Items.Add(key);keys.SelectedItem=(Keys)draft.Key;if(keys.SelectedIndex<0)keys.SelectedIndex=0;Controls.Add(keys);
  var move=new Button{Text="Move / resize panels…",Left=340,Top=232,Width=288,Height=32};move.Click+=(s,e)=>{var current=draft.Copy();current.Purchase=purchase.Checked;current.Buffs=buffs.Checked;using(var editor=new PanelLayoutEditor(data,current))if(editor.ShowDialog(this)==DialogResult.OK){StatsPanel.Copy(editor.Result,draft);PanelSizing.Copy(editor.Result,draft);draft.PanelLayoutVersion=1;draft.BuildPlaced=editor.Result.BuildPlaced;draft.BuffsPlaced=editor.Result.BuffsPlaced;draft.BuildU=editor.Result.BuildU;draft.BuildV=editor.Result.BuildV;draft.BuffsU=editor.Result.BuffsU;draft.BuffsV=editor.Result.BuffsV;purchase.Checked=editor.Result.Purchase;buffs.Checked=editor.Result.Buffs;}};Controls.Add(move);
  Label("PURCHASE TARGET",24,285,260,26);champions.SetBounds(24,321,265,28);champions.DropDownStyle=ComboBoxStyle.DropDownList;foreach(var c in data.Champions.OrderBy(c=>data.Name(c.Key)))champions.Items.Add(new BuildChoice((int)J.N(c.Value,"key"),data.Name(c.Key)));Controls.Add(champions);champions.SelectedItem=champions.Items.Cast<BuildChoice>().FirstOrDefault(c=>data.Resolve(c.Id.ToString())==draft.Champion);
  var load=new Button{Text="Load saved plan…",Left=310,Top=318,Width=150,Height=32};load.Click+=(s,e)=>LoadPlan();Controls.Add(load);var all=new Button{Text="All items",Left=478,Top=318,Width=150,Height=32};all.Click+=(s,e)=>{planIds=null;draft.Source="Manual selection";Populate();};Controls.Add(all);
  Label("Find item",24,365,110,24);search.SetBounds(137,361,491,28);Controls.Add(search);search.TextChanged+=(s,e)=>Populate();items.SetBounds(24,404,604,30);items.DropDownStyle=ComboBoxStyle.DropDownList;Controls.Add(items);Populate();items.SelectedItem=items.Items.Cast<BuildChoice>().FirstOrDefault(c=>c.Id==draft.Target);
  status.SetBounds(24,453,610,62);status.Text="Choose a target before the match. Load a plan saved in Runes / builds to select one of its items. Probuilds feeds are not connected.";Controls.Add(status);
  Label("Gold comparison uses inventory value, not total earned gold.\nBuffs show estimated team windows, not individual holders.\nPurchase estimates use bundled prices; the shop is authoritative.\nLayout editor: gear controls · Ctrl+Enter accepts · Esc cancels · F6 screen.\nRe-enable closed panels here. Layout preview uses sample data.",24,526,625,95);
  var save=new Button{Text="Save overlay settings",Left=420,Top=650,Width=208,Height=34};save.Click+=(s,e)=>{draft.Enabled=enabled.Checked;draft.Gold=gold.Checked;draft.Buffs=buffs.Checked;draft.Purchase=purchase.Checked;draft.Key=(int)(Keys)keys.SelectedItem;var champion=champions.SelectedItem as BuildChoice;var item=items.SelectedItem as BuildChoice;draft.Champion=champion==null?"":data.Resolve(champion.Id.ToString());draft.Target=item==null?0:item.Id;Result=draft;DialogResult=DialogResult.OK;};Controls.Add(save);Theme.Apply(this);
 }
 void Label(string text,int x,int y,int w,int h){Controls.Add(new Label{Text=text,Left=x,Top=y,Width=w,Height=h});}
 void Check(CheckBox box,string text,bool value,int top){box.Text=text;box.Checked=value;box.SetBounds(24,top,620,28);Controls.Add(box);}
 void Populate(){var chosen=items.SelectedItem as BuildChoice;items.Items.Clear();foreach(var pair in data.Items.Where(p=>OverlayData.Purchasable(data,Int32.Parse(p.Key))&&(planIds==null||planIds.Contains(Int32.Parse(p.Key)))&&J.S(p.Value,"name").IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0).OrderBy(p=>J.S(p.Value,"name")))items.Items.Add(new BuildChoice(Int32.Parse(pair.Key),J.S(pair.Value,"name")));if(chosen!=null)items.SelectedItem=items.Items.Cast<BuildChoice>().FirstOrDefault(c=>c.Id==chosen.Id);}
 public static int[] PlanItems(DataStore data,string json,out string champion,out string patch){if(json.Length>65536)throw new ArgumentException("Plan is too large.");var raw=J.Parse(json);champion=J.S(raw,"champion");patch=J.S(raw,"patch");if(J.S(raw,"format")!="rift-loadout-1"||data.Champion(champion)==null)throw new ArgumentException("Choose a saved Rift Ready plan.");var sections=J.A(J.Get(raw,"sections")).Select(row=>new KeyValuePair<string,string>(J.S(row,"name"),J.S(row,"items")));var set=J.Parse(new JavaScriptSerializer().Serialize(Loadouts.ItemSet(data,champion,sections)));return J.A(J.Get(set,"blocks")).SelectMany(b=>J.A(J.Get(b,"items"))).Select(i=>Int32.Parse(J.S(i,"id"))).Distinct().ToArray();}
 void LoadPlan(){try{using(var dialog=new OpenFileDialog{Filter="Rift loadout (*.json)|*.json"})if(dialog.ShowDialog(this)==DialogResult.OK){if(new FileInfo(dialog.FileName).Length>65536)throw new ArgumentException("Plan is too large.");string champ,patch;var ids=PlanItems(data,File.ReadAllText(dialog.FileName,System.Text.Encoding.UTF8),out champ,out patch);planIds=new HashSet<int>(ids);draft.PlanPatch=patch;draft.Source="Saved manual plan";champions.SelectedItem=champions.Items.Cast<BuildChoice>().First(c=>data.Resolve(c.Id.ToString())==champ);search.Clear();Populate();if(items.Items.Count>0)items.SelectedIndex=0;status.Text="Saved plan · data "+patch+". Select a target above. Source feeds are not connected.";}}catch(Exception ex){status.Text=ex.Message;}}
}
}
