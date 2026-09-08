using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace RiftReference {
public class RankPoint {public DateTime At;public string Tier="",Division="",Season="";public int LP;public double Ladder;}
public static class RankHistory {
 static readonly object gate=new object();
 static readonly string[] tiers={"IRON","BRONZE","SILVER","GOLD","PLATINUM","EMERALD","DIAMOND"};
 public static double? Ladder(string tier,string division,int lp){
  if(lp<0||lp>100000)return null;
  if(tier=="MASTER"||tier=="GRANDMASTER"||tier=="CHALLENGER")return 2800.0+lp;
  int t=Array.IndexOf(tiers,tier),d=Array.IndexOf(new[]{"IV","III","II","I"},division);
  return t<0||d<0||lp>100?(double?)null:t*400+d*100+lp;
 }
 static string Hash(string identity){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-","").ToLowerInvariant();}
 static List<RankPoint> ReadPoints(object raw){
  var result=new List<RankPoint>();var values=raw as object[];if(values==null||values.Length>500)throw new InvalidDataException();
  foreach(var p in values){DateTime at;int lp;string tier=J.S(p,"tier"),division=J.S(p,"division"),season=J.S(p,"season");
   if(!DateTime.TryParseExact(J.S(p,"at"),"o",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out at)||at.Kind!=DateTimeKind.Utc||!int.TryParse(J.S(p,"lp"),NumberStyles.None,CultureInfo.InvariantCulture,out lp)||season.Length==0||season.Length>64)throw new InvalidDataException();
   var value=Ladder(tier,division,lp);if(!value.HasValue)throw new InvalidDataException();result.Add(new RankPoint{At=at,Tier=tier,Division=division,Season=season,LP=lp,Ladder=value.Value});
  }return result.OrderBy(p=>p.At).ToList();
 }
 public static void Record(string root,string accountIdentity,HomeProfile profile,DateTime now){
  if(profile==null||string.IsNullOrWhiteSpace(accountIdentity))return;
  now=now.ToUniversalTime();if(profile.Season=="")profile.Season="year:"+now.Year.ToString(CultureInfo.InvariantCulture);
  lock(gate){try{
   string file=Path.Combine(root,"rank-history.json"),key=Hash(accountIdentity);var accounts=new Dictionary<string,List<RankPoint>>();
   if(File.Exists(file)){
    if(new FileInfo(file).Length>4000000)throw new InvalidDataException();var doc=J.Parse(File.ReadAllText(file,Encoding.UTF8));
    if(J.S(doc,"format")!="rift-rank-history-1")throw new InvalidDataException();var stored=J.Get(doc,"accounts") as Dictionary<string,object>;if(stored==null||stored.Count>10)throw new InvalidDataException();
    foreach(var pair in stored){if(pair.Key.Length!=64||pair.Key.Any(c=>!"0123456789abcdef".Contains(c)))throw new InvalidDataException();accounts[pair.Key]=ReadPoints(pair.Value);}
   }
   List<RankPoint> points;if(!accounts.TryGetValue(key,out points))points=new List<RankPoint>();
   profile.RankHistory=points.Where(p=>p.Season==profile.Season).ToList();
   var ladder=profile.LP.HasValue?Ladder(profile.Tier,profile.Division,profile.LP.Value):null;if(!ladder.HasValue)return;
   var last=points.LastOrDefault();if(last!=null&&(now<=last.At||(last.Season==profile.Season&&last.Tier==profile.Tier&&last.Division==profile.Division&&last.LP==profile.LP.Value)))return;
   points.Add(new RankPoint{At=now,Tier=profile.Tier,Division=profile.Division,Season=profile.Season,LP=profile.LP.Value,Ladder=ladder.Value});points=points.Skip(Math.Max(0,points.Count-500)).ToList();accounts[key]=points;
   while(accounts.Count>10){string oldest=accounts.Where(a=>a.Key!=key).OrderBy(a=>a.Value.Count==0?DateTime.MinValue:a.Value.Last().At).First().Key;accounts.Remove(oldest);}
   var output=new Dictionary<string,object>();foreach(var pair in accounts)output[pair.Key]=pair.Value.Select(p=>new {at=p.At.ToString("o",CultureInfo.InvariantCulture),tier=p.Tier,division=p.Division,season=p.Season,lp=p.LP}).ToArray();
   string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(new{format="rift-rank-history-1",accounts=output}),new UTF8Encoding(false));if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);}finally{if(File.Exists(temp))File.Delete(temp);}
   profile.RankHistory=points.Where(p=>p.Season==profile.Season).ToList();
  }catch{profile.Notice+=" Ranked progression could not be saved or read; existing history was preserved.";}}
 }
}
}
