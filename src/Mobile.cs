using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace RiftReference {
public sealed class MobileCompanion : IDisposable {
    readonly string home;
    Process process;
    string pending;
    readonly System.Threading.Timer sender;
    int sending;
    public string Url { get; private set; }
    public Bitmap Qr { get; private set; }
    public bool Running { get { try { return process != null && !process.HasExited; } catch { return false; } } }
    public MobileCompanion(string root) { home = root; sender=new System.Threading.Timer(Send,null,250,250); }
    void Send(object unused) {
        if(System.Threading.Interlocked.Exchange(ref sending,1)!=0)return;
        try {
            string message=System.Threading.Interlocked.Exchange(ref pending,null);
            var child=process;
            if(message!=null && child!=null && !child.HasExited) {child.StandardInput.WriteLine(message);child.StandardInput.Flush();}
        } catch(IOException) {} catch(InvalidOperationException) {}
        finally {System.Threading.Interlocked.Exchange(ref sending,0);}
    }
    public static bool PrivateAddress(IPAddress ip) {
        var b = ip.GetAddressBytes();
        return b.Length == 4 && (b[0] == 10 || b[0] == 192 && b[1] == 168 || b[0] == 172 && b[1] >= 16 && b[1] <= 31);
    }
    public static string[] Addresses() {
        return NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses).Select(a => a.Address).Where(PrivateAddress).Select(a => a.ToString()).Distinct().ToArray();
    }
    public async Task Start(string address) {
        Stop();
        if (!Addresses().Contains(address)) throw new InvalidOperationException("Choose an available home-network address.");
        var bytes = new byte[32]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
        string token = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        var child = Process.Start(new ProcessStartInfo {
            FileName = Path.Combine(home, "runtime", "python.exe"),
            Arguments = "-I -X utf8 -u \"" + Path.Combine(home, "mobile_server.py") + "\"",
            WorkingDirectory = home, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        });
        process = child;
        try {
            child.BeginErrorReadLine();
            child.StandardInput.WriteLine(new JavaScriptSerializer().Serialize(new {address, token}));
            var line = child.StandardOutput.ReadLineAsync();
            if (await Task.WhenAny(line, Task.Delay(7000)) != line) throw new IOException("Local sharing took too long to start.");
            var response = J.Parse(await line ?? "{}");
            if (J.S(response,"url") == "") throw new IOException(J.S(response,"error") == "" ? "Local sharing could not start." : J.S(response,"error"));
            if (process != child) throw new IOException("Sharing was stopped.");
            Url = J.S(response,"url");
            var rows = J.A(J.Get(response,"qr")).Select(Convert.ToString).ToArray();
            int scale = 4, size = rows.Length;
            Qr = new Bitmap((size+8)*scale,(size+8)*scale);
            using (var g = Graphics.FromImage(Qr)) {
                g.Clear(Color.White);
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)if(rows[y][x]=='1')g.FillRectangle(Brushes.Black,(x+4)*scale,(y+4)*scale,scale,scale);
            }
        } catch { if(process==child)Stop(); throw; }
    }
    public void Publish(DataStore data, Snapshot state, Preferences prefs) {
        if (!Running) return;
        System.Threading.Interlocked.Exchange(ref pending,Serialize(data,state,prefs));
    }
    public static string Serialize(DataStore data, Snapshot state, Preferences prefs) {
        if(Postgame.Active(state.Phase))return new JavaScriptSerializer().Serialize(new{phase=state.Phase,demo=state.Demo,pollSeconds=prefs.PollSeconds,postgame=Postgame.Mobile(data,state,prefs.EnemiesLeft)});
        var self = state.Players.FirstOrDefault(p=>p.Self);
        string team = self == null ? "" : self.Team;
        bool adjust=prefs.Adjust && state.Mode=="CLASSIC" && state.Phase!="ChampSelect";
        var sides = new[]{"Enemies","Allies","Unassigned"}.Select(side=>new {
            title=side,
            players=state.Players.Where(p=>side=="Unassigned" ? team=="" || p.Team=="" : team!="" && p.Team!="" && (side=="Allies" ? p.Team==team : p.Team!=team)).Take(10).Select(p=>new {
                name=data.Name(p.Champion), role=String.IsNullOrEmpty(p.Role)?"Role unknown":p.Role, level=p.Level, self=p.Self,
                spells=state.Phase=="ChampSelect"?new string[0]:Enumerable.Range(0,4).Select(i=>data.Spell(p,i,adjust)).ToArray(),
                summoners=state.Phase=="ChampSelect"?new string[0]:Enumerable.Range(0,2).Select(i=>data.Summoner(p,i,adjust)).ToArray(),
                tools=Coaching.Threat(data,p)
            }).ToArray()
        }).Where(s=>s.title!="Unassigned" || s.players.Length>0).ToArray();
        if(!prefs.EnemiesLeft && sides.Length>=2) {var first=sides[0];sides[0]=sides[1];sides[1]=first;}
        var plan=Coaching.LanePlan(data,state);
        return new JavaScriptSerializer().Serialize(new {
            phase=state.Phase, demo=state.Demo, pollSeconds=prefs.PollSeconds,
            focus=state.Phase=="ChampSelect"?"Champion strengths":prefs.TrainingFocus, focusText=state.Phase=="ChampSelect"?Pregame.Strength(data,self):Coaching.FocusText(prefs.TrainingFocus),
            plan=state.Phase=="ChampSelect"?"VULNERABILITIES\n"+Pregame.Weakness(data,self)+"\n\nMATCHUP\n"+Pregame.Matchup(data,state)+"\n\nTEAM PLAN\n"+Pregame.Composition(data,state):prefs.ShowCoaching?plan.Title+"\n\n"+plan.Body:"", sides
        });
    }
    public void Stop() {
        var child=process;process=null;Url=null;System.Threading.Interlocked.Exchange(ref pending,null);
        if(Qr!=null){Qr.Dispose();Qr=null;}
        if(child!=null) {try { if(!child.HasExited)child.Kill(); } catch(InvalidOperationException) {} catch(System.ComponentModel.Win32Exception) {} finally {child.Dispose();}}
    }
    public void Dispose() { sender.Dispose();Stop(); }
}

