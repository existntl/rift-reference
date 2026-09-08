using System;
using System.IO;
using System.Linq;
using RiftReference;
class RankHistoryTests {
 static int count;static void Check(bool value,string label){if(!value)throw new Exception(label);count++;}
 static HomeProfile Profile(int lp){return new HomeProfile{Tier="EMERALD",Division="II",LP=lp};}
 static int Main(){string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"rank-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);try{
  var now=new DateTime(2026,9,7,12,0,0,DateTimeKind.Utc);var p=Profile(30);RankHistory.Record(root,"private-account",p,now);
  Check(p.RankHistory.Count==1&&p.RankHistory[0].Ladder==2230,"First observed position");
  string file=Path.Combine(root,"rank-history.json");Check(!File.ReadAllText(file).Contains("private-account"),"Identity hashed");
  RankHistory.Record(root,"private-account",p,now.AddMinutes(1));Check(p.RankHistory.Count==1,"No duplicate poll");
  p=Profile(57);RankHistory.Record(root,"private-account",p,now.AddMinutes(2));Check(p.RankHistory.Count==2&&File.Exists(file+".bak"),"Changed LP and recovery");
  p=Profile(1);p.Division="I";RankHistory.Record(root,"private-account",p,now.AddMinutes(3));Check(p.RankHistory.Last().Ladder==2301,"Promotion continuity");
  var other=Profile(50);RankHistory.Record(root,"other-account",other,now.AddMinutes(4));Check(other.RankHistory.Count==1,"Account isolation");
  var next=Profile(10);RankHistory.Record(root,"private-account",next,now.AddYears(1));Check(next.RankHistory.Count==1&&next.Season=="year:2027","Calendar reset explicit");
  var unavailable=new HomeProfile();RankHistory.Record(root,"private-account",unavailable,now.AddYears(1).AddMinutes(1));Check(unavailable.RankHistory.Count==1,"Unavailable rank not sampled");
  Check(RankHistory.Ladder("MASTER","I",501)==3301&&RankHistory.Ladder("CHALLENGER","I",501)==3301,"Apex continuous no tier jumps");
  Check(!RankHistory.Ladder("DIAMOND","I",101).HasValue&&!RankHistory.Ladder("UNKNOWN","I",20).HasValue,"Reject invalid rank");
  File.WriteAllText(file,"corrupt");RankHistory.Record(root,"private-account",Profile(90),now.AddYears(1).AddMinutes(2));Check(File.ReadAllText(file)=="corrupt","Corrupt history preserved");
  string bounded=Path.Combine(root,"bounded");Directory.CreateDirectory(bounded);for(int i=0;i<505;i++)RankHistory.Record(bounded,"account",Profile(i%100),now.AddMinutes(i));var final=Profile(4);RankHistory.Record(bounded,"account",final,now.AddMinutes(506));Check(final.RankHistory.Count==500,"Snapshot bound");
  for(int i=0;i<11;i++)RankHistory.Record(bounded,"account-"+i,Profile(20),now.AddMinutes(600+i));var stored=J.Get(J.Parse(File.ReadAllText(Path.Combine(bounded,"rank-history.json"))),"accounts") as System.Collections.Generic.Dictionary<string,object>;Check(stored!=null&&stored.Count==10,"Account storage bound");
  string before=File.ReadAllText(Path.Combine(bounded,"rank-history.json"));RankHistory.Record(bounded,"",Profile(80),now.AddMinutes(700));Check(before==File.ReadAllText(Path.Combine(bounded,"rank-history.json")),"Missing identity never stored");
  Console.WriteLine(count+" rank history checks passed");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{Directory.Delete(root,true);}}
}
