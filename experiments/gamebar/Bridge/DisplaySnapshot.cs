using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Web.Script.Serialization;
using RiftReference;

namespace RiftReady.GameBarBridge {
// Explicit display-only wire model: never serialize a Snapshot or API response.
public sealed class DisplaySnapshot {
 public int version=1;
 public bool fresh,visible,goldVisible,alliesLeft=true,purchaseVisible,statsVisible;
 public double? matchSeconds,allyTotal,enemyTotal;
 public double?[] differences=new double?[5];
 public string targetName;
 public double? neededGold;
 public bool? owned;
 public DisplayStat[] stats=new DisplayStat[0];
 public DisplayBuff[] buffs=new DisplayBuff[0];
 public double viewportX,viewportY,viewportWidth,viewportHeight,boardX,boardY,boardWidth,boardHeight,rowStart,rowGap;
 public double purchaseX,purchaseY,statsX,statsY,buffsX,buffsY;
 public double purchaseWidth=300,purchaseHeight=90,statsWidth=220,statsHeight=140,buffsWidth=260,buffsHeight=90;
 public double purchaseOpacity=1,statsOpacity=1,buffsOpacity=1;
}
public sealed class DisplayStat { public string label,value; }
public sealed class DisplayBuff { public string name;public double remainingSeconds; }
public static class DisplaySnapshotProducer {
 public const int MaxBytes=8192,MaxText=128;
 public static void SetGeometry(DisplaySnapshot frame,OverlayOptions options,Rectangle game){
  if(frame==null||options==null||game.Width<1||game.Height<1)return;
  var bounds=ScoreboardLayout.Bounds(game,options);var scale=ScoreboardLayout.Scale(game.Size,options);
  frame.viewportX=game.X;frame.viewportY=game.Y;frame.viewportWidth=game.Width;frame.viewportHeight=game.Height;frame.boardX=bounds.X-game.X;frame.boardY=bounds.Y-game.Y;frame.boardWidth=bounds.Width;frame.boardHeight=bounds.Height;
  frame.rowStart=frame.boardY+ScoreboardLayout.Start(options)*scale;frame.rowGap=ScoreboardLayout.Gap(options)*scale;
  var purchaseSize=PanelSizing.Fit(new Size(430,202),options.BuildWidth,options.BuildHeight,game.Size);var statsSize=PanelSizing.Fit(StatsPanel.Size(options),options.StatsWidth,options.StatsHeight,game.Size);var buffSize=PanelSizing.Fit(new Size(520,180),options.BuffsWidth,options.BuffsHeight,game.Size);
  frame.purchaseWidth=purchaseSize.Width;frame.purchaseHeight=purchaseSize.Height;frame.statsWidth=statsSize.Width;frame.statsHeight=statsSize.Height;frame.buffsWidth=buffSize.Width;frame.buffsHeight=buffSize.Height;
  var purchase=PanelPositions.Build(game,purchaseSize,options);var stats=StatsPanel.Position(game,statsSize,options);var buffs=PanelPositions.Buffs(game,buffSize,options);
  frame.purchaseX=purchase.X-game.X;frame.purchaseY=purchase.Y-game.Y;frame.statsX=stats.X-game.X;frame.statsY=stats.Y-game.Y;frame.buffsX=buffs.X-game.X;frame.buffsY=buffs.Y-game.Y;
  frame.purchaseOpacity=PanelTransparency.Clamp(options.BuildOpacity)/100.0;frame.statsOpacity=PanelTransparency.Clamp(options.StatsOpacity)/100.0;frame.buffsOpacity=PanelTransparency.Clamp(options.BuffsOpacity)/100.0;
 }
 static double? Number(double? value,bool signed){return value.HasValue&&!Double.IsNaN(value.Value)&&!Double.IsInfinity(value.Value)&&(signed||value.Value>=0)?value:null;}
 static string Text(string value){if(String.IsNullOrWhiteSpace(value)||value.Length>MaxText||value.Any(Char.IsControl))return null;return value;}
 public static DisplaySnapshot Create(DataStore data,Snapshot snapshot,OverlayOptions options,DateTime now,DateTime received,bool scoreboardHeld,bool gameFocused){
  var result=new DisplaySnapshot();
  result.fresh=snapshot!=null&&OverlayData.Fresh(snapshot,now,received)&&Number(snapshot.Time,false).HasValue;
  if(!result.fresh||options==null||!options.Enabled||!gameFocused)return result;
  var source=snapshot.Overlay;
  result.matchSeconds=snapshot.Time;result.alliesLeft=options.AlliesLeft;
  result.goldVisible=options.Gold&&scoreboardHeld;
  if(result.goldVisible){
   result.allyTotal=Number(source.AllyTotal,false);result.enemyTotal=Number(source.EnemyTotal,false);
   for(int i=0;i<5;i++){
    var pairs=(source.Pairs??new System.Collections.Generic.List<GoldPair>()).Where(p=>p!=null&&p.Role==ScoreboardData.Roles[i]).Take(2).ToArray();
    if(pairs.Length==1&&Number(pairs[0].AllyValue,false).HasValue&&Number(pairs[0].EnemyValue,false).HasValue)
     result.differences[i]=Number(ScoreboardData.Difference(pairs[0],options.AlliesLeft),true);
   }
  }
  // A selected target is meaningful only for the current champion. Unknown amounts remain null.
  object item;
  if(options.Purchase&&data!=null&&options.Target>0&&!String.IsNullOrEmpty(source.Champion)&&options.Champion==source.Champion&&data.Items.TryGetValue(options.Target.ToString(),out item)){
   result.targetName=Text(J.S(item,"name"));result.purchaseVisible=result.targetName!=null;
   if(result.purchaseVisible&&source.InventoryKnown&&source.Inventory!=null){
    try{var cost=OverlayData.Cost(data,options.Target,source.Inventory);result.owned=cost.Owned;
     if(cost.Owned)result.neededGold=0;
     else if(Number(source.Gold,false).HasValue&&Number(cost.Remaining,false).HasValue)result.neededGold=Math.Max(0,Math.Ceiling(cost.Remaining-source.Gold.Value));
    }catch(ArgumentException){}
   }
  }
  if(options.Stats){
   var s=source.Stats??new LiveOverlayStats();
   var safe=new LiveOverlayStats{Cs=Number(s.Cs,false),CsPerMinute=Number(s.CsPerMinute,false),Kills=Number(s.Kills,false),Deaths=Number(s.Deaths,false),Assists=Number(s.Assists,false),KillParticipation=Number(s.KillParticipation,false),Vision=Number(s.Vision,false)};
   result.stats=StatsPanel.Rows(safe,options).Take(5).Select(r=>new DisplayStat{label=Text(r.Key),value=Text(r.Value)}).Where(r=>r.label!=null&&r.value!=null).ToArray();
   result.statsVisible=result.stats.Length>0;
  }
  if(options.Buffs&&source.Buffs!=null){
   result.buffs=source.Buffs.Where(b=>b!=null&&(b.Name=="Baron"||b.Name=="Elder")&&Number(b.Ends,false).HasValue&&b.Ends>snapshot.Time&&b.Ends-snapshot.Time<=BuffWindow.Duration(b.Name)).GroupBy(b=>b.Name).Where(g=>g.Count()==1).OrderBy(g=>g.Key=="Baron"?0:1).Select(g=>new DisplayBuff{name=g.Key,remainingSeconds=Math.Ceiling(g.First().Ends-snapshot.Time)}).ToArray();
  }
  result.visible=result.goldVisible||result.purchaseVisible||result.statsVisible||result.buffs.Length>0;
  return result;
 }
 public static string Serialize(DisplaySnapshot snapshot){
  if(snapshot==null)throw new ArgumentNullException("snapshot");
  if(snapshot.version!=1||snapshot.differences==null||snapshot.differences.Length!=5||snapshot.stats==null||snapshot.stats.Length>5||snapshot.buffs==null||snapshot.buffs.Length>2||
   (snapshot.targetName!=null&&Text(snapshot.targetName)==null)||
   snapshot.stats.Any(s=>s==null||Text(s.label)==null||Text(s.value)==null)||
   snapshot.buffs.Any(b=>b==null||(b.name!="Baron"&&b.name!="Elder")||!Number(b.remainingSeconds,false).HasValue||b.remainingSeconds<=0||b.remainingSeconds>BuffWindow.Duration(b.name))||
   snapshot.differences.Any(v=>v.HasValue&&!Number(v,true).HasValue)||
   new[]{snapshot.matchSeconds,snapshot.allyTotal,snapshot.enemyTotal,snapshot.neededGold}.Any(v=>v.HasValue&&!Number(v,false).HasValue))throw new ArgumentException("Invalid display snapshot.");
  var json=new JavaScriptSerializer{MaxJsonLength=MaxBytes}.Serialize(snapshot);
  if(Encoding.UTF8.GetByteCount(json)>MaxBytes)throw new ArgumentException("Display snapshot exceeds size limit.");
  return json;
 }
}
}