public static class MobileSettings {
    public static TabPage Create(MobileCompanion service, Action publish) {
        var page=new TabPage("Phone / tablet"){BackColor=Color.FromArgb(18,26,34),ForeColor=Color.FromArgb(231,239,247)};
        var intro=new Label{Text="Use your phone or tablet as your second screen.\nConnect both devices to the same trusted home network.\nSharing is off each time Rift Ready starts.",Left=18,Top=20,Width=515,Height=65};
        var addresses=new ComboBox{Left=18,Top=100,Width=310,DropDownStyle=ComboBoxStyle.DropDownList};
        addresses.Items.AddRange(MobileCompanion.Addresses());if(addresses.Items.Count>0)addresses.SelectedIndex=0;
        var toggle=new Button{Text=service.Running?"Stop sharing":"Start sharing",Left=345,Top=98,Width=170,Height=32};
        var status=new Label{Text="Choose the PC address for your Wi-Fi or Ethernet network.",Left=18,Top=140,Width=515,Height=40};
        var picture=new PictureBox{Left=18,Top=185,Width=280,Height=280,SizeMode=PictureBoxSizeMode.CenterImage,BackColor=Color.White};
        var instructions=new Label{Text="1. Start sharing.\n\n2. Scan this QR code with your phone’s camera.\n\n3. Keep Rift Ready running on your PC.\n\nMinimizing the PC app keeps sharing active.",Left=320,Top=185,Width=200,Height=270};
        var link=new TextBox{Left=18,Top=480,Width=420,ReadOnly=true};
        var copy=new Button{Text="Copy link",Left=445,Top=478,Width=90,Height=28};
        copy.Click+=(s,e)=>{if(service.Url!=null)try{Clipboard.SetText(service.Url);}catch{status.Text="Select the link and press Ctrl+C to copy it.";}};
        var help=new Label{Text="If blocked, allow the bundled Python helper through Windows Firewall on Private networks only. Guest Wi-Fi may isolate devices.\nLocal HTTP is unencrypted: use a trusted network; do not forward ports.\nStop sharing disconnects all devices. Starting again creates a new code.",Left=18,Top=525,Width=515,Height=82};
        Action refresh=()=>{if(page.IsDisposed)return;var old=picture.Image;picture.Image=service.Qr==null?null:new Bitmap(service.Qr);picture.Visible=service.Running;if(old!=null)old.Dispose();link.Text=service.Url??"";toggle.Text=service.Running?"Stop sharing":"Start sharing";addresses.Enabled=!service.Running;copy.Enabled=service.Running;};
        toggle.Click+=async(s,e)=>{toggle.Enabled=false;try{if(service.Running){service.Stop();status.Text="Sharing stopped. All previous pairing links are now invalid.";}else{status.Text="Starting local sharing…";await service.Start(Convert.ToString(addresses.SelectedItem));publish();if(!page.IsDisposed)status.Text="Sharing on your local network. Scan the QR code below.";}}catch(Exception ex){if(!page.IsDisposed)status.Text=ex.Message;}finally{if(!page.IsDisposed){toggle.Enabled=true;refresh();}}};
        page.Disposed+=(s,e)=>{if(picture.Image!=null)picture.Image.Dispose();};
        page.Controls.AddRange(new Control[]{intro,addresses,toggle,status,picture,instructions,link,copy,help});refresh();return page;
    }
}
}
