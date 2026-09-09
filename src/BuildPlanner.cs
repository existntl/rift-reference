using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace RiftReference {
public static class LoadoutTransfer {
 public static async System.Threading.Tasks.Task Apply(DataStore data,string champion,object body,bool runes,Func<string,string,object,System.Threading.Tasks.Task<object>> request){
  var me=await request("/lol-summoner/v1/current-summoner","GET",null);long id=(long)J.N(me,"summonerId");if(id<=0)throw new IOException("Your client profile is unavailable.");
  if(Convert.ToString(await request("/lol-gameflow/v1/gameflow-phase","GET",null))!="ChampSelect")throw new IOException("Apply is available only during champion select.");
  var draft=await request("/lol-champ-select/v1/session","GET",null);
  if(J.Get(draft,"localPlayerCellId")==null)throw new IOException("Your draft slot is unavailable.");
  var self=J.A(J.Get(draft,"myTeam")).FirstOrDefault(p=>J.N(p,"cellId")==J.N(draft,"localPlayerCellId"));
  if(self==null||data.Champion(champion)==null||data.Resolve(J.S(self,"championId"))!=champion)throw new IOException("Your selected champion changed. Reopen the build planner for the new pick.");
  try{await request(runes?"/lol-perks/v1/pages":"/lol-item-sets/v1/item-sets/"+id+"/sets","POST",body);}
  catch(IOException){throw new IOException(runes?"League did not confirm the rune page. Check the client; if pages are full, free a slot yourself. No existing page was deleted.":"League did not confirm the item set. Check your custom sets before retrying to avoid a duplicate.");}
 }
}
public sealed class BuildChoice {
 public int Id;public string Name;public BuildChoice(int id,string name){Id=id;Name=name;}
 public override string ToString(){return Name;}
}
public static class Loadouts {
 public static object[] Trees(DataStore d){return J.A(J.Parse(File.ReadAllText(Path.Combine(d.Root,"runes.json"))));}
 public static object RunePage(DataStore d,string champion,int primary,int secondary,int[] perks){
  var trees=Trees(d);var first=trees.FirstOrDefault(t=>J.N(t,"id")==primary);var second=trees.FirstOrDefault(t=>J.N(t,"id")==secondary);
  if(first==null||second==null||primary==secondary||perks==null||perks.Length!=9)throw new ArgumentException("Choose different rune trees and complete all rune slots.");
  var slots=J.A(J.Get(first,"slots"));
  for(int i=0;i<4;i++)if(!J.A(J.Get(slots[i],"runes")).Any(r=>J.N(r,"id")==perks[i]))throw new ArgumentException("Choose one primary rune from each row.");
  var secondarySlots=J.A(J.Get(second,"slots"));
  int[] rows=perks.Skip(4).Take(2).Select(id=>Array.FindIndex(secondarySlots,s=>J.A(J.Get(s,"runes")).Any(r=>J.N(r,"id")==id))).ToArray();
  if(rows.Any(i=>i<1)||rows[0]==rows[1])throw new ArgumentException("Choose two secondary runes from different rows.");
  int[][] shards={new[]{5008,5005,5007},new[]{5008,5010,5001},new[]{5011,5013,5001}};
  for(int i=0;i<3;i++)if(!shards[i].Contains(perks[6+i]))throw new ArgumentException("Choose a valid stat shard for each row.");
  return new {name="RR · "+d.Name(champion),primaryStyleId=primary,subStyleId=secondary,selectedPerkIds=perks,current=true};
 }
 public static object ItemSet(DataStore d,string champion,IEnumerable<KeyValuePair<string,string>> sections){
  int championId;if(!Int32.TryParse(J.S(d.Champion(champion),"key"),out championId))throw new ArgumentException("Choose a champion first.");
  var blocks=new List<object>();
  foreach(var section in sections){
   if(String.IsNullOrWhiteSpace(section.Value))continue;
   string title=(section.Key??"").Trim();if(title.Length==0||title.Length>60)throw new ArgumentException("Give every populated section a name of 1–60 characters.");
   var items=new List<object>();
   foreach(string text in section.Value.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries)){
    string value=text.Trim();var matches=d.Items.Where(kv=>(kv.Key==value||J.S(kv.Value,"name").Equals(value,StringComparison.OrdinalIgnoreCase))&&Convert.ToBoolean(J.Get(J.Get(kv.Value,"maps"),"11")??false)&&Convert.ToBoolean(J.Get(J.Get(kv.Value,"gold"),"purchasable")??false)).ToArray();
    if(matches.Length!=1)throw new ArgumentException("Unknown or ambiguous Summoner's Rift item: "+value+". Choose it from the item picker.");
    items.Add(new{id=matches[0].Key,count=1});
   }
   if(items.Count>20)throw new ArgumentException("Use at most 20 items per section.");
   if(items.Count>0)blocks.Add(new{type=title,items=items.ToArray()});
  }
  if(blocks.Count==0||blocks.Count>12)throw new ArgumentException("Add items to 1–12 custom sections.");
  return new{title="RR · "+d.Name(champion),type="custom",map="SR",mode="CLASSIC",associatedChampions=new[]{championId},associatedMaps=new[]{11},preferredItemSlots=new object[0],sortrank=0,startedFrom="blank",uid=Guid.NewGuid().ToString(),blocks=blocks.ToArray()};
 }
}

