using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace RiftReference {
public sealed class Reflection {
    public string Id = Guid.NewGuid().ToString("N");
    public string SavedUtc = "", Champion = "", Focus = "", Influence = "Not assessed";
    public string Disadvantage = "", Conversion = "", NextGame = "";
    public override string ToString() {
        DateTime time;
        string when = DateTime.TryParse(SavedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out time) ? time.ToLocalTime().ToString("MMM d, HH:mm") : "Undated";
        return when + " · " + (Champion == "" ? "Reflection" : Champion);
    }
}
public static class ReflectionStore {
    public static List<Reflection> Read(string root) {
        string path = Path.Combine(root, "reviews.json");
        if (!File.Exists(path)) return new List<Reflection>();
        if (new FileInfo(path).Length > 8000000) throw new IOException("Review file is too large. Keep a backup before starting a new file.");
        var entries = new JavaScriptSerializer { MaxJsonLength = 8000000 }.Deserialize<List<Reflection>>(File.ReadAllText(path, Encoding.UTF8));
        if (entries == null || entries.Any(r => r == null || String.IsNullOrEmpty(r.Id)) || entries.Select(r => r.Id).Distinct().Count() != entries.Count)
            throw new IOException("Review file is invalid. It has been kept unchanged.");
        return entries;
    }
    public static void Save(string root, Reflection reflection) {
        var entries = Read(root); // Never overwrite unreadable prior notes.
        if (String.IsNullOrEmpty(reflection.Id)) throw new IOException("Review ID is missing.");
        int index = entries.FindIndex(r => r.Id == reflection.Id);
        if (index < 0) entries.Add(reflection); else entries[index] = reflection;
        string json = new JavaScriptSerializer { MaxJsonLength = 8000000 }.Serialize(entries);
        if (Encoding.UTF8.GetByteCount(json) > 8000000) throw new IOException("Review storage is full. Keep a backup before starting a new file.");
        string path = Path.Combine(root, "reviews.json"), temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public static class PracticeWindows {
    static readonly Color Background = Theme.Background, Surface = Theme.Panel, Ink = Theme.Ink, Accent = Theme.Accent;
    static Form Window(string title) {
        return new Form { Text = title, Size = new Size(1120,810), MinimumSize = new Size(1120,810), StartPosition = FormStartPosition.CenterParent, BackColor = Background, ForeColor = Ink, AutoScaleMode = AutoScaleMode.None, Font = new Font("Segoe UI",11) };
    }
    static Label Label(string text, int x, int y, int w, int h) {
        return new Label { Text=text, Left=x, Top=y, Width=w, Height=h, ForeColor=Ink };
    }
    public static void Playbook(Form owner, DataStore data, Snapshot state, string renderPath = null) {
        using (var form = Window("Rift Ready · Playbook")) {
            var pages = Coaching.Lessons(data,state);
            var list = new ListBox { Left=20, Top=66, Width=225, Height=660, BackColor=Surface, ForeColor=Ink, BorderStyle=BorderStyle.None, ItemHeight=34, IntegralHeight=false, Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Bottom };
            list.Items.AddRange(pages.Select(p => (object)p.Title).ToArray());
            var body = new TextBox { Left=265, Top=66, Width=815, Height=660, Multiline=true,ScrollBars=ScrollBars.Vertical,ReadOnly=true, BackColor=Surface, ForeColor=Ink, BorderStyle=BorderStyle.None, Font=new Font("Segoe UI",12), Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right };
            list.SelectedIndexChanged += (s,e) => { if (list.SelectedIndex >= 0) { body.Text=pages[list.SelectedIndex].Body.Replace("\n",Environment.NewLine); body.SelectionStart=0; body.ScrollToCaret(); } };
            form.Controls.AddRange(new Control[] { Label("PLAYBOOK · Conditional lessons / " + (state.Demo ? "demo lineup" : "current or last observed lineup"),20,22,1050,32), list, body });
            list.SelectedIndex=0;
            Show(form,owner,renderPath);
        }
    }
    public static void Review(Form owner, string home, string champion, string focus, string renderPath = null) {
        using (var form = Window("Rift Ready · Review")) {
            var list = new ListBox { Left=20, Top=100, Width=245, Height=565, BackColor=Surface, ForeColor=Ink, IntegralHeight=false, Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left };
            var heading=Label("New reflection",290,65,770,28);
            var focusLabel=Label("",290,96,770,25);
            var questions = new[] { "1. What decision created your first major disadvantage?", "2. What advantage did you fail to convert?", "3. What will you do differently next game?" };
            var answers = new TextBox[3];
            for (int i=0;i<3;i++) {
                form.Controls.Add(Label(questions[i],290,138+i*139,770,30));
                answers[i]=new TextBox { Left=290,Top=172+i*139,Width=770,Height=92,Multiline=true,MaxLength=2000,ScrollBars=ScrollBars.Vertical,BackColor=Surface,ForeColor=Ink,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right };
                form.Controls.Add(answers[i]);
            }
            var influence = new ComboBox { Left=595,Top=565,Width=250,DropDownStyle=ComboBoxStyle.DropDownList };
            influence.Items.AddRange(new object[] { "Not assessed", "Yes", "Partly", "Not yet" });
            var save = new Button { Text="Save reflection",Left=865,Top=625,Width=195,Height=36 };
            var fresh = new Button { Text="New reflection",Left=20,Top=65,Width=245,Height=30 };
            var status = Label("Notes stay on this PC. Self-assessment; no causes are inferred.",290,680,775,65);
            Reflection active=null; bool loading=false, dirty=false;
            Action<Reflection> load = r => {
                loading=true; active=r;
                heading.Text=r.SavedUtc==""?"New reflection · "+(r.Champion==""?"no champion selected":r.Champion):r.ToString();
                focusLabel.Text="Focus: "+r.Focus;
                answers[0].Text=r.Disadvantage; answers[1].Text=r.Conversion; answers[2].Text=r.NextGame;
                influence.SelectedItem=r.Influence; if(influence.SelectedIndex<0)influence.SelectedIndex=0;
                dirty=false; loading=false;
            };
            Func<bool> discard = () => !dirty || MessageBox.Show(form,"Discard your unsaved reflection edits?","Unsaved reflection",MessageBoxButtons.YesNo)==DialogResult.Yes;
            foreach (var answer in answers) answer.TextChanged += (s,e) => {if(!loading)dirty=true;};
            influence.SelectedIndexChanged += (s,e) => {if(!loading)dirty=true;};
            list.SelectedIndexChanged += (s,e) => {
                if(loading || list.SelectedItem==null)return;
                if(!discard()){loading=true;list.SelectedItem=list.Items.Cast<Reflection>().FirstOrDefault(r=>r.Id==active.Id);loading=false;return;}
                load((Reflection)list.SelectedItem);
            };
            fresh.Click += (s,e) => { if(!discard())return;loading=true;list.SelectedIndex=-1;loading=false;load(new Reflection {Champion=champion,Focus=Coaching.NormalizeFocus(focus)}); };
            save.Click += (s,e) => {
                if(answers.All(a=>String.IsNullOrWhiteSpace(a.Text))){status.Text="Write at least one answer before saving.";return;}
                var next = new Reflection { Id=active.Id,SavedUtc=DateTime.UtcNow.ToString("o"),Champion=active.Champion,Focus=active.Focus,Influence=Convert.ToString(influence.SelectedItem),Disadvantage=answers[0].Text.Trim(),Conversion=answers[1].Text.Trim(),NextGame=answers[2].Text.Trim() };
                try {
                    ReflectionStore.Save(home,next);
                    loading=true;list.Items.Clear();list.Items.AddRange(ReflectionStore.Read(home).AsEnumerable().Reverse().Cast<object>().ToArray());list.SelectedItem=list.Items.Cast<Reflection>().First(r=>r.Id==next.Id);loading=false;
                    load(next); status.Text="Saved on this PC. Select a previous reflection to revisit or edit it.";
                } catch(Exception ex) {loading=false;status.Text="Could not save: "+ex.Message;}
            };
            form.FormClosing += (s,e) => {if(renderPath==null&&!discard())e.Cancel=true;};
            form.Controls.AddRange(new Control[] { Label("REVIEW · Your decisions, in your words",20,22,1040,32),list,heading,focusLabel,fresh,Label("Did the focus influence a decision?",290,568,300,28),influence,save,status });
            try {list.Items.AddRange(ReflectionStore.Read(home).AsEnumerable().Reverse().Cast<object>().ToArray());}
            catch(Exception ex){status.Text="Existing notes could not be read and will not be overwritten: "+ex.Message;}
            load(new Reflection {Champion=champion,Focus=Coaching.NormalizeFocus(focus)});
            Show(form,owner,renderPath);
        }
    }
    static void Show(Form form, Form owner, string renderPath) {
        Theme.Apply(form);
        Timer timer=null;
        if(renderPath!=null){
            form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);
            timer=new Timer {Interval=200};timer.Tick+=(s,e)=>{timer.Stop();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(renderPath);}form.Close();};timer.Start();
        }
        try {form.ShowDialog(owner);} finally {if(timer!=null)timer.Dispose();}
    }
}
}
