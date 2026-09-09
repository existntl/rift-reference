using System.Collections.Generic;
using System.Reflection;
using RiftReference;

// Compiled only into the standalone test executables, never RiftReference.exe or its package.
static class DashboardFixtures {
 public static Snapshot Demo(this Dashboard form,bool draft){
  var state=new Snapshot{Phase=draft?"ChampSelect":"In game",Account="VALIDATION FIXTURE",Mode="CLASSIC",Time=840,Demo=true};
  string[] names={"Ornn","Vi","Ahri","Vayne","Lulu","Darius","LeeSin","Syndra","Caitlyn","Nautilus"};
  string[] roles={"TOP","JUNGLE","MIDDLE","BOTTOM","UTILITY"};
  for(int i=0;i<10;i++){
   var player=new Player{Champion=names[i],Team=i<5?"ALLY":"ENEMY",Role=roles[i%5],Level=draft?1:(i%5==3?9:10),Self=i==3};
   player.Summoners.Add("Flash");player.Summoners.Add(i%5==1?"Smite":i%5==4?"Exhaust":"Heal");
   if(!draft){player.Items.Add(i%5==4?3158:3078);if(player.Self){player.Items.Clear();player.Ranks["Q"]=5;player.Ranks["W"]=2;player.Ranks["E"]=1;player.Ranks["R"]=1;}}
   state.Players.Add(player);
  }
  return state;
 }
 public static Snapshot Results(Snapshot state){
  state.Phase="EndOfGame";state.Time=1895;state.Result="Victory";state.Notice="VALIDATION FIXTURE · Not a real match";state.GameId=1;
  for(int i=0;i<state.Players.Count;i++){
   var player=state.Players[i];player.Level=16;if(player.Self)player.Items=new List<int>{3153,3006,3124};
   player.Stats=new MatchStats{Kills=i==3?12:3+i%4,Deaths=i==3?4:3+i%5,Assists=8+i,Cs=i==3?241:85+i*12,Gold=9500+i*510,Damage=i==3?32410:12300+i*1300,Vision=11+i*3};
  }
  return state;
 }
 public static void Render(this Dashboard form,string path,bool draft,bool postgame=false){
  var state=postgame?Results(form.Demo(false)):form.Demo(draft);
  typeof(Dashboard).GetField("state",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,state);
  form.Render(path);
 }
}
