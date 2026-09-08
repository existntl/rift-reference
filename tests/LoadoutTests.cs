using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using RiftReference;
class LoadoutTests {
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static void Reject(Action action,string name){try{action();}catch(ArgumentException){return;}throw new Exception(name);}
 static object Json(object value){return J.Parse(new JavaScriptSerializer().Serialize(value));}
 static object Field(object value,string name){return value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);}
 [STAThread]static int Main(){try{
  Application.EnableVisualStyles();string home=AppDomain.CurrentDomain.BaseDirectory;var d=new DataStore(Path.Combine(home,"data"));
  int[] perks={8005,9111,9104,8014,8473,8451,5005,5008,5001};
  var rune=Loadouts.RunePage(d,"Vayne",8000,8400,perks);Check(J.A(J.Get(Json(rune),"selectedPerkIds")).Length==9,"Rune schema");
  Reject(()=>Loadouts.RunePage(d,"Vayne",8000,8000,perks),"Same trees accepted");
  var invalid=(int[])perks.Clone();invalid[5]=8473;Reject(()=>Loadouts.RunePage(d,"Vayne",8000,8400,invalid),"Duplicate secondary accepted");
  invalid=(int[])perks.Clone();invalid[0]=999999;Reject(()=>Loadouts.RunePage(d,"Vayne",8000,8400,invalid),"Unknown rune accepted");
  var sections=new[]{new KeyValuePair<string,string>("Start","Doran's Blade, Health Potion"),new KeyValuePair<string,string>("Custom answer","1001")};
  var item=Loadouts.ItemSet(d,"Vayne",sections);var parsed=Json(item);var blocks=J.A(J.Get(parsed,"blocks"));Check(blocks.Length==2&&J.S(blocks[1],"type")=="Custom answer","Section names/order lost");
  Check(Convert.ToInt32(J.A(J.Get(parsed,"associatedChampions"))[0])==67,"Wrong champion association");
  Reject(()=>Loadouts.ItemSet(d,"Vayne",new[]{new KeyValuePair<string,string>("x","bogus item")}),"Unknown item accepted");
  Reject(()=>Loadouts.ItemSet(d,"Vayne",new KeyValuePair<string,string>[0]),"Empty set accepted");
  foreach(var phase in new[]{"In game","ChampSelect"})foreach(int picked in new[]{67,22}){
   var calls=new List<string>();Func<string,string,object,Task<object>> request=(path,method,body)=>{calls.Add(method+" "+path);object response=null;if(path.Contains("current-summoner"))response=Json(new{summonerId=123});else if(path.Contains("gameflow-phase"))response=phase;else if(path.Contains("champ-select"))response=Json(new{localPlayerCellId=2,myTeam=new[]{new{cellId=2,championId=picked}}});return Task.FromResult(response);};
   bool rejected=false;try{LoadoutTransfer.Apply(d,"Vayne",item,false,request).GetAwaiter().GetResult();}catch(IOException){rejected=true;}
   bool allowed=phase=="ChampSelect"&&picked==67;Check(rejected!=allowed,"Phase/pick guard");Check(calls.Count(c=>c.StartsWith("POST"))==(allowed?1:0),"Unexpected client mutation");Check(!calls.Any(c=>c.StartsWith("DELETE")||c.StartsWith("PUT")),"Existing content overwritten");
  }
  using(var dashboard=new Dashboard(home,true)){
   var state=dashboard.Demo(true);Check(Pregame.Composition(d,state).Contains("Ornn"),"Comp roster missing");state.Players.Clear();Check(Pregame.Composition(d,state).Contains("not identified"),"Missing team guessed");
  }
  using(var client=new LeagueClient(d))using(var form=new BuildPlanner(d,client,"Vayne",()=>true)){
   form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();
   Check(form.Width<=1920&&form.Height<=1040,"Editor does not fit 1080p working area");
   var editor=(RuneEditor)Field(form,"runeEditor");editor.Restore(8000,8400,perks);
   editor.ChooseRune(false,2,8473);Check(editor.Perks[4]==8451&&editor.Perks[5]==0,"Clicking selected secondary should clear it");
   editor.ChooseRune(false,2,8473);editor.ChooseRune(false,2,8444);Check(editor.Perks.Skip(4).Take(2).Contains(8444)&&!editor.Perks.Contains(8473),"Secondary row replacement failed");
   editor.ChooseTree(true,8400);Check(editor.Secondary==0&&editor.Perks.Take(6).All(id=>id==0),"Changing primary retained incompatible choices");editor.Restore(8000,8400,perks);
   var items=(ItemEditor)Field(form,"itemEditor");items.Restore(sections);items.SelectSection(0);items.AddItem(2003);Check(items.Blocks[0].Items.Count==3,"Repeated item lost");items.MoveItem(0,2,-1);items.RemoveItem(0,1);items.MoveSection(1,-1);Check(items.Blocks[0].Name=="Custom answer","Visual section reorder lost");
   items.Restore(new[]{new KeyValuePair<string,string>("Starting items","1055, 2003"),new KeyValuePair<string,string>("First recall","1001, 1042"),new KeyValuePair<string,string>("Core path","3153, 3006, 3124"),new KeyValuePair<string,string>("Situational","3036, 3091, 3026")});
   string saved=form.SerializePlan();editor.ChooseTree(true,8100);items.AddItem(2003);form.RestorePlan(saved);Check(editor.Perks.SequenceEqual(perks),"Rune save/load round trip changed selections");Check(items.Blocks[2].Items.SequenceEqual(new[]{3153,3006,3124}),"Item save/load order changed");
   string before=form.SerializePlan();Reject(()=>form.RestorePlan(saved.Replace("1055, 2003","unknown item")),"Invalid saved item accepted");Check(form.SerializePlan()==before,"Invalid plan partially overwrote current plan");
   var choices=(Panel)Field(editor,"choices");choices.Controls.OfType<LoadoutTile>().First(t=>t.AccessibleName=="Lethal Tempo").PerformClick();Check(editor.Perks[0]==8008,"Rune icon click did not select the rune");editor.Restore(8000,8400,perks);
   foreach(int tab in new[]{0,1}){form.Controls.OfType<NavigationButton>().Single(b=>b.Text==(tab==0?"Rune page":"Item sets")).PerformClick();Application.DoEvents();using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size));bmp.Save(Path.Combine(home,tab==0?"loadout-runes.png":"loadout-items.png"));}}
   var search=(TextBox)Field(items,"search");search.Text="Health Potion";var catalog=(FlowLayoutPanel)Field(items,"catalog");Check(catalog.Controls.OfType<LoadoutTile>().Count()==1,"Catalog search did not narrow items");catalog.Controls.OfType<LoadoutTile>().Single().PerformClick();Check(items.Blocks[0].Items.Last()==2003,"Catalog icon click did not add to selected section");
   search.Text="no item named this";Check(!catalog.Controls.OfType<LoadoutTile>().Any(),"No-results catalog retained old items");
   form.Close();
  }
  Console.WriteLine("PASS: loadout validation, custom sections, phase/champion guards, append-only client writes and editor renders.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
