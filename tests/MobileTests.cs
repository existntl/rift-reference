using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using RiftReference;
class MobileTests {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [STAThread] static int Main(){try{
        string home=AppDomain.CurrentDomain.BaseDirectory;
        var data=new DataStore(Path.Combine(home,"data"));
        var prefs=new Preferences();
        using(var dashboard=new Dashboard(home,true)){
            var state=dashboard.Demo(false);state.Account="PRIVATE_ACCOUNT";
            foreach(var p in state.Players)p.Account="PRIVATE_PLAYER";
            string text=MobileCompanion.Serialize(data,state,prefs);
            File.WriteAllText(Path.Combine(home,"mobile-demo.json"),text);
            Check(!text.Contains("PRIVATE_"),"Account data leaked");
            var root=J.Parse(text);var sides=J.A(J.Get(root,"sides"));
            Check(J.S(sides[0],"title")=="Enemies","Default order");
            Check(J.A(J.Get(sides[0],"players")).Length==5,"Enemy roster");
            var enemy=state.Players[5];var first=J.A(J.Get(sides[0],"players"))[0];
            Check(Convert.ToString(J.A(J.Get(first,"spells"))[0])==data.Spell(enemy,0,true),"Desktop durations differ");
            Check(text.Contains("est. rank"),"Missing rank estimates");
            prefs.EnemiesLeft=false;
            Check(J.S(J.A(J.Get(J.Parse(MobileCompanion.Serialize(data,state,prefs)),"sides"))[0],"title")=="Allies","Side preference");
            state.Phase="ChampSelect";
            var draftRoot=J.Parse(MobileCompanion.Serialize(data,state,prefs));
            File.WriteAllText(Path.Combine(home,"mobile-draft.json"),MobileCompanion.Serialize(data,state,prefs));
            Check(J.A(J.Get(J.A(J.Get(J.A(J.Get(draftRoot,"sides"))[0],"players"))[0],"spells")).Length==0,"Draft cooldowns should be absent");
            Check(J.S(draftRoot,"plan").Contains("TEAM PLAN"),"Draft planning missing");
            state.Players.Clear();
            Check(!MobileCompanion.Serialize(data,state,prefs).Contains("Caitlyn"),"Roster not cleared");
            state.Players.Add(new Player{Champion="Vayne"});
            Check(MobileCompanion.Serialize(data,state,prefs).Contains("Unassigned"),"Unknown team inferred");
        }
        Check(MobileCompanion.PrivateAddress(IPAddress.Parse("192.168.1.4")),"Home LAN rejected");
        Check(!MobileCompanion.PrivateAddress(IPAddress.Parse("8.8.8.8")),"Public bind accepted");
        Check(!MobileCompanion.PrivateAddress(IPAddress.IPv6Any),"Wildcard bind accepted");
        if(MobileCompanion.Addresses().Length>0)using(var service=new MobileCompanion(home)){
            string address=MobileCompanion.Addresses()[0];
            System.Threading.Tasks.Task.Run(()=>service.Start(address)).GetAwaiter().GetResult();
            string first=service.Url;
            Check(service.Running && service.Qr!=null,"Server/QR startup failed");
            using(var fixture=new Dashboard(home,true))service.Publish(data,fixture.Demo(false),prefs);
            System.Threading.Thread.Sleep(600);
            Func<string,string,int> status=(url,secret)=>{
                var request=(HttpWebRequest)WebRequest.Create(url.Split('#')[0]+"state");request.Proxy=null;request.Timeout=3000;request.Headers["Authorization"]="Bearer "+secret;
                try{using(var response=(HttpWebResponse)request.GetResponse())using(var reader=new StreamReader(response.GetResponseStream())){string value=reader.ReadToEnd();Check(!value.Contains("\\u00c2")&&!value.Contains("Â"),"Pipe encoding corrupted");return (int)response.StatusCode;}}
                catch(WebException ex){if(ex.Response==null)return 0;using(var response=(HttpWebResponse)ex.Response)return (int)response.StatusCode;}
            };
            Check(status(first,first.Split('#')[1])==200,"C# snapshot pipe not delivered");
            service.Qr.Save(Path.Combine(home,"pairing-test.png"));
            using(var form=new Form{Size=new System.Drawing.Size(620,760),Location=new System.Drawing.Point(-30000,-30000),StartPosition=FormStartPosition.Manual}){
                var tabs=new TabControl{Left=15,Top=15,Width=575,Height=635};tabs.TabPages.Add(MobileSettings.Create(service,()=>{}));form.Controls.Add(tabs);Theme.Apply(form);form.Show();
                using(var bitmap=new System.Drawing.Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new System.Drawing.Rectangle(System.Drawing.Point.Empty,form.Size));bitmap.Save(Path.Combine(home,"mobile-pairing-settings.png"));}form.Close();
            }
            service.Stop();Check(!service.Running && service.Url==null,"Stop failed");
            Check(status(first,first.Split('#')[1])==0,"Stopped endpoint still responds");
            System.Threading.Tasks.Task.Run(()=>service.Start(address)).GetAwaiter().GetResult();
            Check(first.Split('#')[1]!=service.Url.Split('#')[1],"Pairing secret reused");
            Check(status(service.Url,first.Split('#')[1])==401,"Old token accepted after restart");
        }
        Console.WriteLine("Mobile snapshot privacy, references, settings and clearing checks passed.");return 0;
    }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
