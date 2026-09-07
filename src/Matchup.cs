using System;
using System.Linq;
using System.Collections.Generic;
namespace RiftReference {
public class LaneView {
 public List<Player> Allies=new List<Player>(),Enemies=new List<Player>(),Others=new List<Player>();
 public static string Role(string role){switch((role??"").ToUpperInvariant()){case "ADC":case "BOT":case "BOTTOM":return "BOTTOM";case "SUPPORT":case "UTILITY":return "UTILITY";case "MID":case "MIDDLE":return "MIDDLE";case "JG":case "JUNGLE":return "JUNGLE";case "TOP":return "TOP";default:return "";}}
 public static LaneView Select(Snapshot s){var view=new LaneView();var self=s.Players.FirstOrDefault(p=>p.Self);if(self==null){view.Others.AddRange(s.Players);return view;}
  string role=Role(self.Role);bool bot=role=="BOTTOM"||role=="UTILITY";
  foreach(var p in s.Players){string r=Role(p.Role);bool match=role!=""&&(bot?(r=="BOTTOM"||r=="UTILITY"):r==role);
   if(p.Self||(match&&p.Team==self.Team))view.Allies.Add(p);else if(match&&p.Team!=self.Team)view.Enemies.Add(p);else view.Others.Add(p);
  }
  view.Allies=view.Allies.OrderBy(p=>Role(p.Role)=="UTILITY"?1:0).ToList();view.Enemies=view.Enemies.OrderBy(p=>Role(p.Role)=="UTILITY"?1:0).ToList();return view;
 }
}
public class MatchupRule {public int Slot;public string Trigger,Action,Caution;public MatchupRule(int slot,string trigger,string action,string caution){Slot=slot;Trigger=trigger;Action=action;Caution=caution;}}
public static class Matchups {
 public static readonly Dictionary<string,MatchupRule> Rules=new Dictionary<string,MatchupRule>{
  {"Caitlyn",new MatchupRule(2,"After her net is spent","Take space toward her while she lacks that recoil escape; check the route for traps first.","Her trap root and empowered Headshot still punish a straight chase.")},
  {"Nautilus",new MatchupRule(0,"After his hook misses","Use the gap to step up for a trade on his carry, keeping room to retreat.","His passive root still works in melee; at level 6+ his targeted R can start a fight without Q.")},
  {"Blitzcrank",new MatchupRule(0,"After Rocket Grab misses","Move up with your lane partner and pressure his carry before another hook attempt.","Do not walk into his E knock-up just because Q missed.")},
  {"Leona",new MatchupRule(2,"After Zenith Blade misses","Trade while she cannot use E to close the gap; keep outside her Q attack range.","Minions do not block E. From level 6, Solar Flare provides another engage.")},
  {"Thresh",new MatchupRule(0,"After Death Sentence misses","Take space for a short trade without stepping into Flay range.","If Q hits, its cooldown behaviour differs. Lantern can still extract his carry.")},
  {"Morgana",new MatchupRule(0,"After Dark Binding misses","Use the missed root to trade or change your angle.","Black Shield can still deny your crowd control; wait for it to expire or break before relying on a stun.")},
  {"Lux",new MatchupRule(0,"After Light Binding misses","Approach from outside her E zone and look for a short trade.","Q can hit two units: a single minion in front of you is not enough protection.")},
  {"Ezreal",new MatchupRule(2,"After Arcane Shift is spent","Pressure his new position if his support cannot protect it.","Landed Qs reduce his cooldowns, so this reference is not a guaranteed full window.")},
  {"Lucian",new MatchupRule(2,"After his dash and follow-up attacks","Look for a return trade as his initial burst ends.","Passive attacks reduce E cooldown. Do not assume the displayed duration stays fixed.")},
  {"Tristana",new MatchupRule(2,"After Explosive Charge is spent","Avoid stacking its bomb on yourself; look for a return trade after it resolves.","Her jump can reset, and R can knock you away. An earlier jump is not proof she cannot escape.")},
  {"Jhin",new MatchupRule(1,"After Deadly Flourish misses","Trade during his reload if his support cannot punish your approach.","Respect the fourth shot. A missed W alone does not remove his attack threat.")},
  {"Jinx",new MatchupRule(2,"After Flame Chompers are committed","Approach from a different angle once the trap line no longer cuts off your path.","Spending E does not remove traps already on the ground; do not dash into their root.")},
  {"Ashe",new MatchupRule(1,"After Volley is spent","Look for a short trade through a protected angle rather than a long retreating chase.","Her attacks still slow. At level 6+, an available arrow can reverse the trade.")},
  {"Sivir",new MatchupRule(2,"After Spell Shield expires","Then use the crowd-control spell you were holding for the trade.","Do not feed an important spell into her shield; her ultimate can still help her disengage.")},
  {"Samira",new MatchupRule(1,"After Blade Whirl ends","Use your key projectile or ranged trade after the missile-destruction effect finishes.","Avoid stacking together for her ultimate; a takedown refreshes her dash.")},
  {"Draven",new MatchupRule(2,"After Stand Aside is spent","Approach from an angle that makes catching axes costly, rather than trading into both axes.","Axe catches refresh W. Do not interpret an earlier speed boost as a long opening.")},
  {"Pyke",new MatchupRule(2,"After his dash and returning phantom","Once the stun path has passed, punish his landing position if he cannot retreat safely.","Q remains a separate threat. Do not chase along the returning phantom's line.")},
  {"Rakan",new MatchupRule(1,"After Grand Entrance misses","Pressure his carry while his main knock-up is unavailable.","E can take him back to an ally; do not chase him through the enemy pair.")},
  {"Braum",new MatchupRule(2,"After Unbreakable ends","Resume your projectile trade from a clear angle.","Watch your passive stacks: his shield ending does not remove the risk of a stun.")},
  {"Nami",new MatchupRule(0,"After Aqua Prison misses","Take a short trade while she lacks the bubble to hold you in place.","Her W can swing health back, and empowered attacks still slow you.")},
  {"Lulu",new MatchupRule(1,"After Whimsy is spent","Her polymorph is unavailable after either cast choice; look for a trade if her other protection cannot cover it.","An ally-targeted W is also an attack-speed buff. Her E shield and R remain separate answers.")},
  {"Vayne",new MatchupRule(2,"After Condemn is spent","Pressure from an angle with space behind you; she has lost that knockback for this exchange.","Do not stand against a wall when E is available. Her R changes her Tumble threat.")}
 };
 public static string[] Cards(DataStore d,LaneView lane,bool adjust){var result=new List<string>();
  foreach(var enemy in lane.Enemies.Take(2)){
   MatchupRule r;if(Rules.TryGetValue(d.Resolve(enemy.Champion),out r)){
    string[] cd=d.Spell(enemy,r.Slot,adjust).Split('\n');string spell=cd.Length>1?cd[1]:"";
    string value=cd[0].Split(new[]{" · "},StringSplitOptions.None)[0];
    result.Add(d.Name(enemy.Champion)+" · WATCH "+"QWER"[r.Slot]+" · "+value+" ref.");
    result.Add(r.Trigger+": "+r.Action+"\n\n"+r.Caution);
   }else{
    result.Add(d.Name(enemy.Champion)+" · MECHANIC REFERENCE");
    result.Add(d.Tip(enemy.Champion,true)+"\n\nNo authored punish sequence is available for this champion yet.");
   }
  }
  var self=lane.Allies.FirstOrDefault(p=>p.Self);bool vayne=self!=null&&d.Resolve(self.Champion)=="Vayne";
  bool cait=lane.Enemies.Any(p=>d.Resolve(p.Champion)=="Caitlyn"),naut=lane.Enemies.Any(p=>d.Resolve(p.Champion)=="Nautilus");
  if(vayne&&cait&&naut){result.Add("VAYNE · HOW TO CONVERT");result.Add("Hook misses → take space. Net is spent → consider extending with autos if the trap line is clear.\n\nKeep Condemn to push Nautilus off you; a missed hook is not permission to tumble into his melee root.");}
  else if(vayne&&lane.Enemies.Count>0){result.Add("VAYNE · YOUR COMMITMENT");result.Add("Use Tumble to change the angle of the named threat, not just to add damage. Keep Condemn for the enemy who can reach you.\n\nExtend for Silver Bolts only if you can keep attacking the same target without crossing the remaining control.");}
  else if(self!=null){result.Add(d.Name(self.Champion)+" · YOUR KIT");result.Add(d.Tip(self.Champion,false));}
  if(result.Count==0){result.Add("LANE NOT IDENTIFIED");result.Add("The client has not exposed enough role information to pair lane opponents. All available players remain in the reference list.");}
  return result.Take(6).ToArray();
 }
}
}
