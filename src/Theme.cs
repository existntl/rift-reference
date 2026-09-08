using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace RiftReference {
public class MinimalWindow : Form {
    public const int TitleHeight=32;
    protected int ContentHeight{get{return System.Math.Max(1,ClientSize.Height-TitleHeight);}}
    protected MinimalWindow(){
        FormBorderStyle=FormBorderStyle.None;
        var close=new WindowDot(Color.FromArgb(255,95,87),"Close",0);
        var minimize=new WindowDot(Color.FromArgb(254,188,46),"Minimize",1);
        var maximize=new WindowDot(Color.FromArgb(40,200,64),"Maximize or restore",2);
        int x=ClientSize.Width-80;foreach(var button in new[]{minimize,maximize,close}){button.Bounds=new Rectangle(x,2,24,28);button.Anchor=AnchorStyles.Top|AnchorStyles.Right;Controls.Add(button);x+=24;}
        close.Click+=(s,e)=>Close();minimize.Click+=(s,e)=>WindowState=FormWindowState.Minimized;
        maximize.Click+=(s,e)=>{UpdateMaximizedBounds();WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;};
    }
    void UpdateMaximizedBounds(){var screen=Screen.FromControl(this);var work=screen.WorkingArea;MaximizedBounds=new Rectangle(work.X-screen.Bounds.X,work.Y-screen.Bounds.Y,work.Width,work.Height);}
    protected override void OnHandleCreated(System.EventArgs e){base.OnHandleCreated(e);UpdateMaximizedBounds();}
    protected override void OnLocationChanged(System.EventArgs e){base.OnLocationChanged(e);if(WindowState==FormWindowState.Normal)UpdateMaximizedBounds();}
    protected override void WndProc(ref Message m){
        const int HitTest=0x84;
        if(m.Msg==HitTest){
            base.WndProc(ref m);if((int)m.Result!=1)return;
            long packed=m.LParam.ToInt64();Point point=PointToClient(new Point((short)(packed&65535),(short)((packed>>16)&65535)));
            if(WindowState!=FormWindowState.Maximized){
                bool left=point.X<5,right=point.X>=ClientSize.Width-5,top=point.Y<5,bottom=point.Y>=ClientSize.Height-5;
                int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
                if(hit!=0){m.Result=(System.IntPtr)hit;return;}
            }
            if(point.Y<TitleHeight&&point.X<ClientSize.Width-82)m.Result=(System.IntPtr)2;
            return;
        }
        base.WndProc(ref m);
    }
}
public sealed class WindowDot : Button {
    readonly Color color;readonly int symbol;bool hover;
    public WindowDot(Color color,string label,int symbol){this.color=color;this.symbol=symbol;AccessibleName=label;AccessibleRole=AccessibleRole.PushButton;TabStop=true;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Theme.Background;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnMouseEnter(System.EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(System.EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnGotFocus(System.EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(System.EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.Clear(Theme.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var brush=new SolidBrush(color))g.FillEllipse(brush,6,8,12,12);
        if(hover||Focused)using(var pen=new Pen(Color.FromArgb(90,0,0,0),1.3f)){
            if(symbol==0){g.DrawLine(pen,10,12,14,16);g.DrawLine(pen,14,12,10,16);}
            else if(symbol==1)g.DrawLine(pen,9,14,15,14);
            else{g.DrawLine(pen,9,16,15,10);g.DrawLine(pen,11,10,15,10);g.DrawLine(pen,15,10,15,14);}
        }
        if(Focused)using(var pen=new Pen(Theme.Ink,1))g.DrawEllipse(pen,3,5,18,18);
    }
}
public sealed class HistoryScrollBar : Control {
    int value,maximum,largeChange=1;bool hover,dragging;int grab;
    public event System.EventHandler ValueChanged;
    public int SmallChange{get;set;}
    public int Maximum{get{return maximum;}set{maximum=System.Math.Max(0,value);Value=this.value;Invalidate();}}
    public int LargeChange{get{return largeChange;}set{largeChange=System.Math.Max(1,value);Value=this.value;Invalidate();}}
    public int Limit{get{return System.Math.Max(0,Maximum-LargeChange+1);}}
    public int Value{get{return value;}set{int next=System.Math.Max(0,System.Math.Min(Limit,value));if(next==this.value)return;this.value=next;Invalidate();AccessibilityNotifyClients(AccessibleEvents.ValueChange,-1);if(ValueChanged!=null)ValueChanged(this,System.EventArgs.Empty);}}
    public HistoryScrollBar(){SmallChange=1;TabStop=true;AccessibleRole=AccessibleRole.ScrollBar;BackColor=Theme.Background;SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
    Rectangle Thumb {get{int track=System.Math.Max(1,Height-4),h=System.Math.Min(track,System.Math.Max(36,(int)(track*System.Math.Min(1,LargeChange/(double)(Maximum+1)))));return new Rectangle((Width-8)/2,2+(Limit==0?0:(int)System.Math.Round((track-h)*Value/(double)Limit)),8,h);}}
    static void Pill(Graphics g,Rectangle r,Color color){if(r.Height<=0||r.Width<=0)return;int d=System.Math.Min(r.Width,r.Height);using(var path=new GraphicsPath()){path.AddArc(r.X,r.Y,d,d,180,180);path.AddArc(r.X,r.Bottom-d,d,d,0,180);path.CloseFigure();using(var b=new SolidBrush(color))g.FillPath(b,path);}}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;bool contrast=SystemInformation.HighContrast;
        Pill(g,new Rectangle((Width-8)/2,2,8,System.Math.Max(1,Height-4)),contrast?SystemColors.Control:Color.FromArgb(8,12,15));
        var thumb=Thumb;
        if(contrast||!Enabled)Pill(g,thumb,contrast?SystemColors.Highlight:Theme.Border);
        else{
            bool active=dragging||hover||Focused;
            Color accent=active?ControlPaint.Light(Theme.Accent,.15f):Theme.Accent;
            Pill(g,thumb,accent);
            // Quiet horizontal bands echo the supplied reference without animation.
            using(var band=new SolidBrush(Color.FromArgb(active?38:30,Theme.Background))){
                g.FillRectangle(band,thumb.X,thumb.Y+thumb.Height*.22f,thumb.Width,thumb.Height*.18f);
                g.FillRectangle(band,thumb.X,thumb.Y+thumb.Height*.59f,thumb.Width,thumb.Height*.17f);
            }
        }
    }
    protected override void OnMouseEnter(System.EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(System.EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnGotFocus(System.EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(System.EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Enabled)return;Focus();var thumb=Thumb;if(e.Y>=thumb.Top&&e.Y< thumb.Bottom){grab=e.Y-thumb.Top;dragging=true;Capture=true;}else Value+=e.Y<thumb.Top?-LargeChange:LargeChange;Invalidate();}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging)return;int travel=Height-4-Thumb.Height;if(travel>0)Value=(int)System.Math.Round((e.Y-grab-2)*Limit/(double)travel);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left){dragging=false;Capture=false;Invalidate();}}
    protected override void OnMouseCaptureChanged(System.EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){dragging=false;Invalidate();}}
    protected override void OnMouseWheel(MouseEventArgs e){if(e.Delta!=0)Value+=e.Delta<0?SmallChange:-SmallChange;}
    protected override bool IsInputKey(Keys keyData){switch(keyData&Keys.KeyCode){case Keys.Up:case Keys.Down:case Keys.Home:case Keys.End:case Keys.PageUp:case Keys.PageDown:return true;}return base.IsInputKey(keyData);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);switch(e.KeyCode){case Keys.Up:Value-=SmallChange;break;case Keys.Down:Value+=SmallChange;break;case Keys.PageUp:Value-=LargeChange;break;case Keys.PageDown:Value+=LargeChange;break;case Keys.Home:Value=0;break;case Keys.End:Value=Limit;break;default:return;}e.Handled=true;e.SuppressKeyPress=true;}
    protected override AccessibleObject CreateAccessibilityInstance(){return new ScrollAccessible(this);}
    sealed class ScrollAccessible : ControlAccessibleObject {readonly HistoryScrollBar owner;public ScrollAccessible(HistoryScrollBar owner):base(owner){this.owner=owner;}public override string Value{get{return owner.Value.ToString();}set{int n;if(int.TryParse(value,out n))owner.Value=n;}}}
}
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
        root.BackColor=root is TextBoxBase || root is ListBox || root is ComboBox || root is SettingsCard || root.Parent is SettingsCard ? Panel : Background;
        root.ForeColor=Ink;
        var button=root as Button;
        if(button!=null){button.FlatStyle=FlatStyle.Flat;button.BackColor=Panel;button.FlatAppearance.BorderColor=Border;button.FlatAppearance.MouseOverBackColor=Color.FromArgb(39,47,53);button.Cursor=Cursors.Hand;}
        var combo=root as ComboBox;
        if(combo!=null){combo.FlatStyle=FlatStyle.Flat;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.DrawItem-=DrawComboItem;combo.DrawItem+=DrawComboItem;}
        if(root is PictureBox||root.Name=="QrPlaceholder")root.BackColor=Color.White;
        if(root.Name=="QrPlaceholder")root.ForeColor=Color.FromArgb(45,48,56);
        foreach(Control child in root.Controls)Apply(child);
    }
    static void DrawComboItem(object sender,DrawItemEventArgs e){var combo=(ComboBox)sender;e.DrawBackground();using(var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?Accent:Ink)){string value=e.Index>=0?System.Convert.ToString(combo.Items[e.Index]):combo.Text;e.Graphics.DrawString(value,combo.Font,brush,new RectangleF(e.Bounds.X+6,e.Bounds.Y+2,e.Bounds.Width-8,e.Bounds.Height-4));}if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();}
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
                else if(Symbol=="general"){g.DrawLine(pen,x,y+3,x+16,y+3);g.DrawEllipse(pen,x+3,y,6,6);g.DrawLine(pen,x,y+9,x+16,y+9);g.DrawEllipse(pen,x+9,y+6,6,6);g.DrawLine(pen,x,y+15,x+16,y+15);g.DrawEllipse(pen,x+5,y+12,6,6);}
                else if(Symbol=="overlay"){g.DrawRectangle(pen,x,y,17,12);g.DrawLine(pen,x+5,y+16,x+12,y+16);g.DrawLine(pen,x+8,y+12,x+8,y+16);}
                else if(Symbol=="audio"){g.DrawLine(pen,x,y+6,x+5,y+6);g.DrawLine(pen,x+5,y+6,x+10,y+2);g.DrawLine(pen,x+10,y+2,x+10,y+16);g.DrawLine(pen,x+10,y+16,x+5,y+12);g.DrawLine(pen,x+5,y+12,x,y+12);g.DrawLine(pen,x,y+12,x,y+6);g.DrawArc(pen,x+8,y+4,7,10,-60,120);}
                else if(Symbol=="update"){g.DrawArc(pen,x,y,16,16,35,285);g.DrawLine(pen,x+12,y,x+16,y+1);g.DrawLine(pen,x+16,y+1,x+15,y+5);}
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
