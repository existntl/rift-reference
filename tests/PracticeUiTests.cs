using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using RiftReference;

class PracticeUiTests {
    static string home=AppDomain.CurrentDomain.BaseDirectory;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);}
    static void Capture(Form form,string name){using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(home,name+".png"));}}
    static object Get(Dashboard form,string field){return typeof(Dashboard).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);}
    static void Set(Dashboard form,string field,object value){typeof(Dashboard).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,value);}
    static System.Collections.Generic.IEnumerable<T> Desc<T>(Control root) where T:Control {foreach(Control child in root.Controls){var value=child as T;if(value!=null)yield return value;foreach(var nested in Desc<T>(child))yield return nested;}}
    static void CheckPreferencesHeader(Form form){
        var dialog=(PreferencesDialog)form;var original=form.ClientSize;
        foreach(int width in new[]{1040,980}){
            form.ClientSize=new Size(width,900);
            for(int page=0;page<3;page++){
                dialog.SelectSection(page);Application.DoEvents();
                var save=form.Controls.OfType<Button>().Single(b=>b.Name=="SavePreferences");
                var cancel=form.Controls.OfType<Button>().Single(b=>b.Name=="CancelPreferences");
                Check(save.Right<=form.ClientSize.Width-24&&cancel.Right+12<=save.Left,"Preferences action buttons overlap or leave the window");
                foreach(var button in new[]{save,cancel}){
                    foreach(var label in form.Controls.OfType<Label>())Check(!label.Bounds.IntersectsWith(button.Bounds),"Preferences header label covers "+button.Text);
                    foreach(var point in new[]{new Point(button.Left+2,button.Top+2),new Point(button.Right-3,button.Bottom-3),new Point(button.Left+button.Width/2,button.Top+button.Height/2)})Check(form.GetChildAtPoint(point)==button,"Preferences action is clipped or not clickable: "+button.Text);
                    Check(TextRenderer.MeasureText(button.Text,button.Font).Width+12<button.Width,"Preferences action text does not fit");
                }
                Capture(form,"preferences-header-"+width+"-"+page);
            }
        }
        form.ClientSize=original;dialog.SelectSection(0);
    }
    static void Modal(Action open,string title,Action<Form> operate){
        Exception failure=null;int ticks=0;
        using(var timer=new Timer{Interval=100}){
            timer.Tick+=(s,e)=>{
                var form=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Text.Contains(title));
                if(form==null){if(++ticks>50){timer.Stop();failure=new Exception("Modal did not open: "+title);Application.ExitThread();}return;}
                timer.Stop();form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);
                try{operate(form);}catch(Exception ex){failure=ex;}finally{form.Close();}
            };
            timer.Start();open();
        }
        if(failure!=null)throw failure;
    }
    [STAThread]static int Main(){try{
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        File.WriteAllText(Path.Combine(home,"preferences.json"),"{\"SettingsVersion\":1,\"EnemiesLeft\":false,\"AudioVolume\":37,\"Focus\":\"Positioning\"}");
        var savedOverlay="{\"Enabled\":true,\"GameBar\":true,\"Stats\":true}";File.WriteAllText(Path.Combine(home,"overlay.json"),savedOverlay);
        using(var dashboard=new Dashboard(home,true)){
            dashboard.Render(Path.Combine(home,"migrated.png"),false);
            Modal(()=>dashboard.OpenPreferences(),"Preferences",form=>{
                CheckPreferencesHeader(form);
                Check(!Desc<TabControl>(form).Any(),"Native settings tabs remain");
                Check(Desc<CheckBox>(form).Any()&&Desc<CheckBox>(form).All(c=>c is RiftToggle),"Preferences contain an unthemed checkbox");
                var categories=Desc<NavigationButton>(form).Select(b=>b.Text).ToArray();
                Check(categories.SequenceEqual(new[]{"General","Audio & reminders","Phone / tablet"}),"Retired overlay category remains in preferences");
                var general=Desc<Panel>(form).Single(p=>p.Name=="GeneralPage");
                var focus=Desc<RiftComboBox>(general).Single(c=>c.Name=="TrainingFocus");
                var format=Desc<RiftComboBox>(general).Single(c=>c.Name=="CooldownFormat");
                Check(format.Items.Cast<object>().Select(Convert.ToString).SequenceEqual(new[]{"Minutes and seconds","Seconds only"}),"Cooldown option wording changed");
                format.SelectedIndex=1;
                Check(Convert.ToString(focus.SelectedItem)=="Main threat","Settings did not show migrated focus");
                focus.SelectedItem="Recall purpose";
                Desc<CheckBox>(general).Single(c=>c.Text.StartsWith("Show lane plan")).Checked=false;
                Check(!Desc<OverlayPreferencesPage>(form).Any(),"Retired overlay controls were constructed");
                form.Controls.OfType<Button>().Single(b=>b.Name=="SavePreferences").PerformClick();
            });
            var prefs=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(Path.Combine(home,"preferences.json")));
            Check(prefs.TrainingFocus=="Recall purpose"&&!prefs.ShowCoaching&&!prefs.EnemiesLeft&&prefs.AudioVolume==37&&!prefs.MinutesAndSeconds,"Preferences save lost values");
            Check(File.ReadAllText(Path.Combine(home,"overlay.json"))==savedOverlay,"Save modified retired overlay settings");
            Modal(()=>dashboard.OpenPreferences(null,1),"Preferences",form=>{
                var enabled=Desc<CheckBox>(form).Single(c=>c.Text=="Enable audio during live games");
                enabled.Checked=!enabled.Checked;
                form.Controls.OfType<Button>().Single(b=>b.Name=="CancelPreferences").PerformClick();
            });
            Check(File.ReadAllText(Path.Combine(home,"overlay.json"))==savedOverlay,"Cancel changed saved overlay preferences");
            dashboard.Render(Path.Combine(home,"original-cards.png"),false);
            dashboard.Show();
            var mainNavigation=dashboard.Controls.OfType<NavigationButton>().Select(b=>b.Text).ToArray();
            Check(mainNavigation.Contains("Preferences")&&mainNavigation.Contains("Updates")&&!mainNavigation.Contains("Phone / tablet")&&!mainNavigation.Contains("Game overlay"),"Main navigation did not consolidate settings");
            Modal(()=>dashboard.OpenPreferences(null,2),"Preferences",form=>{
                Check(Desc<Panel>(form).Single(p=>p.Name=="PhonePage").Visible,"Phone preferences did not open directly");
            });
            Modal(()=>dashboard.Controls.OfType<Button>().Single(b=>b.Text=="Updates").PerformClick(),"Updates",form=>{var popup=(UpdatesDialog)form;Check(popup.TopLevel&&popup.Modal&&!popup.IsPage,"Updates did not open as a popup");Check(Desc<CheckBox>(popup).All(c=>c is RiftToggle),"Updates contain an unthemed checkbox");Desc<CheckBox>(popup).Single(c=>c.Text.StartsWith("Automatically check")).Checked=false;});
            dashboard.Controls.OfType<Button>().Single(b=>b.Text=="Live game").PerformClick();
            prefs=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(Path.Combine(home,"preferences.json")));
            Check(!prefs.AutoUpdates,"Update preference did not save from the separate Updates area");
            ((Preferences)Get(dashboard,"prefs")).ShowCoaching=true;
            ((Preferences)Get(dashboard,"prefs")).EnemiesLeft=true;
            var demo=dashboard.Demo(false);demo.Players[4].Champion="Thresh";demo.Players[8].Champion="Ashe";Set(dashboard,"state",demo);Capture(dashboard,"pattern-guide");
            dashboard.Size=new Size(1720,980);Capture(dashboard,"default-window");dashboard.Size=new Size(1920,1040);
            demo.Players[9].Role="";Capture(dashboard,"incomplete-lane");
            demo=dashboard.Demo(false);demo.Players[3].Self=false;demo.Players[0].Self=true;Set(dashboard,"state",demo);Capture(dashboard,"solo-lane");
            var post=new Snapshot{Phase="EndOfGame"};Set(dashboard,"state",post);Capture(dashboard,"postgame");
            Check(dashboard.Controls.OfType<Button>().Single(b=>b.Text=="Review").Enabled,"Postgame review unavailable");
            Set(dashboard,"state",dashboard.Demo(false));Capture(dashboard,"live-review-disabled");
            Check(!dashboard.Controls.OfType<Button>().Single(b=>b.Text=="Review").Enabled,"Review available in a live game");
            dashboard.Hide();
            var data=new DataStore(Path.Combine(home,"data"));
            Modal(()=>PracticeWindows.Playbook(dashboard,data,dashboard.Demo(false)),"Playbook",form=>{
                var list=form.Controls.OfType<ListBox>().Single();var body=form.Controls.OfType<TextBox>().Single();
                for(int i=0;i<list.Items.Count;i++){list.SelectedIndex=i;Check(body.Text.Length>100,"Empty lesson");Capture(form,"lesson-"+i);}
            });
            Modal(()=>PracticeWindows.Review(dashboard,home,"Vayne","Main threat"),"Review",form=>{
                var boxes=form.Controls.OfType<TextBox>().OrderBy(c=>c.Top).ToArray();
                boxes[0].Text="Stayed for another wave without a purchase plan.";boxes[1].Text="Won a trade but delayed my reset.";boxes[2].Text="Name what another wave buys before staying.";
                form.Controls.OfType<RiftComboBox>().Single().SelectedItem="Partly";
                form.Controls.OfType<Button>().Single(b=>b.Text=="Save reflection").PerformClick();
                Check(ReflectionStore.Read(home).Count==1,"Review Save did not persist");Capture(form,"saved-review");
            });
            Modal(()=>PracticeWindows.Review(dashboard,home,"Ashe","Recall purpose"),"Review",form=>{
                form.Controls.OfType<ListBox>().Single().SelectedIndex=0;
                var boxes=form.Controls.OfType<TextBox>().OrderBy(c=>c.Top).ToArray();
                Check(boxes[2].Text.Contains("another wave"),"Saved review did not reopen");
                boxes[2].Text="Decide my recall purpose before staying.";
                form.Controls.OfType<Button>().Single(b=>b.Text=="Save reflection").PerformClick();
                var entry=ReflectionStore.Read(home).Single();Check(entry.Champion=="Vayne"&&entry.Focus=="Main threat"&&entry.NextGame.StartsWith("Decide"),"Edit changed review context or duplicated entry");
            });
        }
        using(var reopened=new Dashboard(home,true)){var prefs=(Preferences)Get(reopened,"prefs");Check(prefs.TrainingFocus=="Recall purpose"&&!prefs.ShowCoaching&&!prefs.MinutesAndSeconds,"Preferences did not survive reopen");}
        Console.WriteLine("PASS: preferences save/reopen, original-card option, alternate/partial/solo layouts, all playbook pages, postgame availability, reflection save/reopen/edit.");return 0;
    }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
