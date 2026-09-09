using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RiftReference;

class RiftToggleTests {
    const int KeyDown=0x0100,KeyUp=0x0101,ButtonClick=0x00F5;
    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    [STAThread]static int Main(){try{
        Application.EnableVisualStyles();
        using(var form=new Form{StartPosition=FormStartPosition.Manual,Location=new Point(-20000,-20000),ClientSize=new Size(430,110),BackColor=Theme.Panel})
        using(var toggle=new RiftToggle{Text="Keyboard and accessibility toggle",Bounds=new Rectangle(20,20,360,28)}){
            form.Controls.Add(toggle);form.Show();Application.DoEvents();
            Check(toggle.TabStop&&toggle.AccessibleRole==AccessibleRole.CheckButton,"Toggle is not exposed as a keyboard-focusable check button");
            Check(toggle.AccessibilityObject.Role==AccessibleRole.CheckButton&&toggle.AccessibilityObject.Name==toggle.Text,"Accessible role or name changed");
            toggle.Focus();SendMessage(toggle.Handle,KeyDown,(IntPtr)Keys.Space,IntPtr.Zero);SendMessage(toggle.Handle,KeyUp,(IntPtr)Keys.Space,IntPtr.Zero);Application.DoEvents();
            Check(toggle.Checked,"Space did not turn the toggle on");
            Check((toggle.AccessibilityObject.State&AccessibleStates.Checked)!=0,"Accessible checked state did not update");
            SendMessage(toggle.Handle,ButtonClick,IntPtr.Zero,IntPtr.Zero);Application.DoEvents();
            Check(!toggle.Checked,"Button activation did not turn the toggle off");
            using(var off=new Bitmap(toggle.Width,toggle.Height))using(var on=new Bitmap(toggle.Width,toggle.Height)){
                toggle.Checked=false;toggle.DrawToBitmap(off,new Rectangle(Point.Empty,toggle.Size));toggle.Checked=true;toggle.DrawToBitmap(on,new Rectangle(Point.Empty,toggle.Size));
                int changed=0;for(int y=0;y<toggle.Height;y++)for(int x=toggle.Width-36;x<toggle.Width;x++)if(off.GetPixel(x,y).ToArgb()!=on.GetPixel(x,y).ToArgb())changed++;
                Check(changed>40,"Custom switch states did not render distinctly");
            }
            form.Close();
        }
        Console.WriteLine("Rift toggle keyboard, accessibility, activation and rendering checks passed");return 0;
    }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
