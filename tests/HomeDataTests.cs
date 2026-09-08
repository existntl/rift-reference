using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using RiftReference;
class HomeDataTests {
 static int checks;
 static object Json(object o){return J.Parse(new JavaScriptSerializer().Serialize(o));}
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
 static int Main(){try{
  var data=new DataStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data"));var summoner=Json(new{puuid="self",summonerId=42,gameName="Sample",tagLine="TEST"});
  var rank=Json(new{queueMap=new{RANKED_SOLO_5x5=new{tier="EMERALD",division="II",leaguePoints=0,wins=12,losses=8},RANKED_FLEX_SR=new{tier="DIAMOND",division="I",leaguePoints=99}}});
  var players=Enumerable.Range(1,5).Select(i=>new{participantId=i,teamId=100,championId=i==3?67:22,timeline=new{lane="BOTTOM",role="DUO_CARRY"},stats=new{kills=2,deaths=0,assists=3,totalMinionsKilled=100,neutralMinionsKilled=0,visionScore=0,totalDamageDealtToChampions=1000,win=false}}).ToArray();
  var game=Json(new{gameCreation=1700000000000L,gameDuration=1200,queueId=420,participantIdentities=new[]{new{participantId=3,player=new{puuid="self",summonerId=42}}},participants=players});
  var history=Json(new{games=new{games=new[]{game}}});var p=HomeData.Parse(data,summoner,rank,history);var m=p.Matches.Single();
  Check(p.Name=="Sample#TEST"&&p.Rank=="Emerald II"&&p.LP==0&&p.Wins==12,"solo rank and true zero");
  Check(m.Champion=="Vayne"&&m.Result=="Defeat"&&m.Role=="ADC"&&m.Queue==420,"identity selects third participant");
  Check(m.CS==100&&m.Vision==0&&m.Deaths==0,"explicit zero preserved");
  Check(m.KillParticipation==0.5&&m.DamageShare==0.2,"complete team ratios");
  Check(m.Played.HasValue&&m.Duration==1200,"match time and duration");
  Check(HomeData.Parse(data,Json(new{puuid="other",summonerId=999}),rank,history).Matches.Count==0,"different account cannot reuse matches");
  var g=(Dictionary<string,object>)game;g["participantIdentities"]=new object[0];
  Check(HomeData.Parse(data,summoner,rank,Json(new{games=new[]{game}})).Matches.Count==0,"no first participant fallback");
  g["participantIdentities"]=J.A(J.Get(Json(new{x=new[]{new{participantId=3,player=new{summonerId=42}}}}),"x"));
  Check(HomeData.Parse(data,summoner,rank,Json(new{games=new[]{game}})).Matches.Count==1,"summoner id legacy match");
  g["participants"]=new[]{Json(new{participantId=3,championId=67,teamId=100,stats=new{kills=0}})};g["gameDuration"]=0;
  m=HomeData.Parse(data,summoner,rank,Json(new{games=new[]{game}})).Matches.Single();
  Check(m.Kills==0&&!m.Deaths.HasValue&&!m.CS.HasValue&&!m.Damage.HasValue&&m.Result=="Unknown","missing stats distinct from zero");
  Check(m.Duration==0&&!m.KillParticipation.HasValue&&!m.DamageShare.HasValue,"partial team never infers ratios");
  p=HomeData.Parse(data,summoner,null,null);Check(p.Matches.Count==0&&!p.LP.HasValue&&p.Notice.Contains("unavailable"),"unavailable endpoints honest");
  p=HomeData.Parse(data,summoner,Json(new{queueMap=new{RANKED_FLEX_SR=new{tier="DIAMOND"}}}),history);Check(p.Rank==""&&!p.LP.HasValue,"flex cannot masquerade as solo");
  p=HomeData.Parse(data,summoner,Json(new{queues=new[]{new{queueType="RANKED_SOLO_5x5",tier="NONE",leaguePoints=0}}}),history);Check(p.Rank=="Unranked"&&!p.LP.HasValue,"unranked does not invent LP");
  p=HomeData.Parse(data,summoner,rank,Json(new{games=Enumerable.Repeat(game,120).ToArray()}));Check(p.Matches.Count==100,"extended history bounded to 100 matches");
  Console.WriteLine(checks+" home data checks passed");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
