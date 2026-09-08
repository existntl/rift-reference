using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using RiftReference;
class LayoutTest {
    static Rectangle Header(Dashboard form,string name){return (Rectangle)typeof(Dashboard).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);}
    static void Mouse(Dashboard form,string method,int x,int y){typeof(Dashboard).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
    [STAThread]static void Main(){
        string home=AppDomain.CurrentDomain.BaseDirectory;
        File.WriteAllText(Path.Combine(home,"preferences.json"),"{\"RespawnLaneOnly\":true,\"AudioVolume\":37}");
        using(var form=new Dashboard(home,true)){
            form.Render(Path.Combine(home,"layout-test.png"),false);
            var json=new JavaScriptSerializer();var prefs=json.Deserialize<Preferences>(File.ReadAllText(Path.Combine(home,"preferences.json")));
            if(prefs.RespawnLaneOnly||!prefs.EnemiesLeft||prefs.AudioVolume!=37)throw new Exception("Migration failed");
            var left=Header(form,"leftHeader");var right=Header(form,"rightHeader");
            if(left.IsEmpty||right.IsEmpty||left.IntersectsWith(right))throw new Exception("Lane headers missing or overlapping");
            // Use displayed bounds, which change with the navigation layout.
            Mouse(form,"OnMouseDown",left.X+20,left.Y+8);Mouse(form,"OnMouseUp",right.X+20,right.Y+8);
            prefs=json.Deserialize<Preferences>(File.ReadAllText(Path.Combine(home,"preferences.json")));
            if(prefs.EnemiesLeft)throw new Exception("Drag not saved");
            Mouse(form,"OnMouseDown",50,left.Y+8);Mouse(form,"OnMouseUp",right.X+20,right.Y+8);
            prefs=json.Deserialize<Preferences>(File.ReadAllText(Path.Combine(home,"preferences.json")));
            if(prefs.EnemiesLeft)throw new Exception("Navigation drag changed teams");
            form.Render(Path.Combine(home,"draft-layout-test.png"),true);
            left=Header(form,"leftHeader");right=Header(form,"rightHeader");
            if(left.IsEmpty||right.IsEmpty)throw new Exception("Draft headers missing");
            Mouse(form,"OnMouseDown",left.X+20,left.Y+8);Mouse(form,"OnMouseUp",right.X+20,right.Y+8);
            prefs=json.Deserialize<Preferences>(File.ReadAllText(Path.Combine(home,"preferences.json")));
            if(!prefs.EnemiesLeft)throw new Exception("Draft drag not saved");
        }
        Console.WriteLine("PASS: preference migration, header drag persistence and navigation hit-test separation.");
    }
}
