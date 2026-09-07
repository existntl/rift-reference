using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace RiftReference {
public static class CoachingTests {
    public static Snapshot Fixture() {
        var s=new Snapshot {Mode="CLASSIC",Phase="In game",Demo=true};
        s.Players.Add(new Player {Champion="Vayne",Team="A",Role="BOTTOM",Self=true});
        s.Players.Add(new Player {Champion="Lulu",Team="A",Role="UTILITY"});
        s.Players.Add(new Player {Champion="Caitlyn",Team="B",Role="BOTTOM"});
        s.Players.Add(new Player {Champion="Nautilus",Team="B",Role="UTILITY"});
        s.Players.Add(new Player {Champion="LeeSin",Team="B",Role="JUNGLE"});
        s.Players.Add(new Player {Champion="Syndra",Team="B",Role="MIDDLE"});
        s.Players.Add(new Player {Champion="Darius",Team="B",Role="TOP"});
        return s;
    }
    public static void Run(DataStore d,string home,Action<bool,string> check) {
        var s=Fixture();var specific=Coaching.LanePlan(d,s);
        check(specific.Title.Contains("Vayne + Lulu vs Caitlyn + Nautilus")&&specific.Body.Contains("Lulu E"),"quartet plan includes partner-dependent conditions");
        s.Players[1].Champion="Thresh";var changed=Coaching.LanePlan(d,s);
        check(changed.Body.Contains("lantern")&&!changed.Body.Contains("Lulu E"),"changing allied support changes plan and removes old tools");
        s.Players[2].Champion="Ashe";check(Coaching.LanePlan(d,s).Body.Contains("arrow")&&!Coaching.LanePlan(d,s).Body.Contains("Caitlyn"),"changing enemy carry changes threat conditions");
        s.Players[3].Champion="UnknownChampion";check(Coaching.LanePlan(d,s).Body.Contains("not reviewed"),"unreviewed enemy never treated as no threat");
        s=Fixture();s.Players[0].Self=false;s.Players[1].Self=true;
        check(Coaching.LanePlan(d,s).Title==specific.Title,"support perspective retains correct four-player pairing");
        s.Players[1].Role="";check(Coaching.LanePlan(d,s).Title.Contains("GENERAL"),"unknown own role gets general reference");
        s=Fixture();s.Players[3].Role="";check(Coaching.LanePlan(d,s).Title.Contains("INCOMPLETE"),"missing enemy role is not inferred by champion");
        s.Players[3].Role="BOTTOM";check(Coaching.LanePlan(d,s).Title.Contains("INCOMPLETE"),"ambiguous enemy role refuses pairing");
        s=Fixture();s.Mode="ARAM";check(Coaching.LanePlan(d,s).Title.Contains("UNSUPPORTED"),"nonclassic lane assumptions not applied");
        s.Phase="ChampSelect";s.Mode="";check(Coaching.LanePlan(d,s).Title.Contains("Vayne"),"draft reference works before mode exposed");
        s=Fixture();string threats=Coaching.ThreatCard(d,s).Body;
        check(threats.Contains("Lee Sin")&&threats.Contains("Syndra")&&threats.Contains("Darius")&&!threats.Contains("Lulu"),"full enemy roster including non-lane threats");
        s.Players[4].Champion="UnknownChampion";check(Coaching.ThreatCard(d,s).Body.Contains("not reviewed"),"unknown threat shown explicitly");
        s.Players.RemoveAt(4);check(Coaching.ThreatCard(d,s).Body.Contains("Incomplete roster"),"partial draft preserves uncertainty");
        s.Players.ForEach(p=>p.Self=false);check(Coaching.ThreatCard(d,s).Title.Contains("WAITING"),"unknown self does not choose enemy team");
        s=Fixture();string team=Coaching.Teamfight(d,s);check(team.Contains("Lulu:")&&team.Contains("E shield")&&team.Contains("Vayne:"),"teamfight references own response and actual ally protection");
        var serializer=new JavaScriptSerializer();
        var old=serializer.Deserialize<Preferences>("{\"SettingsVersion\":1,\"EnemiesLeft\":false,\"RespawnLaneOnly\":true,\"AudioVolume\":37,\"Focus\":\"Wave and macro\",\"JungleFirst\":150}");
        old.Migrate();check(old.TrainingFocus=="Recall purpose"&&!old.EnemiesLeft&&old.RespawnLaneOnly&&old.AudioVolume==37&&old.JungleFirst==150,"v1 migration preserves user options and maps focus");
        old.TrainingFocus="Main threat";check(!old.Migrate()&&old.TrainingFocus=="Main threat","migration is idempotent");
        var fresh=new Preferences();fresh.Migrate();check(fresh.JungleFirst==120&&fresh.JungleInterval==90&&fresh.JungleEnd==480&&!fresh.SecondClick,"accepted jungle timing and click default unchanged");
        old.TrainingFocus=null;old.Migrate();check(old.TrainingFocus=="Support position","missing focus repaired");

        string root=Path.Combine(home,"reflection-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var first=new Reflection {Champion="Vayne",Focus="Main threat",NextGame="Keep an exit — 猎手",SavedUtc=DateTime.UtcNow.ToString("o")};
        ReflectionStore.Save(root,first);check(ReflectionStore.Read(root).Single().NextGame==first.NextGame,"Unicode review survives reload");
        var second=new Reflection {NextGame="Check partner"};ReflectionStore.Save(root,second);
        first.Disadvantage="Late recall";ReflectionStore.Save(root,first);
        var saved=ReflectionStore.Read(root);check(saved.Count==2&&saved[0].Disadvantage=="Late recall"&&saved[1].Id==second.Id,"editing a reflection preserves other entries without duplication");
        check(File.Exists(Path.Combine(root,"reviews.json.bak")),"review update retains recovery copy");
        string broken="not valid JSON";File.WriteAllText(Path.Combine(root,"reviews.json"),broken,Encoding.UTF8);
        bool rejected=false;try{ReflectionStore.Save(root,second);}catch{rejected=true;}
        check(rejected&&File.ReadAllText(Path.Combine(root,"reviews.json"),Encoding.UTF8)==broken,"corrupt review is not silently overwritten");
    }
}
}
