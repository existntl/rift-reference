using System;using System.Drawing;using System.Linq;using System.Windows.Forms;using RiftReference;
class ChromeProbe:MinimalWindow {
 public int Hit(int x,int y){var p=PointToScreen(new Point(x,y));var m=Message.Create(Handle,0x84,IntPtr.Zero,(IntPtr)((p.Y<<16)|(p.X&65535)));WndProc(ref m);return (int)m.Result;}
 public void FixedDialog(){UseFixedDialogChrome();}
}
class WindowChromeTests {
 [STAThread]static void Main(){Application.EnableVisualStyles();using(var f=new ChromeProbe()){
  f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Size=new Size(1280,950);f.Show();
  if(f.Hit(200,16)!=2||f.Hit(200,80)!=1||f.Hit(1,100)!=10||f.Hit(1279,949)!=17)throw new Exception("Title drag / resize hit testing failed");
  var buttons=f.Controls.OfType<Button>().ToArray();if(buttons.Length!=3)throw new Exception("Window buttons missing");
  foreach(int width in new[]{1920,1280}){f.Width=width;if(buttons.Min(b=>b.Left)!=f.ClientSize.Width-120||buttons.Max(b=>b.Right)!=f.ClientSize.Width||f.Hit(30,16)!=2)throw new Exception("Right alignment or left title drag failed");}
  var ordered=buttons.OrderBy(b=>b.Left).ToArray();if(ordered[0].AccessibleName!="Minimize"||ordered[1].AccessibleName!="Maximize or restore"||ordered[2].AccessibleName!="Close"||buttons.Any(b=>!(b is WindowCaptionButton)))throw new Exception("Windows caption order or compact control style changed");
  buttons.Single(b=>b.AccessibleName=="Minimize").PerformClick();if(f.WindowState!=FormWindowState.Minimized)throw new Exception("Minimize failed");f.WindowState=FormWindowState.Normal;
  var max=buttons.Single(b=>b.AccessibleName=="Maximize or restore");max.PerformClick();if(f.WindowState!=FormWindowState.Maximized)throw new Exception("Maximize failed");if(f.Hit(200,1)!=2)throw new Exception("Maximized top edge must drag");max.PerformClick();if(f.WindowState!=FormWindowState.Normal)throw new Exception("Restore failed");
  bool closed=false;f.FormClosed+=(s,e)=>closed=true;buttons.Single(b=>b.AccessibleName=="Close").PerformClick();if(!closed)throw new Exception("Close failed");
 }
 foreach(var screen in Screen.AllScreens)using(var f=new ChromeProbe()){
  f.ShowInTaskbar=false;f.Opacity=0;f.StartPosition=FormStartPosition.Manual;f.Bounds=screen.WorkingArea;f.Show();
  f.WindowState=FormWindowState.Maximized;Application.DoEvents();
  if(f.Bounds!=screen.WorkingArea)throw new Exception("Maximized window escaped monitor work area: "+f.Bounds+" expected "+screen.WorkingArea);
  f.WindowState=FormWindowState.Normal;f.Controls.OfType<Button>().Single(b=>b.AccessibleName=="Maximize or restore").PerformClick();Application.DoEvents();
  if(f.Bounds!=screen.WorkingArea)throw new Exception("Maximize button escaped monitor work area");f.Close();
 }
 using(var popup=new ChromeProbe()){
  popup.StartPosition=FormStartPosition.Manual;popup.Location=new Point(-20000,-20000);popup.Size=new Size(1040,900);popup.FixedDialog();popup.Show();Application.DoEvents();
  var visible=popup.Controls.OfType<Button>().Where(b=>b.Visible).ToArray();if(visible.Length!=1||visible[0].AccessibleName!="Close"||popup.Hit(200,16)!=1||popup.Hit(1,100)!=1||popup.Region==null)throw new Exception("Fixed close-only dialog chrome failed");popup.Close();
 }
 bool dimmed=false;using(var owner=new ChromeProbe())using(var dialog=new ChromeProbe())using(var timer=new Timer{Interval=50}){
  owner.StartPosition=FormStartPosition.Manual;owner.Bounds=new Rectangle(-20000,-20000,1280,950);owner.Show();timer.Tick+=(s,e)=>{var shade=Application.OpenForms.Cast<Form>().FirstOrDefault(form=>form.Name=="RiftReadyModalBackdrop");if(shade==null)return;dimmed=shade.Owner==owner&&shade.Bounds==owner.Bounds&&shade.Opacity>.5&&shade.Opacity<.7;dialog.DialogResult=DialogResult.Cancel;dialog.Close();};timer.Start();ModalBackdrop.Show(owner,dialog);timer.Stop();owner.Close();
 }if(!dimmed)throw new Exception("Modal backdrop did not cover and dim its owner");
 Console.WriteLine("Window controls, drag/resize and all-monitor maximize bounds passed");}
}
