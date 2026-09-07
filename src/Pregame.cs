using System;
using System.Linq;
using System.Collections.Generic;
namespace RiftReference {
public static class Pregame {
 public static string Strength(DataStore d, Player p) {
  if(p==null||d.Champion(p.Champion)==null)return "Select a champion to see its kit guidance.";
  if(p.Champion=="Vayne")return "Sustained single-target damage and Silver Bolts against durable targets. Tumble and Condemn offer ways to create space; allied protection helps you keep attacking.";
  return J.Clean(d.Tip(p.Champion,false));
 }
 public static string Weakness(DataStore d, Player p) {
  if(p==null||d.Champion(p.Champion)==null)return "Champion information is not available yet.";
  if(p.Champion=="Vayne")return "Limited waveclear and shorter attack reach than long-range lane opponents. Targeted control and layered engage can deny your movement. Compare giving up dangerous CS with taking repeated poke.";
  return J.Clean(d.Tip(p.Champion,true));
 }
 public static string Composition(DataStore d, Snapshot s) {
  var self=s.Players.FirstOrDefault(p=>p.Self);
  if(self==null||self.Team=="")return "Your team is not identified yet. Team plans will update as picks appear.";
  var allies=s.Players.Where(p=>p.Team==self.Team&&d.Champion(p.Champion)!=null).ToArray();var lines=new List<string>();
  var tanks=allies.Where(p=>d.Tags(p.Champion).Contains("Tank")).Select(p=>d.Name(p.Champion)).ToArray();
  var carries=allies.Where(p=>d.Tags(p.Champion).Contains("Marksman")).Select(p=>d.Name(p.Champion)).ToArray();
  if(tanks.Length>0&&carries.Length>0)lines.Add("FRONT TO BACK · "+String.Join(" / ",tanks)+" can offer a front line for "+String.Join(" / ",carries)+". Compare holding protection for a reachable target with committing everyone forward.");
  if(allies.Any(p=>d.Tags(p.Champion).Contains("Mage")))lines.Add("SET UP FIRST · Your draft includes a mage. Compare arriving early to contest space with walking into an enemy setup. Check the actual range and control tools before choosing a fight.");
  if(tanks.Length==0)lines.Add("ACCESS MATTERS · No Tank-tagged ally is selected. Check who can enter safely; picks, side pressure or disengaging may offer alternatives to a direct teamfight.");
  lines.Add("CONVERT TO THE MAP · After forcing a retreat, compare a safe objective or tower with a reset to spend. Health, waves and enemy arrivals decide which option works.");
  if(allies.Length<5)lines.Add("DRAFT INCOMPLETE · "+allies.Length+" allied picks known; reassess as picks change.");
  return String.Join("\n\n",lines)+"\n\nPatterns from champion tags; not a predicted win rate.";
 }
 public static string Matchup(DataStore d,Snapshot s) {
  var lane=LaneView.Select(s);
  if(lane.Enemies.Count==0)return "Lane opponent is not identified. No counter matchup has been guessed. Compare role, patch and sample size when checking source sites.";
  return String.Join("\n\n",lane.Enemies.Select(p=>d.Name(p.Champion)+" · "+Coaching.Threat(d,p)+"\n"+J.Clean(d.Tip(p.Champion,true))))+"\n\nKit considerations, not statistical counter rankings.";
 }
}
}
