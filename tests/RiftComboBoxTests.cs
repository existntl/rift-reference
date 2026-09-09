using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using RiftReference;

class RiftComboBoxTests {
    const int KeyDown=0x0100,Character=0x0102;
    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    [STAThread]static int Main(string[] args){try{
        Application.EnableVisualStyles();
        using(var form=new Form{StartPosition=FormStartPosition.Manual,Location=args.Length>0?new Point(240,180):new Point(-20000,-20000),ClientSize=new Size(290,225),BackColor=Theme.Background,Font=new Font("Segoe UI",10)})
        using(var combo=new RiftComboBox{Text="Dark",Bounds=new Rectangle(74,24,142,32),AccessibleName="Theme"}){
            combo.Items.AddRange(new object[]{"Dark","Blue","Minimal","A longer menu item","Fifth item","Sixth item","Seventh item","Eighth item","Ninth item"});combo.SelectedIndex=0;form.Controls.Add(combo);form.Show();Application.DoEvents();
            Check(combo.DropDownStyle==ComboBoxStyle.DropDownList,"Dropdown is not selection-only");
            Check(combo.TabStop&&combo.AccessibilityObject.Role==AccessibleRole.ComboBox,"Dropdown is not exposed as a keyboard-focusable combo box");
            Check(combo.AccessibilityObject.Name=="Theme"&&combo.AccessibilityObject.Value=="Dark","Dropdown accessible name or value is wrong");
            int changed=0;combo.SelectedIndexChanged+=(s,e)=>changed++;combo.Focus();SendMessage(combo.Handle,KeyDown,(IntPtr)Keys.Down,IntPtr.Zero);Application.DoEvents();
            Check(combo.SelectedIndex==1&&Convert.ToString(combo.SelectedItem)=="Blue"&&changed==1,"Down arrow did not select the next item exactly once");
            using(var dark=new Bitmap(combo.Width,combo.Height))using(var blue=new Bitmap(combo.Width,combo.Height)){
                combo.SelectedIndex=0;combo.DrawToBitmap(dark,new Rectangle(Point.Empty,combo.Size));combo.SelectedIndex=1;combo.DrawToBitmap(blue,new Rectangle(Point.Empty,combo.Size));
                int different=0;for(int y=0;y<combo.Height;y++)for(int x=0;x<combo.Width;x++)if(dark.GetPixel(x,y).ToArgb()!=blue.GetPixel(x,y).ToArgb())different++;
                Check(different>25,"Selected dropdown text did not render distinctly");
            }
            combo.DroppedDown=true;Application.DoEvents();Check(combo.DroppedDown&&(combo.AccessibilityObject.State&AccessibleStates.Expanded)!=0,"Dropdown did not expose its expanded state");
            if(args.Length>0){Thread.Sleep(120);Application.DoEvents();using(var image=new Bitmap(330,250))using(var graphics=Graphics.FromImage(image)){graphics.CopyFromScreen(form.Left,form.Top,0,0,image.Size);image.Save(args[0]);}}
            combo.DroppedDown=false;Application.DoEvents();Check(!combo.DroppedDown&&(combo.AccessibilityObject.State&AccessibleStates.Collapsed)!=0,"Dropdown did not expose its collapsed state");
            SendMessage(combo.Handle,Character,(IntPtr)'M',IntPtr.Zero);Application.DoEvents();Check(Convert.ToString(combo.SelectedItem)=="Minimal","Type-to-select did not find a long-list item");
            form.Close();
        }
        Console.WriteLine("Rift dropdown selection, keyboard, accessibility and rendering checks passed");return 0;
    }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
