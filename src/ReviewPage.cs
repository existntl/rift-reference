using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RiftReference {
// The editor stays alive when navigating. Destructive transitions are confirmed inline.
public sealed class ReviewPage : UserControl {
 readonly string root;string champion,focus;readonly ListBox history=new ListBox{Name="ReviewHistory",IntegralHeight=false};
 readonly TextBox[] answers=new TextBox[3];readonly RiftComboBox influence=new RiftComboBox{Name="ReviewInfluence"};
 readonly Label heading,status;readonly Panel editor=new Panel{AutoScroll=true};readonly FlowLayoutPanel confirmation=new FlowLayoutPanel{Visible=false,Height=44};
 Reflection active;bool loading,editable=true;Action pending;readonly Button save;
 public bool Dirty{get;private set;}
 public ReviewPage(string root,string champion,string focus){this.root=root;this.champion=champion;this.focus=focus;Name="ReviewPage";Size=new Size(1200,850);BackColor=Theme.Background;
  PageControls.Label(this,"Review",24,14,800,45,25);PageControls.Label(this,"Your decisions, in your words. Drafts stay here as you browse; saved notes stay on this PC.",26,66,1100,28,10,Theme.Muted);
  PageControls.Button(this,"New reflection",24,118,232,()=>ConfirmLeave(()=>Load(new Reflection{Champion=this.champion,Focus=this.focus}),"Discard these edits and start a new reflection?"));
  Controls.Add(history);Controls.Add(editor);heading=PageControls.Label(editor,"",0,0,850,34,15,Theme.Accent);
  string[] questions={"What decision created your first major disadvantage?","What advantage did you fail to convert?","What will you do differently next game?"};
  for(int i=0;i<3;i++){PageControls.Label(editor,(i+1)+". "+questions[i],0,60+i*148,870,28);var box=new TextBox{Name="ReviewAnswer"+i,Multiline=true,MaxLength=2000,ScrollBars=ScrollBars.Vertical,Bounds=new Rectangle(0,95+i*148,850,94)};box.TextChanged+=(s,e)=>{if(!loading){Dirty=true;status.Text="Unsaved draft · Kept while you browse other pages.";}};answers[i]=box;editor.Controls.Add(box);}
  PageControls.Label(editor,"Did the focus influence a decision?",0,516,370,28);influence.SetBounds(380,512,240,34);influence.Items.AddRange(new object[]{"Not assessed","Yes","Partly","Not yet"});influence.SelectedIndexChanged+=(s,e)=>{if(!loading)Dirty=true;};editor.Controls.Add(influence);
  save=PageControls.Button(editor,"Save reflection",0,574,200,()=>Save());status=PageControls.Label(editor,"",0,625,900,44,10,Theme.Muted);
  editor.Controls.Add(confirmation);confirmation.Top=681;confirmation.Width=900;
  AddConfirm("Save & continue",()=>{if(Save())Continue();});AddConfirm("Discard edits",()=>{Dirty=false;Continue();});AddConfirm("Keep editing",()=>{pending=null;confirmation.Visible=false;});
  history.SelectedIndexChanged+=(s,e)=>{if(loading||history.SelectedItem==null)return;var chosen=(Reflection)history.SelectedItem;loading=true;history.SelectedItem=history.Items.Cast<Reflection>().FirstOrDefault(r=>active!=null&&r.Id==active.Id);loading=false;ConfirmLeave(()=>Load(chosen),"Save or discard this draft before opening another reflection.");};
  Resize+=(s,e)=>{history.SetBounds(24,170,232,Math.Max(120,Height-194));editor.SetBounds(288,118,Math.Max(500,Width-320),Math.Max(200,Height-142));foreach(var box in answers)box.Width=Math.Max(400,editor.ClientSize.Width-24);heading.Width=status.Width=confirmation.Width=Math.Max(400,editor.ClientSize.Width-24);};
  Theme.Apply(this);Reload();Load(new Reflection{Champion=champion,Focus=focus});
 }
 void AddConfirm(string text,Action action){var button=new Button{Text=text,Width=170,Height=34};button.Click+=(s,e)=>action();confirmation.Controls.Add(button);}
 void Reload(){loading=true;try{history.Items.Clear();history.Items.AddRange(ReflectionStore.Read(root).AsEnumerable().Reverse().Cast<object>().ToArray());}catch(Exception ex){status.Text="Existing notes are preserved: "+ex.Message;}finally{loading=false;}}
 new void Load(Reflection value){loading=true;active=value;heading.Text=(value.SavedUtc==""?"New reflection":value.ToString())+" · Focus: "+value.Focus;answers[0].Text=value.Disadvantage;answers[1].Text=value.Conversion;answers[2].Text=value.NextGame;influence.SelectedItem=value.Influence;if(influence.SelectedIndex<0)influence.SelectedIndex=0;history.SelectedItem=history.Items.Cast<Reflection>().FirstOrDefault(r=>r.Id==value.Id);loading=false;Dirty=false;pending=null;confirmation.Visible=false;}
 public bool Save(){if(!editable){status.Text="Finish the match before saving a review. Your draft is kept.";return false;}if(answers.All(a=>String.IsNullOrWhiteSpace(a.Text))){status.Text="Write at least one answer before saving.";return false;}var next=new Reflection{Id=active.Id,SavedUtc=DateTime.UtcNow.ToString("o"),Champion=active.Champion,Focus=active.Focus,Influence=Convert.ToString(influence.SelectedItem),Disadvantage=answers[0].Text.Trim(),Conversion=answers[1].Text.Trim(),NextGame=answers[2].Text.Trim()};try{ReflectionStore.Save(root,next);var continuation=pending;Reload();Load(next);pending=continuation;status.Text="Saved locally. Select a previous reflection to revisit or edit it.";return true;}catch(Exception ex){status.Text="Could not save; your draft is retained: "+ex.Message;return false;}}
 public void UpdateContext(string nextChampion,string nextFocus){if(champion==nextChampion&&focus==nextFocus)return;champion=nextChampion;focus=nextFocus;if(!Dirty&&active.SavedUtc=="")Load(new Reflection{Champion=champion,Focus=focus});}
 public void SetEditable(bool value){if(editable==value)return;editable=value;save.Enabled=value;foreach(var box in answers)box.ReadOnly=!value;influence.Enabled=value;status.Text=value?(Dirty?"Unsaved draft · Kept while you browse other pages.":"Review is ready."):"Review is paused during your match. Your draft is kept.";}
 public void ConfirmLeave(Action action,string message){if(!Dirty){action();return;}pending=action;status.Text=message;confirmation.Visible=true;editor.ScrollControlIntoView(confirmation);}
 void Continue(){var action=pending;pending=null;confirmation.Visible=false;if(action!=null)action();}
}
}
