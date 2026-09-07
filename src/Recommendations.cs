using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace RiftReference {
public static class Recommendations {
 public static string Patch(string version){return String.Join(".",(version??"").Split('.').Take(2));}
 public static async Task<object> Fetch(){
  string endpoint=Environment.GetEnvironmentVariable("RIFT_RECOMMENDATIONS_URL");
  if(String.IsNullOrWhiteSpace(endpoint))throw new IOException("Diamond+ data is not connected yet. Build choices will appear when the recommendation feed is available.");
  Uri uri;if(!Uri.TryCreate(endpoint,UriKind.Absolute,out uri)||!String.IsNullOrEmpty(uri.UserInfo)||(uri.Scheme!="https"&&!(uri.Scheme=="http"&&uri.IsLoopback)))throw new IOException("Recommendation feed must use HTTPS (local testing may use loopback HTTP).");
  using(var handler=new HttpClientHandler{AllowAutoRedirect=false})using(var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(20)}){
   using(var response=await client.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead)){
    if(!response.IsSuccessStatusCode)throw new IOException("Recommendation feed is unavailable. Try again later.");
    using(var stream=await response.Content.ReadAsStreamAsync())using(var output=new MemoryStream()){
     var buffer=new byte[8192];int count;var deadline=DateTime.UtcNow.AddSeconds(20);
     while(true){var read=stream.ReadAsync(buffer,0,buffer.Length);if(await Task.WhenAny(read,Task.Delay(20000))!=read)throw new IOException("Recommendation download timed out.");count=await read;if(count==0)break;if(output.Length+count>2000000||DateTime.UtcNow>deadline)throw new IOException("Recommendation feed exceeds download limits.");output.Write(buffer,0,count);}
     return J.Parse(System.Text.Encoding.UTF8.GetString(output.ToArray()));
    }
   }
  }
 }
 public static object[] Validate(DataStore data,object feed,string champion,string role){
  double now=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds,created=J.N(feed,"generatedAt");
  var regions=J.A(J.Get(feed,"regions")).Select(Convert.ToString).OrderBy(x=>x).ToArray();
  if(!new[]{"rift-diamond-1","rift-diamond-2"}.Contains(J.S(feed,"format"))||J.S(feed,"patch")!=Patch(data.Version)||J.N(feed,"queue")!=420||J.S(feed,"rankBasis")!="diamond-plus-at-collection"||!regions.SequenceEqual(new[]{"EUW1","KR","NA1"})||J.N(feed,"windowDays")!=7)throw new IOException("Feed does not match the current patch or the Diamond+ NA / EUW / KR solo-queue filters.");
  if(created>now+300||created<now-86400)throw new IOException("Recommendation feed is over 24 hours old or has an invalid timestamp. Refresh the publisher's data.");
  return J.A(J.Get(feed,"results")).Where(r=>J.N(r,"championId")==J.N(data.Champion(champion),"key")&&J.S(r,"role")==role).ToArray();
 }
 public static void ValidateChoice(object choice){double games=J.N(choice,"games"),wins=J.N(choice,"wins"),players=J.N(choice,"players");if(games<30||players<10||players>games||wins<0||wins>games||games!=Math.Floor(games)||wins!=Math.Floor(wins)||players!=Math.Floor(players))throw new IOException("This choice does not meet the 30-game / 10-player minimum.");}
 public static string Plan(DataStore data,string champion,string role,object rune,object item){
  ValidateChoice(rune);ValidateChoice(item);var page=J.Get(rune,"value");var ids=J.A(J.Get(page,"perks")).Select(Convert.ToInt32).ToArray();
  int primary=(int)J.N(page,"primary"),secondary=(int)J.N(page,"secondary");Loadouts.RunePage(data,champion,primary,secondary,ids);
  var items=J.A(J.Get(item,"value")).Select(Convert.ToInt32).ToArray();if(items.Length!=3||items.Distinct().Count()!=3)throw new IOException("Invalid three-item purchase path.");
  string path=String.Join(",",items);Loadouts.ItemSet(data,champion,new[]{new KeyValuePair<string,string>("Core purchase order",path)});
  return new JavaScriptSerializer().Serialize(new{format="rift-loadout-1",champion=champion,patch=data.Version,
   source="Riot matches · Diamond+ at collection · NA/EUW/KR · "+role+" · 7 days · runes "+J.N(rune,"games")+" games, core "+J.N(item,"games")+" games",primary=primary,secondary=secondary,perks=ids,sections=new[]{new{name="Core purchase order",items=path}}});
 }
 static int[] Numbers(object value,int minimum,int maximum){var raw=J.A(value);if(raw.Length<minimum||raw.Length>maximum)throw new IOException("Invalid recommendation sequence length.");return raw.Select(v=>{double n=Convert.ToDouble(v);if(Double.IsNaN(n)||Double.IsInfinity(n)||n<1||n>Int32.MaxValue||n!=Math.Floor(n))throw new IOException("Invalid recommendation identifier.");return (int)n;}).ToArray();}
 static void Items(DataStore data,int[] ids,bool purchasable){foreach(int id in ids){object item;if(!data.Items.TryGetValue(id.ToString(),out item)||!Convert.ToBoolean(J.Get(J.Get(item,"maps"),"11")??false)||(purchasable&&!Convert.ToBoolean(J.Get(J.Get(item,"gold"),"purchasable")??false)))throw new IOException("Build includes an item unavailable in this patch.");}}
 public static object[] Bundles(DataStore data,object feed,string champion,string role){
  var rows=Validate(data,feed,champion,role);if(J.S(feed,"format")!="rift-diamond-2")throw new IOException("The data publisher needs to refresh connected builds. Older independent choices cannot be combined automatically.");
  if(rows.Length>1)throw new IOException("Duplicate champion / role result.");if(rows.Length==0)return new object[0];var builds=J.A(J.Get(rows[0],"builds"));if(builds.Length>20)throw new IOException("Too many build alternatives.");
  var keys=new HashSet<string>();foreach(var build in builds){ValidateBundle(data,champion,build);var v=J.Get(build,"value");string key=J.N(v,"primary")+":"+J.N(v,"secondary")+":"+String.Join(",",Numbers(J.Get(v,"perks"),9,9))+":"+String.Join(",",Numbers(J.Get(v,"coreItems"),3,3));if(!keys.Add(key))throw new IOException("Duplicate build alternative.");}return builds;
 }
 public static void ValidateBundle(DataStore data,string champion,object build){
  ValidateChoice(build);var v=J.Get(build,"value");var perks=Numbers(J.Get(v,"perks"),9,9);Loadouts.RunePage(data,champion,(int)J.N(v,"primary"),(int)J.N(v,"secondary"),perks);
  var core=Numbers(J.Get(v,"coreItems"),3,3);if(core.Distinct().Count()!=3)throw new IOException("Core purchase path repeats an item.");Items(data,core,true);
  var details=J.Get(build,"details");foreach(string kind in new[]{"summonerSpells","startingItems","purchaseOrder","finalItems","skillOrder"}){
   var detail=J.Get(details,kind);if(detail==null)continue;ValidateChoice(detail);
   if(J.N(detail,"games")>J.N(build,"games")||J.N(detail,"players")>J.N(build,"players")||J.N(detail,"wins")>J.N(build,"wins")||J.N(detail,"games")-J.N(detail,"wins")>J.N(build,"games")-J.N(build,"wins"))throw new IOException("Detail sample exceeds its build group.");
   var ids=Numbers(J.Get(detail,"value"),kind=="summonerSpells"?2:1,kind=="summonerSpells"?2:kind=="finalItems"?6:kind=="skillOrder"?18:20);
   if(kind=="summonerSpells"){
    var spells=(Dictionary<string,object>)J.Get(J.Parse(File.ReadAllText(Path.Combine(data.Root,"summoner.json"))),"data");
    if(ids.Distinct().Count()!=2||ids.Any(id=>!spells.Values.Any(s=>J.N(s,"key")==id&&J.A(J.Get(s,"modes")).Select(Convert.ToString).Contains("CLASSIC"))))throw new IOException("Unknown Summoner's Rift spell pair.");
   }else if(kind=="skillOrder"){if(ids.Any(id=>id>4))throw new IOException("Unknown ability slot.");}
   else Items(data,ids,kind!="finalItems");
  }
 }
 public static string BundlePlan(DataStore data,string champion,string role,object build){
  ValidateBundle(data,champion,build);var value=J.Get(build,"value");var core=Numbers(J.Get(value,"coreItems"),3,3);
  var sections=new List<KeyValuePair<string,string>>{new KeyValuePair<string,string>("Core purchase order",String.Join(",",core))};
  foreach(var spec in new[]{new[]{"startingItems","Starting purchases"},new[]{"purchaseOrder","Observed purchase sequence"}}){var detail=J.Get(J.Get(build,"details"),spec[0]);if(detail!=null)sections.Add(new KeyValuePair<string,string>(spec[1],String.Join(",",Numbers(J.Get(detail,"value"),1,20))));}
  Loadouts.ItemSet(data,champion,sections);
  return new JavaScriptSerializer().Serialize(new{format="rift-loadout-1",champion=champion,patch=data.Version,source="Riot matches · Diamond+ at collection · NA/EUW/KR · "+role+" · 7 days · paired rune/core build: "+J.N(build,"games")+" games / "+J.N(build,"players")+" players",primary=(int)J.N(value,"primary"),secondary=(int)J.N(value,"secondary"),perks=Numbers(J.Get(value,"perks"),9,9),sections=sections.Select(s=>new{name=s.Key,items=s.Value}).ToArray()});
 }
}

}
