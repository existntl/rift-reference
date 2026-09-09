using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using RiftReference;
class PostgameTests {
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("Stale/invalid result accepted");}
 static object Json(object o){return J.Parse(new JavaScriptSerializer().Serialize(o));}
 [STAThread]static int Main(){try{
  Application.EnableVisualStyles();string home=AppDomain.CurrentDomain.BaseDirectory;var d=new DataStore(Path.Combine(home,"data"));
  var stats=new Dictionary<string,object>{{"CHAMPIONS_KILLED",12},{"NUM_DEATHS",0},{"ASSISTS",8},{"MINIONS_KILLED",220},{"NEUTRAL_MINIONS_KILLED",21},{"GOLD_EARNED",15100},{"TOTAL_DAMAGE_DEALT_TO_CHAMPIONS",32410},{"VISION_SCORE",23}};
  var raw=Json(new{gameId=789,gameLength=1800,gameMode="CLASSIC",multiUserChatPassword="PRIVATE_SECRET",localPlayer=new{summonerId=123},teams=new[]{new{teamId=100,isWinningTeam=true,players=new[]{new{championId=67,level=17,summonerId=123,puuid="PRIVATE_ID",riotIdGameName="PRIVATE_NAME",stats,items=new[]{3153,3006}}}},new{teamId=200,isWinningTeam=false,players=new[]{new{championId=22,level=16,summonerId=456,puuid="OTHER_PRIVATE",riotIdGameName="PRIVATE_OTHER",stats=new Dictionary<string,object>(),items=new int[0]}}}}});
  var s=Postgame.Parse(d,raw,789,"EndOfGame");Check(s.Result=="Victory","Result");var self=s.Players.Single(p=>p.Self);Check(self.Stats.Cs==241,"CS must include neutral monsters");Check(Postgame.Ratio(Postgame.PerMinute(self.Stats.Cs,s.Time))=="8.0","CS/min");Check(Postgame.Summary(d,s)[0].Body.Contains("Deathless"),"Zero death ratio");Check(s.Players[1].Stats.Kills==null,"Missing became zero");Check(Postgame.PerMinute(0,0)==null,"Zero duration");
  foreach(string phase in new[]{"ChampSelect","In game","Lobby"})Reject(()=>Postgame.Parse(d,raw,789,phase));Reject(()=>Postgame.Parse(d,raw,790,"EndOfGame"));Reject(()=>Postgame.Parse(d,raw,0,"EndOfGame"));
  Check(Postgame.LiveEnded(Json(new{events=new{Events=new[]{new{EventName="GameEnd"}}}})),"Live end event");Check(!Postgame.LiveEnded(Json(new{})),"Missing event inferred");
  Check(Postgame.Number(Json(new{value="NaN"}),"value")==null&&Postgame.Number(Json(new{value=-1}),"value")==null,"Invalid numeric stats");
  string mobile=MobileCompanion.Serialize(d,s,new Preferences());Check(!mobile.Contains("PRIVATE")&&!mobile.Contains("spells")&&!mobile.Contains("summoners"),"Postgame mobile privacy/durations");
  using(var form=new Dashboard(home,true)){
   form.Render(Path.Combine(home,"demo-postgame.png"),false,true);
   var demo=DashboardFixtures.Results(form.Demo(false));File.WriteAllText(Path.Combine(home,"mobile-postgame.json"),MobileCompanion.Serialize(d,demo,new Preferences()));
   foreach(var size in new[]{new Size(1720,980),new Size(1280,950)}){typeof(Dashboard).GetField("state",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(form,demo);form.Size=size;form.Show();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(home,"postgame-"+size.Width+".png"));}form.Hide();}
   typeof(Dashboard).GetField("state",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(form,new Snapshot{Phase="WaitingForStats"});form.Show();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(home,"postgame-pending.png"));}form.Hide();
  }
  Console.WriteLine("PASS: final stat parsing, missing/zero values, match identity and phase guards, live end detection, mobile privacy, postgame renders.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
