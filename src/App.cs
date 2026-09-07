using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using RiftReference.Release;

namespace RiftReference {
public class Preferences {public bool Adjust=true;public bool SecondMonitor=true;public int PollSeconds=5;public string Focus="Punish windows";public string TrainingFocus="Support position";public bool ShowCoaching=true;public bool AutoUpdates=true,AudioEnabled=true,ToneCues=true,VoiceCues=true,RespawnCues=true,RespawnLaneOnly=false,MinutesAndSeconds=true,TenSecondCue=true,ThirtySecondCue=true,MinuteCue=true;public int SettingsVersion=0;public bool EnemiesLeft=true;public bool SecondClick=false;public int AudioVolume=25,JungleFirst=120,JungleInterval=90,JungleEnd=480;
 public bool Migrate(){bool changed=SettingsVersion<2;if(SettingsVersion<1){RespawnLaneOnly=false;EnemiesLeft=true;}if(SettingsVersion<2){TrainingFocus=Coaching.NormalizeFocus(Focus);SettingsVersion=2;}string normalized=Coaching.NormalizeFocus(TrainingFocus);changed|=normalized!=TrainingFocus;TrainingFocus=normalized;return changed;}
}
public class Dashboard : Form {
 GameOverlay overlay;OverlayOptions overlayOptions=new OverlayOptions();Button overlayButton;
 DataStore data;LeagueClient client;Snapshot state=new Snapshot(); Snapshot lastGame;MobileCompanion mobile;
 Preferences prefs=new Preferences(); string home; bool busy, demo; DateTime lastSuccess;
 Timer timer=new Timer(); Dictionary<string,Image> portraits=new Dictionary<string,Image>();
 Timer audioTimer=new Timer{Interval=1000};AttentionSchedule schedule=new AttentionSchedule();AttentionAudio audio=new AttentionAudio();bool audioBusy,settingsOpen;string audioStatus="";
 RespawnWatch respawns=new RespawnWatch();DateTime lastEventVoice=DateTime.MinValue;
 UpdateManager updater;bool updateBusy;DateTime lastUpdateCheck=DateTime.MinValue;
 Color bg=Theme.Background,panel=Theme.Panel,muted=Theme.Muted,ink=Theme.Ink,accent=Theme.Accent;
 Font normal=new Font("Segoe UI",11),small=new Font("Segoe UI",9),title=new Font("Segoe UI",25,FontStyle.Bold),bold=new Font("Segoe UI",12,FontStyle.Bold);
 Font cooldown=new Font("Segoe UI",25,FontStyle.Bold),compactCd=new Font("Segoe UI",17,FontStyle.Bold),micro=new Font("Segoe UI",8);
 Color gold=Color.FromArgb(255,213,116);
 Button live,demoButton,draftButton,postButton,settings,playbook,review,dashboardButton,phoneButton,buildButton;
 const int NavigationWidth=180;
 Rectangle leftHeader,rightHeader;int dragSide=-1;Point dragStart;
 void SaveLayout(){try{File.WriteAllText(Path.Combine(home,"preferences.json"),new JavaScriptSerializer().Serialize(prefs));}catch{audioStatus="Could not save layout preferences";}}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){dragSide=leftHeader.Contains(e.Location)?0:rightHeader.Contains(e.Location)?1:-1;dragStart=e.Location;if(dragSide>=0)Capture=true;}}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);Cursor=dragSide>=0?Cursors.SizeWE:(leftHeader.Contains(e.Location)||rightHeader.Contains(e.Location)?Cursors.Hand:Cursors.Default);}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(dragSide>=0&&Math.Abs(e.X-dragStart.X)>SystemInformation.DragSize.Width&&((dragSide==0&&rightHeader.Contains(e.Location))||(dragSide==1&&leftHeader.Contains(e.Location)))){prefs.EnemiesLeft=!prefs.EnemiesLeft;SaveLayout();Invalidate();}dragSide=-1;Capture=false;Cursor=Cursors.Default;}
 Color AbilityColor(int i){return new[]{Color.FromArgb(105,196,255),Color.FromArgb(182,147,255),Color.FromArgb(255,170,100),Color.FromArgb(255,119,170)}[i];}
 public Dashboard(string root,bool startDemo) {
  home=root;data=new DataStore(Path.Combine(root,"data"));client=new LeagueClient(data);mobile=new MobileCompanion(root);
  try{overlayOptions=new JavaScriptSerializer().Deserialize<OverlayOptions>(File.ReadAllText(Path.Combine(home,"overlay.json"),System.Text.Encoding.UTF8))??new OverlayOptions();}catch{}
  overlay=new GameOverlay(data,()=>overlayOptions);
  overlayButton=Button("Game overlay",()=>OpenOverlay());
  updater=new UpdateManager(home);
  Icon=Brand.Icon;
  try {prefs=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(Path.Combine(home,"preferences.json")))??new Preferences();}catch{}
  if(prefs.Migrate())SaveLayout();
  data.MinutesAndSeconds=prefs.MinutesAndSeconds;
  Text="Rift Ready · "+ReleaseInfo.Version;BackColor=bg;ForeColor=ink;DoubleBuffered=true;MinimumSize=new Size(1280,950);Size=new Size(1720,980);AutoScaleMode=AutoScaleMode.None;
  live=Button("Connect",()=>{demo=false;state=new Snapshot();PublishMobile();Invalidate();Tick();});
  demoButton=Button("Demo match",()=>{demo=true;state=Demo(false);PublishMobile();Invalidate();});
  draftButton=Button("Demo draft",()=>{demo=true;state=Demo(true);PublishMobile();Invalidate();});
  postButton=Button("Demo results",()=>{demo=true;state=Postgame.Demo(data,Demo(false));PublishMobile();Invalidate();});
  settings=Button("Preferences",()=>Settings());
  playbook=Button("Playbook",()=>OpenPractice(false));
  review=Button("Review",()=>OpenPractice(true));
  dashboardButton=Button("Overview",()=>{Invalidate();});
  phoneButton=Button("Phone / tablet",()=>Settings(null,3));
  buildButton=Button("Runes / builds",()=>{var self=state.Players.FirstOrDefault(p=>p.Self);using(var form=new BuildPlanner(data,client,self==null?"":self.Champion,()=>demo||state.Demo))form.ShowDialog(this);});
  ((NavigationButton)dashboardButton).Symbol="dashboard";((NavigationButton)dashboardButton).Active=true;
  ((NavigationButton)playbook).Symbol="book";((NavigationButton)review).Symbol="review";((NavigationButton)settings).Symbol="settings";((NavigationButton)phoneButton).Symbol="phone";
  Resize+=(s,e)=>LayoutButtons();Shown+=(s,e)=>{if(prefs.SecondMonitor && Screen.AllScreens.Length>1){Bounds=Screen.AllScreens.First(x=>!x.Primary).WorkingArea;WindowState=FormWindowState.Maximized;}LayoutButtons();};
  timer.Interval=overlayOptions.Enabled?1000:Math.Max(3,Math.Min(30,prefs.PollSeconds))*1000;timer.Tick+=(s,e)=>Tick();timer.Start();
  demo=startDemo;if(demo)state=Demo(false);else Tick();
  audioTimer.Tick+=(s,e)=>AudioTick();audioTimer.Start();
 }
 Button Button(string label,Action action){var b=new NavigationButton{Text=label,BackColor=bg,ForeColor=ink,Font=normal,Size=new Size(140,34),TabStop=true};b.Click+=(s,e)=>action();Controls.Add(b);return b;}
 void OpenOverlay(){overlay.Suspend();using(var form=new OverlaySettings(data,overlayOptions)){if(form.ShowDialog(this)==DialogResult.OK){try{OverlayStorage.Save(home,form.Result);overlayOptions=form.Result;timer.Interval=overlayOptions.Enabled?1000:Math.Max(3,Math.Min(30,prefs.PollSeconds))*1000;}catch(Exception ex){MessageBox.Show(this,"Could not save overlay settings: "+ex.Message);}}}}
 async void AudioTick(){
  if(IsDisposed||audioBusy)return;
  if(!prefs.AudioEnabled||demo||settingsOpen||state.Phase!="In game"){schedule.Reset();respawns.Reset();if(!settingsOpen)audio.Stop();return;}
  audioBusy=true;
  try{double clock=await client.Clock();if(IsDisposed||demo||settingsOpen||!prefs.AudioEnabled||state.Phase!="In game")return;
   var cue=schedule.Observe(clock,true,prefs.ToneCues,prefs.VoiceCues,prefs.JungleFirst,prefs.JungleInterval,prefs.JungleEnd,prefs.TenSecondCue,prefs.ThirtySecondCue,prefs.MinuteCue,prefs.SecondClick);
   if(cue==AttentionCue.JungleCheck){if((DateTime.UtcNow-lastEventVoice).TotalSeconds>12)audio.Speak(AttentionAudio.JungleMessage(state),prefs.AudioVolume);}
   else if(cue!=AttentionCue.None)audio.Tone(cue,prefs.AudioVolume,false);
   string next=audio.Status;if(next!=audioStatus){audioStatus=next;Invalidate();}
  }catch{schedule.Reset();audio.Stop();audioStatus="Audio paused: match clock unavailable";Invalidate();}finally{audioBusy=false;}
 }
 void LayoutButtons(){
  int x=ClientSize.Width-500;foreach(var b in new[]{live,demoButton,draftButton,postButton}){b.Size=new Size(112,34);b.Location=new Point(x,35);x+=118;}
  int y=165;foreach(var b in new[]{dashboardButton,buildButton,playbook,review,phoneButton,overlayButton}){b.Size=new Size(NavigationWidth-24,44);b.Location=new Point(12,y);y+=52;}
  settings.Size=new Size(NavigationWidth-24,44);settings.Location=new Point(12,ClientSize.Height-142);
 }
 bool CanReview(){return demo||(state.Phase!="In game"&&state.Phase!="InProgress"&&state.Phase!="Reconnect"&&state.Phase!="ChampSelect");}
 void OpenPractice(bool reflection,string renderPath=null){
  if(reflection&&!CanReview())return;
  settingsOpen=true;audio.Stop();try{var context=state.Players.Count>0?state:lastGame??state;
   if(reflection){var self=context.Players.FirstOrDefault(p=>p.Self);PracticeWindows.Review(this,home,self==null?"":data.Name(self.Champion),prefs.TrainingFocus,renderPath);}
   else PracticeWindows.Playbook(this,data,context,renderPath);
  }finally{settingsOpen=false;schedule.Reset();}
 }
 bool CanUpdate(){return state.Phase!="In game"&&state.Phase!="ChampSelect"&&state.Phase!="InProgress"&&state.Phase!="Reconnect"&&!UpdateManager.GameRunning();}
 async Task CheckUpdates(){if(updateBusy)return;if(!CanUpdate()){updater.Status="Updates are paused until your match or champion select ends.";return;}updateBusy=true;lastUpdateCheck=DateTime.UtcNow;try{await updater.Check();}catch(Exception ex){updater.Status=ex.Message;}finally{updateBusy=false;if(!IsDisposed){settings.Text=updater.Available==null?"Preferences":"Update available";Invalidate();}}}
 void PublishMobile(){if(!IsDisposed){mobile.Publish(data,state,prefs);overlay.Update(state);}}
 async void Tick(){if(IsDisposed)return;if(demo){PublishMobile();return;}if(busy)return;busy=true;try{var next=await client.Poll();if(IsDisposed||demo)return;
  if(next.Phase=="In game"){lastGame=next;lastSuccess=DateTime.Now;}
  string respawn=respawns.Observe(next,data,prefs.RespawnLaneOnly,Math.Max(15,prefs.PollSeconds+5));
  if(respawn!=null&&prefs.AudioEnabled&&prefs.RespawnCues&&!settingsOpen){audio.Speak(respawn,prefs.AudioVolume);lastEventVoice=DateTime.UtcNow;}
  state=next;PublishMobile();Invalidate();if(prefs.AutoUpdates&&(DateTime.UtcNow-lastUpdateCheck).TotalHours>=4)await CheckUpdates();
 }catch(Exception){respawns.Reset();state=new Snapshot{Notice="Connection failed. Live values cleared."};PublishMobile();Invalidate();}finally{busy=false;}}
 public Snapshot Demo(bool draft){var s=new Snapshot{Phase=draft?"ChampSelect":"In game",Account="Demo Vayne · NA",Mode="CLASSIC",Time=840,Demo=true};
  string[] names={"Ornn","Vi","Ahri","Vayne","Lulu","Darius","LeeSin","Syndra","Caitlyn","Nautilus"};string[] roles={"TOP","JUNGLE","MIDDLE","BOTTOM","UTILITY"};
  for(int i=0;i<10;i++){var p=new Player{Champion=names[i],Team=i<5?"ALLY":"ENEMY",Role=roles[i%5],Level=draft?1:(i%5==3?9:10),Self=i==3};p.Summoners.Add("Flash");p.Summoners.Add(i%5==1?"Smite":i%5==4?"Exhaust":"Heal");
   if(!draft){p.Items.Add(i%5==4?3158:3078);if(p.Self){p.Items.Clear();p.Haste=0;p.Ranks["Q"]=5;p.Ranks["W"]=2;p.Ranks["E"]=1;p.Ranks["R"]=1;}}
   s.Players.Add(p);
  }return s;
 }
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.Clear(bg);DrawNavigation(g);
  int width=Math.Max(1,ClientSize.Width-NavigationWidth);using(var canvas=new Bitmap(width,Math.Max(1,ClientSize.Height)))using(var content=Graphics.FromImage(canvas)){DrawMatch(content,width);g.DrawImageUnscaled(canvas,NavigationWidth,0);}
  if(!leftHeader.IsEmpty)leftHeader.Offset(NavigationWidth,0);if(!rightHeader.IsEmpty)rightHeader.Offset(NavigationWidth,0);
 }
 void DrawNavigation(Graphics g){
  using(var b=new SolidBrush(Color.FromArgb(12,13,17)))g.FillRectangle(b,0,0,NavigationWidth,ClientSize.Height);
  using(var p=new Pen(Theme.Border))g.DrawLine(p,NavigationWidth-1,0,NavigationWidth-1,ClientSize.Height);
  g.DrawImage(Brand.Logo,20,12,55,55);
  using(var f=new Font("Segoe UI",12,FontStyle.Bold))TextAt(g,"RIFT READY",f,ink,20,68,150,24);
  using(var p=new Pen(Theme.Border))g.DrawLine(p,20,108,NavigationWidth-20,108);
  TextAt(g,"LEAGUE OF LEGENDS",micro,muted,20,124,150,23);
  TextAt(g,"YOUR COMPANION",micro,muted,20,ClientSize.Height-198,145,24);
  TextAt(g,mobile.Running?"Phone sharing on":"Local to your PC",small,mobile.Running?accent:muted,20,ClientSize.Height-174,145,24);
  TextAt(g,"Rift Ready "+ReleaseInfo.Version,small,muted,20,ClientSize.Height-82,148,24);
  TextAt(g,"ANALYZE · PLAN · CLIMB",micro,muted,20,ClientSize.Height-54,145,36);
 }
 void DrawDraft(Graphics g,int w,int y){
  var self=state.Players.FirstOrDefault(p=>p.Self);int gap=16,half=(w-56-gap)/2;
  leftHeader=new Rectangle(28,y,half,27);rightHeader=new Rectangle(28+half+gap,y,half,27);
  for(int side=0;side<2;side++){
   bool ally=prefs.EnemiesLeft?side==1:side==0;int x=28+side*(half+gap);
   var roster=self==null?new Player[0]:state.Players.Where(p=>ally?p.Team==self.Team:p.Team!=self.Team&&p.Team!="").Take(5).ToArray();
   TextAt(g,ally?"ALLIED PICKS · drag to swap":"ENEMY PICKS · drag to swap",bold,ally?accent:Color.FromArgb(242,157,153),x,y,half,27);
   int cell=half/5;
   for(int i=0;i<5;i++){var p=i<roster.Length?roster[i]:null;int px=x+i*cell;var portrait=p==null?null:Portrait(p.Champion);if(portrait!=null)g.DrawImage(portrait,px+10,y+38,48,48);TextAt(g,p==null?"Pick pending":data.Name(p.Champion)+(p.Self?" · YOU":""),small,ink,px+8,y+94,cell-12,38);TextAt(g,p==null?"":p.Role,micro,muted,px+8,y+134,cell-12,20);}
  }
  y+=174;int right=28+half+gap;
  Card(g,28,y,half,142,(self==null?"YOUR CHAMPION":data.Name(self.Champion).ToUpperInvariant())+" · STRENGTHS",Pregame.Strength(data,self));
  Card(g,right,y,half,142,"VULNERABILITIES · PLAN AROUND THEM",Pregame.Weakness(data,self));y+=158;
  int tall=Math.Max(275,ClientSize.Height-y-134);
  Card(g,28,y,half,tall,"MATCHUP CONSIDERATIONS",Pregame.Matchup(data,state));
  Card(g,right,y,half,tall,"TEAM WIN CONDITIONS · OPTIONS",Pregame.Composition(data,state));y+=tall+16;
  Card(g,28,y,w-56,90,"PREPARE YOUR LOADOUT · RUNES / BUILDS IN THE SIDEBAR","Compare Onetricks.gg or Probuilds in your browser, choose runes and arrange custom shop sections. Preview and apply each separately to your client. Source feeds are not connected; no automatic changes.");
 }
 void DrawPostgame(Graphics g,int w,int y){
  if(state.Players.Count==0){Card(g,28,y,w-56,180,"MATCH FINISHED · RESULTS PENDING",state.Notice==""?"Waiting for the League client to provide this match's final scoreboard. No cooldowns or estimated match statistics are shown.":state.Notice);return;}
  var summaries=Postgame.Summary(data,state);int cw=(w-56-48)/4;
  for(int i=0;i<4;i++)Card(g,28+i*(cw+16),y,cw,135,summaries[i].Title,summaries[i].Body);y+=157;
  var self=state.Players.FirstOrDefault(p=>p.Self);string own=self==null?"":self.Team;int half=(w-72)/2;
  var teams=state.Players.GroupBy(p=>p.Team).OrderBy(t=>(t.Key==own)==prefs.EnemiesLeft?1:0).ToArray();
  if(teams.Length!=2){Card(g,28,y,w-56,150,"SCOREBOARD UNAVAILABLE","This view supports two-team results. Your available personal statistics are above.");y+=170;}
  else {int maxRows=teams.Max(t=>t.Count());int row=Math.Max(42,Math.Min(62,(ClientSize.Height-y-318)/Math.Max(1,maxRows)));
   for(int side=0;side<2;side++){int x=28+side*(half+16);var team=teams[side];
    TextAt(g,own==""?"TEAM "+team.Key:team.Key==own?"ALLIES":"ENEMIES",bold,team.Key==own?accent:Color.FromArgb(242,157,153),x,y,half,27);
    int champ=Math.Min(220,half*3/10);float cell=(half-champ)/5f;string[] labels={"K / D / A","CS / MIN","CHAMP DMG","GOLD","VISION"};
    for(int col=0;col<5;col++)TextAt(g,labels[col],micro,muted,x+champ+col*cell,y+33,cell-4,25);
    int index=0;foreach(var p in team){int top=y+64+index++*row;using(var brush=new SolidBrush(p.Self?Color.FromArgb(25,51,53):panel))g.FillRectangle(brush,x,top,half,row-4);
     int portraitSize=Math.Min(40,row-12);var portrait=Portrait(p.Champion);int portraitY=top+(row-4-portraitSize)/2;
     if(portrait!=null)g.DrawImage(portrait,x+8,portraitY,portraitSize,portraitSize);
     else {using(var outline=new Pen(Theme.Border))g.DrawRectangle(outline,x+8,portraitY,portraitSize,portraitSize);TextAt(g,"?",small,muted,x+8,portraitY+5,portraitSize,portraitSize-5);}
     int nameX=x+portraitSize+18,nameWidth=champ-portraitSize-23;
     TextAt(g,data.Name(p.Champion)+(p.Self?" · YOU":""),small,p.Self?accent:ink,nameX,top+3,nameWidth,24);TextAt(g,p.Role,micro,muted,nameX,top+row-24,nameWidth,18);
     var values=Postgame.Cells(p,state.Time);for(int col=0;col<5;col++)TextAt(g,values[col],small,ink,x+champ+col*cell,top+12,cell-4,row-12);
    }
   }y+=64+maxRows*row+18;
  }
  Card(g,28,y,w-56,110,"YOUR FINAL ITEMS",Postgame.Items(data,self));y+=126;
  Card(g,28,y,w-56,110,"TAKE ONE LESSON FORWARD","Open Review to pair these results with your own notes. Scoreboard totals describe the match; they do not explain every decision or measure missed opportunities.");
 }
 void DrawMatch(Graphics g,int w){g.Clear(bg);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
  TextAt(g,"LEAGUE OF LEGENDS  /  OVERVIEW",small,muted,28,17,w-440,23);
  using(var heading=new Font("Segoe UI",24,FontStyle.Bold))TextAt(g,Postgame.Active(state.Phase)?"Match summary":state.Players.Count==0?"Your next game starts here.":state.Phase=="ChampSelect"?"Champion select":"Match overview",heading,ink,26,42,w-550,47);review.Enabled=CanReview();
  ((NavigationButton)live).Active=!demo;((NavigationButton)demoButton).Active=demo&&state.Phase=="In game";((NavigationButton)draftButton).Active=demo&&state.Phase=="ChampSelect";((NavigationButton)postButton).Active=demo&&Postgame.Active(state.Phase);postButton.Invalidate();
  live.Invalidate();demoButton.Invalidate();draftButton.Invalidate();
  string phase=state.Phase=="ChampSelect"?"CHAMPION SELECT":state.Phase.ToUpperInvariant();
  string info=(demo?"DEMO · SIMULATED DATA":phase)+"    /    "+(state.Account==""?"Waiting for League":state.Account)+"    /    "+(audioStatus!=""?audioStatus:!prefs.AudioEnabled?"Audio off":demo?"Audio paused":"Audio on · "+prefs.AudioVolume+"%");
  if(Postgame.Active(state.Phase))info=(demo?"DEMO · SIMULATED DATA":phase)+"    /    "+(state.Players.Count>0?"Final scoreboard":"Waiting for final statistics");
  TextAt(g,info,small,demo?Color.FromArgb(213,184,126):accent,28,94,w-50,24);
  TextAt(g,Postgame.Active(state.Phase)?(state.Result==""?"Results pending":state.Result)+" · Duration "+Postgame.Duration(state.Time)+" · "+state.Notice:(state.Phase=="ChampSelect"?"Draft brief · Kit guidance and composition patterns · Data ":"Cooldown references · Unknown ranks are estimates · Data ")+data.Version+" · Patch match unverified",small,muted,28,120,w-50,23);
  leftHeader=Rectangle.Empty;rightHeader=Rectangle.Empty;int y=147;
  if(Postgame.Active(state.Phase)){DrawPostgame(g,w,y);return;}
  if(state.Phase=="ChampSelect"){DrawDraft(g,w,y);return;}
  if(state.Players.Count==0){DrawEmpty(g,w,y);return;}
  bool draft=state.Phase=="ChampSelect"; bool adjust=prefs.Adjust && state.Mode=="CLASSIC" && !draft;
  var lane=LaneView.Select(state);var referenceLane=lane;
  bool compactRoster=prefs.ShowCoaching&&(ClientSize.Height<1000||w<1700);
  bool rosterFallback=compactRoster||lane.Allies.Count!=lane.Enemies.Count||lane.Allies.Count>2||lane.Enemies.Count>2;
  if(rosterFallback)lane=new LaneView{Others=state.Players.ToList()};
  int gap=16;int half=(w-56-gap)/2;
  int pairs=Math.Max(lane.Allies.Count,lane.Enemies.Count);
  if(pairs>0){
   int allyX=prefs.EnemiesLeft?28+half+gap:28,enemyX=prefs.EnemiesLeft?28:28+half+gap;
   leftHeader=new Rectangle(28,y,half,25);rightHeader=new Rectangle(28+half+gap,y,half,25);
   TextAt(g,"ALLIES · drag header to swap sides",small,accent,allyX,y,half,21);TextAt(g,"ENEMIES · drag header to swap sides",small,Color.FromArgb(242,157,153),enemyX,y,half,21);y+=25;
   for(int i=0;i<pairs;i++){
    if(i<lane.Allies.Count)LaneCard(g,lane.Allies[i],allyX,y,half,130,adjust,true);
    if(i<lane.Enemies.Count)LaneCard(g,lane.Enemies[i],enemyX,y,half,130,adjust,false);
    else Card(g,enemyX,y,half,130,"OPPONENT NOT IDENTIFIED","Waiting for the client to expose lane roles. No opponent has been guessed.");
    y+=138;
   }
  }
  var others=lane.Others.OrderBy(p=>p.Team==(state.Players.FirstOrDefault(v=>v.Self)??state.Players[0]).Team?0:1).ToArray();
  if(others.Length>0){
   int champ=230;float cell=(w-56-champ)/6f;
   string[] headers={compactRoster?"MATCH · COMPACT VIEW":rosterFallback?"LANE INCOMPLETE · ROSTER":"REST OF THE MATCH","Q","W","E","R","SUMMONER 1","SUMMONER 2"};
   TextAt(g,headers[0],small,muted,35,y,champ,22);for(int j=1;j<7;j++)TextAt(g,headers[j],bold,j<=4?AbilityColor(j-1):muted,28+champ+(j-1)*cell,y,cell,22);y+=24;
   int row=Math.Max(44,Math.Min(48,(ClientSize.Height-y-(prefs.ShowCoaching?284:219))/others.Length));
   string own=(state.Players.FirstOrDefault(p=>p.Self)??state.Players[0]).Team;
   for(int i=0;i<others.Length;i++){
    var p=others[i];int top=y+i*row;using(var b=new SolidBrush(i%2==0?panel:Color.FromArgb(20,22,27)))g.FillRectangle(b,28,top,w-56,row-3);
    using(var b=new SolidBrush(p.Team==own?accent:Color.FromArgb(235,137,143)))g.FillRectangle(b,28,top,3,row-3);
    var image=Portrait(p.Champion);if(image!=null)g.DrawImage(image,38,top+5,row-12,row-12);
    int tx=38+row-5;TextAt(g,data.Name(p.Champion)+(p.Self?" · YOU":""),bold,ink,tx,top+2,champ-(tx-28),24);
    TextAt(g,(draft?"":("Lv "+p.Level+" · "))+p.Role,small,muted,tx,top+24,champ-(tx-28),18);
    for(int j=0;j<6;j++){
     string value=j<4?data.Spell(p,j,adjust):data.Summoner(p,j-4,adjust);string[] lines=value.Split('\n');string[] parts=lines[0].Split(new[]{" · "},StringSplitOptions.None);float x=28+champ+j*cell;
     bool numeric=parts[0].Length>0&&char.IsDigit(parts[0][0]);TextAt(g,parts[0],numeric?compactCd:normal,numeric?gold:muted,x+3,top,cell-8,28);
     if(numeric&&parts.Length>1){int numberWidth=TextRenderer.MeasureText(parts[0],compactCd).Width;TextAt(g,string.Join(" · ",parts.Skip(1)),micro,muted,x+numberWidth+8,top+9,cell-numberWidth-15,18);}
     if(lines.Length>1)TextAt(g,lines[1],small,muted,x+3,top+24,cell-9,18);
    }
   }y+=others.Length*row+9;
  }
  int h=Math.Max(100,ClientSize.Height-y-54);
  if(prefs.ShowCoaching){
   var plan=Coaching.LanePlan(data,state);var threats=Coaching.ThreatCard(data,state);
   int available=w-56-2*gap;int planWidth=(int)(available*.42),threatWidth=(int)(available*.34);int focusWidth=available-planWidth-threatWidth;
   Card(g,28,y,planWidth,h,plan.Title,plan.Body);
   Card(g,28+planWidth+gap,y,threatWidth,h,threats.Title,threats.Body+"\nKit tools only; casts and readiness are not observed.");
   Card(g,28+planWidth+threatWidth+2*gap,y,focusWidth,h,compactRoster?"ONE TRAINING FOCUS":"ONE FOCUS · "+prefs.TrainingFocus.ToUpperInvariant(),Coaching.FocusText(prefs.TrainingFocus)+"\n\nFIGHT CHECK\nCompare target access, ally protection and your exit.\nPlaybook: lessons. Review: after the game.");
  }else{
   var cards=Matchups.Cards(data,referenceLane,adjust);int count=cards.Length/2;int cw=(w-56-(count-1)*gap)/count;
   for(int i=0;i<count;i++)Card(g,28+i*(cw+gap),y,cw,h,cards[i*2],cards[i*2+1]);
  }
  TextAt(g,"Observe the cast yourself. Gold numbers are reference durations, not remaining time. Unknown ranks use independent maximum-rank assumptions. Data "+data.Version+" · patch match unverified.",small,muted,28,ClientSize.Height-42,w-56,35);
 }
 void LaneCard(Graphics g,Player p,int x,int y,int w,int h,bool adjust,bool ally){
  Theme.Surface(g,new Rectangle(x,y,w,h),panel,accent);
  using(var b=new SolidBrush(ally?accent:Color.FromArgb(225,125,141)))g.FillRectangle(b,x+1,y+10,3,h-20);
  var image=Portrait(p.Champion);if(image!=null)g.DrawImage(image,x+13,y+9,32,32);
  TextAt(g,data.Name(p.Champion)+(p.Self?" · YOU":""),bold,ink,x+54,y+7,w-260,26);
  TextAt(g,(state.Phase=="ChampSelect"?"":("LEVEL "+p.Level+" · "))+p.Role,small,muted,x+w-212,y+12,200,21);
  float cell=(w-24)/6f;
  for(int j=0;j<6;j++){
   string raw=j<4?data.Spell(p,j,adjust):data.Summoner(p,j-4,adjust);string[] lines=raw.Split('\n');string[] parts=lines[0].Split(new[]{" · "},StringSplitOptions.None);float sx=x+12+j*cell;
   string spell=lines.Length>1?lines[1]:"";TextAt(g,j<4?"QWER"[j].ToString():"SUM "+(j-3),bold,j<4?AbilityColor(j):ink,sx,y+38,cell-5,24);
   bool numeric=parts[0].Length>0&&char.IsDigit(parts[0][0]);Font valueFont=numeric?(TextRenderer.MeasureText(parts[0],cooldown).Width>cell-5?compactCd:cooldown):(TextRenderer.MeasureText(parts[0],bold).Width>cell-5?small:bold);TextAt(g,parts[0],valueFont,numeric?gold:muted,sx,y+59,cell-5,38);
   string note=parts.Length>1?string.Join(" · ",parts.Skip(1)):numeric?"reference":"special mechanic";
   TextAt(g,spell,small,ink,sx,y+95,cell-5,19);
   TextAt(g,note,micro,muted,sx,y+114,cell-5,16);
  }
 }

 Image Portrait(string champion){string id=data.Resolve(champion);if(portraits.ContainsKey(id))return portraits[id];string p=Path.Combine(data.Root,"portraits",id+".png");if(!File.Exists(p))return null;var image=Image.FromFile(p);portraits[id]=image;return image;}
 void DrawEmpty(Graphics g,int w,int y){string headline="Ready when you are.";string body="Open League and enter champion select or a practice game. This app checks the local client automatically.\n\nTry Demo match or Demo draft to explore the dashboard without a game.";
  if(state.Phase=="InProgress"||state.Phase=="Reconnect"){headline="Waiting for live game data";body="The League client reports a game, but the local game feed is unavailable. Reference values are cleared until the connection returns.";}
  if(state.Phase=="EndOfGame"||state.Phase=="PreEndOfGame"||state.Phase=="WaitingForStats"){headline="Take one lesson into the next game.";body="Open Review to save your reflection and revisit previous notes.\nWhat decision created your first major disadvantage?\nWhat advantage did you fail to convert?\nWhat will you do differently next game?\n\nSelf-assessment only. Missed opportunities were not measured.";}
  Card(g,28,y,w-56,260,headline,body+"\n\n"+state.Notice);
  Card(g,28,y+278,(w-70)/2,180,"ONE FOCUS · "+prefs.TrainingFocus.ToUpperInvariant(),Coaching.FocusText(prefs.TrainingFocus)+"\n\nChoose your focus in Preferences. Explore wave, recall and conversion lessons in Playbook before queueing.");
  Card(g,42+(w-70)/2,y+278,(w-70)/2,180,"PRIVATE BY DEFAULT","No cloud AI, game inputs or match uploads. This preview reads local League endpoints. Rune application and build exports are not yet enabled. Updates use GitHub.");
 }
 string[] Advice(bool draft,string own){var self=state.Players.FirstOrDefault(p=>p.Self)??state.Players[0];var enemies=state.Players.Where(p=>p.Team!=own && p.Champion!="").ToList();var allies=state.Players.Where(p=>p.Team==own&&p.Champion!="").ToList();
  var threat=enemies.FirstOrDefault(p=>p.Role==self.Role)??enemies.FirstOrDefault();
  int tanks=allies.Count(p=>data.Tags(p.Champion).Contains("Tank")),mages=allies.Count(p=>data.Tags(p.Champion).Contains("Mage")),supports=allies.Count(p=>data.Tags(p.Champion).Contains("Support"));
  string comp="Composition heuristic, not a win probability. ";
  comp+=tanks==0?"No tank-tagged ally: avoid assuming someone can absorb the engage. ":"Let your frontline start on its terms. ";
  comp+=supports>0?"Stay within your support's protection.":"Identify who can protect your carries.";
  if(draft){string picks=tanks==0?"Consider frontline options: Ornn (top), Sejuani (jungle), Nautilus (support).":mages==0?"Consider magic-damage options: Orianna (mid), Lillia (jungle), Zyra (support).":"Consider protection: Lulu (support), Braum (support), or a comfort pick in the remaining role.";
   if(allies.Count>=5)picks="All five allied champions are selected. Focus on executing your composition; there are no remaining pick slots.";
   return new[]{"DRAFT SHAPE",comp,"PICK OPTIONS · HEURISTIC",picks+" Check bans and comfort.","YOUR LANE PLAN",data.Tip(self.Champion,false)};}
  string focus=threat==null?"Opponent not identified.":"Against "+data.Name(threat.Champion)+": "+data.Tip(threat.Champion,true);
  string strategy=state.Time<840?"Check the wave and your support before trading. If a key enemy defensive spell is used, judge your opportunity using its reference duration.":"Before contesting, compare who can arrive and who has spent gold. If your team cannot establish safe access, consider farming or a cross-map trade.";
  if(prefs.Focus=="Positioning")strategy=data.Tip(self.Champion,false);
  if(prefs.Focus=="Wave and macro")strategy="Before leaving lane, consider which wave you give up and who can collect it. Contest only if your team can arrive together; positions and vision are not observed by this app.";
  return new[]{"PUNISH WINDOW · CONDITIONAL",focus,"NEXT PRIORITY · "+prefs.Focus.ToUpperInvariant(),strategy,"TEAMFIGHT CONDITIONS",comp};
 }
 string[] ExtraAdvice(string own){var self=state.Players.FirstOrDefault(p=>p.Self)??state.Players[0];var jungle=state.Players.FirstOrDefault(p=>p.Team!=own&&(p.Role=="JUNGLE"||p.Role=="jungle"));
  string power="Ultimate ranks often become available at levels 6 / 11 / 16. Champion exceptions apply; available rank is not proof of a learned spell.";
  var ults=J.A(J.Get(data.Champion(self.Champion),"spells"));if(ults.Length==4 && J.A(J.Get(ults[3],"cooldown")).Length==3)power=data.Name(self.Champion)+": watch the 6 / 11 / 16 ultimate rank thresholds. Reassess after completed items; the app does not measure combat strength.";
  string purchase=data.Tags(self.Champion).Contains("Marksman")?"If you cannot keep attacking safely, consider a defensive purchase before more damage. Compare the enemy's control and damage types; no pro build is loaded.":"Match defensive stats to the threats stopping your role. Compare completed items before committing; no pro build is loaded.";
  string route=(jungle==null?"Enemy jungle":data.Name(jungle.Champion))+": a full clear or an early three-camp gank are possibilities, not observed routes. Exact clear benchmarks are not yet validated.";
  return new[]{"POWERSPIKE REFERENCE",power,"PURCHASE DECISION",purchase,"JUNGLE · ROUTE POSSIBILITIES",route};
 }
 void Card(Graphics g,int x,int y,int w,int h,string heading,string body){Theme.Surface(g,new Rectangle(x,y,w,h),panel,accent);TextAt(g,heading,bold,ink,x+18,y+12,w-36,28);using(var line=new Pen(Theme.Border))g.DrawLine(line,x+18,y+42,x+w-18,y+42);TextAt(g,body,normal,Color.FromArgb(196,202,213),x+18,y+51,w-36,h-58);}
 void TextAt(Graphics g,string text,Font f,Color c,float x,float y,float w,float h){TextRenderer.DrawText(g,text,f,new Rectangle((int)x,(int)y,(int)Math.Max(1,w),(int)Math.Max(1,h)),c,TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);}
 public void Settings(string renderFolder=null,int selectedTab=0){settingsOpen=true;schedule.Reset();audio.Stop();
  try{using(var f=new Form{Text="Rift Ready preferences",Size=new Size(620,Math.Min(760,Screen.FromControl(this).WorkingArea.Height)),AutoScroll=true,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
   var tabs=new TabControl{Left=15,Top=15,Width=575,Height=635};var general=new TabPage("Dashboard");var sounds=new TabPage("Attention training");var updates=new TabPage("Updates");tabs.TabPages.Add(general);tabs.TabPages.Add(sounds);tabs.TabPages.Add(updates);tabs.TabPages.Add(MobileSettings.Create(mobile,PublishMobile));tabs.SelectedIndex=Math.Max(0,Math.Min(3,selectedTab));
   var versionLabel=new Label{Text="Installed version: "+ReleaseInfo.Version,Left=20,Top=25,Width=500};var automatic=new CheckBox{Text="Automatically check for new versions outside matches",Checked=prefs.AutoUpdates,Left=20,Top=65,Width=520};
   var updateStatus=new Label{Text=updater.Status,Left=20,Top=110,Width=520,Height=70};var releaseNotes=new TextBox{Text=updater.Available==null?"":updater.Available.notes,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Left=20,Top=195,Width=520,Height=220};
   var check=new Button{Text="Check for updates",Left=20,Top=445,Width=170,Height=35};var install=new Button{Text="Download and install",Left=215,Top=445,Width=190,Height=35,Enabled=updater.Available!=null};
   check.Click+=async(s,e)=>{check.Enabled=false;updateStatus.Text="Checkingâ€¦";await CheckUpdates();if(f.IsDisposed)return;updateStatus.Text=updater.Status;releaseNotes.Text=updater.Available==null?"":updater.Available.notes;install.Enabled=updater.Available!=null;check.Enabled=true;};
   install.Click+=async(s,e)=>{if(!CanUpdate()){updateStatus.Text="Finish champion select or your match before updating.";return;}install.Enabled=false;check.Enabled=false;try{updateStatus.Text="Downloading and verifying the releaseâ€¦";string file=await updater.Prepare();if(IsDisposed||f.IsDisposed)return;if(!CanUpdate()){updateStatus.Text="A match started. Install after the match finishes.";return;}updater.Launch(file);f.Close();Close();}catch(Exception ex){if(!f.IsDisposed)updateStatus.Text=ex.Message;}finally{if(!f.IsDisposed){check.Enabled=true;install.Enabled=updater.Available!=null;}}};
   updates.Controls.AddRange(new Control[]{versionLabel,automatic,updateStatus,releaseNotes,check,install,new Label{Text="Your preferences are kept. Installation restarts Rift Ready.\nReleases are downloaded over HTTPS and checked against the publisher signature.",Left=20,Top=510,Width=520,Height=65}});
   var adjust=new CheckBox{Text="Apply supported haste adjustments",Checked=prefs.Adjust,Left=20,Top=22,Width=480};
   var monitor=new CheckBox{Text="Open on the second monitor",Checked=prefs.SecondMonitor,Left=20,Top=55,Width=480};
   var label=new Label{Text="Dashboard refresh interval (seconds)",Left=20,Top=98,Width=480};
   var poll=new NumericUpDown{Minimum=3,Maximum=30,Value=Math.Max(3,Math.Min(30,prefs.PollSeconds)),Left=20,Top=124};
   var focusLabel=new Label{Text="One training focus",Left=20,Top=157,Width=480};
   var focus=new ComboBox{Left=20,Top=184,Width=480,DropDownStyle=ComboBoxStyle.DropDownList};focus.Items.AddRange(Coaching.FocusNames.Cast<object>().ToArray());focus.SelectedItem=prefs.TrainingFocus;if(focus.SelectedIndex<0)focus.SelectedIndex=0;
   var formatLabel=new Label{Text="Cooldown display",Left=20,Top=225,Width=480};
   var format=new ComboBox{Left=20,Top=252,Width=480,DropDownStyle=ComboBoxStyle.DropDownList};format.Items.AddRange(new object[]{"Minutes + seconds at 1 minute or more (1m 40s)","Seconds only (100s)"});format.SelectedIndex=prefs.MinutesAndSeconds?0:1;
   var enemiesLeft=new CheckBox{Text="Enemies on the left (or drag the lane headers)",Checked=prefs.EnemiesLeft,Left=20,Top=300,Width=520};
   var coaching=new CheckBox{Text="Show lane plan, enemy tools and training focus",Checked=prefs.ShowCoaching,Left=20,Top=342,Width=520};
   var coachingNote=new Label{Text="Unchecked: show the original matchup cards.\nFull matchup, teamfight and wave lessons are in Playbook.\nReview saves your own notes locally after a game.\n\nRift Ready is not endorsed by Riot Games.\nMechanic sources and coverage: Playbook > Sources and coverage.",Left=20,Top=386,Width=520,Height=150};
   general.Controls.AddRange(new Control[]{adjust,monitor,label,poll,focusLabel,focus,formatLabel,format,enemiesLeft,coaching,coachingNote});
   var master=new CheckBox{Text="Enable audio during live games",Checked=prefs.AudioEnabled,Left=20,Top=18,Width=500};
   var tones=new CheckBox{Text="Enable timing sounds (choose intervals below)",Checked=prefs.ToneCues,Left=20,Top=52,Width=500};
   var secondClick=new CheckBox{Text="1s click",Checked=prefs.SecondClick,Left=20,Top=83,Width=110};
   var tenCue=new CheckBox{Text="10 seconds",Checked=prefs.TenSecondCue,Left=145,Top=83,Width=115};
   var thirtyCue=new CheckBox{Text="30 seconds",Checked=prefs.ThirtySecondCue,Left=270,Top=83,Width=115};
   var minuteCue=new CheckBox{Text="1 minute",Checked=prefs.MinuteCue,Left=395,Top=83,Width=115};
   var voice=new CheckBox{Text="Spoken jungle-awareness reminders",Checked=prefs.VoiceCues,Left=20,Top=117,Width=500};
   var respawnToggle=new CheckBox{Text="Announce enemy respawns",Checked=prefs.RespawnCues,Left=20,Top=149,Width=500};
   var laneOnly=new CheckBox{Text="Respawns: only my lane opponents (uncheck for all enemies)",Checked=prefs.RespawnLaneOnly,Left=20,Top=181,Width=530};
   var volumeLabel=new Label{Text="Volume: "+prefs.AudioVolume+"%",Left=20,Top=214,Width=500};
   var volume=new TrackBar{Minimum=0,Maximum=100,TickFrequency=10,Value=Math.Max(0,Math.Min(100,prefs.AudioVolume)),Left=20,Top=240,Width=500};volume.ValueChanged+=(s,e)=>volumeLabel.Text="Volume: "+volume.Value+"%";
   var explain=new Label{Text="Only the longest enabled interval sounds at a shared boundary.\nVoice replaces a coinciding tone. No automatic sound in demo or lobby.",Left=20,Top=292,Width=515,Height=43};
   var firstLabel=new Label{Text="First jungle check (sec)",Left=20,Top=350,Width=170};var everyLabel=new Label{Text="Repeat every (sec)",Left=200,Top=350,Width=155};var endLabel=new Label{Text="Stop after (sec)",Left=380,Top=350,Width=155};
   var first=new NumericUpDown{Minimum=30,Maximum=1800,Increment=30,Value=Math.Max(30,Math.Min(1800,prefs.JungleFirst)),Left=20,Top=377,Width=135};
   var every=new NumericUpDown{Minimum=30,Maximum=600,Increment=30,Value=Math.Max(30,Math.Min(600,prefs.JungleInterval)),Left=200,Top=377,Width=135};
   var end=new NumericUpDown{Minimum=30,Maximum=3600,Increment=30,Value=Math.Max(30,Math.Min(3600,prefs.JungleEnd)),Left=380,Top=377,Width=135};
   var caution=new Label{Text="These are scheduled attention prompts, not a prediction or detection\nof a gank. Default: first at 2:00, every 90 seconds, ending at 8:00.",Left=20,Top=419,Width=520,Height=44};
   var preview10=new Button{Text="Hear 10s",Left=20,Top=484,Width=115};var preview30=new Button{Text="Hear 30s",Left=145,Top=484,Width=115};var preview60=new Button{Text="Hear minute",Left=270,Top=484,Width=115};var previewVoice=new Button{Text="Hear voice",Left=395,Top=484,Width=115};
   preview10.Click+=(s,e)=>audio.Tone(AttentionCue.TenSeconds,volume.Value,true);preview30.Click+=(s,e)=>audio.Tone(AttentionCue.ThirtySeconds,volume.Value,true);preview60.Click+=(s,e)=>audio.Tone(AttentionCue.Minute,volume.Value,true);previewVoice.Click+=(s,e)=>audio.Speak(AttentionAudio.JungleMessage(state),volume.Value);
   var previewClick=new Button{Text="Hear 1s click",Left=170,Top=526,Width=130};previewClick.Click+=(s,e)=>audio.Tone(AttentionCue.SecondClick,volume.Value,true);
   var previewRespawn=new Button{Text="Hear respawn",Left=20,Top=526,Width=130};previewRespawn.Click+=(s,e)=>audio.Speak("Caitlyn is back alive.",volume.Value);
   var status=new Label{Text="Uses your Windows output device and installed English speech voice.",Left=20,Top=568,Width=520,Height=43};var statusTimer=new Timer{Interval=400};statusTimer.Tick+=(s,e)=>{if(audio.Status!="")status.Text=audio.Status;};statusTimer.Start();
   sounds.Controls.AddRange(new Control[]{master,tones,secondClick,tenCue,thirtyCue,minuteCue,voice,respawnToggle,laneOnly,volumeLabel,volume,explain,firstLabel,everyLabel,endLabel,first,every,end,caution,preview10,preview30,preview60,previewVoice,previewRespawn,previewClick,status});
   var save=new Button{Text="Save preferences",Left=415,Top=675,Width=175,Height=32};save.Click+=(s,e)=>{
    if(end.Value<first.Value){MessageBox.Show("The last jungle check must be at or after the first check.");return;}
    var next=new Preferences{SettingsVersion=2,TrainingFocus=Convert.ToString(focus.SelectedItem),ShowCoaching=coaching.Checked,EnemiesLeft=enemiesLeft.Checked,AutoUpdates=automatic.Checked,Adjust=adjust.Checked,SecondMonitor=monitor.Checked,PollSeconds=(int)poll.Value,Focus=prefs.Focus,AudioEnabled=master.Checked,ToneCues=tones.Checked,SecondClick=secondClick.Checked,TenSecondCue=tenCue.Checked,ThirtySecondCue=thirtyCue.Checked,MinuteCue=minuteCue.Checked,VoiceCues=voice.Checked,RespawnCues=respawnToggle.Checked,RespawnLaneOnly=laneOnly.Checked,MinutesAndSeconds=format.SelectedIndex==0,AudioVolume=volume.Value,JungleFirst=(int)first.Value,JungleInterval=(int)every.Value,JungleEnd=(int)end.Value};
    try{File.WriteAllText(Path.Combine(home,"preferences.json"),new JavaScriptSerializer().Serialize(next));}catch(Exception ex){MessageBox.Show("Could not save preferences: "+ex.Message);return;}prefs=next;data.MinutesAndSeconds=prefs.MinutesAndSeconds;timer.Interval=overlayOptions.Enabled?1000:prefs.PollSeconds*1000;audioStatus="";f.Close();Invalidate();};
   f.Controls.AddRange(new Control[]{tabs,save});Theme.Apply(f);Timer shot=null;if(renderFolder!=null){f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-30000,-30000);shot=new Timer{Interval=250};shot.Tick+=(s,e)=>{shot.Stop();for(int page=0;page<tabs.TabPages.Count;page++){tabs.SelectedIndex=page;using(var bmp=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bmp,new Rectangle(Point.Empty,f.Size));bmp.Save(Path.Combine(renderFolder,new[]{"display-settings.png","audio-settings.png","update-settings.png","mobile-settings.png"}[page]));}}f.Close();};shot.Start();}try{f.ShowDialog(this);}finally{statusTimer.Dispose();if(shot!=null)shot.Dispose();}
  }}finally{settingsOpen=false;audio.Stop();schedule.Reset();}
 }
 public void Render(string path,bool draft,bool postgame=false){demo=true;state=postgame?Postgame.Demo(data,Demo(false)):Demo(draft);prefs.SecondMonitor=false;StartPosition=FormStartPosition.Manual;Location=new Point(-30000,-30000);Size=new Size(1920,1040);Show();LayoutButtons();using(var bmp=new Bitmap(Width,Height)){DrawToBitmap(bmp,new Rectangle(Point.Empty,Size));bmp.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Hide();}
 public void RenderPractice(){demo=true;state=Demo(false);OpenPractice(false,Path.Combine(home,"playbook.png"));OpenPractice(true,Path.Combine(home,"review.png"));}
 public void Benchmark(){prefs.SecondMonitor=false;StartPosition=FormStartPosition.Manual;Location=new Point(-30000,-30000);var stop=new Timer{Interval=10000};stop.Tick+=(s,e)=>{stop.Stop();using(var p=System.Diagnostics.Process.GetCurrentProcess()){p.Refresh();File.WriteAllText(Path.Combine(home,"performance.txt"),"10-second offscreen lobby smoke test; includes startup CPU. Not an in-game benchmark.\nUI working set MB: "+(p.WorkingSet64/1048576.0).ToString("0.0")+"; cumulative CPU seconds: "+p.TotalProcessorTime.TotalSeconds.ToString("0.000")+"\n"+client.Metrics());}stop.Dispose();Close();};stop.Start();}
 protected override void Dispose(bool disposing){if(disposing){overlay.Dispose();mobile.Dispose();audioTimer.Dispose();audio.Dispose();timer.Dispose();client.Dispose();foreach(var p in portraits.Values)p.Dispose();normal.Dispose();small.Dispose();title.Dispose();bold.Dispose();cooldown.Dispose();compactCd.Dispose();micro.Dispose();}base.Dispose(disposing);}
}
static class Program {
 [STAThread] static int Main(string[] args){try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);string home=AppDomain.CurrentDomain.BaseDirectory;
  if(args.Contains("--test")){Tests.Run(home);return 0;}
  if(args.Contains("--audio-export")){foreach(var cue in new[]{AttentionCue.SecondClick,AttentionCue.TenSeconds,AttentionCue.ThirtySeconds,AttentionCue.Minute})File.WriteAllBytes(Path.Combine(home,cue+".wav"),AttentionAudio.Wave(cue,25));try{using(var voice=new System.Speech.Synthesis.SpeechSynthesizer()){voice.SetOutputToWaveFile(Path.Combine(home,"voice-preview.wav"));voice.Speak("Jungle check. A gank is possible. Check the minimap and river vision before pushing. Caitlyn is back alive.");}File.WriteAllText(Path.Combine(home,"audio-validation.txt"),"Three PCM tone files generated. Windows speech successfully synthesized the jungle-check and respawn messages to a WAV file. Audio device playback still needs user verification.");}catch(Exception ex){File.WriteAllText(Path.Combine(home,"audio-validation.txt"),"PCM tones generated; speech unavailable: "+ex.ToString());}return 0;}
  if(args.Contains("--probe")){using(var c=new LeagueClient(new DataStore(Path.Combine(home,"data")))){var s=c.Poll().GetAwaiter().GetResult();File.WriteAllText(Path.Combine(home,"connection-test.txt"),"Phase: "+s.Phase+"\nAccount detected: "+(s.Account!="")+"\nPlayers: "+s.Players.Count+"\n"+s.Notice);}return 0;}
  using(var form=new Dashboard(home,args.Contains("--demo")||args.Contains("--render-settings")||args.Contains("--render")||args.Contains("--render-practice"))){if(args.Contains("--render-practice")){form.RenderPractice();return 0;}if(args.Contains("--render-settings")){form.Settings(home);return 0;}if(args.Contains("--render")){form.Render(Path.Combine(home,"demo-match.png"),false);form.Render(Path.Combine(home,"demo-draft.png"),true);form.Render(Path.Combine(home,"demo-postgame.png"),false,true);return 0;}if(args.Contains("--benchmark"))form.Benchmark();Application.Run(form);}return 0;
 }catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error.log"),ex.ToString());if(!args.Contains("--test"))MessageBox.Show(ex.Message,"Rift Ready");return 1;}}
}
public static class Tests {
 public static void Run(string home){var d=new DataStore(Path.Combine(home,"data"));int checks=0;
  Action<bool,string> assert=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);checks++;};
  assert(d.Duration(100)=="1m 40s"&&d.Duration(300)=="5m 0s","mixed duration format");
  assert(d.Duration(59.5)=="1m 0s"&&d.Duration(60)=="1m 0s","one-minute format boundary");
  assert(d.Duration(119.99)=="2m 0s","duration rounding carries minutes");
  assert(d.Duration(7.5)=="8s"&&d.Duration(7.4)=="7s","whole-second rounding");
  assert(AttentionAudio.JungleMessage(new Snapshot())=="Jungle check.","brief jungle cue");
  d.MinutesAndSeconds=false;assert(d.Duration(100)=="100s","seconds-only preference");
  assert(Math.Abs(DataStore.Cooldown(100,25)-80)<0.001,"haste calculation");assert(DataStore.Cooldown(100,0)==100,"zero haste");
  var p=new Player{Champion="Vayne",Level=9,Haste=0};p.Ranks["Q"]=5;p.Ranks["W"]=2;p.Ranks["E"]=0;
  assert(d.Spell(p,0,true).StartsWith("2s"),"Vayne learned Q rank");assert(d.Spell(p,1,true).StartsWith("Passive"),"Vayne passive W");assert(d.Spell(p,2,true).StartsWith("Unlearned"),"unlearned spell");
  p.Items.Add(3158);assert(d.ItemHaste(p,false)==10,"boot ability haste");assert(d.ItemHaste(p,true)==10,"separate summoner haste");
  p.Summoners.Add("Flash");assert(d.Summoner(p,0,true).StartsWith("273s"),"summoner haste calculation");
  assert(d.Resolve("Lee Sin")=="LeeSin","champion display-name mapping");assert(d.Resolve("67")=="Vayne","draft champion id mapping");
  assert(d.Spell(new Player{Champion="Missing"},0,true).StartsWith("Unavailable"),"unknown data fallback");
  assert(d.Spell(new Player{Champion="Vi",Level=9},2,true).StartsWith("Charges"),"charge spells are not ordinary cooldowns");
  p.CosmicInsight=true;assert(d.Summoner(p,0,true).StartsWith("234s"),"known Cosmic Insight stacks with summoner item haste");
  var laneFixture=new Snapshot();
  string[] roles={"TOP","JUNGLE","MIDDLE","BOTTOM","UTILITY"};
  for(int i=0;i<10;i++)laneFixture.Players.Add(new Player{Champion=i==3?"Vayne":i==8?"Caitlyn":i==9?"Nautilus":"Ahri",Team=i<5?"ALLY":"ENEMY",Role=roles[i%5],Self=i==3,Level=9});
  var lane=LaneView.Select(laneFixture);
  assert(lane.Allies.Count==2&&lane.Enemies.Count==2&&lane.Others.Count==6,"bot pair grouping");
  assert(lane.Enemies[0].Champion=="Caitlyn"&&lane.Enemies[1].Champion=="Nautilus","ADC/support alignment");
  assert(lane.Allies.Concat(lane.Enemies).Concat(lane.Others).Distinct().Count()==10,"every player shown exactly once");
  var cards=Matchups.Cards(d,lane,true);assert(cards.Length==6&&cards[0].Contains("Caitlyn")&&cards[2].Contains("Nautilus")&&cards[4].Contains("VAYNE"),"specific lane guidance");
  laneFixture.Players[3].Self=false;laneFixture.Players[0].Self=true;var top=LaneView.Select(laneFixture);
  assert(top.Allies.Count==1&&top.Enemies.Count==1&&top.Others.Count==8,"solo lane grouping");
  laneFixture.Players[0].Role="";var unknown=LaneView.Select(laneFixture);
  assert(unknown.Enemies.Count==0&&unknown.Others.Count==9,"unknown role does not guess opponent");
  assert(LaneView.Role("support")=="UTILITY"&&LaneView.Role("mid")=="MIDDLE","client role spelling normalization");
  var beat=new AttentionSchedule();
  Func<double,bool,bool,AttentionCue> step=(t,tones,voice)=>beat.Observe(t,true,tones,voice,120,90,480);
  assert(step(9,true,false)==AttentionCue.None,"no sound on attach");
  assert(step(10,true,false)==AttentionCue.TenSeconds,"ten second note");
  assert(step(10.5,true,false)==AttentionCue.None,"no duplicate beat");
  step(29,true,false);assert(step(30,true,false)==AttentionCue.ThirtySeconds,"half minute replaces normal beat");
  step(59,true,false);assert(step(60,true,false)==AttentionCue.Minute,"minute replaces both shorter beats");
  step(119,true,true);assert(step(120,true,true)==AttentionCue.JungleCheck,"voice wins coinciding minute cue");
  step(209,true,true);assert(step(210,true,true)==AttentionCue.JungleCheck,"configured reminder interval");
  step(569,false,true);assert(step(570,false,true)==AttentionCue.None,"reminders stop at configured end");
  beat.Reset();assert(step(120,true,true)==AttentionCue.None,"no old warning on reconnect");
  assert(step(0,true,true)==AttentionCue.None,"clock reset silent");assert(step(80,true,true)==AttentionCue.None,"no catchup after clock jump");
  assert(beat.Observe(90,false,true,true,120,90,480)==AttentionCue.None,"inactive game silent");
  beat.Reset();step(9,false,false);assert(step(10,false,false)==AttentionCue.None,"disabled tones silent");
  beat.Reset();step(29,true,false);assert(step(29,true,false)==AttentionCue.None,"paused game clock silent");
  beat.Reset();beat.Observe(4,true,true,false,120,90,480,false,false,false,true);assert(beat.Observe(5,true,true,false,120,90,480,false,false,false,true)==AttentionCue.SecondClick,"optional every-second click");
  beat.Reset();beat.Observe(59,true,true,false,120,90,480,true,true,true,true);assert(beat.Observe(60,true,true,false,120,90,480,true,true,true,true)==AttentionCue.Minute,"longest enabled interval wins");
  beat.Reset();beat.Observe(59,true,true,false,120,90,480,true,true,false,true);assert(beat.Observe(60,true,true,false,120,90,480,true,true,false,true)==AttentionCue.ThirtySeconds,"disabled minute falls back to enabled 30s");
  beat.Reset();beat.Observe(29,true,true,false,120,90,480,false,false,false,true);assert(beat.Observe(30,true,true,false,120,90,480,false,false,false,true)==AttentionCue.SecondClick,"click-only configuration");
  beat.Reset();beat.Observe(59,true,true,false,120,90,480,false,false,false,false);assert(beat.Observe(60,true,true,false,120,90,480,false,false,false,false)==AttentionCue.None,"all intervals independently disabled");
  Func<double,bool?,bool?,Snapshot> respawnState=(time,adc,jg)=>{var snap=new Snapshot{Phase="In game",Account="test",Time=time};snap.Players.Add(new Player{Champion="Vayne",Team="A",Role="BOTTOM",Self=true});snap.Players.Add(new Player{Champion="Caitlyn",Team="B",Role="BOTTOM",Dead=adc});snap.Players.Add(new Player{Champion="Vi",Team="B",Role="JUNGLE",Dead=jg});return snap;};
  var watcher=new RespawnWatch();assert(watcher.Observe(respawnState(100,true,true),d,true,15)==null,"no respawn on attach");
  assert(watcher.Observe(respawnState(105,false,false),d,true,15)=="Caitlyn is back alive.","lane-only respawn filter");
  assert(watcher.Observe(respawnState(110,false,false),d,true,15)==null,"respawn is announced once");
  watcher.Reset();watcher.Observe(respawnState(100,true,true),d,false,15);assert(watcher.Observe(respawnState(105,false,false),d,false,15).Contains("Vi"),"all enemies respawn option");
  watcher.Reset();watcher.Observe(respawnState(100,null,null),d,false,15);assert(watcher.Observe(respawnState(105,false,false),d,false,15)==null,"unknown death state does not fabricate respawn");
  watcher.Reset();watcher.Observe(respawnState(100,true,true),d,false,15);assert(watcher.Observe(respawnState(160,false,false),d,false,15)==null,"stale respawn suppressed");
  watcher.Reset();watcher.Observe(respawnState(100,true,true),d,false,15);assert(watcher.Observe(respawnState(0,false,false),d,false,15)==null,"new match does not announce old respawn");
  assert(AttentionAudio.Wave(AttentionCue.Minute,25).Length>AttentionAudio.Wave(AttentionCue.ThirtySeconds,25).Length&&AttentionAudio.Wave(AttentionCue.ThirtySeconds,25).Length>AttentionAudio.Wave(AttentionCue.TenSeconds,25).Length,"distinct note pattern lengths");
  foreach(var c in d.Champions.Keys)for(int slot=0;slot<4;slot++)assert(!string.IsNullOrEmpty(d.Spell(new Player{Champion=c,Level=18},slot,true)),"all champion spell coverage");
  CoachingTests.Run(d,home,assert);
  File.WriteAllText(Path.Combine(home,"test-results.txt"),checks+" checks passed. Covers formulas, rank handling, missing data, haste, spell records, coaching conditions, preferences migration and reflection storage. Does not validate live League integration or special mechanics.");
 }
}
}
