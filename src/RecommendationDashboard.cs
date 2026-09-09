using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RiftReference {
// A complete bundle is selected as one unit: its details never come from another build.
public sealed class RecommendationPicker : MinimalWindow {
 readonly DataStore data; readonly LoadoutIcons icons; readonly ToolTip tips=new ToolTip();
 readonly RiftComboBox champions,role; readonly Button common,winRate,refresh,use,paths,options,allPaths;
 readonly Label status,selectionTitle,selectionSubtitle,pathFilter; readonly FlowLayoutPanel buildList,details;
 readonly Panel runePanel,skillPanel; object feed,selected; object[] builds=new object[0]; bool rankByWins,showOptions; int firstItemFilter;
 public string PlanJson {get;private set;}
 readonly LeagueClient client; readonly Func<bool> demo; readonly bool standalone;
 Button save,applyRunes,applyItems; bool applying,loading;
 sealed class ChampionChoice {public string Id,Name;public override string ToString(){return Name;}}
 public RecommendationPicker(DataStore d,string champ):this(d,champ,true){}
 public RecommendationPicker(DataStore d,string champ,bool fetchOnShown):this(d,champ,null,null,fetchOnShown,false){}
 public RecommendationPicker(DataStore d,string champ,LeagueClient c,Func<bool> isDemo):this(d,champ,c,isDemo,true,true){}
 public RecommendationPicker(DataStore d,string champ,LeagueClient c,Func<bool> isDemo,bool fetchOnShown):this(d,champ,c,isDemo,fetchOnShown,true){}
 RecommendationPicker(DataStore d,string champ,LeagueClient c,Func<bool> isDemo,bool fetchOnShown,bool mainScreen){
  client=c;demo=isDemo;standalone=mainScreen;
  data=d;icons=new LoadoutIcons(d);Text="Rift Ready · Builds & runes";ClientSize=new Size(1280,860+TitleHeight);MinimumSize=MaximumSize=Size;StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;Font=new Font("Segoe UI",10);BackColor=Theme.Background;ForeColor=Theme.Ink;Icon=Brand.Icon;
  LabelAt(this,"BUILD EXPLORER",24,17,360,32,18,Theme.Ink);
  LabelAt(this,"Anonymous Diamond+ match aggregates · NA / EUW / Korea · Ranked solo · Patch "+Recommendations.Patch(d.Version)+" · Last 7 days",24,55,1000,25,10,Theme.Muted);
  champions=new RiftComboBox{Location=new Point(646,19),Size=new Size(218,30),AccessibleName="Recommendation champion"};
  foreach(var key in d.Champions.Keys.OrderBy(k=>d.Name(k)))champions.Items.Add(new ChampionChoice{Id=key,Name=d.Name(key)});
  Controls.Add(champions);for(int i=0;i<champions.Items.Count;i++)if(((ChampionChoice)champions.Items[i]).Id==d.Resolve(champ))champions.SelectedIndex=i;if(champions.SelectedIndex<0)champions.SelectedIndex=0;
  role=new RiftComboBox{Location=new Point(880,19),Size=new Size(170,30),AccessibleName="Recommendation role"};role.Items.AddRange(new object[]{"TOP","JUNGLE","MIDDLE","BOTTOM","UTILITY"});role.SelectedIndex=3;Controls.Add(role);
  refresh=ButtonAt(this,"Refresh data",1066,16,190,34,async()=>await LoadFeed());
  common=ButtonAt(this,"Common",24,104,132,38,()=>{rankByWins=false;SortBuilds();});winRate=ButtonAt(this,"Win rate",162,104,136,38,()=>{rankByWins=true;SortBuilds();});
  paths=ButtonAt(this,"Paths",24,153,134,34,()=>{showOptions=false;SortBuilds();});options=ButtonAt(this,"Options",164,153,134,34,()=>{showOptions=true;SortBuilds();});
  allPaths=ButtonAt(this,"All paths",24,195,85,34,()=>{firstItemFilter=0;showOptions=false;SortBuilds();});allPaths.AccessibleName="Clear first core item filter";
  pathFilter=LabelAt(this,"Any first core item",117,201,181,24,9,Theme.Muted);pathFilter.AutoEllipsis=true;
  buildList=new FlowLayoutPanel{Location=new Point(24,241),Size=new Size(274,409),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Theme.Background};Controls.Add(buildList);
  LabelAt(this,"Runes and core items share a cohort. Each detail shows that cohort’s most common qualifying sequence.",24,665,267,60,9,Theme.Muted);
  LabelAt(this,"30+ games · 10+ players per choice\nNo verified pro-player feed connected.",24,728,267,44,9,Theme.Muted);
  selectionTitle=LabelAt(this,"Choose a build",326,102,528,32,15,Theme.Ink);
  selectionSubtitle=LabelAt(this,"Select one complete rune + core cohort; details never mix between builds.",326,138,528,42,9,Theme.Muted);
  runePanel=new Panel{Location=new Point(320,186),Size=new Size(536,412),BackColor=Theme.Panel};Controls.Add(runePanel);
  skillPanel=new Panel{Location=new Point(320,610),Size=new Size(536,162),BackColor=Theme.Panel};Controls.Add(skillPanel);
  details=new FlowLayoutPanel{Location=new Point(874,103),Size=new Size(382,670),AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Theme.Panel,Padding=new Padding(14,10,8,10)};Controls.Add(details);
  status=LabelAt(this,"No recommendation data loaded. Choose a build when data is available.",24,793,986,51,9,Theme.Muted);
  use=ButtonAt(this,"Use selected build",1032,797,224,42,UseBuild);use.Enabled=false;
  if(standalone){use.Visible=false;status.SetBounds(24,838,1232,42);MaximumSize=Size.Empty;ClientSize=new Size(1280,884+TitleHeight);MinimumSize=MaximumSize=Size;
   save=ButtonAt(this,"Save plan…",24,789,160,38,SavePlan);
   applyRunes=ButtonAt(this,"Preview / apply runes",724,789,247,38,()=>Apply(true));
   applyItems=ButtonAt(this,"Preview / apply item set",986,789,270,38,()=>Apply(false));
  }
  foreach(Control control in Controls.Cast<Control>().Where(control=>!(control is WindowCaptionButton)).ToArray())control.Top+=TitleHeight;
  Theme.Apply(champions);Theme.Apply(role);foreach(var button in new[]{common,winRate,refresh,use,paths,options,allPaths,save,applyRunes,applyItems})if(button!=null)Theme.Apply(button);
  UpdateActions();FormClosing+=(s,e)=>{if(applying)e.Cancel=true;};
  champions.SelectedIndexChanged+=(s,e)=>Populate();role.SelectedIndexChanged+=(s,e)=>Populate();
  EmptyDetails();StyleSort();if(fetchOnShown)Shown+=async(s,e)=>await LoadFeed();
 }
 string Champion {get{return ((ChampionChoice)champions.SelectedItem).Id;}}
 string Role {get{return Convert.ToString(role.SelectedItem);}}
 static Label LabelAt(Control parent,string text,int x,int y,int w,int h,int font,Color color){var label=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Segoe UI",font),ForeColor=color,BackColor=Color.Transparent};parent.Controls.Add(label);return label;}
 static Button ButtonAt(Control parent,string text,int x,int y,int w,int h,Action action){var button=new Button{Text=text,Location=new Point(x,y),Size=new Size(w,h),FlatStyle=FlatStyle.Flat,BackColor=Theme.Panel,ForeColor=Theme.Ink,Cursor=Cursors.Hand};button.FlatAppearance.BorderColor=Theme.Border;button.Click+=(s,e)=>action();parent.Controls.Add(button);return button;}
 static void Clear(Control panel){foreach(Control child in panel.Controls.Cast<Control>().ToArray())child.Dispose();}
 static int[] Values(object choice){return J.A(J.Get(choice,"value")).Select(Convert.ToInt32).ToArray();}
 static string Summary(object choice){return J.N(choice,"games").ToString("N0")+" games · "+J.N(choice,"players").ToString("N0")+" players · "+(100*J.N(choice,"wins")/Math.Max(1,J.N(choice,"games"))).ToString("0.0")+"% wins";}
 public void LoadFeedForPreview(object value){feed=value;Populate();}
 async Task LoadFeed(){if(loading||applying)return;loading=true;UpdateActions();status.Text="Loading anonymous match aggregates…";try{var value=await Recommendations.Fetch();if(!IsDisposed){feed=value;Populate();}}catch(Exception ex){if(!IsDisposed){feed=null;builds=new object[0];selected=null;Clear(buildList);EmptyDetails();status.Text=ex.Message;}}finally{loading=false;if(!IsDisposed)UpdateActions();}}
 void Populate(){if(buildList==null||use==null)return;selected=null;builds=new object[0];firstItemFilter=0;UpdateActions();Clear(buildList);EmptyDetails();StyleSort();if(feed==null)return;
  try{builds=Recommendations.Bundles(data,feed,Champion,Role);SortBuilds();status.Text=builds.Length==0?"No builds meet the 30-game / 10-player minimum for this champion and role.":"Win rates include games reaching three core items. Select a build to review its runes and purchases. Nothing is applied automatically.";}
  catch(Exception ex){builds=new object[0];Clear(buildList);EmptyDetails();status.Text=ex.Message;}
 }
 void StyleSort(){common.BackColor=rankByWins?Theme.Panel:Color.FromArgb(30,64,64);winRate.BackColor=rankByWins?Color.FromArgb(30,64,64):Theme.Panel;common.ForeColor=rankByWins?Theme.Ink:Theme.Accent;winRate.ForeColor=rankByWins?Theme.Accent:Theme.Ink;common.FlatAppearance.BorderColor=rankByWins?Theme.Border:Theme.Accent;winRate.FlatAppearance.BorderColor=rankByWins?Theme.Accent:Theme.Border;paths.BackColor=showOptions?Theme.Panel:Color.FromArgb(30,64,64);options.BackColor=showOptions?Color.FromArgb(30,64,64):Theme.Panel;paths.ForeColor=showOptions?Theme.Ink:Theme.Accent;options.ForeColor=showOptions?Theme.Accent:Theme.Ink;paths.FlatAppearance.BorderColor=showOptions?Theme.Border:Theme.Accent;options.FlatAppearance.BorderColor=showOptions?Theme.Accent:Theme.Border;allPaths.Text="All paths";allPaths.ForeColor=firstItemFilter==0?Theme.Muted:Theme.Accent;allPaths.FlatAppearance.BorderColor=firstItemFilter==0?Theme.Border:Theme.Accent;pathFilter.Text=firstItemFilter==0?"Any first core item":ItemName(firstItemFilter);tips.SetToolTip(pathFilter,pathFilter.Text);tips.SetToolTip(allPaths,firstItemFilter==0?"Options groups listed builds by their first core item.":"First core item: "+ItemName(firstItemFilter)+". Click to show all paths.");}
 static int FirstItem(object build){return Convert.ToInt32(J.A(J.Get(J.Get(build,"value"),"coreItems"))[0]);}
 void SortBuilds(){StyleSort();Clear(buildList);var available=builds.Where(b=>firstItemFilter==0||FirstItem(b)==firstItemFilter).ToArray();var ordered=rankByWins?available.OrderByDescending(b=>J.N(b,"wins")/Math.Max(1,J.N(b,"games"))).ThenByDescending(b=>J.N(b,"games")):available.OrderByDescending(b=>J.N(b,"games")).ThenByDescending(b=>J.N(b,"wins"));
  if(showOptions){BuildOptions();if(selected==null&&ordered.Any())SelectBuild(ordered.First());return;}
  foreach(var raw in ordered){var value=J.Get(raw,"value");var ids=J.A(J.Get(value,"coreItems")).Select(Convert.ToInt32).ToArray();int keystone=Convert.ToInt32(J.A(J.Get(value,"perks"))[0]);var button=new BundleButton{Raw=raw,Title=RuneName(keystone),Description=String.Join(" → ",ids.Select(ItemName)),SummaryText=Summary(raw),ItemIcons=ids.Select(id=>icons.Get("items",id)).ToArray(),Size=new Size(250,156),Margin=new Padding(0,0,0,9),AccessibleName=RuneName(keystone)+". "+Summary(raw),BackColor=Theme.Panel};button.Click+=(s,e)=>SelectBuild(raw);buildList.Controls.Add(button);}
  if(selected!=null&&available.Contains(selected))SelectBuild(selected);else if(buildList.Controls.Count>0)SelectBuild(((BundleButton)buildList.Controls[0]).Raw);
 }
 void BuildOptions(){
  var heading=new Label{Text="FIRST CORE ITEM\nShare of listed builds only",Size=new Size(250,44),ForeColor=Theme.Muted,Margin=new Padding(0,0,0,8)};buildList.Controls.Add(heading);
  double total=builds.Sum(b=>J.N(b,"games"));var groups=builds.GroupBy(FirstItem).Select(g=>new{Id=g.Key,Games=g.Sum(b=>J.N(b,"games")),Wins=g.Sum(b=>J.N(b,"wins")),Paths=g.Count()});
  var ordered=rankByWins?groups.OrderByDescending(g=>g.Wins/Math.Max(1,g.Games)).ThenByDescending(g=>g.Games):groups.OrderByDescending(g=>g.Games).ThenByDescending(g=>g.Wins);
  foreach(var group in ordered){int id=group.Id;string summary=group.Games.ToString("N0")+" games · "+(100*group.Wins/Math.Max(1,group.Games)).ToString("0.0")+"% wins";string share=(100*group.Games/Math.Max(1,total)).ToString("0.0")+"% of listed builds · "+group.Paths+" paths";var button=new FirstItemButton{Title=ItemName(id),SummaryText=summary,ShareText=share,ItemIcon=icons.Get("items",id),Chosen=firstItemFilter==id,Size=new Size(250,108),Margin=new Padding(0,0,0,9),AccessibleName="Filter first core item: "+ItemName(id)+". "+summary+". "+share};button.Click+=(s,e)=>{firstItemFilter=id;showOptions=false;SortBuilds();};buildList.Controls.Add(button);}
 }
 void SelectBuild(object raw){selected=raw;foreach(var button in buildList.Controls.OfType<BundleButton>()){button.Chosen=Object.ReferenceEquals(button.Raw,raw);button.Invalidate();}UpdateActions();var value=J.Get(raw,"value");var perks=J.A(J.Get(value,"perks")).Select(Convert.ToInt32).ToArray();selectionTitle.Text=data.Name(Champion)+" · "+RuneName(perks[0]);selectionSubtitle.Text=Summary(raw)+"\nRune page + three-item core combination";BuildRunes(value);BuildSkills(J.Get(J.Get(raw,"details"),"skillOrder"));BuildDetails(raw);}
 void UpdateActions(){bool ready=selected!=null&&!applying&&!loading;use.Enabled=ready;
  if(save!=null)save.Enabled=applyRunes.Enabled=applyItems.Enabled=ready;
  refresh.Enabled=!applying&&!loading;champions.Enabled=role.Enabled=common.Enabled=winRate.Enabled=paths.Enabled=options.Enabled=allPaths.Enabled=buildList.Enabled=!applying;
 }
 public string SelectedPlanJson(){if(selected==null||loading||applying)throw new InvalidOperationException("Select an available build first.");Recommendations.Bundles(data,feed,Champion,Role);return Recommendations.BundlePlan(data,Champion,Role,selected);}
 void SavePlan(){try{string json=SelectedPlanJson();using(var dialog=new SaveFileDialog{Filter="Rift loadout (*.json)|*.json",FileName="RR-"+Champion+".json"})if(dialog.ShowDialog(this)==DialogResult.OK){File.WriteAllText(dialog.FileName,json,System.Text.Encoding.UTF8);status.Text="Plan saved. Select it in Preferences > Game overlay to use its item targets.";}}catch(Exception ex){status.Text=ex.Message;}}
 async void Apply(bool rune){if(applying)return;try{
  var plan=J.Parse(SelectedPlanJson());string champ=J.S(plan,"champion");int[] perks=J.A(J.Get(plan,"perks")).Select(Convert.ToInt32).ToArray();
  var sections=J.A(J.Get(plan,"sections")).Select(row=>new KeyValuePair<string,string>(J.S(row,"name"),J.S(row,"items"))).ToArray();
  object payload=rune?Loadouts.RunePage(data,champ,(int)J.N(plan,"primary"),(int)J.N(plan,"secondary"),perks):Loadouts.ItemSet(data,champ,sections);
  string preview=rune?String.Join("\n",perks.Select(RuneName)):String.Join("\n\n",sections.Select(s=>s.Key+": "+String.Join(", ",s.Value.Split(',').Select(v=>ItemName(Int32.Parse(v.Trim()))))));
  if(MessageBox.Show(this,data.Name(champ)+"\n\n"+preview+"\n\n"+J.S(plan,"source")+"\n\nCreate "+(rune?"and select a new rune page":"a new custom item set")+" in League?","Review loadout",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return;
  if(demo!=null&&demo()){status.Text="Demo preview complete. Nothing was sent to League.";return;}
  if(client==null)throw new InvalidOperationException("League client is unavailable. Nothing was sent.");
  applying=true;UpdateActions();status.Text="Checking your selected champion and sending to League…";
  await client.ApplyLoadout(champ,payload,rune);
  if(!IsDisposed)status.Text=rune?"League accepted the new rune page. Check your runes in the client before lock-in.":"League accepted the custom item set. Choose RR · "+data.Name(champ)+" in the shop's Item Sets dropdown.";
 }catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}finally{applying=false;if(!IsDisposed)UpdateActions();}}
 void UseBuild(){if(selected==null)return;try{PlanJson=SelectedPlanJson();DialogResult=DialogResult.OK;}catch(Exception ex){status.Text=ex.Message;}}
 void EmptyDetails(){selectionTitle.Text="Choose a build";selectionSubtitle.Text="Selected runes and observed skill order";Clear(runePanel);Clear(skillPanel);Clear(details);LabelAt(runePanel,"RUNES",18,15,490,28,12,Theme.Accent);LabelAt(runePanel,"Rune choices appear here when a qualifying build is available.",18,75,490,70,11,Theme.Muted);LabelAt(skillPanel,"SKILL ORDER",18,15,490,25,11,Theme.Accent);LabelAt(skillPanel,"No observed sequence available.",18,56,490,55,10,Theme.Muted);foreach(string title in new[]{"SUMMONER SPELLS","STARTING ITEMS","CORE PURCHASE ORDER","MATCH-END ITEMS","SITUATIONAL ITEMS"})DetailSection(title,null,"Select a qualifying build to view its data.",false,false);}
 string RuneName(int id){foreach(var tree in Loadouts.Trees(data)){if(J.N(tree,"id")==id)return J.S(tree,"name");foreach(var slot in J.A(J.Get(tree,"slots")))foreach(var rune in J.A(J.Get(slot,"runes")))if(J.N(rune,"id")==id)return J.S(rune,"name");}switch(id){case 5008:return "Adaptive force";case 5005:return "Attack speed";case 5007:return "Ability haste";case 5001:return "Scaling health";case 5010:return "Move speed";case 5011:return "Health";case 5013:return "Tenacity";default:return "Rune "+id;}}
 string ItemName(int id){object item;return data.Items.TryGetValue(id.ToString(),out item)?J.S(item,"name"):"Item "+id;}
 string SpellName(int id){var spell=data.Summoners.Values.FirstOrDefault(s=>J.S(s,"key")==id.ToString());return spell==null?"Spell "+id:J.S(spell,"name");}
 void RuneTile(int id,int x,int y,int w,int h,bool chosen,string caption){var tile=new LoadoutTile{Icon=icons.Get("runes",id),Chosen=chosen,Round=true,Caption=caption,AccessibleName=caption+(chosen?", selected":", not selected"),Location=new Point(x,y),Size=new Size(w,h),BackColor=Theme.Panel,TabStop=chosen,Cursor=Cursors.Default};tips.SetToolTip(tile,caption+(chosen?" · selected":""));runePanel.Controls.Add(tile);}
 void BuildRunes(object value){Clear(runePanel);int primary=(int)J.N(value,"primary"),secondary=(int)J.N(value,"secondary");var perks=J.A(J.Get(value,"perks")).Select(Convert.ToInt32).ToArray();
  LabelAt(runePanel,"RUNES",16,12,500,24,12,Theme.Accent);
  for(int side=0;side<2;side++){int x=16+side*264,id=side==0?primary:secondary;var tree=Loadouts.Trees(data).First(t=>J.N(t,"id")==id);LabelAt(runePanel,RuneName(id)+(side==0?" · Primary":" · Secondary"),x,44,248,25,10,Theme.Ink);var rows=J.A(J.Get(tree,"slots"));
   for(int r=side==0?0:1;r<rows.Length;r++){var options=J.A(J.Get(rows[r],"runes"));int width=248/options.Length,y=77+(side==0?r:r-1)*74;for(int j=0;j<options.Length;j++){int rid=(int)J.N(options[j],"id");RuneTile(rid,x+j*width,y,width-3,70,perks.Take(6).Contains(rid),J.S(options[j],"name"));}}
  }
  int[][] shards={new[]{5008,5005,5007},new[]{5008,5010,5001},new[]{5011,5013,5001}};
  for(int row=0;row<3;row++)for(int col=0;col<3;col++){int id=shards[row][col];RuneTile(id,280+col*82,302+row*33,79,31,perks[row+6]==id,RuneName(id));}
 }
 void BuildSkills(object choice){Clear(skillPanel);LabelAt(skillPanel,"SKILL UPGRADE ORDER · POINTS SPENT",16,10,504,24,11,Theme.Accent);if(choice==null){LabelAt(skillPanel,"Not enough matching timeline samples yet.",16,49,504,52,10,Theme.Muted);return;}var sequence=Values(choice);var grid=new SkillGrid{Sequence=sequence,Location=new Point(16,40),Size=new Size(504,85),BackColor=Theme.Panel,AccessibleName="Observed skill upgrade order: "+String.Join(", ",sequence.Select((id,index)=>"upgrade "+(index+1)+" "+(id>=1&&id<=4?"QWER"[id-1].ToString():"unknown")))};skillPanel.Controls.Add(grid);LabelAt(skillPanel,J.N(choice,"games").ToString("N0")+" games · Upgrade number, not champion level. Blank = unobserved.",16,132,504,26,8,Theme.Muted);}
 void BuildDetails(object raw){Clear(details);var observations=J.Get(raw,"details");DetailSection("SUMMONER SPELLS",J.Get(observations,"summonerSpells"),"No qualifying spell pair yet.",true,false);DetailSection("STARTING ITEMS",J.Get(observations,"startingItems"),"No qualifying starting inventory yet.",false,false);
  var value=J.Get(raw,"value");DetailSection("THREE-ITEM CORE",new Dictionary<string,object>{{"value",J.Get(value,"coreItems")},{"games",J.Get(raw,"games")},{"wins",J.Get(raw,"wins")},{"players",J.Get(raw,"players")}},"",false,true);
  DetailSection("OBSERVED PURCHASE ORDER",J.Get(observations,"purchaseOrder"),"No qualifying component purchase sequence yet.",false,true);DetailSection("MATCH-END ITEMS",J.Get(observations,"finalItems"),"No qualifying match-end inventory yet.",false,false);DetailSection("SITUATIONAL ITEMS",null,"Not inferred from final inventory. Context-specific alternatives are not available yet.",false,false);
 }
 void DetailSection(string title,object choice,string empty,bool spells,bool ordered){var section=new Panel{Width=340,Height=95,BackColor=Theme.Panel,Margin=new Padding(0,0,0,10)};LabelAt(section,title,0,0,337,24,11,Theme.Accent);
  if(choice==null){LabelAt(section,empty,0,31,333,56,9,Theme.Muted);}else{var ids=Values(choice);int columns=5,tileWidth=66,rows=(ids.Length+columns-1)/columns;for(int i=0;i<ids.Length;i++){int id=ids[i];string name=spells?SpellName(id):ItemName(id);var tile=new LoadoutTile{Icon=icons.Get(spells?"spells":"items",id),Caption=(ordered?(i+1)+". ":"")+name,AccessibleName=(ordered?"Purchase "+(i+1)+", ":"")+name,Chosen=true,Location=new Point((i%columns)*tileWidth,31+(i/columns)*87),Size=new Size(63,83),BackColor=Theme.Panel,Cursor=Cursors.Default,TabStop=false};section.Controls.Add(tile);tips.SetToolTip(tile,name);}int y=33+rows*87;LabelAt(section,J.N(choice,"games").ToString("N0")+" matching games · "+J.N(choice,"players").ToString("N0")+" players",0,y,334,21,8,Theme.Muted);section.Height=y+27;}details.Controls.Add(section);
 }
 protected override void Dispose(bool disposing){if(disposing){tips.Dispose();icons.Dispose();}base.Dispose(disposing);}

 sealed class FirstItemButton:Button{
  public string Title,SummaryText,ShareText;public Image ItemIcon;public bool Chosen;
  public FirstItemButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;Cursor=Cursors.Hand;}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Theme.Background);Theme.Surface(g,new Rectangle(0,0,Width,Height),Chosen?Color.FromArgb(29,48,51):Theme.Panel,Theme.Accent);if(Chosen)using(var marker=new SolidBrush(Theme.Accent))g.FillRectangle(marker,1,10,3,Height-20);if(ItemIcon!=null)g.DrawImage(ItemIcon,new Rectangle(12,12,38,38));using(var font=new Font("Segoe UI",10,FontStyle.Bold))TextRenderer.DrawText(g,Title,font,new Rectangle(60,12,178,42),Theme.Ink,TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);using(var font=new Font("Segoe UI",8)){TextRenderer.DrawText(g,SummaryText,font,new Rectangle(12,59,228,20),Theme.Accent);TextRenderer.DrawText(g,ShareText,font,new Rectangle(12,81,228,20),Theme.Muted);}if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-10,Height-10),Theme.Accent,BackColor);}
 }
 sealed class BundleButton:Button{
  public object Raw;public string Title,Description,SummaryText;public Image[] ItemIcons;public bool Chosen;
  public BundleButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;Cursor=Cursors.Hand;}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Theme.Background);Theme.Surface(g,new Rectangle(0,0,Width,Height),Chosen?Color.FromArgb(29,48,51):Theme.Panel,Theme.Accent);if(Chosen)using(var brush=new SolidBrush(Theme.Accent))g.FillRectangle(brush,1,10,3,Height-20);using(var titleFont=new Font("Segoe UI",11,FontStyle.Bold))TextRenderer.DrawText(g,Title,titleFont,new Rectangle(13,11,224,25),Theme.Ink,TextFormatFlags.EndEllipsis);using(var textFont=new Font("Segoe UI",8)){TextRenderer.DrawText(g,Description,textFont,new Rectangle(13,40,224,40),Theme.Muted,TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);TextRenderer.DrawText(g,SummaryText,textFont,new Rectangle(13,126,228,25),Theme.Accent,TextFormatFlags.EndEllipsis);}for(int i=0;i<ItemIcons.Length;i++){var box=new Rectangle(13+i*49,83,34,34);if(ItemIcons[i]!=null)g.DrawImage(ItemIcons[i],box);using(var pen=new Pen(Chosen?Theme.Accent:Theme.Border))g.DrawRectangle(pen,box);if(i<ItemIcons.Length-1)TextRenderer.DrawText(g,"›",Font,new Point(box.Right+4,box.Y+7),Theme.Muted);}if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-10,Height-10),Theme.Accent,BackColor);}
 }
 sealed class SkillGrid:Control{
  public int[] Sequence=new int[0];
  public SkillGrid(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Theme.Panel);Color[] colors={Color.FromArgb(238,120,135),Color.FromArgb(91,196,226),Color.FromArgb(104,213,173),Color.FromArgb(239,199,102)};int cell=25;using(var font=new Font("Segoe UI",8)){for(int level=0;level<18;level++){int x=34+level*cell;TextRenderer.DrawText(g,(level+1).ToString(),font,new Rectangle(x,0,cell,18),Theme.Muted,TextFormatFlags.HorizontalCenter);for(int row=0;row<4;row++){int y=20+row*16;using(var brush=new SolidBrush(Color.FromArgb(19,21,27)))g.FillRectangle(brush,x+2,y,21,14);if(level<Sequence.Length&&Sequence[level]==row+1)TextRenderer.DrawText(g,"QWER"[row].ToString(),font,new Rectangle(x,y,cell,16),colors[row],TextFormatFlags.HorizontalCenter);}}for(int row=0;row<4;row++)TextRenderer.DrawText(g,"QWER"[row].ToString(),font,new Rectangle(0,20+row*16,27,16),colors[row],TextFormatFlags.HorizontalCenter);}}
 }
}
}
