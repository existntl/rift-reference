using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace RiftReference {
public class MinimalWindow : Form {
    public bool IsPage {get;private set;}
    public void UsePageLayout(){
        if(IsPage)return;IsPage=true;TopLevel=false;ShowInTaskbar=false;FormBorderStyle=FormBorderStyle.None;
        MinimumSize=Size.Empty;MaximumSize=Size.Empty;Region=null;
        foreach(Control control in Controls){if(control is WindowCaptionButton)control.Visible=false;else control.Top-=TitleHeight;}
        ClientSize=new Size(ClientSize.Width,ClientSize.Height-TitleHeight);
    }
    public const int TitleHeight=32;
    protected virtual int CaptionHeight{get{return TitleHeight;}}
    protected int ContentHeight{get{return System.Math.Max(1,ClientSize.Height-TitleHeight);}}
    readonly WindowCaptionButton close,minimize,maximize;
    bool titleMovable=true,edgeResize=true,roundedDialog;
    protected MinimalWindow(){
        FormBorderStyle=FormBorderStyle.None;
        minimize=new WindowCaptionButton("Minimize",1);maximize=new WindowCaptionButton("Maximize or restore",2);close=new WindowCaptionButton("Close",0);
        int x=ClientSize.Width-120;foreach(var button in new[]{minimize,maximize,close}){button.Bounds=new Rectangle(x,0,40,TitleHeight);button.Anchor=AnchorStyles.Top|AnchorStyles.Right;Controls.Add(button);x+=40;}
        close.Click+=(s,e)=>Close();minimize.Click+=(s,e)=>WindowState=FormWindowState.Minimized;
        maximize.Click+=(s,e)=>{UpdateMaximizedBounds();WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;maximize.Invalidate();};
    }
    protected void UseFixedDialogChrome(){titleMovable=false;edgeResize=false;roundedDialog=true;minimize.Visible=false;maximize.Visible=false;MinimizeBox=false;MaximizeBox=false;UpdateRoundedRegion();Invalidate();}
    void UpdateMaximizedBounds(){var screen=Screen.FromControl(this);var work=screen.WorkingArea;MaximizedBounds=new Rectangle(work.X-screen.Bounds.X,work.Y-screen.Bounds.Y,work.Width,work.Height);}
    protected override void OnHandleCreated(System.EventArgs e){base.OnHandleCreated(e);UpdateMaximizedBounds();}
    protected override void OnLocationChanged(System.EventArgs e){base.OnLocationChanged(e);if(WindowState==FormWindowState.Normal)UpdateMaximizedBounds();}
    protected override void OnSizeChanged(System.EventArgs e){base.OnSizeChanged(e);UpdateRoundedRegion();}
    protected override void OnTextChanged(System.EventArgs e){base.OnTextChanged(e);Invalidate(new Rectangle(0,0,ClientSize.Width,TitleHeight));}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);DrawWindowChrome(e.Graphics);}
    protected void DrawWindowChrome(Graphics g){
        if(IsPage)return;
        using(var surface=new SolidBrush(Theme.TitleSurface))g.FillRectangle(surface,0,0,ClientSize.Width,TitleHeight);
        using(var line=new Pen(Theme.Border))g.DrawLine(line,0,TitleHeight-1,ClientSize.Width,TitleHeight-1);
        using(var brandFont=new Font("Segoe UI",8.5f,FontStyle.Bold))using(var detailFont=new Font("Segoe UI",8f)){
            var flags=TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding|TextFormatFlags.EndEllipsis;
            int x=13;TextRenderer.DrawText(g,"RIFT",brandFont,new Rectangle(x,0,42,TitleHeight),Theme.Ink,flags);x+=TextRenderer.MeasureText(g,"RIFT ",brandFont,new Size(100,TitleHeight),flags).Width;
            TextRenderer.DrawText(g,"READY",brandFont,new Rectangle(x,0,48,TitleHeight),Theme.Accent,flags);x+=TextRenderer.MeasureText(g,"READY ",brandFont,new Size(100,TitleHeight),flags).Width+6;
            string detail=Text??"";const string prefix="Rift Ready · ";if(detail.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))detail=detail.Substring(prefix.Length);else if(String.Equals(detail,"Rift Ready",StringComparison.OrdinalIgnoreCase))detail="";
            Version parsed;if(Version.TryParse(detail,out parsed))detail="v"+detail;
            if(detail!="")TextRenderer.DrawText(g,detail,detailFont,new Rectangle(x,0,Math.Max(1,ClientSize.Width-x-(close.Visible?close.Left==ClientSize.Width-40?48:128:8)),TitleHeight),Theme.Muted,flags);
        }
        if(roundedDialog)using(var pen=new Pen(Theme.Border))g.DrawRectangle(pen,0,0,Math.Max(1,ClientSize.Width-1),Math.Max(1,ClientSize.Height-1));
    }
    void UpdateRoundedRegion(){if(!roundedDialog||ClientSize.Width<20||ClientSize.Height<20)return;using(var path=Rounded(new Rectangle(0,0,ClientSize.Width,ClientSize.Height),14)){var previous=Region;Region=new Region(path);if(previous!=null)previous.Dispose();}}
    static GraphicsPath Rounded(Rectangle box,int radius){var path=new GraphicsPath();int d=radius*2;path.AddArc(box.Left,box.Top,d,d,180,90);path.AddArc(box.Right-d,box.Top,d,d,270,90);path.AddArc(box.Right-d,box.Bottom-d,d,d,0,90);path.AddArc(box.Left,box.Bottom-d,d,d,90,90);path.CloseFigure();return path;}
    protected override void WndProc(ref Message m){
        if(IsPage){base.WndProc(ref m);return;}
        const int HitTest=0x84;
        if(m.Msg==HitTest){
            base.WndProc(ref m);if((int)m.Result!=1)return;
            long packed=m.LParam.ToInt64();Point point=PointToClient(new Point((short)(packed&65535),(short)((packed>>16)&65535)));
            if(edgeResize&&WindowState!=FormWindowState.Maximized){
                bool left=point.X<5,right=point.X>=ClientSize.Width-5,top=point.Y<5,bottom=point.Y>=ClientSize.Height-5;
                int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
                if(hit!=0){m.Result=(System.IntPtr)hit;return;}
            }
            int controls=close.Visible&&minimize.Visible?120:40;if(titleMovable&&point.Y<CaptionHeight&&point.X<ClientSize.Width-controls)m.Result=(System.IntPtr)2;
            return;
        }
        base.WndProc(ref m);
    }
}
public sealed class WindowCaptionButton : Button {
    readonly int symbol;bool hover;
    public WindowCaptionButton(string label,int symbol){this.symbol=symbol;AccessibleName=label;AccessibleRole=AccessibleRole.PushButton;TabStop=true;TabIndex=1000+symbol;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Theme.TitleSurface;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnMouseEnter(System.EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(System.EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnGotFocus(System.EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(System.EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.Clear(hover?(symbol==0?Color.FromArgb(196,43,28):Color.FromArgb(38,41,49)):Theme.TitleSurface);g.SmoothingMode=SmoothingMode.AntiAlias;
        var color=hover&&symbol==0?Color.White:hover?Theme.Ink:Theme.Muted;int cx=Width/2,cy=Height/2;
        using(var pen=new Pen(color,1.15f)){
            if(symbol==0){g.DrawLine(pen,cx-4,cy-4,cx+4,cy+4);g.DrawLine(pen,cx+4,cy-4,cx-4,cy+4);}
            else if(symbol==1)g.DrawLine(pen,cx-4,cy+3,cx+4,cy+3);
            else if(FindForm()!=null&&FindForm().WindowState==FormWindowState.Maximized){g.DrawRectangle(pen,cx-3,cy-4,7,7);g.DrawRectangle(pen,cx-5,cy-2,7,7);}
            else g.DrawRectangle(pen,cx-4,cy-4,8,8);
        }
        if(Focused)using(var focus=new SolidBrush(Theme.Accent))g.FillRectangle(focus,8,Height-2,Width-16,2);
    }
}
public static class ModalBackdrop {
    public static DialogResult Show(Form owner,Form dialog){
        using(var shade=new Form{Name="RiftReadyModalBackdrop",FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Bounds=owner.Bounds,BackColor=Color.Black,Opacity=.58,AutoScaleMode=AutoScaleMode.None}){
            shade.Show(owner);try{dialog.StartPosition=FormStartPosition.CenterParent;return dialog.ShowDialog(shade);}finally{shade.Close();if(!owner.IsDisposed)owner.Activate();}
        }
    }
}
public sealed class RiftToggle : CheckBox {
    const int SwitchWidth=30,SwitchHeight=16,SwitchGap=8;
    bool hover;
    public RiftToggle(){
        AutoSize=false;Appearance=Appearance.Normal;FlatStyle=FlatStyle.Flat;UseVisualStyleBackColor=false;
        TabStop=true;AccessibleRole=AccessibleRole.CheckButton;Cursor=Cursors.Hand;
        SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
    }
    Rectangle SwitchBounds {get{return new Rectangle(Math.Max(2,Width-SwitchWidth-3),Math.Max(0,(Height-SwitchHeight)/2),SwitchWidth,SwitchHeight);}}
    static GraphicsPath Pill(Rectangle box){
        var path=new GraphicsPath();int d=Math.Min(box.Width,box.Height);
        path.AddArc(box.Left,box.Top,d,d,90,180);path.AddArc(box.Right-d,box.Top,d,d,270,180);path.CloseFigure();return path;
    }
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        bool contrast=SystemInformation.HighContrast,on=CheckState!=CheckState.Unchecked;
        var track=SwitchBounds;int knobSize=SwitchHeight-4;var knob=new Rectangle(on?track.Right-knobSize-2:track.Left+2,track.Top+2,knobSize,knobSize);
        Color textColor,trackColor,borderColor,knobColor;
        if(contrast){
            textColor=Enabled?SystemColors.ControlText:SystemColors.GrayText;
            trackColor=on?SystemColors.Highlight:SystemColors.Control;
            borderColor=Enabled?SystemColors.WindowText:SystemColors.GrayText;
            knobColor=on?SystemColors.HighlightText:borderColor;
        }else if(!Enabled){
            textColor=Color.FromArgb(91,96,107);trackColor=Color.FromArgb(30,32,38);borderColor=Theme.Border;knobColor=Color.FromArgb(75,80,90);
        }else{
            textColor=Theme.Ink;trackColor=on?Color.FromArgb(27,72,72):Color.FromArgb(42,45,54);
            borderColor=on?(hover?ControlPaint.Light(Theme.Accent,.12f):Theme.Accent):(hover?Color.FromArgb(77,82,94):Theme.Border);
            knobColor=on?(hover?ControlPaint.Light(Theme.Accent,.18f):Theme.Accent):(hover?Color.FromArgb(166,172,184):Color.FromArgb(132,138,150));
        }
        var flags=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine;
        TextRenderer.DrawText(g,Text,Font,new Rectangle(0,0,Math.Max(1,track.Left-SwitchGap),Height),textColor,flags);
        using(var path=Pill(track))using(var brush=new SolidBrush(trackColor))using(var pen=new Pen(borderColor,1f)){g.FillPath(brush,path);g.DrawPath(pen,path);}
        using(var brush=new SolidBrush(knobColor))g.FillEllipse(brush,knob);
        if(Focused){var focus=track;focus.Inflate(2,2);using(var path=Pill(focus))using(var pen=new Pen(contrast?SystemColors.Highlight:Theme.Accent,1.5f))g.DrawPath(pen,path);}
    }
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
    protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);Invalidate();}
}
public sealed class RiftComboBox : Control {
    const int TriggerRadius=8,PopupRadius=10,ItemHeight=34,PopupPadding=6,MaximumVisibleItems=8;
    static readonly Color TriggerSurface=Color.FromArgb(12,32,43),TriggerHover=Color.FromArgb(17,44,57),PopupSurface=Color.FromArgb(5,17,25),SelectedSurface=Color.FromArgb(23,49,59),HoverSurface=Color.FromArgb(15,36,47);
    readonly ObjectCollection items;ToolStripDropDown popup;DropList list;int selectedIndex=-1;bool hover,expanded;string searchPrefix="";DateTime searchUtc;
    public event EventHandler SelectedIndexChanged;
    public RiftComboBox(){
        items=new ObjectCollection(this);MaxDropDownItems=MaximumVisibleItems;Size=new Size(160,30);Font=new Font("Segoe UI",9.5f);BackColor=Theme.Panel;ForeColor=Theme.Ink;Cursor=Cursors.Hand;TabStop=true;AccessibleRole=AccessibleRole.ComboBox;
        SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.SupportsTransparentBackColor,true);
    }
    public ObjectCollection Items {get{return items;}}
    public ComboBoxStyle DropDownStyle {get{return ComboBoxStyle.DropDownList;}set{if(value!=ComboBoxStyle.DropDownList)throw new NotSupportedException("Rift Ready dropdowns use selection-only lists.");}}
    public int MaxDropDownItems {get;set;}
    public bool DroppedDown {get{return expanded;}set{if(value)OpenPopup();else ClosePopup();}}
    public int SelectedIndex {
        get{return selectedIndex;}
        set{if(value< -1||value>=items.Count)throw new ArgumentOutOfRangeException("value");SetSelectedIndex(value);}
    }
    public object SelectedItem {
        get{return selectedIndex>=0&&selectedIndex<items.Count?items[selectedIndex]:null;}
        set{int found=-1;for(int i=0;i<items.Count;i++)if(Object.Equals(items[i],value)){found=i;break;}SetSelectedIndex(found);}
    }
    public override string Text {get{return Convert.ToString(SelectedItem)??"";}set{for(int i=0;i<items.Count;i++)if(String.Equals(Convert.ToString(items[i]),value,StringComparison.CurrentCulture)){SetSelectedIndex(i);return;}SetSelectedIndex(-1);}}
    void SetSelectedIndex(int value){
        if(value==selectedIndex)return;selectedIndex=value;base.Text=value>=0?Convert.ToString(items[value]):"";Invalidate();AccessibilityNotifyClients(AccessibleEvents.ValueChange,-1);if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);
    }
    internal void ItemsChanged(){if(selectedIndex>=items.Count)SetSelectedIndex(-1);ClosePopup();Invalidate();}
    static GraphicsPath Rounded(Rectangle box,int radius){
        var path=new GraphicsPath();int d=Math.Max(2,Math.Min(radius*2,Math.Min(box.Width,box.Height)));path.AddArc(box.Left,box.Top,d,d,180,90);path.AddArc(box.Right-d,box.Top,d,d,270,90);path.AddArc(box.Right-d,box.Bottom-d,d,d,0,90);path.AddArc(box.Left,box.Bottom-d,d,d,90,90);path.CloseFigure();return path;
    }
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;bool contrast=SystemInformation.HighContrast;
        var box=new Rectangle(0,0,Math.Max(1,Width-1),Math.Max(1,Height-1));Color fill=contrast?SystemColors.Window:!Enabled?Color.FromArgb(34,36,43):hover||expanded?TriggerHover:TriggerSurface;Color border=contrast?SystemColors.WindowText:Focused||expanded?Theme.Accent:Color.FromArgb(72,76,87);Color ink=contrast?SystemColors.WindowText:Enabled?Theme.Ink:Color.FromArgb(105,110,121);
        using(var path=Rounded(box,TriggerRadius))using(var brush=new SolidBrush(fill))using(var pen=new Pen(border,Focused||expanded?1.4f:1f)){g.FillPath(brush,path);g.DrawPath(pen,path);}
        int arrowWidth=Math.Min(34,Math.Max(26,Width/4));var textBox=new Rectangle(12,0,Math.Max(1,Width-arrowWidth-14),Height);TextRenderer.DrawText(g,Text,Font,textBox,ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding);
        float cx=Width-arrowWidth/2f-1,cy=Height/2f+.5f;using(var pen=new Pen(expanded||Focused?(contrast?SystemColors.Highlight:Theme.Accent):ink,1.8f)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;if(expanded){g.DrawLine(pen,cx-3.5f,cy+2,cx,cy-1.5f);g.DrawLine(pen,cx,cy-1.5f,cx+3.5f,cy+2);}else{g.DrawLine(pen,cx-3.5f,cy-2,cx,cy+1.5f);g.DrawLine(pen,cx,cy+1.5f,cx+3.5f,cy-2);}}
    }
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Enabled)return;Focus();if(expanded)ClosePopup();else OpenPopup();}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);if(!Enabled)ClosePopup();Invalidate();}
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(!Visible)ClosePopup();}
    protected override bool IsInputKey(Keys keyData){switch(keyData&Keys.KeyCode){case Keys.Up:case Keys.Down:case Keys.Home:case Keys.End:return true;}return base.IsInputKey(keyData);}
    protected override void OnKeyDown(KeyEventArgs e){
        base.OnKeyDown(e);if(!Enabled)return;
        if(e.KeyCode==Keys.F4||(e.Alt&&e.KeyCode==Keys.Down)||e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){if(expanded)ClosePopup();else OpenPopup();e.Handled=true;e.SuppressKeyPress=true;return;}
        if(e.KeyCode==Keys.Escape&&expanded){ClosePopup();e.Handled=true;e.SuppressKeyPress=true;return;}
        int next=selectedIndex;if(e.KeyCode==Keys.Down)next=Math.Min(items.Count-1,Math.Max(0,selectedIndex+1));else if(e.KeyCode==Keys.Up)next=Math.Max(0,selectedIndex<0?0:selectedIndex-1);else if(e.KeyCode==Keys.Home)next=items.Count==0?-1:0;else if(e.KeyCode==Keys.End)next=items.Count-1;else return;
        if(next>=0)SetSelectedIndex(next);e.Handled=true;e.SuppressKeyPress=true;
    }
    protected override void OnKeyPress(KeyPressEventArgs e){base.OnKeyPress(e);if(!Enabled||Char.IsControl(e.KeyChar))return;SelectByPrefix(e.KeyChar);e.Handled=true;}
    internal int SelectByPrefix(char key){
        var now=DateTime.UtcNow;string letter=key.ToString();bool continuing=(now-searchUtc).TotalMilliseconds<900;string next=continuing?searchPrefix+letter:letter;int start=continuing&&searchPrefix.Length==1&&Char.ToUpperInvariant(searchPrefix[0])==Char.ToUpperInvariant(key)?selectedIndex+1:0;int found=FindPrefix(next,start);if(found<0&&next.Length>1){next=letter;found=FindPrefix(next,selectedIndex+1);}searchPrefix=next;searchUtc=now;if(found>=0)SetSelectedIndex(found);return selectedIndex;
    }
    int FindPrefix(string prefix,int start){if(items.Count==0)return -1;start=Math.Max(0,start);for(int offset=0;offset<items.Count;offset++){int index=(start+offset)%items.Count;if((Convert.ToString(items[index])??"").StartsWith(prefix,StringComparison.CurrentCultureIgnoreCase))return index;}return -1;}
    void OpenPopup(){
        if(expanded||!Enabled||items.Count==0)return;ClosePopup();int rows=Math.Min(items.Count,Math.Max(1,MaxDropDownItems));int menuWidth=Width;using(var g=CreateGraphics())foreach(object item in items)menuWidth=Math.Max(menuWidth,TextRenderer.MeasureText(g,Convert.ToString(item),Font,new Size(Int32.MaxValue,ItemHeight),TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width+54);menuWidth=Math.Min(440,menuWidth);
        list=new DropList(this,menuWidth,rows*ItemHeight+PopupPadding*2);var host=new ToolStripControlHost(list){AutoSize=false,Margin=Padding.Empty,Padding=Padding.Empty,Size=list.Size};popup=new ToolStripDropDown{AutoSize=false,Padding=Padding.Empty,Margin=Padding.Empty,BackColor=SystemInformation.HighContrast?SystemColors.Window:PopupSurface,DropShadowEnabled=true,Size=list.Size};popup.Items.Add(host);popup.Opened+=(s,e)=>{expanded=true;Invalidate();AccessibilityNotifyClients(AccessibleEvents.StateChange,-1);using(var regionPath=Rounded(new Rectangle(Point.Empty,popup.Size),PopupRadius)){var old=popup.Region;popup.Region=new Region(regionPath);if(old!=null)old.Dispose();}list.Focus();};popup.Closed+=(s,e)=>{expanded=false;Invalidate();AccessibilityNotifyClients(AccessibleEvents.StateChange,-1);var old=(ToolStripDropDown)s;if(Object.ReferenceEquals(popup,old)){popup=null;list=null;}if(!IsDisposed&&IsHandleCreated)BeginInvoke((MethodInvoker)(()=>{if(!old.IsDisposed)old.Dispose();}));if(!IsDisposed&&Visible)Focus();};
        popup.Show(this,new Point(0,Height+4),ToolStripDropDownDirection.BelowRight);
    }
    void ClosePopup(){if(popup!=null&&!popup.IsDisposed)popup.Close();}
    internal void CommitFromPopup(int index){SetSelectedIndex(index);ClosePopup();}
    protected override void Dispose(bool disposing){if(disposing&&popup!=null){var open=popup;popup=null;list=null;open.Close();if(!open.IsDisposed)open.Dispose();}base.Dispose(disposing);}
    protected override AccessibleObject CreateAccessibilityInstance(){return new ComboAccessible(this);}
    sealed class ComboAccessible : ControlAccessibleObject {
        readonly RiftComboBox owner;public ComboAccessible(RiftComboBox owner):base(owner){this.owner=owner;}
        public override string Value {get{return owner.Text;}set{owner.Text=value;}}
        public override AccessibleStates State {get{return base.State|(owner.expanded?AccessibleStates.Expanded:AccessibleStates.Collapsed);}}
        public override void DoDefaultAction(){owner.OpenPopup();}
    }
    public sealed class ObjectCollection : IList {
        readonly RiftComboBox owner;readonly List<object> values=new List<object>();internal ObjectCollection(RiftComboBox owner){this.owner=owner;}
        public int Add(object value){values.Add(value);owner.ItemsChanged();return values.Count-1;}
        public void AddRange(object[] value){if(value==null)throw new ArgumentNullException("value");values.AddRange(value);owner.ItemsChanged();}
        public void Clear(){values.Clear();owner.ItemsChanged();}
        public bool Contains(object value){return values.Contains(value);}public int IndexOf(object value){return values.IndexOf(value);}
        public void Insert(int index,object value){values.Insert(index,value);owner.ItemsChanged();}
        public void Remove(object value){if(values.Remove(value))owner.ItemsChanged();}
        public void RemoveAt(int index){values.RemoveAt(index);owner.ItemsChanged();}
        public object this[int index]{get{return values[index];}set{values[index]=value;owner.ItemsChanged();}}
        public int Count{get{return values.Count;}}public bool IsReadOnly{get{return false;}}public bool IsFixedSize{get{return false;}}public bool IsSynchronized{get{return false;}}public object SyncRoot{get{return ((ICollection)values).SyncRoot;}}
        public void CopyTo(Array array,int index){((ICollection)values).CopyTo(array,index);}public IEnumerator GetEnumerator(){return values.GetEnumerator();}
    }
    sealed class DropList : Control {
        readonly RiftComboBox owner;int active,hot=-1,first;bool scrolling;
        public DropList(RiftComboBox owner,int width,int height){this.owner=owner;Size=new Size(width,height);BackColor=PopupSurface;ForeColor=Theme.Ink;Font=owner.Font;TabStop=true;Cursor=Cursors.Hand;active=Math.Max(0,owner.SelectedIndex);SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);EnsureVisible();}
        int Rows{get{return Math.Max(1,(Height-PopupPadding*2)/ItemHeight);}}
        void EnsureVisible(){if(active<first)first=active;if(active>=first+Rows)first=active-Rows+1;first=Math.Max(0,Math.Min(first,Math.Max(0,owner.Items.Count-Rows)));Invalidate();}
        int IndexAt(Point point){int right=owner.Items.Count>Rows?Width-18:Width-PopupPadding;if(point.X<PopupPadding||point.X>=right||point.Y<PopupPadding)return -1;int row=(point.Y-PopupPadding)/ItemHeight;if(row<0||row>=Rows)return -1;int index=first+row;return index<owner.Items.Count?index:-1;}
        void ScrollTo(int y){int trackHeight=Height-PopupPadding*2,limit=Math.Max(0,owner.Items.Count-Rows);first=limit==0?0:Math.Max(0,Math.Min(limit,(int)Math.Round((y-PopupPadding)*limit/(double)Math.Max(1,trackHeight))));active=Math.Max(first,Math.Min(active,first+Rows-1));Invalidate();}
        protected override void OnPaint(PaintEventArgs e){
            var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;bool contrast=SystemInformation.HighContrast;Color surface=contrast?SystemColors.Window:PopupSurface,borderColor=contrast?SystemColors.WindowText:Color.FromArgb(37,40,48);using(var outer=Rounded(new Rectangle(0,0,Width-1,Height-1),PopupRadius))using(var fill=new SolidBrush(surface))using(var border=new Pen(borderColor)){g.FillPath(fill,outer);g.DrawPath(border,outer);}int right=owner.Items.Count>Rows?Width-18:Width-PopupPadding;
            for(int row=0,index=first;row<Rows&&index<owner.Items.Count;row++,index++){var item=new Rectangle(PopupPadding,PopupPadding+row*ItemHeight,Math.Max(1,right-PopupPadding),ItemHeight);bool selected=index==owner.SelectedIndex,over=index==hot||index==active&&Focused;if(selected||over)using(var path=Rounded(new Rectangle(item.X,item.Y+1,item.Width,item.Height-2),6))using(var brush=new SolidBrush(contrast?(selected?SystemColors.Highlight:SystemColors.Control):selected?SelectedSurface:HoverSurface))g.FillPath(brush,path);Color ink=contrast?(selected?SystemColors.HighlightText:SystemColors.WindowText):Enabled?Theme.Ink:Theme.Muted;TextRenderer.DrawText(g,Convert.ToString(owner.Items[index]),Font,new Rectangle(item.X+9,item.Y,item.Width-41,item.Height),ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding);if(selected){float cx=item.Right-19,cy=item.Top+ItemHeight/2f;using(var check=new Pen(contrast?SystemColors.HighlightText:Theme.Accent,1.8f)){check.StartCap=LineCap.Round;check.EndCap=LineCap.Round;g.DrawLine(check,cx-4,cy,cx-1,cy+3);g.DrawLine(check,cx-1,cy+3,cx+5,cy-4);}}}
            if(owner.Items.Count>Rows){int trackHeight=Height-PopupPadding*2,thumbHeight=Math.Max(24,(int)Math.Round(trackHeight*Rows/(double)owner.Items.Count)),travel=Math.Max(1,trackHeight-thumbHeight),limit=Math.Max(1,owner.Items.Count-Rows),top=PopupPadding+(int)Math.Round(travel*first/(double)limit);using(var track=new SolidBrush(contrast?SystemColors.Control:Color.FromArgb(31,33,40)))g.FillRectangle(track,Width-10,PopupPadding,3,trackHeight);using(var thumb=new SolidBrush(contrast?SystemColors.Highlight:Theme.Accent))g.FillRectangle(thumb,Width-10,top,3,thumbHeight);}
        }
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(scrolling){ScrollTo(e.Y);return;}int next=IndexAt(e.Location);if(next!=hot){hot=next;Invalidate();}}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hot=-1;Invalidate();}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;if(owner.Items.Count>Rows&&e.X>=Width-18){scrolling=true;Capture=true;ScrollTo(e.Y);return;}int index=IndexAt(e.Location);if(index>=0)owner.CommitFromPopup(index);}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left){scrolling=false;Capture=false;}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)scrolling=false;}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(owner.Items.Count<=Rows)return;first=Math.Max(0,Math.Min(owner.Items.Count-Rows,first+(e.Delta<0?1:-1)));active=Math.Max(first,Math.Min(active,first+Rows-1));Invalidate();}
        protected override bool IsInputKey(Keys keyData){switch(keyData&Keys.KeyCode){case Keys.Up:case Keys.Down:case Keys.Home:case Keys.End:case Keys.PageUp:case Keys.PageDown:return true;}return base.IsInputKey(keyData);}
        protected override void OnKeyPress(KeyPressEventArgs e){base.OnKeyPress(e);if(Char.IsControl(e.KeyChar))return;active=owner.SelectByPrefix(e.KeyChar);EnsureVisible();e.Handled=true;}
        protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Escape){owner.ClosePopup();e.Handled=true;return;}if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){owner.CommitFromPopup(active);e.Handled=true;e.SuppressKeyPress=true;return;}int next=active;if(e.KeyCode==Keys.Down)next++;else if(e.KeyCode==Keys.Up)next--;else if(e.KeyCode==Keys.Home)next=0;else if(e.KeyCode==Keys.End)next=owner.Items.Count-1;else if(e.KeyCode==Keys.PageDown)next+=Rows;else if(e.KeyCode==Keys.PageUp)next-=Rows;else return;active=Math.Max(0,Math.Min(owner.Items.Count-1,next));EnsureVisible();e.Handled=true;e.SuppressKeyPress=true;}
    }
}
public sealed class HistoryScrollBar : Control {
    int value,maximum,largeChange=1;bool hover,dragging;int grab;
    public event System.EventHandler ValueChanged;
    public int SmallChange{get;set;}
    public int Maximum{get{return maximum;}set{int next=System.Math.Max(0,value);if(next==maximum)return;maximum=next;Value=this.value;Invalidate();}}
    public int LargeChange{get{return largeChange;}set{int next=System.Math.Max(1,value);if(next==largeChange)return;largeChange=next;Value=this.value;Invalidate();}}
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
    public static readonly Color Background=Color.FromArgb(4,14,21),TitleSurface=Color.FromArgb(4,16,23),Panel=Color.FromArgb(9,26,35),Border=Color.FromArgb(24,59,73),Accent=Color.FromArgb(66,205,198),Ink=Color.FromArgb(235,245,250),Muted=Color.FromArgb(151,180,198);
    public static void Surface(Graphics g, Rectangle box, Color fill, Color accent) {
        if(box.Width<20||box.Height<20)return;
        var old=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var path=new GraphicsPath()){
            const int d=12;int x=box.X,y=box.Y,w=box.Width-1,h=box.Height-1;
            path.AddArc(x,y,d,d,180,90);path.AddArc(x+w-d,y,d,d,270,90);path.AddArc(x+w-d,y+h-d,d,d,0,90);path.AddArc(x,y+h-d,d,d,90,90);path.CloseFigure();
            using(var brush=new LinearGradientBrush(box,fill,ControlPaint.Dark(fill,.12f),LinearGradientMode.Vertical))g.FillPath(brush,path);
            using(var pen=new Pen(Border))g.DrawPath(pen,path);
        }g.SmoothingMode=old;
    }
    public static void Apply(Control root) {
        var form=root as Form;if(form!=null){form.Icon=Brand.Icon;TitleBar(form);}
        root.BackColor=root is TextBoxBase || root is ListBox || root is ComboBox || root is RiftComboBox || root is SettingsCard || root.Parent is SettingsCard ? Panel : Background;
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
    public bool HeaderTab;
    public string Symbol="";
    bool hover;
    public NavigationButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;Cursor=Cursors.Hand;}
    protected override void OnMouseEnter(System.EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(System.EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){
        if(HeaderTab){var canvas=e.Graphics;canvas.Clear(hover?Theme.Panel:Theme.TitleSurface);var tone=!Enabled?Theme.Muted:Active?Theme.Accent:Theme.Ink;TextRenderer.DrawText(canvas,Text,Font,ClientRectangle,tone,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);if(Active||Focused)using(var line=new SolidBrush(Theme.Accent))canvas.FillRectangle(line,12,Height-3,Width-24,3);return;}
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
        if(Width>44||Symbol=="")TextRenderer.DrawText(g,Text,Font,new Rectangle(offset,0,Width-offset,Height),color,TextFormatFlags.VerticalCenter|(Symbol==""?TextFormatFlags.HorizontalCenter:TextFormatFlags.Left)|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(4,4,Width-8,Height-8),Theme.Accent,BackColor);
    }
}
}
