using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using RiftReference;
using RiftReady.GameBarBridge;

class GameBarDisplayTests {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static object Raw(object value){return J.Parse(new JavaScriptSerializer().Serialize(value));}
 static object Player(string id,string team,string role,int item){return new{riotId=id,summonerName="PRIVATE-NAME",championName="Vayne",team=team,position=role,items=new[]{new{itemID=item,count=1}}};}
 static int Main(){try{
  var data=new DataStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data"));
  data.Items=new Dictionary<string,object>{{"1",Raw(new{name="Sword",gold=new{total=300}})},{"2",Raw(new{name="Blade",gold=new{total=1000},from=new[]{"1"}})}};
  var state=new Snapshot{Phase="In game",Mode="PRACTICETOOL",Time=120,Account="SECRET-ACCOUNT",Notice="SECRET-TOKEN",GameId=987654321,Overlay=new OverlaySnapshot{Champion="Vayne",Opponent="PRIVATE-NAME",InventoryKnown=true,Inventory=new List<int>{1},Gold=200,Stats=new LiveOverlayStats{Cs=12,CsPerMinute=6,Kills=1,Deaths=0,Assists=2}}};
  state.Players.Add(new RiftReference.Player{Account="SECRET-PUUID"});
  var players=new List<object>();for(int i=0;i<5;i++){players.Add(Player("SECRET-ALLY-"+i,"ORDER",ScoreboardData.Roles[i],2));players.Add(Player("SECRET-ENEMY-"+i,"CHAOS",ScoreboardData.Roles[i],1));}
  ScoreboardData.Populate(data,J.A(Raw(players.ToArray())),"ORDER",state.Overlay);
  state.Overlay.Buffs.Add(new BuffWindow{Name="Baron",Team="SECRET-TEAM",Ends=280});
  state.Overlay.Buffs.Add(new BuffWindow{Name="Elder",Ends=270});
  state.Overlay.Buffs.Add(new BuffWindow{Name="Fire",Ends=200});
  var opt=new OverlayOptions{Enabled=true,Champion="Vayne",Target=2,Stats=true};var now=DateTime.UtcNow;
  Func<bool,bool,DisplaySnapshot> create=(held,focused)=>DisplaySnapshotProducer.Create(data,state,opt,now,now,held,focused);
  var display=create(true,true);var json=DisplaySnapshotProducer.Serialize(display);
  Check(display.visible&&display.fresh&&display.goldVisible&&display.matchSeconds==120,"Fresh Practice Tool hidden");
  Check(display.allyTotal==5000&&display.enemyTotal==1500&&display.differences.All(v=>v==700),"Inventory calculations changed");
  Check(display.targetName=="Blade"&&display.neededGold==500&&display.owned==false,"Purchase components or held gold incorrect");
  Check(display.stats.Length==2&&display.stats[0].value=="6.0"&&display.stats[1].value=="—","Stats display changed or unknown invented");
  Check(display.buffs.Length==2&&display.buffs[0].remainingSeconds==160&&display.buffs[1].remainingSeconds==150,"Buff duration incorrect");
  Check(!json.Contains("SECRET")&&!json.Contains("PRIVATE")&&!json.Contains("Vayne")&&!json.Contains("TOP")&&!json.Contains("987654321"),"Private identity or role escaped DTO");
  Check(Encoding.UTF8.GetByteCount(json)<8192&&J.Get(J.Parse(json),"version")!=null,"Wire payload invalid");
  opt.AlliesLeft=false;Check(create(true,true).differences.All(v=>v==-700),"Reversed board sign incorrect");opt.AlliesLeft=true;
  display=create(false,true);Check(!display.goldVisible&&display.allyTotal==null&&display.enemyTotal==null&&display.differences.All(v=>!v.HasValue)&&display.purchaseVisible,"Tab release leaked gold or hid purchase");
  display=create(true,false);Check(display.fresh&&!display.visible&&display.targetName==null&&display.matchSeconds==null&&display.buffs.Length==0,"Focus loss leaked display");
  opt.Enabled=false;Check(!create(true,true).visible&&create(true,true).targetName==null,"Disabled overlay leaked data");opt.Enabled=true;
  foreach(double age in new[]{-1.0,4.001,100.0}){display=DisplaySnapshotProducer.Create(data,state,opt,now,now.AddSeconds(-age),true,true);Check(!display.fresh&&!display.visible&&display.allyTotal==null,"Stale/future snapshot displayed");}
  Check(DisplaySnapshotProducer.Create(data,state,opt,now,now.AddSeconds(-4),true,true).fresh,"Four-second boundary changed");
  state.Demo=true;Check(!create(true,true).fresh,"Demo displayed");state.Demo=false;
  state.Mode="ARAM";Check(!create(true,true).fresh,"Wrong mode displayed");state.Mode="PRACTICETOOL";
  opt.Champion="Ahri";display=create(true,true);Check(!display.purchaseVisible&&display.targetName==null&&display.neededGold==null,"Wrong champion purchase target leaked");opt.Champion="Vayne";
  state.Overlay.InventoryKnown=false;display=create(true,true);Check(display.purchaseVisible&&display.owned==null&&display.neededGold==null,"Unknown inventory became known");state.Overlay.InventoryKnown=true;
  state.Overlay.Gold=null;Check(create(true,true).neededGold==null,"Missing current gold invented");state.Overlay.Inventory.Add(2);display=create(true,true);Check(display.owned==true&&display.neededGold==0,"Owned target not recognized without held gold");
  state.Overlay.Buffs.Add(new BuffWindow{Name="Baron",Ends=250});Check(create(true,true).buffs.Length==1,"Ambiguous duplicate buff accepted");
  state.Time=280;Check(create(true,true).buffs.Length==0,"Expired buff remains");
  opt.Gold=false;opt.Purchase=false;opt.Stats=false;opt.Buffs=false;display=create(true,true);Check(!display.visible&&display.stats.Length==0&&display.buffs.Length==0&&display.targetName==null,"Panel toggles ignored");
  opt.Gold=true;state.Overlay=new OverlaySnapshot();ScoreboardData.Populate(data,J.A(Raw(players.Take(3).ToArray())),"ORDER",state.Overlay);display=create(true,true);Check(display.allyTotal==null&&display.enemyTotal==null&&display.differences[0]==700&&display.differences.Skip(1).All(v=>v==null),"Incomplete team values fabricated");
  opt.Purchase=true;state.Overlay.Champion="Vayne";data.Items["2"]=Raw(new{name=new string('x',129),gold=new{total=1000}});Check(!create(true,true).purchaseVisible,"Oversized name admitted");
  data.Items["2"]=Raw(new{name="Bad\nname",gold=new{total=1000}});Check(!create(true,true).purchaseVisible,"Control character admitted");
  state.Time=Double.NaN;Check(!create(true,true).fresh,"Nonfinite match time admitted");
  bool rejected=false;try{DisplaySnapshotProducer.Serialize(new DisplaySnapshot{targetName=new string('x',9000)});}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}Check(rejected,"Oversized payload serialized");
  Console.WriteLine(checks+" Game Bar display contract checks passed.");return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
