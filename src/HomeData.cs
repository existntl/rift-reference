using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RiftReference {
public class HomeProfile {
 public string Name="",Rank="",Notice=""; public int? LP,Wins,Losses;
 public string Tier="",Division="",Season="";public List<RankPoint> RankHistory=new List<RankPoint>();
 public List<HomeMatch> Matches=new List<HomeMatch>();
}
public class HomeMatch {
 public long GameId;
 public string Champion="",Role="",Result="";public int Queue;public double Duration;public DateTime? Played;
 public int? Kills,Deaths,Assists,CS,Vision,Damage;public double? KillParticipation,DamageShare;
 public int?[] Items=new int?[7];
}
public static class HomeData {
 public static string MostPlayedChampion(HomeProfile profile){return Filter(profile,"",0,100).Where(m=>!String.IsNullOrWhiteSpace(m.Champion)).GroupBy(m=>m.Champion,StringComparer.OrdinalIgnoreCase).OrderByDescending(group=>group.Count()).Select(group=>group.First().Champion).FirstOrDefault()??"";}
 public static List<HomeMatch> Filter(HomeProfile profile,string search,int queue,int limit){
  var rows=profile==null||profile.Matches==null?Enumerable.Empty<HomeMatch>():profile.Matches.Where(m=>m!=null);
  if(queue!=0)rows=rows.Where(m=>m.Queue==queue);
  string query=(search??"").Trim();if(query.Length>0)rows=rows.Where(m=>(m.Champion??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||(m.Role??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0);
  return rows.Take(Math.Max(1,Math.Min(100,limit))).ToList();
 }
 static int? Number(object o,string key){double n;var raw=J.Get(o,key);if(raw==null||raw is bool||!double.TryParse(Convert.ToString(raw,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out n)||double.IsNaN(n)||double.IsInfinity(n)||n<0||n>int.MaxValue||Math.Floor(n)!=n)return null;return (int)n;}
 static bool Self(object player,object summoner){string a=J.S(player,"puuid"),b=J.S(summoner,"puuid");if(a!=""&&b!="")return a==b;a=J.S(player,"summonerId");b=J.S(summoner,"summonerId");return a!=""&&a!="0"&&b!=""&&a==b;}
 static string Role(object p){string role=J.S(p,"teamPosition");var timeline=J.Get(p,"timeline");if(role=="")role=J.S(timeline,"lane");if(role=="BOTTOM"&&J.S(timeline,"role")=="DUO_SUPPORT")return "SUPPORT";if(role=="MIDDLE")return "MID";if(role=="BOTTOM")return "ADC";return role=="NONE"?"":role;}
 public static HomeProfile Parse(DataStore data,object summoner,object ranked,object history){
  var profile=new HomeProfile();string name=J.S(summoner,"gameName"),tag=J.S(summoner,"tagLine");profile.Name=name==""?J.S(summoner,"displayName"):name+(tag==""?"":"#"+tag);
  object solo=J.Get(J.Get(ranked,"queueMap"),"RANKED_SOLO_5x5");if(solo==null)solo=J.A(J.Get(ranked,"queues")).FirstOrDefault(q=>J.S(q,"queueType")=="RANKED_SOLO_5x5");
  if(solo!=null){string tier=J.S(solo,"tier"),division=J.S(solo,"division");profile.Rank=tier==""?"":tier=="NONE"||tier=="UNRANKED"?"Unranked":CultureInfo.InvariantCulture.TextInfo.ToTitleCase(tier.ToLowerInvariant())+(division==""||division=="NA"?"":" "+division);if(tier!="NONE"&&tier!="UNRANKED"&&tier!="")profile.LP=Number(solo,"leaguePoints");profile.Wins=Number(solo,"wins");profile.Losses=Number(solo,"losses");}
  profile.Tier=J.S(solo,"tier").ToUpperInvariant();profile.Division=J.S(solo,"division").ToUpperInvariant();var season=Number(solo,"seasonId");if(season.HasValue&&season.Value>0)profile.Season="season:"+season.Value.ToString(CultureInfo.InvariantCulture);
  var games=J.A(J.Get(J.Get(history,"games"),"games"));if(games.Length==0)games=J.A(J.Get(history,"games"));
  foreach(var game in games.Take(100)){
   var participants=J.A(J.Get(game,"participants"));var identities=J.A(J.Get(game,"participantIdentities")).Where(x=>Self(J.Get(x,"player"),summoner)).ToArray();object me=null;
   if(identities.Length==1){var id=Number(identities[0],"participantId");if(id.HasValue&&id>0){var found=participants.Where(p=>Number(p,"participantId")==id).ToArray();if(found.Length==1)me=found[0];}}
   if(me==null){var found=participants.Where(p=>Self(p,summoner)).ToArray();if(found.Length==1)me=found[0];}if(me==null)continue;
   var stats=J.Get(me,"stats");var m=new HomeMatch{Champion=data.Resolve(J.S(me,"championId")),Queue=Number(game,"queueId")??0,Duration=Number(game,"gameDuration")??0,Role=Role(me),Kills=Number(stats,"kills"),Deaths=Number(stats,"deaths"),Assists=Number(stats,"assists"),Vision=Number(stats,"visionScore"),Damage=Number(stats,"totalDamageDealtToChampions")};
   long gameId;if(Int64.TryParse(J.S(game,"gameId"),NumberStyles.None,CultureInfo.InvariantCulture,out gameId)&&gameId>0)m.GameId=gameId;
   for(int slot=0;slot<m.Items.Length;slot++)m.Items[slot]=Number(stats,"item"+slot);
   var cs=Number(stats,"totalMinionsKilled");var jungle=Number(stats,"neutralMinionsKilled");if(cs.HasValue&&jungle.HasValue&&(long)cs+jungle<=int.MaxValue)m.CS=cs+jungle;
   object win=J.Get(stats,"win");m.Result=win is bool?((bool)win?"Victory":"Defeat"):"Unknown";
   double stamp=J.N(game,"gameCreation");if(stamp>0&&stamp<253402300799000){try{m.Played=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddMilliseconds(stamp);}catch{}}
   var team=Number(me,"teamId");var allies=participants.Where(p=>team.HasValue&&Number(p,"teamId")==team).ToArray();
   // Summaries can contain only the local participant. Ratios need a complete team.
   if(allies.Length==5&&allies.Select(p=>Number(p,"participantId")).Distinct().Count()==5){
    var kills=allies.Select(p=>Number(J.Get(p,"stats"),"kills")).ToArray();var damage=allies.Select(p=>Number(J.Get(p,"stats"),"totalDamageDealtToChampions")).ToArray();
    long totalKills=kills.Sum(n=>(long)(n??0)),totalDamage=damage.Sum(n=>(long)(n??0));if(kills.All(n=>n.HasValue)&&totalKills>0&&m.Kills.HasValue&&m.Assists.HasValue&&(long)m.Kills+m.Assists<=totalKills)m.KillParticipation=((double)m.Kills+m.Assists.Value)/totalKills;
    if(damage.All(n=>n.HasValue)&&totalDamage>0&&m.Damage.HasValue)m.DamageShare=(double)m.Damage.Value/totalDamage;
   }
   profile.Matches.Add(m);
  }
  profile.Matches=profile.Matches.OrderByDescending(m=>m.Played??DateTime.MinValue).ToList();
  profile.Notice=history==null?"Recent matches are unavailable from the local League client.":profile.Matches.Count==0?"No recent matches could be matched to this account.":"Recent matches from the local League client.";
  if(profile.Rank=="")profile.Notice+=" Ranked data unavailable.";return profile;
 }
}
}
