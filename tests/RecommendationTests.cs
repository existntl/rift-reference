using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using RiftReference;
class RecommendationTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
 static object Json(object raw){return J.Parse(new JavaScriptSerializer().Serialize(raw));}
 static void Reject(Action action,string name){try{action();}catch(Exception){checks++;return;}throw new Exception(name);}
 [STAThread] static int Main(){try{
  Application.EnableVisualStyles();string home=AppDomain.CurrentDomain.BaseDirectory;var d=new DataStore(Path.Combine(home,"data"));
  var rune=Json(new{games=40,wins=22,players=12,value=new{primary=8000,secondary=8400,perks=new[]{8005,9111,9104,8014,8444,8451,5005,5008,5001}}});
  var item=Json(new{games=35,wins=19,players=11,value=new[]{3153,3124,3091}});
  var feed=Json(new{format="rift-diamond-1",patch=Recommendations.Patch(d.Version),generatedAt=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds,regions=new[]{"NA1","EUW1","KR"},queue=420,rankBasis="diamond-plus-at-collection",windowDays=7,results=new[]{new{championId=67,role="BOTTOM",runes=new[]{rune},items=new[]{item}}}});
  Check(Recommendations.Validate(d,feed,"Vayne","BOTTOM").Length==1,"Matching feed");
  Check(Recommendations.Validate(d,feed,"Vayne","TOP").Length==0,"Role isolation");
  var map=(Dictionary<string,object>)feed;var date=map["generatedAt"];map["generatedAt"]=0;Reject(()=>Recommendations.Validate(d,feed,"Vayne","BOTTOM"),"Stale feed accepted");map["generatedAt"]=date;
  map["patch"]="1.1";Reject(()=>Recommendations.Validate(d,feed,"Vayne","BOTTOM"),"Wrong patch accepted");map["patch"]=Recommendations.Patch(d.Version);
  Reject(()=>Recommendations.ValidateChoice(Json(new{games=99,wins=50,players=9})),"One-player dominated sample accepted");
  Reject(()=>Recommendations.ValidateChoice(Json(new{games=40,wins=41,players=12})),"Impossible wins accepted");
  string plan=Recommendations.Plan(d,"Vayne","BOTTOM",rune,item),champ,patch;
  Check(OverlaySettings.PlanItems(d,plan,out champ,out patch).SequenceEqual(new[]{3153,3124,3091}),"Plan overlay bridge");
  using(var planner=new BuildPlanner(d,null,"Vayne",()=>true)){
   planner.RestorePlan(plan);var saved=J.Parse(planner.SerializePlan());Check(J.S(saved,"champion")=="Vayne"&&J.A(J.Get(saved,"perks")).Length==9,"Editor restoration");
   Render(planner,Path.Combine(home,"recommendation-editor.png"));
  }
  Reject(()=>Recommendations.Bundles(d,feed,"Vayne","BOTTOM"),"Independent v1 choices combined without evidence");
  var bundle=Json(new{games=80,wins=40,players=20,value=new{primary=8000,secondary=8400,perks=new[]{8005,9111,9104,8014,8444,8451,5005,5008,5001},coreItems=new[]{3153,3124,3091}},details=new{summonerSpells=Choice(new[]{4,7}),startingItems=Choice(new[]{1055,2003}),purchaseOrder=Choice(new[]{1055,2003,1036,1042,3153,3124,3091}),finalItems=Choice(new[]{3153,3124,3091,3006}),skillOrder=Choice(new[]{1,2,3,2,2,4,2,1,2,1,4})},situationalItems=new int[0]});
  var alternate=Json(new{games=40,wins=28,players=12,value=new{primary=8000,secondary=8400,perks=new[]{8021,9111,9104,8014,8444,8451,5005,5008,5001},coreItems=new[]{3031,3046,3091}},details=new{},situationalItems=new int[0]});
  var paired=Json(new{format="rift-diamond-2",patch=Recommendations.Patch(d.Version),generatedAt=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds,regions=new[]{"NA1","EUW1","KR"},queue=420,rankBasis="diamond-plus-at-collection",windowDays=7,results=new[]{new{championId=67,role="BOTTOM",builds=new[]{bundle,alternate}}}});
  Check(Recommendations.Bundles(d,paired,"Vayne","BOTTOM").Length==2,"Connected build feed");
  Check(Recommendations.Bundles(d,paired,"Ashe","BOTTOM").Length==0,"Champion isolation");
  var bad=Json(bundle);((Dictionary<string,object>)J.Get(bad,"value"))["coreItems"]=new[]{3153,3153,3091};Reject(()=>Recommendations.ValidateBundle(d,"Vayne",bad),"Repeated core accepted");
  bad=Json(bundle);((Dictionary<string,object>)J.Get(J.Get(bad,"details"),"summonerSpells"))["value"]=new[]{4,999999};Reject(()=>Recommendations.ValidateBundle(d,"Vayne",bad),"Unknown spell accepted");
  bad=Json(bundle);((Dictionary<string,object>)J.Get(J.Get(bad,"details"),"skillOrder"))["games"]=81;Reject(()=>Recommendations.ValidateBundle(d,"Vayne",bad),"Detail exceeds cohort");
  bad=Json(bundle);((Dictionary<string,object>)J.Get(J.Get(bad,"details"),"skillOrder"))["value"]=new[]{1,2,5};Reject(()=>Recommendations.ValidateBundle(d,"Vayne",bad),"Unknown ability accepted");
  var duplicate=Json(paired);((Dictionary<string,object>)J.A(J.Get(duplicate,"results"))[0])["builds"]=new[]{bundle,bundle};Reject(()=>Recommendations.Bundles(d,duplicate,"Vayne","BOTTOM"),"Duplicate build accepted");
  var original=new OverlayOptions{Champion="Ashe",Target=3031};
  using(var overlay=new OverlaySettings(d,original)){
   overlay.UseRecommendationPlan(Recommendations.BundlePlan(d,"Vayne","BOTTOM",bundle));
   Check(original.Champion=="Ashe"&&original.Target==3031,"Overlay preview mutated original settings");
   Render(overlay,Path.Combine(home,"recommendation-overlay-settings.png"));
   overlay.Controls.OfType<Button>().Single(b=>b.Text=="Save overlay settings").PerformClick();
   Check(overlay.Result.Champion=="Vayne"&&overlay.Result.Target==3153&&overlay.Result.Source=="Diamond+ selected build","Overlay did not select first core item");
  }
  using(var picker=new RecommendationPicker(d,"Vayne",false)){
   picker.Show();Application.DoEvents();var use=picker.Controls.OfType<Button>().Single(b=>b.Text=="Use selected build");
   Check(!use.Enabled,"Unavailable feed enabled use");Render(picker,Path.Combine(home,"recommendation-unavailable.png"));
   picker.LoadFeedForPreview(feed);Check(!use.Enabled,"Independent legacy feed became selectable");
   picker.LoadFeedForPreview(paired);Check(use.Enabled,"Valid bundle not selectable");picker.Text="Rift Ready · SYNTHETIC TEST DATA · Builds & runes";Render(picker,Path.Combine(home,"recommendation-choices.png"));
   var cards=picker.Controls.OfType<FlowLayoutPanel>().Single(p=>p.Location.X==24);
   Check(cards.Controls.Count==2,"Alternative cards not shown");
   picker.Controls.OfType<Button>().Single(b=>b.Text=="Options").PerformClick();
   Check(cards.Controls.OfType<Button>().Count()==2,"First-item options not grouped");
   Render(picker,Path.Combine(home,"recommendation-path-options.png"));
   cards.Controls.OfType<Button>().First().PerformClick();
   Check(cards.Controls.Count==1,"First-item filter did not restrict paths");
   Render(picker,Path.Combine(home,"recommendation-path-filtered.png"));
   picker.Controls.OfType<Button>().Single(b=>b.Text=="All paths").PerformClick();
   Check(cards.Controls.Count==2,"Clearing first-item filter did not restore paths");
   var role=picker.Controls.OfType<ComboBox>().Single(c=>c.AccessibleName=="Recommendation role");role.SelectedItem="TOP";Check(!use.Enabled&&cards.Controls.Count==0,"Role change retained stale selection");role.SelectedItem="BOTTOM";
   picker.Controls.OfType<Button>().Single(b=>b.Text=="Win rate").PerformClick();Check(cards.Controls[0].AccessibleName.Contains("Fleet Footwork"),"Win-rate sorting did not reorder alternatives");
   ((Button)cards.Controls[0]).PerformClick();Render(picker,Path.Combine(home,"recommendation-alternate.png"));use.PerformClick();var picked=J.Parse(picker.PlanJson);
   Check(Convert.ToInt32(J.A(J.Get(picked,"perks"))[0])==8021,"Selection retained other build runes");
   Check(OverlaySettings.PlanItems(d,picker.PlanJson,out champ,out patch).SequenceEqual(new[]{3031,3046,3091}),"Selection mixed purchase paths");picker.Close();
  }
  Console.WriteLine(checks+" recommendation checks passed");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
 static object Choice(int[] value){return new{games=40,wins=22,players=12,value=value};}
 static void Render(Form form,string path){form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();Application.DoEvents();using(var canvas=new Bitmap(1920,1080))using(var shot=new Bitmap(form.Width,form.Height))using(var g=Graphics.FromImage(canvas)){g.Clear(Color.FromArgb(24,25,28));form.DrawToBitmap(shot,new Rectangle(Point.Empty,form.Size));g.DrawImageUnscaled(shot,(1920-shot.Width)/2,(1080-shot.Height)/2);canvas.Save(path);}}
}
