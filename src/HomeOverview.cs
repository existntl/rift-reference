using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RiftReference {
// One retained surface: moving the window reuses pixels; scrolling redraws only the table.
// The accessible children describe the same records, never a second sample data source.
public sealed class HomeOverview : Control {
 readonly DataStore data;readonly Func<string,Image> portrait,rankBadge;readonly Func<int,Image> itemIcon;
 readonly Action<HomeMatch> open;readonly Action<int> scroll;
 HomeProfile profile;List<HomeMatch> matches=new List<HomeMatch>();string search="";int queue,limit,offset,selected=-1;
 Bitmap canvas;bool fullDirty=true,rowsDirty;int fullRenderCount,rowRenderCount;
 public int SelectedIndex{get{return selected;}}
 public int Offset{get{return offset;}}
 public int MatchCount{get{return matches.Count;}}
 public HomeOverview(DataStore data,Func<string,Image> portrait,Func<int,Image> itemIcon,Func<string,Image> rankBadge,Action<HomeMatch> open,Action<int> scroll){
  this.data=data;this.portrait=portrait;this.itemIcon=itemIcon;this.rankBadge=rankBadge;this.open=open;this.scroll=scroll;
  Name="HomeOverview";AccessibleName="Player overview and recent matches";AccessibleRole=AccessibleRole.List;
  AccessibleDescription="Use Up and Down to choose a match, Page Up or Page Down to scroll, and Enter to open match details. Control F searches matches.";
  BackColor=Theme.Background;TabStop=true;SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
 }
 public void Configure(HomeProfile value,string query,int queueId,int range,int first){
  if(!ReferenceEquals(profile,value)||search!=query||queue!=queueId||limit!=range){
   bool accountChanged=profile==null||value==null||profile.Name!=value.Name;
   profile=value;search=query;queue=queueId;limit=range;matches=HomeData.Filter(profile,search,queue,limit);
   selected=accountChanged?-1:Math.Min(selected,matches.Count-1);fullDirty=true;Invalidate();
   AccessibilityNotifyClients(AccessibleEvents.Reorder,-1);
  }
  SetOffset(first);
 }
 public void SetOffset(int value){int next=Math.Max(0,Math.Min(value,Math.Max(0,matches.Count-HomeDashboard.VisibleMatches(ClientRectangle))));if(next==offset)return;offset=next;rowsDirty=true;Invalidate(TableArea);AccessibilityNotifyClients(AccessibleEvents.LocationChange,-1);}
 Rectangle TableArea{get{var table=HomeDashboard.TableBounds(ClientRectangle);return new Rectangle(table.X,table.Y+92,table.Width,Math.Max(1,table.Height-92));}}
 protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);fullDirty=true;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){
  if(Width<600||Height<300)return;
  if(canvas==null||canvas.Size!=Size){if(canvas!=null)canvas.Dispose();canvas=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);fullDirty=true;}
  if(fullDirty||rowsDirty)using(var g=Graphics.FromImage(canvas)){
   g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
   bool onlyRows=!fullDirty;if(fullDirty){g.Clear(BackColor);fullRenderCount++;}else rowRenderCount++;
   HomeDashboard.Draw(g,ClientRectangle,data,profile,portrait,offset,search,queue,limit,itemIcon,rankBadge,onlyRows,matches);
   fullDirty=rowsDirty=false;
  }
  e.Graphics.DrawImageUnscaled(canvas,Point.Empty);
  if(Focused&&selected>=offset&&selected<offset+HomeDashboard.VisibleMatches(ClientRectangle)){
   var row=HomeDashboard.RowBounds(ClientRectangle,selected-offset);row.Inflate(-2,-2);
   using(var pen=new Pen(SystemInformation.HighContrast?SystemColors.Highlight:Theme.Accent,2))e.Graphics.DrawRectangle(pen,row);
  }
 }
 int IndexAt(Point point){for(int i=0;i<HomeDashboard.VisibleMatches(ClientRectangle)&&offset+i<matches.Count;i++)if(HomeDashboard.RowBounds(ClientRectangle,i).Contains(point))return offset+i;return -1;}
 public void SelectMatch(int index){if(matches.Count==0)return;selected=Math.Max(0,Math.Min(index,matches.Count-1));int visible=HomeDashboard.VisibleMatches(ClientRectangle);if(selected<offset)scroll(selected);else if(selected>=offset+visible)scroll(selected-visible+1);Focus();Invalidate(TableArea);AccessibilityNotifyClients(AccessibleEvents.Focus,selected+2);AccessibilityNotifyClients(AccessibleEvents.Selection,selected+2);}
 void OpenSelected(){if(selected>=0&&selected<matches.Count)open(matches[selected]);}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);Cursor=IndexAt(e.Location)>=0?Cursors.Hand:Cursors.Default;}
 protected override void OnMouseClick(MouseEventArgs e){base.OnMouseClick(e);if(e.Button!=MouseButtons.Left)return;int index=IndexAt(e.Location);if(index>=0){SelectMatch(index);OpenSelected();}}
 protected override void OnMouseWheel(MouseEventArgs e){if(e.Delta!=0)scroll(offset+(e.Delta<0?1:-1));var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);if(matches.Count>0&&(selected<offset||selected>=offset+HomeDashboard.VisibleMatches(ClientRectangle)))selected=offset;Invalidate(TableArea);if(selected>=0)AccessibilityNotifyClients(AccessibleEvents.Focus,selected+2);}
 protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate(TableArea);}
 protected override bool IsInputKey(Keys keys){switch(keys&Keys.KeyCode){case Keys.Up:case Keys.Down:case Keys.Home:case Keys.End:case Keys.PageUp:case Keys.PageDown:case Keys.Enter:case Keys.Space:return true;}return base.IsInputKey(keys);}
 protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.Modifiers!=Keys.None)return;int current=Math.Max(offset,selected);switch(e.KeyCode){case Keys.Up:SelectMatch(current-1);break;case Keys.Down:SelectMatch(current+1);break;case Keys.Home:SelectMatch(0);break;case Keys.End:SelectMatch(matches.Count-1);break;case Keys.PageUp:SelectMatch(current-HomeDashboard.VisibleMatches(ClientRectangle));break;case Keys.PageDown:SelectMatch(current+HomeDashboard.VisibleMatches(ClientRectangle));break;case Keys.Enter:case Keys.Space:OpenSelected();break;default:return;}e.Handled=true;e.SuppressKeyPress=true;}
 protected override AccessibleObject CreateAccessibilityInstance(){return new OverviewAccessible(this);}
 sealed class OverviewAccessible : ControlAccessibleObject {
  readonly HomeOverview owner;public OverviewAccessible(HomeOverview owner):base(owner){this.owner=owner;}
  public override int GetChildCount(){return owner.matches.Count+2+owner.Controls.Count;}
  public override AccessibleObject GetChild(int index){if(index<0||index>=GetChildCount())return null;return index<owner.matches.Count+2?new EntryAccessible(owner,index):owner.Controls[index-owner.matches.Count-2].AccessibilityObject;}
  public override AccessibleObject GetFocused(){return owner.Focused&&owner.selected>=0?GetChild(owner.selected+2):base.GetFocused();}
  public override AccessibleObject GetSelected(){return owner.selected>=0?GetChild(owner.selected+2):null;}
  public override AccessibleObject HitTest(int x,int y){var p=owner.PointToClient(new Point(x,y));if(!owner.ClientRectangle.Contains(p))return null;foreach(Control child in owner.Controls)if(child.Visible&&child.Bounds.Contains(p))return child.AccessibilityObject;int index=owner.IndexAt(p);return index>=0?GetChild(index+2):HomeDashboard.SummaryBounds(owner.ClientRectangle).Contains(p)?GetChild(1):p.X<HomeDashboard.SidebarWidth(owner.ClientRectangle)?GetChild(0):this;}
 }
 sealed class EntryAccessible : AccessibleObject {
  readonly HomeOverview owner;readonly int index;
  public EntryAccessible(HomeOverview owner,int index){this.owner=owner;this.index=index;}
  bool Valid{get{return index<owner.matches.Count+2;}}
  public override AccessibleObject Parent{get{return owner.AccessibilityObject;}}
  public override AccessibleRole Role{get{return index<2?AccessibleRole.StaticText:AccessibleRole.ListItem;}}
  public override string Name{get{return (!Valid?"Unavailable":index==0?HomeDashboard.ProfileDescription(owner.profile):index==1?HomeDashboard.SummaryDescription(owner.matches):HomeDashboard.MatchDescription(owner.data,owner.matches[index-2])).Replace("—","unavailable");}set{}}
  public override string DefaultAction{get{return index>=2?"Open match details":"";}}
  public override string Description{get{return index>=2?"Select this match to open its final statistics and inventory.":"Statistics from the local League client. Missing values are unavailable.";}}
  public override Rectangle Bounds{get{if(!Valid||!owner.Visible)return Rectangle.Empty;Rectangle r;if(index==0)r=new Rectangle(0,0,HomeDashboard.SidebarWidth(owner.ClientRectangle),owner.Height);else if(index==1)r=HomeDashboard.SummaryBounds(owner.ClientRectangle);else {int row=index-2-owner.offset;if(row<0||row>=HomeDashboard.VisibleMatches(owner.ClientRectangle))return Rectangle.Empty;r=HomeDashboard.RowBounds(owner.ClientRectangle,row);}return owner.RectangleToScreen(r);}}
  public override AccessibleStates State{get{return (index<2?AccessibleStates.ReadOnly:AccessibleStates.Focusable|AccessibleStates.Selectable)|(Bounds.IsEmpty?AccessibleStates.Offscreen:AccessibleStates.None)|(index-2==owner.selected&&index>=2?AccessibleStates.Selected|(owner.Focused?AccessibleStates.Focused:AccessibleStates.None):AccessibleStates.None);}}
  public override void DoDefaultAction(){if(index<2||!Valid||!owner.Visible)return;owner.SelectMatch(index-2);owner.OpenSelected();}
  public override void Select(AccessibleSelection flags){if(index>=2&&Valid&&owner.Visible)owner.SelectMatch(index-2);}
  public override AccessibleObject Navigate(AccessibleNavigation direction){if(direction==AccessibleNavigation.Next||direction==AccessibleNavigation.Down)return Parent.GetChild(index+1);if(direction==AccessibleNavigation.Previous||direction==AccessibleNavigation.Up)return Parent.GetChild(index-1);return null;}
 }
 protected override void Dispose(bool disposing){if(disposing&&canvas!=null){canvas.Dispose();canvas=null;}base.Dispose(disposing);}
}
}
