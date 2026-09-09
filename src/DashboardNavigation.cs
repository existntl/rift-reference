using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RiftReference {
public partial class Dashboard {
 readonly PageHistory pageHistory=new PageHistory();
 readonly Panel pageHost=new Panel{Name="PageHost",AccessibleName="Current page",Visible=false,TabIndex=30};
 readonly Panel pageTrail=new Panel{Name="PageNavigation",AccessibleName="Page navigation",TabIndex=10};
 readonly Dictionary<string,Control> pages=new Dictionary<string,Control>();
 readonly Dictionary<string,HomeMatch> detailMatches=new Dictionary<string,HomeMatch>();
 Button backPage,forwardPage;string pageAccount="";bool allowClose;
 void InitializePages(){
  Controls.Add(pageHost);Controls.Add(pageTrail);pageHost.BackColor=pageTrail.BackColor=Theme.Background;
  backPage=PageControls.Button(pageTrail,"←",0,0,42,()=>NavigatePage(pageHistory.Back(),false));backPage.AccessibleName="Back";
  forwardPage=PageControls.Button(pageTrail,"→",48,0,42,()=>NavigatePage(pageHistory.Forward(),false));forwardPage.AccessibleName="Forward";
  PageControls.Button(pageTrail,"Overview",110,0,126,()=>NavigatePage("overview"));
  PageControls.Button(pageTrail,"Champions",248,0,150,()=>NavigatePage("champions"));
  PageControls.Button(pageTrail,"LP history",410,0,126,()=>NavigatePage("rank"));
  MouseClick+=(s,e)=>{if(e.Button!=MouseButtons.Left||!ShowHome)return;var matches=HomeData.Filter(CurrentHome,matchSearch.Text,SelectedQueue,SelectedRange);for(int i=0;i<HomeDashboard.VisibleMatches(HomeBounds)&&i+homeOffset<matches.Count;i++){var bounds=HomeDashboard.RowBounds(HomeBounds,i);bounds.Offset(0,ShellHeight);if(!bounds.Contains(e.Location))continue;OpenMatch(matches[i+homeOffset]);break;}};
  FormClosing+=(s,e)=>{ReviewPage page;if(!allowClose&&pages.ContainsKey("review")&&(page=pages["review"] as ReviewPage)!=null&&page.Dirty){e.Cancel=true;NavigatePage("review");page.ConfirmLeave(()=>{allowClose=true;Close();},"Save or discard your reflection before closing Rift Ready.");}};
 }
 void OpenMatch(HomeMatch match){
  string key=detailMatches.Where(p=>p.Value==match||(match.GameId>0&&p.Value.GameId==match.GameId)||(match.Played.HasValue&&p.Value.Played==match.Played&&p.Value.Champion==match.Champion&&p.Value.Queue==match.Queue)).Select(p=>p.Key).FirstOrDefault();
  if(key==null)key="detail:"+Guid.NewGuid().ToString("N");
  // Refresh a reused record without retaining a second native control tree.
  Control old;if(detailMatches.ContainsKey(key)&&!ReferenceEquals(detailMatches[key],match)&&pages.TryGetValue(key,out old)){old.Dispose();pages.Remove(key);}
  detailMatches[key]=match;NavigatePage(key);
 }
 void LayoutPages(){if(backPage==null)return;pageTrail.SetBounds(40,ShellHeight+(ShowHome?182:8),Math.Max(540,ClientSize.Width-80),38);pageTrail.Visible=true;pageHost.SetBounds(16,ShellHeight+58,ClientSize.Width-32,ContentHeight-74);}
 void NavigatePage(string route,bool record=true){
  if(route=="updates"){Updates();return;}
  if(pageAccount!=ContextAccount)RefreshPageContext();
  if(record)pageHistory.Navigate(route);selectedView=route;
  if(route=="overview"||route=="match")pageHost.Visible=false;
  else {
   Control page;if(!pages.TryGetValue(route,out page)){
    if(route=="champions")page=new ChampionsPage(data,Portrait,key=>NavigatePage("champion:"+key));
    else if(route=="rank")page=new RankHistoryPage();
    else if(route=="matchups")page=new LessonsPage(data);
    else if(route=="review")page=new ReviewPage(home,ReviewChampion,prefs.TrainingFocus);
    else if(route.StartsWith("champion:")){string key=route.Substring(9);page=new ChampionPage(data,key,CurrentHome,Portrait,()=>{historyQueue.SelectedIndex=0;matchSearch.Text=key;NavigatePage("overview");});}
    else if(route.StartsWith("detail:")&&detailMatches.ContainsKey(route)){var match=detailMatches[route];page=new MatchDetailPage(data,match,()=>NavigatePage("champion:"+match.Champion));}
    else {NavigatePage("overview");return;}
    var userPage=page as UserControl;if(userPage!=null)userPage.AutoScaleMode=AutoScaleMode.None;page.Font=new Font("Segoe UI",11);page.Dock=DockStyle.Fill;page.Visible=false;pages.Add(route,page);pageHost.Controls.Add(page);
   }
   foreach(Control child in pageHost.Controls)child.Visible=ReferenceEquals(child,page);page.BringToFront();pageHost.Visible=true;
  }
  foreach(string key in pages.Keys.Where(k=>(k.StartsWith("detail:")||k.StartsWith("champion:"))&&!pageHistory.Contains(k)&&k!=route).ToArray()){pages[key].Dispose();pages.Remove(key);detailMatches.Remove(key);}
  backPage.Enabled=pageHistory.CanBack;forwardPage.Enabled=pageHistory.CanForward;
  foreach(var pair in new[]{Tuple.Create(dashboardButton,"overview"),Tuple.Create(championsButton,"champions"),Tuple.Create(playbook,"matchups"),Tuple.Create(review,"review"),Tuple.Create(liveGameButton,"match"),Tuple.Create(updatesButton,"updates")}){((NavigationButton)pair.Item1).Active=route==pair.Item2||(pair.Item2=="champions"&&route.StartsWith("champion:"));pair.Item1.Invalidate();}
  RefreshPageContext();LayoutPages();LayoutHistoryScroll();Invalidate();
 }
 string ContextAccount{get{return state.Account==""&&Postgame.Active(state.Phase)?pageAccount:state.Account;}}
 string ReviewChampion{get{var context=state.Players.Count>0?state:lastGame??state;var self=context.Players.FirstOrDefault(p=>p.Self);var recent=HomeData.Filter(CurrentHome,"",0,1).FirstOrDefault();return self!=null?data.Name(self.Champion):recent==null?"":data.Name(recent.Champion);}}
 void RefreshPageContext(){
  if(backPage==null)return;
  // Account-bound cached pages must not leak into the next user's session. Review notes
  // are intentionally PC-local and retained, including unsaved edits.
  if(pageAccount!=ContextAccount){pageAccount=ContextAccount;if(lastGame!=null&&lastGame.Account!=pageAccount)lastGame=null;foreach(string key in pages.Keys.Where(k=>k=="champions"||k=="rank"||k=="matchups"||k.StartsWith("champion:")||k.StartsWith("detail:")).ToArray()){pages[key].Dispose();pages.Remove(key);}detailMatches.Clear();pageHistory.Reset();if(selectedView=="champions"||selectedView=="rank"||selectedView=="matchups"||selectedView.StartsWith("champion:")||selectedView.StartsWith("detail:")){NavigatePage("overview");return;}}
  Control page;if(pages.TryGetValue(selectedView,out page)){
   var champions=page as ChampionsPage;if(champions!=null)champions.UpdateProfile(CurrentHome);
   var champion=page as ChampionPage;if(champion!=null)champion.UpdateProfile(CurrentHome);
   var rank=page as RankHistoryPage;if(rank!=null)rank.UpdateProfile(CurrentHome);
   var lessons=page as LessonsPage;if(lessons!=null)lessons.UpdateContext(state.Players.Count>0?state:lastGame??state);
   var reflection=page as ReviewPage;if(reflection!=null){reflection.UpdateContext(ReviewChampion,prefs.TrainingFocus);reflection.SetEditable(CanReview());}
  }
  review.Enabled=CanReview();backPage.Enabled=pageHistory.CanBack;forwardPage.Enabled=pageHistory.CanForward;
 }
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==(Keys.Control|Keys.F)&&ShowHome){matchSearch.FocusInput();return true;}if(keyData==(Keys.Alt|Keys.Left)&&pageHistory.CanBack){NavigatePage(pageHistory.Back(),false);if(ShowHome)homeOverview.Focus();return true;}if(keyData==(Keys.Alt|Keys.Right)&&pageHistory.CanForward){NavigatePage(pageHistory.Forward(),false);if(ShowHome)homeOverview.Focus();return true;}return base.ProcessCmdKey(ref msg,keyData);}
}
}
