using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RiftReference {
public partial class Dashboard {
 readonly PageHistory pageHistory=new PageHistory();
 readonly Panel pageHost=new Panel{Name="PageHost",Visible=false};
 readonly Panel pageTrail=new Panel{Name="PageNavigation"};
 readonly Dictionary<string,Control> pages=new Dictionary<string,Control>();
 readonly Dictionary<string,HomeMatch> detailMatches=new Dictionary<string,HomeMatch>();
 Button backPage,forwardPage;string pageAccount="";bool allowClose;
 void InitializePages(){
  Controls.Add(pageHost);Controls.Add(pageTrail);pageHost.BackColor=pageTrail.BackColor=Theme.Background;
  backPage=PageControls.Button(pageTrail,"←",0,0,42,()=>NavigatePage(pageHistory.Back(),false));backPage.AccessibleName="Back";
  forwardPage=PageControls.Button(pageTrail,"→",48,0,42,()=>NavigatePage(pageHistory.Forward(),false));forwardPage.AccessibleName="Forward";
  PageControls.Button(pageTrail,"Overview",110,0,126,()=>NavigatePage("overview"));
  PageControls.Button(pageTrail,"Champion pool",248,0,150,()=>NavigatePage("champions"));
  PageControls.Button(pageTrail,"LP history",410,0,126,()=>NavigatePage("rank"));
  MouseClick+=(s,e)=>{if(e.Button!=MouseButtons.Left||!ShowHome)return;var matches=HomeData.Filter(CurrentHome,matchSearch.Text,SelectedQueue,SelectedRange);for(int i=0;i<HomeDashboard.VisibleMatches(HomeBounds)&&i+homeOffset<matches.Count;i++){var bounds=HomeDashboard.RowBounds(HomeBounds,i);bounds.Offset(0,ShellHeight);if(!bounds.Contains(e.Location))continue;var match=matches[i+homeOffset];string key="detail:"+Guid.NewGuid().ToString("N");detailMatches[key]=match;NavigatePage(key);break;}};
  FormClosing+=(s,e)=>{ReviewPage page;if(!allowClose&&pages.ContainsKey("review")&&(page=pages["review"] as ReviewPage)!=null&&page.Dirty){e.Cancel=true;NavigatePage("review");page.ConfirmLeave(()=>{allowClose=true;Close();},"Save or discard your reflection before closing Rift Ready.");}};
 }
 void LayoutPages(){if(backPage==null)return;pageTrail.SetBounds(40,ShellHeight+(ShowHome?182:8),Math.Max(540,ClientSize.Width-80),38);pageTrail.Visible=selectedView!="match";pageHost.SetBounds(16,ShellHeight+58,ClientSize.Width-32,ContentHeight-74);pageTrail.BringToFront();}
 void NavigatePage(string route,bool record=true){
  if(pageAccount!=state.Account)RefreshPageContext();
  if(record)pageHistory.Navigate(route);selectedView=route;
  if(route=="overview"||route=="match")pageHost.Visible=false;
  else {
   Control page;if(!pages.TryGetValue(route,out page)){
    if(route=="champions")page=new ChampionsPage(data,Portrait,key=>NavigatePage("champion:"+key));
    else if(route=="rank")page=new RankHistoryPage();
    else if(route=="matchups")page=new LessonsPage(data);
    else if(route=="review"){var context=state.Players.Count>0?state:lastGame??state;var self=context.Players.FirstOrDefault(p=>p.Self);page=new ReviewPage(home,self==null?"":data.Name(self.Champion),prefs.TrainingFocus);}
    else if(route=="updates"){var updates=new UpdatesDialog(updater,prefs.AutoUpdates,CheckUpdates,()=>CanUpdate()&&!pages.Values.OfType<ReviewPage>().Any(p=>p.Dirty),value=>{prefs.AutoUpdates=value;SaveLayout();});updates.InstallCompleted+=()=>Close();page=new HostedPage(updates);}
    else if(route.StartsWith("champion:")){string key=route.Substring(9);page=new ChampionPage(data,key,CurrentHome,Portrait,()=>{historyQueue.SelectedIndex=0;matchSearch.Text=key;NavigatePage("overview");});}
    else if(route.StartsWith("detail:")&&detailMatches.ContainsKey(route)){var match=detailMatches[route];page=new MatchDetailPage(data,match,()=>NavigatePage("champion:"+match.Champion));}
    else {NavigatePage("overview");return;}
    var userPage=page as UserControl;if(userPage!=null)userPage.AutoScaleMode=AutoScaleMode.None;page.Font=new Font("Segoe UI",11);page.Dock=DockStyle.Fill;page.Visible=false;pages.Add(route,page);pageHost.Controls.Add(page);
   }
   foreach(Control child in pageHost.Controls)child.Visible=ReferenceEquals(child,page);page.BringToFront();pageHost.Visible=true;
  }
  backPage.Enabled=pageHistory.CanBack;forwardPage.Enabled=pageHistory.CanForward;
  foreach(var pair in new[]{Tuple.Create(dashboardButton,"overview"),Tuple.Create(championsButton,"champions"),Tuple.Create(playbook,"matchups"),Tuple.Create(review,"review"),Tuple.Create(liveGameButton,"match"),Tuple.Create(updatesButton,"updates")}){((NavigationButton)pair.Item1).Active=route==pair.Item2||(pair.Item2=="champions"&&route.StartsWith("champion:"));pair.Item1.Invalidate();}
  RefreshPageContext();LayoutPages();LayoutHistoryScroll();Invalidate();
 }
 void RefreshPageContext(){
  if(backPage==null)return;
  // Account-bound cached pages must not leak into the next user's session. Review notes
  // are intentionally PC-local and retained, including unsaved edits.
  if(pageAccount!=state.Account){pageAccount=state.Account;foreach(string key in pages.Keys.Where(k=>k=="champions"||k=="rank"||k.StartsWith("champion:")||k.StartsWith("detail:")).ToArray()){pages[key].Dispose();pages.Remove(key);}detailMatches.Clear();pageHistory.Reset();if(selectedView=="champions"||selectedView=="rank"||selectedView.StartsWith("champion:")||selectedView.StartsWith("detail:")){NavigatePage("overview");return;}}
  Control page;if(pages.TryGetValue(selectedView,out page)){
   var champions=page as ChampionsPage;if(champions!=null)champions.UpdateProfile(CurrentHome);
   var rank=page as RankHistoryPage;if(rank!=null)rank.UpdateProfile(CurrentHome);
   var lessons=page as LessonsPage;if(lessons!=null)lessons.UpdateContext(state.Players.Count>0?state:lastGame??state);
   var reflection=page as ReviewPage;if(reflection!=null)reflection.SetEditable(CanReview());
   var hosted=page as HostedPage;if(hosted!=null&&hosted.Content is UpdatesDialog)((UpdatesDialog)hosted.Content).RefreshStatus();
  }
  review.Enabled=CanReview();backPage.Enabled=pageHistory.CanBack;forwardPage.Enabled=pageHistory.CanForward;
 }
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==(Keys.Alt|Keys.Left)&&pageHistory.CanBack){NavigatePage(pageHistory.Back(),false);return true;}if(keyData==(Keys.Alt|Keys.Right)&&pageHistory.CanForward){NavigatePage(pageHistory.Forward(),false);return true;}return base.ProcessCmdKey(ref msg,keyData);}
}
}
