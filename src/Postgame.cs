using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
namespace RiftReference {
public sealed class MatchStats {
 public double? Kills,Deaths,Assists,Cs,Gold,Damage,Vision;
}
public static class Postgame {
 public static bool Active(string phase){return phase=="PreEndOfGame"||phase=="EndOfGame"||phase=="WaitingForStats";}
 public static bool LiveEnded(object root){return J.A(J.Get(J.Get(root,"events"),"Events")).Any(e=>J.S(e,"EventName")=="GameEnd");}
 public static double? Number(object raw,string key){double n;object value=J.Get(raw,key);return value!=null&&!(value is bool)&&Double.TryParse(Convert.ToString(value,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&!Double.IsInfinity(n)&&!Double.IsNaN(n)&&n>=0?(double?)n:null;}
 public static string Value(double? value){return value.HasValue?value.Value.ToString("N0",CultureInfo.InvariantCulture):"—";}
 public static string Ratio(double? value){return value.HasValue?value.Value.ToString("0.0",CultureInfo.InvariantCulture):"—";}
 public static double? PerMinute(double? value,double seconds){return seconds>0?value/(seconds/60):null;}
 public static string Duration(double seconds){return seconds>0?((int)seconds/60)+":"+((int)seconds%60).ToString("00"):"Unavailable";}
 public static string Kda(MatchStats m){return m==null?"— / — / —":Value(m.Kills)+" / "+Value(m.Deaths)+" / "+Value(m.Assists);}
 public static Snapshot Parse(DataStore d,object raw,long expectedGameId,string phase){
  double? gameId=Number(raw,"gameId");
  if(!Active(phase)||expectedGameId<=0||gameId!=expectedGameId||J.Get(raw,"invalid") is bool&&(bool)J.Get(raw,"invalid"))throw new InvalidDataException("Current match results are not ready.");
  var s=new Snapshot{Phase=phase,Mode=J.S(raw,"gameMode"),Time=Number(raw,"gameLength")??0,Result="Result unavailable",GameId=expectedGameId};
  var local=J.Get(raw,"localPlayer");string localId=J.S(local,"summonerId");
  foreach(var team in J.A(J.Get(raw,"teams")))foreach(var row in J.A(J.Get(team,"players"))){
   var stats=J.Get(row,"stats");var minions=Number(stats,"MINIONS_KILLED");var neutral=Number(stats,"NEUTRAL_MINIONS_KILLED");
   var p=new Player{Champion=d.Resolve(J.S(row,"championId")),Team=J.S(team,"teamId"),Role=J.S(row,"detectedTeamPosition"),Level=(int)(Number(row,"level")??0),Self=J.Get(row,"isLocalPlayer") is bool&&(bool)J.Get(row,"isLocalPlayer")||localId!=""&&localId!="0"&&localId==J.S(row,"summonerId"),Stats=new MatchStats{Kills=Number(stats,"CHAMPIONS_KILLED"),Deaths=Number(stats,"NUM_DEATHS"),Assists=Number(stats,"ASSISTS"),Cs=minions+neutral,Gold=Number(stats,"GOLD_EARNED"),Damage=Number(stats,"TOTAL_DAMAGE_DEALT_TO_CHAMPIONS"),Vision=Number(stats,"VISION_SCORE")}};
   foreach(var item in J.A(J.Get(row,"items"))){int id;if(Int32.TryParse(Convert.ToString(item),out id)&&id>0)p.Items.Add(id);}
   if(p.Self&&J.Get(team,"isWinningTeam") is bool)s.Result=(bool)J.Get(team,"isWinningTeam")?"Victory":"Defeat";
   s.Players.Add(p);
  }
  if(s.Players.Count==0||s.Players.Count>20)throw new InvalidDataException("Match scoreboard is unavailable.");
  if(s.Players.Count(p=>p.Self)>1)throw new InvalidDataException("Local player is ambiguous.");
  s.Notice="Final results from your League client · — means unavailable";return s;
 }
 public static string[] Cells(Player p,double seconds){var m=p.Stats??new MatchStats();return new[]{Kda(m),Value(m.Cs)+"  /  "+Ratio(PerMinute(m.Cs,seconds)),Value(m.Damage),Value(m.Gold),Value(m.Vision)};}
 public static string Items(DataStore d,Player p){return p==null||p.Items.Count==0?"Final items unavailable":String.Join(" · ",p.Items.Select(id=>d.Items.ContainsKey(id.ToString())?J.S(d.Items[id.ToString()],"name"):"Item "+id));}
 public static CoachingCard[] Summary(DataStore d,Snapshot s){
  var self=s.Players.FirstOrDefault(p=>p.Self);var m=self==null||self.Stats==null?new MatchStats():self.Stats;
  double? kda=m.Kills.HasValue&&m.Assists.HasValue&&m.Deaths.HasValue?(m.Kills+m.Assists)/Math.Max(1,m.Deaths.Value):null;
  return new[]{new CoachingCard("YOUR K / D / A",Kda(m)+"\n"+(m.Deaths==0&&m.Kills.HasValue&&m.Assists.HasValue?"Deathless":Ratio(kda)+" KDA ratio")),new CoachingCard("FARM",Value(m.Cs)+" CS\n"+Ratio(PerMinute(m.Cs,s.Time))+" CS / min"),new CoachingCard("DAMAGE TO CHAMPIONS",Value(m.Damage)+"\n"+Ratio(PerMinute(m.Damage,s.Time))+" / min"),new CoachingCard("GOLD / VISION",Value(m.Gold)+" gold\n"+Value(m.Vision)+" vision score")};
 }
 public static object Mobile(DataStore d,Snapshot s,bool enemiesLeft){
  var self=s.Players.FirstOrDefault(p=>p.Self);string own=self==null?"":self.Team;
  var teams=s.Players.GroupBy(p=>p.Team).OrderBy(g=>(g.Key==own)==enemiesLeft?1:0).Select(g=>new{title=own==""?"Team "+g.Key:g.Key==own?"Allies":"Enemies",players=g.Select(p=>new{name=d.Name(p.Champion)+(p.Self?" · You":""),role=p.Role,values=Cells(p,s.Time)}).ToArray()}).ToArray();
  return new{result=s.Result==""?"Results pending":s.Result,duration=Duration(s.Time),notice=s.Notice,summary=Summary(d,s).Select(c=>new{title=c.Title,body=c.Body}).ToArray(),items=Items(d,self),teams};
 }
}
}
