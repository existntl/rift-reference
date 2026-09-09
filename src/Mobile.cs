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
    bool disposed;
    public string Url { get; private set; }
    public Bitmap Qr { get; private set; }
    public bool Running { get { try { return process != null && !process.HasExited; } catch { return false; } } }
    public MobileCompanion(string root) { home = root; sender=new System.Threading.Timer(Send,null,System.Threading.Timeout.Infinite,System.Threading.Timeout.Infinite); }
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
        if(disposed)throw new ObjectDisposedException("MobileCompanion");
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
            if (disposed || process != child) throw new IOException("Sharing was stopped.");
            Url = J.S(response,"url");
            var rows = J.A(J.Get(response,"qr")).Select(Convert.ToString).ToArray();
            int scale = 4, size = rows.Length;
            Qr = new Bitmap((size+8)*scale,(size+8)*scale);
            using (var g = Graphics.FromImage(Qr)) {
                g.Clear(Color.White);
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)if(rows[y][x]=='1')g.FillRectangle(Brushes.Black,(x+4)*scale,(y+4)*scale,scale,scale);
            }
            sender.Change(250,250);
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
        if(!disposed)sender.Change(System.Threading.Timeout.Infinite,System.Threading.Timeout.Infinite);
        var child=process;process=null;Url=null;System.Threading.Interlocked.Exchange(ref pending,null);
        if(Qr!=null){Qr.Dispose();Qr=null;}
        if(child!=null) {try { if(!child.HasExited)child.Kill(); } catch(InvalidOperationException) {} catch(System.ComponentModel.Win32Exception) {} finally {child.Dispose();}}
    }
    public void Dispose() { if(disposed)return;disposed=true;sender.Dispose();Stop(); }
}

public static class MobileSettings {
    public static Panel CreatePanel(MobileCompanion service, Action publish) {
        var page=new Panel{Name="PhonePage",BackColor=Theme.Background,ForeColor=Theme.Ink,AutoScroll=true,AutoScrollMinSize=new Size(0,704)};
        var connection=new SettingsCard("PhoneConnectionCard",0,186,Theme.Accent);
        connection.Controls.Add(new Label{Text="LOCAL CONNECTION",Left=24,Top=17,Width=680,Height=24,Font=new Font("Segoe UI",11,FontStyle.Bold),ForeColor=Theme.Ink});
        connection.Controls.Add(new Label{Text="Sharing is off whenever Rift Ready starts.",Left=24,Top=40,Width=680,Height=24,ForeColor=Theme.Muted});
        var intro=new Label{Text="Use your phone or tablet as a second screen. Connect both devices to the same trusted home network.",Left=24,Top=70,Width=680,Height=42};
        var addresses=new RiftComboBox{Left=24,Top=116,Width=390};
        addresses.Items.AddRange(MobileCompanion.Addresses());if(addresses.Items.Count>0)addresses.SelectedIndex=0;
        var toggle=new Button{Text=service.Running?"Stop sharing":"Start sharing",Left=432,Top=114,Width=190,Height=34};
        var status=new Label{Text="Choose the PC address for your Wi-Fi or Ethernet network.",Left=24,Top=153,Width=680,Height=28,ForeColor=Theme.Muted};
        var pairing=new SettingsCard("PhonePairingCard",202,486,Theme.Accent);
        pairing.Controls.Add(new Label{Text="PAIR YOUR DEVICE",Left=24,Top=17,Width=680,Height=24,Font=new Font("Segoe UI",11,FontStyle.Bold),ForeColor=Theme.Ink});
        pairing.Controls.Add(new Label{Text="Start sharing to create a fresh private link and QR code.",Left=24,Top=40,Width=680,Height=24,ForeColor=Theme.Muted});
        var picture=new PictureBox{Left=24,Top=76,Width=260,Height=260,SizeMode=PictureBoxSizeMode.CenterImage,BackColor=Color.White};
        var placeholder=new Label{Name="QrPlaceholder",Text="QR code appears here\nafter sharing starts.",Left=24,Top=76,Width=260,Height=260,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.White,ForeColor=Color.FromArgb(45,48,56)};
        var instructions=new Label{Text="1. Start sharing.\n\n2. Scan the QR code with your phone’s camera.\n\n3. Keep Rift Ready running on your PC.\n\nMinimizing the PC app keeps sharing active.",Left=320,Top=82,Width=360,Height=238};
        var link=new TextBox{Left=24,Top=356,Width=536,ReadOnly=true};
        var copy=new Button{Text="Copy link",Left=576,Top=354,Width=126,Height=30};
        copy.Click+=(s,e)=>{if(service.Url!=null)try{Clipboard.SetText(service.Url);}catch{status.Text="Select the link and press Ctrl+C to copy it.";}};
        var help=new Label{Text="Use a trusted private network and do not forward ports. Guest Wi-Fi may isolate devices. Stop sharing disconnects every device and invalidates the previous link.",Left=24,Top=404,Width=678,Height=58,ForeColor=Theme.Muted};
        Action refresh=()=>{if(page.IsDisposed)return;var old=picture.Image;picture.Image=service.Qr==null?null:new Bitmap(service.Qr);picture.Visible=service.Running;placeholder.Visible=!service.Running;if(old!=null)old.Dispose();link.Text=service.Url??"";toggle.Text=service.Running?"Stop sharing":"Start sharing";addresses.Enabled=!service.Running;copy.Enabled=service.Running;};
        toggle.Click+=async(s,e)=>{toggle.Enabled=false;try{if(service.Running){service.Stop();status.Text="Sharing stopped. All previous pairing links are now invalid.";}else{status.Text="Starting local sharing…";await service.Start(Convert.ToString(addresses.SelectedItem));publish();if(!page.IsDisposed)status.Text="Sharing on your local network. Scan the QR code below.";}}catch(Exception ex){if(!page.IsDisposed)status.Text=ex.Message;}finally{if(!page.IsDisposed){toggle.Enabled=true;refresh();}}};
        page.Disposed+=(s,e)=>{if(picture.Image!=null)picture.Image.Dispose();};
        connection.Controls.AddRange(new Control[]{intro,addresses,toggle,status});pairing.Controls.AddRange(new Control[]{placeholder,picture,instructions,link,copy,help});page.Controls.AddRange(new Control[]{connection,pairing});refresh();return page;
    }
    public static TabPage Create(MobileCompanion service, Action publish) {
        var tab=new TabPage("Phone / tablet"){BackColor=Color.FromArgb(18,26,34),ForeColor=Color.FromArgb(231,239,247)};
        var page=CreatePanel(service,publish);page.Dock=DockStyle.Fill;tab.Controls.Add(page);return tab;
    }
}
}
