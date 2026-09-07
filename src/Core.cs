using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RiftReference {
public static class J {
 public static object Parse(string s) { return new JavaScriptSerializer { MaxJsonLength=16000000 }.DeserializeObject(s); }
 public static object Get(object o,string k) { var d=o as Dictionary<string,object>; return d!=null && d.ContainsKey(k)?d[k]:null; }
 public static string S(object o,string k) { return Convert.ToString(Get(o,k)) ?? ""; }
 public static double N(object o,string k) { double n; return double.TryParse(S(o,k),out n)?n:0; }
 public static object[] A(object o) { return o as object[] ?? new object[0]; }
 public static string Clean(string s) { return WebUtility.HtmlDecode(Regex.Replace(s??"","<[^>]*>"," ")).Trim(); }
}
public class Player {
 public MatchStats Stats;
 public string Champion="", Account="", Team="", Role=""; public int Level=1; public bool Self;
 public List<int> Items=new List<int>(); public List<string> Summoners=new List<string>();
 public Dictionary<string,int> Ranks=new Dictionary<string,int>(); public double? Haste;
 public bool CosmicInsight;
 public bool? Dead;
}
public class Snapshot {
 public OverlaySnapshot Overlay;
 public string Result="";public long GameId;
 public string Phase="Waiting",Account="",Mode="",Notice=""; public double Time;
 public List<Player> Players=new List<Player>(); public bool Demo;
}
public class DataStore {
 public string Root,Version; public Dictionary<string,object> Champions=new Dictionary<string,object>();
 public Dictionary<string,string> Ids=new Dictionary<string,string>(); public Dictionary<string,object> Items,Summoners;
 public DataStore(string root) {
  Root=root; Version=File.ReadAllText(Path.Combine(root,"version.txt")).Trim();
  foreach(var f in Directory.GetFiles(Path.Combine(root,"champions"),"*.json")) {
   var d=(Dictionary<string,object>)J.Get(J.Parse(File.ReadAllText(f)),"data");
   foreach(var kv in d) {Champions[kv.Key]=kv.Value; Ids[J.S(kv.Value,"key")]=kv.Key;}
  }
  Items=(Dictionary<string,object>)J.Get(J.Parse(File.ReadAllText(Path.Combine(root,"item.json"))),"data");
  Summoners=(Dictionary<string,object>)J.Get(J.Parse(File.ReadAllText(Path.Combine(root,"summoner.json"))),"data");
 }
 public string Resolve(string name) {
  if(Champions.ContainsKey(name)) return name;
  if(Ids.ContainsKey(name)) return Ids[name];
  foreach(var c in Champions) if(J.S(c.Value,"name")==name || name.EndsWith("_"+c.Key)) return c.Key;
  return name;
 }
 public object Champion(string name) {object o; Champions.TryGetValue(Resolve(name),out o); return o;}
 public string Name(string name) {var c=Champion(name); return c==null?(name==""?"Pick pending":name):J.S(c,"name");}
 public string[] Tags(string name) {return J.A(J.Get(Champion(name),"tags")).Select(Convert.ToString).ToArray();}
 public string Tip(string name,bool enemy) {return J.A(J.Get(Champion(name),enemy?"enemytips":"allytips")).Select(Convert.ToString).FirstOrDefault()??"Matchup-specific guidance is not available in the bundled source.";}
 public double ItemHaste(Player p,bool summoner) {
  double total=0;
  foreach(int id in p.Items.Distinct()) {object item; if(!Items.TryGetValue(id.ToString(),out item))continue;
   string desc=J.S(item,"description");
   if(!summoner) {var stats=Regex.Match(desc,"<stats>(.*?)</stats>",RegexOptions.Singleline); desc=stats.Success?stats.Groups[1].Value:"";}
   var m=Regex.Match(J.Clean(desc),@"(\d+(?:\.\d+)?)\s+"+(summoner?"Summoner Spell Haste":"Ability Haste"));
   if(m.Success) total+=double.Parse(m.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
  } return total;
 }
 public static double Cooldown(double basis,double haste) {return basis*100/(100+Math.Max(0,haste));}
 public bool MinutesAndSeconds=true;
 public string Duration(double seconds){long rounded=(long)Math.Round(seconds,MidpointRounding.AwayFromZero);if(!MinutesAndSeconds||rounded<60)return rounded+"s";return (rounded/60)+"m "+(rounded%60)+"s";}
 public string Spell(Player p,int slot,bool adjust) {
  var spells=J.A(J.Get(Champion(p.Champion),"spells")); if(slot>=spells.Length)return "Unavailable\nNo spell data";
  var spell=spells[slot]; var cds=J.A(J.Get(spell,"cooldown")); if(cds.Length==0)return "Special\nSee mechanic";
  int ammo; if(int.TryParse(J.S(spell,"maxammo"),out ammo) && ammo>1)return "Charges · special\n"+J.S(spell,"name");
  string key="QWER"[slot].ToString(); int rank; bool known=p.Ranks.TryGetValue(key,out rank);
  if(known && rank==0)return "Unlearned\n"+J.S(spell,"name");
  if(!known) rank=slot==3?Math.Max(1,p.Level>=16?3:p.Level>=11?2:1):Math.Min(cds.Length,Math.Max(1,(p.Level+1)/2));
  rank=Math.Min(cds.Length,Math.Max(1,rank)); double basis=Convert.ToDouble(cds[rank-1]);
  if(basis==0)return "Passive / special\n"+J.S(spell,"name");
  double haste=adjust?(p.Haste??ItemHaste(p,false)):0;
  string label=known?"rank "+rank:"est. rank "+rank;
  if(!adjust)label="base · "+label;
  return Duration(Cooldown(basis,haste))+" · "+label+"\n"+J.S(spell,"name");
 }
 public string Summoner(Player p,int slot,bool adjust) {
  if(slot>=p.Summoners.Count)return "—\nNot exposed"; string input=p.Summoners[slot];
  var spell=Summoners.Values.Where(s=>J.A(J.Get(s,"modes")).Select(Convert.ToString).Contains("CLASSIC")).FirstOrDefault(s=>J.S(s,"name")==input || J.S(s,"key")==input || J.S(s,"id")==input);
  if(spell==null)return input+"\nUnavailable";
  var cds=J.A(J.Get(spell,"cooldown")); if(cds.Length==0)return input+"\nSpecial";
  if(J.S(spell,"id")=="SummonerSmite")return "Charges · special\nSmite";
  return Duration(Cooldown(Convert.ToDouble(cds[0]),adjust?ItemHaste(p,true)+(p.CosmicInsight?18:0):0))+" · "+(adjust?"estimate":"base")+"\n"+J.S(spell,"name");
 }
}
public class LeagueClient : IDisposable {
 DataStore data; string lockPath="";Process transport;object transportLock=new object();
 public LeagueClient(DataStore d) {
  data=d;ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
 }
 Task<object> Get(string url,string auth){return Request(url,auth,"GET",null);}
 async Task<object> Request(string url,string auth,string method,object body) {
  return await Task.Run(()=>{lock(transportLock){
   if(transport==null||transport.HasExited){string home=AppDomain.CurrentDomain.BaseDirectory;transport=Process.Start(new ProcessStartInfo{FileName=Path.Combine(home,"runtime","python.exe"),Arguments="-I -u \""+Path.Combine(home,"transport.py")+"\"",WorkingDirectory=Path.GetTempPath(),UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true});}
   transport.StandardInput.WriteLine(new JavaScriptSerializer().Serialize(new {url=url,auth=auth,method=method,body=body}));transport.StandardInput.Flush();
   var line=transport.StandardOutput.ReadLineAsync();if(!line.Wait(5000)){transport.Kill();throw new TimeoutException("Local transport timed out");}
   var reply=J.Parse(line.Result??"{}");if(!Convert.ToBoolean(J.Get(reply,"ok")??false))throw new IOException("Local endpoint unavailable");return J.Get(reply,"data");
  }}).ConfigureAwait(false);
 }
 string FindLock() {
  if(File.Exists(lockPath))return lockPath;
  var paths=new List<string>{@"C:\Riot Games\League of Legends\lockfile",@"D:\Riot Games\League of Legends\lockfile"};
  foreach(var p in Process.GetProcessesByName("LeagueClientUx")) {try {paths.Insert(0,Path.Combine(Path.GetDirectoryName(p.MainModule.FileName),"lockfile"));}catch{}finally{p.Dispose();}}
  lockPath=paths.FirstOrDefault(File.Exists)??""; return lockPath;
 }
 public async Task ApplyLoadout(string champion,object body,bool runes){
  string path=FindLock();if(path=="")throw new IOException("Open League and enter champion select first.");
  string[] bits;using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(f))bits=r.ReadToEnd().Split(':');
  int port;if(bits.Length<5||!Int32.TryParse(bits[2],out port)||port<1||port>65535)throw new IOException("League connection unavailable.");
  string url="https://127.0.0.1:"+port,auth=Convert.ToBase64String(Encoding.UTF8.GetBytes("riot:"+bits[3]));
  await LoadoutTransfer.Apply(data,champion,body,runes,(endpoint,method,payload)=>Request(url+endpoint,auth,method,payload));
 }
 public async Task<Snapshot> Poll() {
  bool ended=false;
  try {
   var root=await Get("https://127.0.0.1:2999/liveclientdata/allgamedata",null).ConfigureAwait(false);
   ended=Postgame.LiveEnded(root);if(ended)throw new IOException("Match ended; waiting for final results.");
   var active=J.Get(root,"activePlayer"); string account=J.S(active,"riotId"); if(account=="")account=J.S(active,"summonerName");
   var s=new Snapshot {Phase="In game",Account=account,Mode=J.S(J.Get(root,"gameData"),"gameMode"),Time=J.N(J.Get(root,"gameData"),"gameTime")};
   foreach(var raw in J.A(J.Get(root,"allPlayers"))) {
    string id=J.S(raw,"riotId"); if(id=="")id=J.S(raw,"summonerName");
    var p=new Player {Champion=data.Resolve(J.S(raw,"championName")),Account=id,Self=id==account,Level=(int)J.N(raw,"level"),Team=J.S(raw,"team"),Role=J.S(raw,"position")};
    if(J.Get(raw,"isDead") is bool)p.Dead=(bool)J.Get(raw,"isDead");
    foreach(var item in J.A(J.Get(raw,"items")))p.Items.Add((int)J.N(item,"itemID"));
    foreach(var key in new[]{"summonerSpellOne","summonerSpellTwo"})p.Summoners.Add(J.S(J.Get(J.Get(raw,"summonerSpells"),key),"displayName"));
    if(p.Self) {p.CosmicInsight=J.A(J.Get(J.Get(active,"fullRunes"),"generalRunes")).Any(r=>J.N(r,"id")==8347);var stats=J.Get(active,"championStats"); if(J.Get(stats,"abilityHaste")!=null)p.Haste=J.N(stats,"abilityHaste");
     foreach(char key in "QWER") {var ability=J.Get(J.Get(active,"abilities"),key.ToString()); if(ability!=null)p.Ranks[key.ToString()]=(int)J.N(ability,"abilityLevel");}}
    s.Players.Add(p);
   } s.Overlay=OverlayData.Parse(data,root,s);return s;
  } catch(Exception) { }
  try {
   string path=FindLock(); if(path=="")return new Snapshot{Phase=ended?"WaitingForStats":"Waiting"};
   string[] bits; using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(f))bits=r.ReadToEnd().Split(':');
   int port; if(bits.Length<5 || !int.TryParse(bits[2],out port) || port<1 || port>65535)return new Snapshot();
   string auth=Convert.ToBase64String(Encoding.UTF8.GetBytes("riot:"+bits[3])); string url="https://127.0.0.1:"+port;
   var phase=Convert.ToString(await Get(url+"/lol-gameflow/v1/gameflow-phase",auth).ConfigureAwait(false));
   if(ended&&!Postgame.Active(phase))phase="WaitingForStats";
   if(Postgame.Active(phase)){
    try{var session=await Get(url+"/lol-gameflow/v1/session",auth).ConfigureAwait(false);long gameId=(long)J.N(J.Get(session,"gameData"),"gameId");
     var result=await Get(url+"/lol-end-of-game/v1/eog-stats-block",auth).ConfigureAwait(false);return Postgame.Parse(data,result,gameId,phase);
    }catch{return new Snapshot{Phase=phase,Notice="Waiting for this match's final results. Retrying automatically; unavailable stats are not estimated."};}
   }
   var me=await Get(url+"/lol-summoner/v1/current-summoner",auth).ConfigureAwait(false);
   var s=new Snapshot{Phase=phase,Account=J.S(me,"gameName")+"#"+J.S(me,"tagLine")};
   if(phase=="ChampSelect") {
    var draft=await Get(url+"/lol-champ-select/v1/session",auth).ConfigureAwait(false); int local=(int)J.N(draft,"localPlayerCellId");
    foreach(string team in new[]{"myTeam","theirTeam"})foreach(var raw in J.A(J.Get(draft,team))) {
     var p=new Player{Champion=data.Resolve(J.S(raw,"championId")),Team=team=="myTeam"?"ALLY":"ENEMY",Role=J.S(raw,"assignedPosition"),Self=team=="myTeam"&&(int)J.N(raw,"cellId")==local};
     if(p.Champion=="0")p.Champion=""; p.Summoners.Add(J.S(raw,"spell1Id"));p.Summoners.Add(J.S(raw,"spell2Id"));s.Players.Add(p);
    }
   } return s;
  } catch(Exception ex) {var web=ex as WebException;string detail=web==null?ex.GetType().Name:web.Status.ToString();if(web!=null && web.Response is HttpWebResponse)detail+=" "+(int)((HttpWebResponse)web.Response).StatusCode;if(ex.InnerException!=null)detail+=" / "+ex.InnerException.Message;return new Snapshot{Notice="Client connection unavailable ("+detail+"). References cleared; retrying automatically."};}
 }
 public async Task<double> Clock(){var raw=await Get("https://127.0.0.1:2999/liveclientdata/gamestats",null).ConfigureAwait(false);if(J.Get(raw,"gameTime")==null)throw new IOException("Game clock unavailable");return J.N(raw,"gameTime");}
 public void Dispose(){lock(transportLock){if(transport!=null){try{if(!transport.HasExited){transport.StandardInput.Close();if(!transport.WaitForExit(500)){transport.Kill();transport.WaitForExit(2000);}}}catch{}transport.Dispose();transport=null;}}}
 public string Metrics(){lock(transportLock){if(transport==null||transport.HasExited)return "Transport not running";transport.Refresh();return "Transport working set MB: "+(transport.WorkingSet64/1048576.0).ToString("0.0")+"; cumulative CPU seconds: "+transport.TotalProcessorTime.TotalSeconds.ToString("0.000");}}
}
}
