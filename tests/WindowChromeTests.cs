using System;using System.Drawing;using System.Linq;using System.Windows.Forms;using RiftReference;
class ChromeProbe:MinimalWindow {
 public int Hit(int x,int y){var p=PointToScreen(new Point(x,y));var m=Message.Create(Handle,0x84,IntPtr.Zero,(IntPtr)((p.Y<<16)|(p.X&65535)));WndProc(ref m);return (int)m.Result;}
}
class WindowChromeTests {
 [STAThread]static void Main(){Application.EnableVisualStyles();using(var f=new ChromeProbe()){
  f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-20000,-20000);f.Size=new Size(1280,950);f.Show();
  if(f.Hit(200,16)!=2||f.Hit(200,80)!=1||f.Hit(1,100)!=10||f.Hit(1279,949)!=17)throw new Exception("Title drag / resize hit testing failed");
  var buttons=f.Controls.OfType<Button>().ToArray();if(buttons.Length!=3)throw new Exception("Window buttons missing");
  foreach(int width in new[]{1920,1280}){f.Width=width;if(buttons.Min(b=>b.Left)!=f.ClientSize.Width-80||buttons.Max(b=>b.Right)!=f.ClientSize.Width-8||f.Hit(30,16)!=2)throw new Exception("Right alignment or left title drag failed");}
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
 Console.WriteLine("Window controls, drag/resize and all-monitor maximize bounds passed");}
}