public sealed class BuildPlanner:Form {
 readonly DataStore data;readonly LeagueClient client;readonly Func<bool> demo;readonly LoadoutIcons icons;
 RiftComboBox champion;RuneEditor runeEditor;ItemEditor itemEditor;
 Label status;TextBox source;Button applyRunes,applyItems;bool applying;
 public BuildPlanner(DataStore d,LeagueClient c,string selected,Func<bool> isDemo){
  data=d;client=c;demo=isDemo;icons=new LoadoutIcons(d);Text="Rift Ready · Runes and builds";ClientSize=new Size(1280,860);MinimumSize=Size;MaximumSize=Size;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.None;
  LabelAt("RIFT READY  /  LOADOUTS",24,16,1100,32,16);
  LabelAt("Your rune page and custom shop path · data "+d.Version,24,53,1100,26,10);
  champion=new RiftComboBox{Location=new Point(24,91),Size=new Size(230,30),AccessibleName="Champion"};Controls.Add(champion);
  foreach(var entry in d.Champions.OrderBy(x=>d.Name(x.Key)))champion.Items.Add(new BuildChoice(Int32.Parse(J.S(entry.Value,"key")),d.Name(entry.Key)));
  SelectChampion(selected);
  AddButton("Onetricks.gg",270,88,145,()=>OpenSource("https://www.onetricks.gg/champions/builds/"));
  AddButton("Probuilds",425,88,135,()=>OpenSource("https://probuilds.net/champions/details/"));
  AddButton("Diamond+ choices…",580,88,220,()=>{using(var picker=new RecommendationPicker(data,SelectedChampion()))if(picker.ShowDialog(this)==DialogResult.OK)RestorePlan(picker.PlanJson);});
  LabelAt("Riot matches · NA / EUW / KR",818,91,420,32,9);
  LabelAt("Source / patch note",24,137,170,23,9);source=new TextBox{Location=new Point(190,133),Size=new Size(1062,28),MaxLength=300,Text="My choices · compare the source's role, patch and sample size"};Controls.Add(source);
  var runeTab=new Panel{Location=new Point(24,213),Size=new Size(1232,538)};var itemTab=new Panel{Location=runeTab.Location,Size=runeTab.Size,Visible=false};Controls.Add(runeTab);Controls.Add(itemTab);
  var runeNav=new NavigationButton{Text="Rune page",Location=new Point(24,177),Size=new Size(155,34),Active=true};var itemNav=new NavigationButton{Text="Item sets",Location=new Point(185,177),Size=new Size(155,34)};Controls.Add(runeNav);Controls.Add(itemNav);
  runeNav.Click+=(s,e)=>{runeTab.Visible=true;itemTab.Visible=false;runeNav.Active=true;itemNav.Active=false;runeNav.Invalidate();itemNav.Invalidate();};itemNav.Click+=(s,e)=>{runeTab.Visible=false;itemTab.Visible=true;runeNav.Active=false;itemNav.Active=true;runeNav.Invalidate();itemNav.Invalidate();};
  runeEditor=new RuneEditor(d,icons){Location=new Point(3,3)};runeTab.Controls.Add(runeEditor);
  itemEditor=new ItemEditor(d,icons){Location=new Point(3,3)};itemTab.Controls.Add(itemEditor);
  AddButton("Save plan…",24,768,125,Save);AddButton("Load plan…",159,768,125,LoadPlan);
  applyRunes=AddButton("Preview / apply runes",724,768,247,()=>Apply(true));applyItems=AddButton("Preview / apply item set",986,768,270,()=>Apply(false));
  status=LabelAt("Nothing is applied automatically. Demo mode allows editing and saving, but cannot change League.",24,815,1232,35,10);
  FormClosing+=(s,e)=>{if(applying)e.Cancel=true;};Theme.Apply(this);
 }
 void SelectChampion(string key){champion.SelectedItem=champion.Items.Cast<BuildChoice>().FirstOrDefault(x=>x.Id==(int)J.N(data.Champion(key),"key"));}
 string SelectedChampion(){var choice=champion.SelectedItem as BuildChoice;if(choice==null)throw new ArgumentException("Select a champion first.");return data.Resolve(choice.Id.ToString());}
 void OpenSource(string url){try{Process.Start(new ProcessStartInfo(url+Uri.EscapeDataString(SelectedChampion())){UseShellExecute=true});}catch(Exception e){status.Text=e.Message;}}
 Label LabelAt(string text,int x,int y,int w,int h,int size){var label=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Segoe UI",size),ForeColor=Theme.Ink};Controls.Add(label);return label;}
 Button AddButton(string text,int x,int y,int w,Action action){var button=new Button{Text=text,Location=new Point(x,y),Size=new Size(w,36)};button.Click+=(s,e)=>{try{action();}catch(Exception ex){status.Text=ex.Message;}};Controls.Add(button);return button;}
 object Payload(bool rune){string champ=SelectedChampion();return rune?Loadouts.RunePage(data,champ,runeEditor.Primary,runeEditor.Secondary,runeEditor.Perks):Loadouts.ItemSet(data,champ,itemEditor.Sections());}
 async void Apply(bool rune){if(applying)return;try{
  var payload=Payload(rune);string champ=SelectedChampion();
  string preview=rune?String.Join("\n",runeEditor.Names()):String.Join("\n\n",itemEditor.Sections().Where(s=>!String.IsNullOrWhiteSpace(s.Value)).Select(s=>s.Key+": "+String.Join(", ",s.Value.Split(',').Select(v=>J.S(data.Items[v.Trim()],"name")))));
  if(MessageBox.Show(this,data.Name(champ)+"\n\n"+preview+"\n\n"+source.Text+"\n\nCreate "+(rune?"and select a new rune page":"a new custom item set")+" in League?","Review loadout",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return;
  if(demo()){status.Text="Demo preview complete. Nothing was sent to League.";return;}
  applying=true;applyRunes.Enabled=applyItems.Enabled=false;status.Text="Checking your selected champion and sending to League…";
  await client.ApplyLoadout(champ,payload,rune);status.Text=rune?"League accepted the new rune page. Check your runes in the client before lock-in.":"League accepted the custom item set. Choose RR · "+data.Name(champ)+" in the shop's Item Sets dropdown.";
 }catch(Exception e){status.Text=e.Message;}finally{applying=false;applyRunes.Enabled=applyItems.Enabled=true;}}
 public string SerializePlan(){return new JavaScriptSerializer().Serialize(new{format="rift-loadout-1",champion=SelectedChampion(),patch=data.Version,source=source.Text,primary=runeEditor.Primary,secondary=runeEditor.Secondary,perks=runeEditor.Perks,sections=itemEditor.Sections().Select(s=>new{name=s.Key,items=s.Value}).ToArray()});}
 public void RestorePlan(string json){if(json.Length>65536)throw new ArgumentException("Plan file is too large.");var raw=J.Parse(json);if(J.S(raw,"format")!="rift-loadout-1"||data.Champion(J.S(raw,"champion"))==null)throw new ArgumentException("Choose a saved Rift Ready loadout file.");var saved=J.A(J.Get(raw,"sections"));var ids=J.A(J.Get(raw,"perks")).Select(Convert.ToInt32).ToArray();int primary=(int)J.N(raw,"primary"),secondary=(int)J.N(raw,"secondary");
  // Parse item sections transactionally before changing the current plan.
  itemEditor.Restore(saved.Select(row=>new KeyValuePair<string,string>(J.S(row,"name"),J.S(row,"items"))));
  SelectChampion(J.S(raw,"champion"));runeEditor.Restore(primary,secondary,ids);source.Text=J.S(raw,"source");status.Text="Loaded plan from data "+J.S(raw,"patch")+". Review against your current patch before applying.";
 }
 void Save(){using(var dialog=new SaveFileDialog{Filter="Rift loadout (*.json)|*.json",FileName="RR-"+SelectedChampion()+".json"})if(dialog.ShowDialog(this)==DialogResult.OK){File.WriteAllText(dialog.FileName,SerializePlan(),System.Text.Encoding.UTF8);status.Text="Plan saved. No client changes.";}}
 void LoadPlan(){using(var dialog=new OpenFileDialog{Filter="Rift loadout (*.json)|*.json"})if(dialog.ShowDialog(this)==DialogResult.OK){if(new FileInfo(dialog.FileName).Length>65536)throw new ArgumentException("Plan file is too large.");RestorePlan(File.ReadAllText(dialog.FileName,System.Text.Encoding.UTF8));}}
 protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)icons.Dispose();}
}
}
