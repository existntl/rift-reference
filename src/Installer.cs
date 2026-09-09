using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using RiftReference.Release;
namespace RiftReferenceSetup {
public class InstallRecord {public string product,version;}
static class Program {
 const string Marker="installation.json";
 static string Target(string input){string target=Path.GetFullPath(input).TrimEnd(Path.DirectorySeparatorChar);if(target.Length<8||target==Path.GetPathRoot(target).TrimEnd('\\'))throw new IOException("Choose an application folder, not a drive root.");
  if(Directory.Exists(target)){
   if((File.GetAttributes(target)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked installation folders are not supported.");
   if(Directory.GetFileSystemEntries(target).Length>0){string marker=Path.Combine(target,Marker);if(!File.Exists(marker))throw new IOException("This folder contains other files. Choose an empty folder or an existing Rift Ready installation.");var record=new JavaScriptSerializer().Deserialize<InstallRecord>(File.ReadAllText(marker));if(record.product!=ReleaseInfo.Product)throw new IOException("This is not a Rift Ready installation.");if(new Version(record.version)>new Version(ReleaseInfo.Version))throw new IOException("A newer version is installed; downgrading is disabled.");}
  }return target;
 }
 static void Extract(string stage){Directory.CreateDirectory(stage);string root=Path.GetFullPath(stage).TrimEnd('\\')+"\\";
  using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))using(var zip=new ZipArchive(resource,ZipArchiveMode.Read))foreach(var entry in zip.Entries){
   if(entry.FullName.Contains(":"))throw new IOException("Invalid package path.");string path=Path.GetFullPath(Path.Combine(stage,entry.FullName));if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Invalid package path.");
   if(entry.FullName.EndsWith("/")){Directory.CreateDirectory(path);continue;}Directory.CreateDirectory(Path.GetDirectoryName(path));entry.ExtractToFile(path,true);
  }
  if(!File.Exists(Path.Combine(stage,"RiftReference.exe")))throw new IOException("The package is incomplete.");
 }
 static string Install(string input,bool register,bool failAfterBackup=false){string target=Target(input);Directory.SetCurrentDirectory(Path.GetTempPath());EnsureClosed(target);string parent=Path.GetDirectoryName(target);Directory.CreateDirectory(parent);string stage=Path.Combine(parent,".RiftReference-stage-"+Guid.NewGuid().ToString("N"));string backup=target+".previous-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);bool moved=false;
  try{
   Extract(stage);foreach(string name in new[]{"preferences.json","preferences.json.bak","reviews.json","reviews.json.bak","overlay.json","overlay.json.bak","rank-history.json","rank-history.json.bak"}){string settings=Path.Combine(target,name);if(File.Exists(settings)){if((File.GetAttributes(settings)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked user data files are not supported.");File.Copy(settings,Path.Combine(stage,name),true);}}
   File.WriteAllText(Path.Combine(stage,Marker),new JavaScriptSerializer().Serialize(new InstallRecord{product=ReleaseInfo.Product,version=ReleaseInfo.Version}));File.Copy(Assembly.GetExecutingAssembly().Location,Path.Combine(stage,"Uninstall.exe"),true);
   if(Directory.Exists(target)){MoveWithRetry(target,backup);moved=true;}if(failAfterBackup)throw new IOException("Injected rollback test.");MoveWithRetry(stage,target);
  }catch{if(moved&&!Directory.Exists(target))MoveWithRetry(backup,target);throw;}
  if(register)Register(target);return target;
 }
 static void EnsureClosed(string target){foreach(var p in Process.GetProcessesByName("RiftReference")){using(p){try{if(string.Equals(Path.GetDirectoryName(p.MainModule.FileName),target,StringComparison.OrdinalIgnoreCase))throw new IOException("Rift Ready is still open in this installation. Close all its windows, then try again.");}catch(System.ComponentModel.Win32Exception){}}}}
 static void MoveWithRetry(string source,string target){for(int attempt=0;;attempt++){try{Directory.Move(source,target);return;}catch(IOException ex){int code=ex.HResult&65535;if((code!=32&&code!=33)||attempt>=20)throw new IOException("Windows could not move the installation folder. Close any older Rift Ready installer error window and all Rift Ready windows, then retry. Your previous installation is kept. Folder: "+source,ex);Thread.Sleep(250);}}}
 static void Shortcut(string path,string target){object shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));object link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});link.GetType().InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{target});link.GetType().InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,link,new object[]{Path.GetDirectoryName(target)});link.GetType().InvokeMember("IconLocation",BindingFlags.SetProperty,null,link,new object[]{target+",0"});link.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);}
 static void RemoveOwnedLegacyShortcut(string folder,string target){string path=Path.Combine(folder,"Rift Reference.lnk");if(!File.Exists(path))return;object shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));object link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});string actual=Convert.ToString(link.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,link,null));if(string.Equals(actual,Path.Combine(target,"RiftReference.exe"),StringComparison.OrdinalIgnoreCase))File.Delete(path);}
 static void Register(string target){
  using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\RiftReference")){key.SetValue("DisplayName","Rift Ready");key.SetValue("DisplayVersion",ReleaseInfo.Version);key.SetValue("Publisher","Rift Ready");key.SetValue("InstallLocation",target);key.SetValue("UninstallString","\""+Path.Combine(target,"Uninstall.exe")+"\" --uninstall \""+target+"\"");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);}
  try{foreach(var folder in new[]{Environment.GetFolderPath(Environment.SpecialFolder.Programs),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}){Shortcut(Path.Combine(folder,"Rift Ready.lnk"),Path.Combine(target,"RiftReference.exe"));RemoveOwnedLegacyShortcut(folder,target);}}catch{MessageBox.Show("Installed successfully, but Windows could not create shortcuts. Open RiftReference.exe in "+target);}
 }
 static void Wait(int pid){try{using(var p=Process.GetProcessById(pid)){if(!p.WaitForExit(30000))throw new IOException("Close Rift Ready and try the update again. No running process was terminated.");}}catch(ArgumentException){}}
 static bool Game(){var games=Process.GetProcessesByName("League of Legends");try{return games.Length>0;}finally{foreach(var game in games)game.Dispose();}}
 static void Launch(string target){Process.Start(new ProcessStartInfo{FileName=Path.Combine(target,"RiftReference.exe"),UseShellExecute=true});}
 [STAThread] static int Main(string[] args){Application.EnableVisualStyles();try{
  if(args.Length==2&&args[0]=="--extract-test"){Install(args[1],false);return 0;}
  if(args.Length==2&&args[0]=="--rollback-test"){try{Install(args[1],false,true);}catch(IOException){return Directory.Exists(args[1])&&File.Exists(Path.Combine(args[1],Marker))?0:1;}return 1;}
  if(args.Length==4&&args[0]=="--apply"){
   var release=ReleaseInfo.Verify(File.ReadAllText(args[3]),ReleaseInfo.PublicKey());if(release.version!=ReleaseInfo.Version)throw new IOException("Installer version does not match the signed release.");ReleaseInfo.VerifyFile(Assembly.GetExecutingAssembly().Location,release);
   Wait(int.Parse(args[2]));if(Game())throw new IOException("Finish your League match before installing the update.");Launch(Install(args[1],true));return 0;
  }
  if(args.Length==2&&args[0]=="--uninstall"){
   string target=Target(args[1]);if(MessageBox.Show("Remove Rift Ready and its saved preferences? Previous-version backup folders are kept.","Uninstall Rift Ready",MessageBoxButtons.YesNo)!=DialogResult.Yes)return 0;
   string worker=Path.Combine(Path.GetTempPath(),"RiftReference-Uninstall-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(Assembly.GetExecutingAssembly().Location,worker);Process.Start(new ProcessStartInfo{FileName=worker,Arguments="--remove \""+target+"\" "+Process.GetCurrentProcess().Id,UseShellExecute=false,CreateNoWindow=true});return 0;
  }
  if(args.Length==3&&args[0]=="--remove"){
   Wait(int.Parse(args[2]));string target=Target(args[1]);foreach(var p in Process.GetProcessesByName("RiftReference")){using(p){try{if(string.Equals(Path.GetDirectoryName(p.MainModule.FileName),target,StringComparison.OrdinalIgnoreCase))throw new IOException("Close Rift Ready before uninstalling.");}catch(System.ComponentModel.Win32Exception){}}}
   if(!File.Exists(Path.Combine(target,Marker)))throw new IOException("Installation marker missing; nothing was removed.");Directory.Delete(target,true);Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\RiftReference",false);
   foreach(var folder in new[]{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.Programs)}){string shortcut=Path.Combine(folder,"Rift Ready.lnk");if(File.Exists(shortcut))File.Delete(shortcut);}MessageBox.Show("Rift Ready was uninstalled.");return 0;
  }
  using(var form=new Form{Icon=Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),Text="Install Rift Ready "+ReleaseInfo.Version,Size=new Size(610,300),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false}){
   var label=new Label{Text="Install Rift Ready for your Windows account.\nIncludes shortcuts, an uninstaller and in-app update checks.",Left=20,Top=20,Width=560,Height=55};var path=new TextBox{Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","RiftReference"),Left=20,Top=90,Width=450};
   var browse=new Button{Text="Browse",Left=480,Top=88,Width=85};browse.Click+=(s,e)=>{using(var dialog=new FolderBrowserDialog()){if(dialog.ShowDialog()==DialogResult.OK)path.Text=dialog.SelectedPath;}};
   var open=new CheckBox{Text="Open Rift Ready when finished",Checked=true,Left=20,Top=137,Width=450};var button=new Button{Text="Install",Left=405,Top=195,Width=160,Height=35};
   button.Click+=async(s,e)=>{button.Enabled=false;try{if(Game())throw new IOException("Finish your League match before installing.");string destination=path.Text;await Task.Run(()=>Install(destination,false));Register(destination);if(open.Checked)Launch(destination);form.Close();}catch(Exception ex){MessageBox.Show(ex.Message,"Installation could not finish");button.Enabled=true;}};
   form.Controls.AddRange(new Control[]{label,path,browse,open,button});Application.Run(form);
  }return 0;
 }catch(Exception e){if(args.Length>0&&args[0].EndsWith("test"))File.WriteAllText(Path.Combine(Path.GetTempPath(),"RiftReference-install-test.txt"),e.ToString());else {string log=Path.Combine(Path.GetTempPath(),"RiftReference-installer-error.log");try{File.WriteAllText(log,e.ToString());}catch{}MessageBox.Show(e.Message+"\n\nDetails: "+log,"Rift Ready installer");}return 1;}}
}
}
