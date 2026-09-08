using System;
using System.IO;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using RiftReference;
// Isolated compatibility test producer: uses the existing API reader and visuals.
// Does not modify installed application settings or start any process loader.
class LivePublisher {
 [STAThread]static int Main(string[] args){try{
  if(args.Length!=3){Console.Error.WriteLine("Usage: LivePublisher <session-guid-N> <installed-app-directory> <seconds>");return 2;}
  Guid session;int seconds;if(!Guid.TryParseExact(args[0],"N",out session)||!Int32.TryParse(args[2],out seconds)||seconds<1||seconds>600)return 2;
  string home=Path.GetFullPath(args[1]);var data=new DataStore(Path.Combine(home,"data"));
  var file=Path.Combine(home,"overlay.json");var opt=File.Exists(file)?new JavaScriptSerializer().Deserialize<OverlayOptions>(File.ReadAllText(file)):new OverlayOptions();opt.Enabled=true;
  Environment.SetEnvironmentVariable("RIFT_READY_RENDER_SESSION",session.ToString("N"));
  Application.EnableVisualStyles();var loop=new ApplicationContext();
  using(var client=new LeagueClient(data))using(var overlay=new GameOverlay(data,()=>opt,false))using(var timer=new Timer{Interval=1000}){
   bool polling=false;string last="";DateTime end=DateTime.UtcNow.AddSeconds(seconds);
   timer.Tick+=async(s,e)=>{if(DateTime.UtcNow>=end){timer.Stop();if(!polling)loop.ExitThread();return;}if(polling)return;polling=true;try{var state=await client.Poll();overlay.Update(state);string status="Phase="+state.Phase+" Mode="+state.Mode+" Fresh="+OverlayData.Fresh(state,DateTime.UtcNow,DateTime.UtcNow);if(status!=last){Console.WriteLine(status);last=status;}}catch(Exception ex){overlay.Suspend();Console.WriteLine("API sample unavailable: "+ex.GetType().Name);}finally{polling=false;if(DateTime.UtcNow>=end)loop.ExitThread();}};
   timer.Start();Console.WriteLine("Live renderer bridge ready; desktop panels disabled in this test producer.");Application.Run(loop);
  }return 0;
 }catch(Exception ex){Console.Error.WriteLine("Live producer failed: "+ex.GetType().Name+" "+ex.Message);return 1;}}
}
