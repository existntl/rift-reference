using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace RiftReference {
public static class Theme {
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(System.IntPtr window,int attribute,ref int value,int size);
    public static void TitleBar(Form form) {
        form.HandleCreated-=TitleBarCreated;form.HandleCreated+=TitleBarCreated;
        if(form.IsHandleCreated)ApplyTitleBar(form);
    }
    static void TitleBarCreated(object sender,System.EventArgs e){ApplyTitleBar((Form)sender);}
    static void ApplyTitleBar(Form form){
        if(form.FormBorderStyle==FormBorderStyle.None)return;
        try{
            int dark=SystemInformation.HighContrast?0:1;
            if(DwmSetWindowAttribute(form.Handle,20,ref dark,4)<0)DwmSetWindowAttribute(form.Handle,19,ref dark,4);
            int caption=SystemInformation.HighContrast?-1:ColorTranslator.ToWin32(Background);
            int text=SystemInformation.HighContrast?-1:ColorTranslator.ToWin32(Ink);
            int border=SystemInformation.HighContrast?-1:ColorTranslator.ToWin32(Border);
            // Exact caption colors are supported on Windows 11; Windows 10 retains its native dark frame.
            DwmSetWindowAttribute(form.Handle,35,ref caption,4);
            DwmSetWindowAttribute(form.Handle,36,ref text,4);
            DwmSetWindowAttribute(form.Handle,34,ref border,4);
        }catch(System.DllNotFoundException){}catch(System.EntryPointNotFoundException){}
    }
    public static readonly Color Background=Color.FromArgb(15,16,20),Panel=Color.FromArgb(25,27,33),Border=Color.FromArgb(43,46,54),Accent=Color.FromArgb(66,205,198),Ink=Color.FromArgb(238,240,244),Muted=Color.FromArgb(148,154,167);
    public static void Surface(Graphics g, Rectangle box, Color fill, Color accent) {
        if(box.Width<20||box.Height<20)return;
        var old=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var path=new GraphicsPath()){
            const int d=12;int x=box.X,y=box.Y,w=box.Width-1,h=box.Height-1;
            path.AddArc(x,y,d,d,180,90);path.AddArc(x+w-d,y,d,d,270,90);path.AddArc(x+w-d,y+h-d,d,d,0,90);path.AddArc(x,y+h-d,d,d,90,90);path.CloseFigure();
            using(var brush=new SolidBrush(fill))g.FillPath(brush,path);
            using(var pen=new Pen(Border))g.DrawPath(pen,path);
        }g.SmoothingMode=old;
    }
    public static void Apply(Control root) {
        var form=root as Form;if(form!=null){form.Icon=Brand.Icon;TitleBar(form);}
        root.BackColor=root is TextBoxBase || root is ListBox || root is ComboBox ? Panel : Background;
        root.ForeColor=Ink;
        var button=root as Button;
        if(button!=null){button.FlatStyle=FlatStyle.Flat;button.BackColor=Panel;button.FlatAppearance.BorderColor=Border;button.FlatAppearance.MouseOverBackColor=Color.FromArgb(39,47,53);button.Cursor=Cursors.Hand;}
        if(root is PictureBox)root.BackColor=Color.White;
        foreach(Control child in root.Controls)Apply(child);
    }
}

public sealed class NavigationButton : Button {
    public bool Active;
    public string Symbol="";
    bool hover;
    public NavigationButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;Cursor=Cursors.Hand;}
    protected override void OnMouseEnter(System.EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(System.EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.Clear(Active?Color.FromArgb(25,58,61):hover?Color.FromArgb(31,34,41):BackColor);
        var color=!Enabled?Color.FromArgb(83,88,99):Active?Theme.Accent:Theme.Ink;
        if(Active)using(var b=new SolidBrush(Theme.Accent))g.FillRectangle(b,0,0,3,Height);
        int offset=Symbol==""?0:35;
        if(Symbol!=""){
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(color,1.7f)){
                int x=13,y=(Height-16)/2;
                if(Symbol=="dashboard"){g.DrawRectangle(pen,x,y,6,6);g.DrawRectangle(pen,x+10,y,6,6);g.DrawRectangle(pen,x,y+10,6,6);g.DrawRectangle(pen,x+10,y+10,6,6);}
                else if(Symbol=="phone"){g.DrawRectangle(pen,x+3,y-2,11,21);g.DrawLine(pen,x+6,y+15,x+11,y+15);}
                else if(Symbol=="book"){g.DrawRectangle(pen,x,y,16,17);g.DrawLine(pen,x+5,y,x+5,y+17);g.DrawLine(pen,x+8,y+5,x+13,y+5);}
                else if(Symbol=="review"){g.DrawRectangle(pen,x+2,y,13,17);g.DrawLine(pen,x+5,y+5,x+12,y+5);g.DrawLine(pen,x+5,y+9,x+12,y+9);g.DrawLine(pen,x+5,y+13,x+10,y+13);}
                else {g.DrawEllipse(pen,x,y,16,16);g.DrawEllipse(pen,x+5,y+5,6,6);g.DrawLine(pen,x+8,y-3,x+8,y);g.DrawLine(pen,x+8,y+16,x+8,y+19);}
            }
        }
        TextRenderer.DrawText(g,Text,Font,new Rectangle(offset,0,Width-offset,Height),color,TextFormatFlags.VerticalCenter|(Symbol==""?TextFormatFlags.HorizontalCenter:TextFormatFlags.Left)|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(4,4,Width-8,Height-8),Theme.Accent,BackColor);
    }
}
}
