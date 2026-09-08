using System;
using RiftReady.GameBar;
class LiveFrameTests
{
    const string Geometry = ",\"purchaseWidth\":300,\"purchaseHeight\":90,\"statsWidth\":220,\"statsHeight\":140,\"buffsWidth\":260,\"buffsHeight\":90" + ",\"viewportX\":0,\"viewportY\":0,\"viewportWidth\":1920,\"viewportHeight\":1080,\"boardX\":740,\"boardY\":100,\"boardWidth\":440,\"boardHeight\":578,\"rowStart\":344,\"rowGap\":75,\"purchaseX\":1400,\"purchaseY\":650,\"statsX\":20,\"statsY\":200,\"buffsX\":20,\"buffsY\":100,\"purchaseOpacity\":1,\"statsOpacity\":1,\"buffsOpacity\":1";
    const string Frame = "{\"version\":1,\"fresh\":true,\"visible\":true,\"goldVisible\":true,\"alliesLeft\":true,\"purchaseVisible\":false,\"statsVisible\":false,\"matchSeconds\":120,\"allyTotal\":1800,\"enemyTotal\":null,\"differences\":[0,20,-10,null,null],\"targetName\":null,\"neededGold\":null,\"owned\":null,\"stats\":[],\"buffs\":[]" + Geometry + "}";
    static int checks;
    static void Check(bool pass, string name) { checks++; if (!pass) throw new Exception(name); }
    static void Reject(string json, string name)
    {
        bool rejected = false;
        try { LiveFrame.Parse(json); }
        catch (ArgumentException) { rejected = true; }
        catch (System.Runtime.InteropServices.COMException) { rejected = true; }
        Check(rejected, name);
    }
    static int Main()
    {
        var f = LiveFrame.Parse(Frame);
        Check(f.Fresh && f.Visible && f.Gold && f.Differences.Length == 5, "valid display frame");
        Check(f.EnemyTotal == null && f.Differences[3] == null && f.Owned == null, "unknown values preserved");
        Check(f.Differences[2] == -10, "signed difference preserved");
        Check(f.HasGeometry && f.RowStart == 344 && f.RowStart + 4 * f.RowGap == 644 && f.BoardWidth == 440, "calibrated row geometry preserved");
        Check(f.LocalX(960, 100, 1) == 860 && f.LocalY(344, 20, 1) == 324, "clamped canvas keeps physical coordinates");
        Check(f.LocalY(344, 20, 2) == 152 && f.LocalY(644, 20, 2) - f.LocalY(344, 20, 2) == 150, "DPI conversion preserves physical row spacing");
        Check(LiveFrame.Parse(Frame.Replace("\"viewportX\":0", "\"viewportX\":-1920")).ViewportX == -1920, "negative monitor origin");
        Reject(Frame.Replace("\"viewportWidth\":1920", "\"viewportWidth\":0"), "partial viewport");
        Reject(Frame.Replace("\"rowGap\":75", "\"rowGap\":1000"), "rows outside viewport");
        Reject(Frame.Replace("\"boardWidth\":440", "\"boardWidth\":1500"), "board outside viewport");
        Reject(Frame.Replace("\"purchaseOpacity\":1", "\"purchaseOpacity\":1.1"), "opacity bounds");
        var stale = LiveFrame.Parse(Frame.Replace("\"fresh\":true", "\"fresh\":false").Replace("\"visible\":true", "\"visible\":false"));
        Check(!stale.Fresh && !stale.Visible, "stale hidden frame accepted");
        Reject(Frame.Replace("\"fresh\":true", "\"fresh\":false"), "visible stale frame");
        Reject("{", "malformed JSON"); Reject(null, "disconnect");
        Reject(Frame.Replace("\"stats\":[]", "\"stats\":false"), "wrong array type");
        Reject(Frame.Replace("\"stats\":[],", ""), "missing array");
        Reject(Frame.Replace("\"stats\":[]", "\"stats\":[null]"), "wrong row type");
        Reject(Frame.Replace("\"version\":1", "\"version\":\"one\""), "wrong version type");
        Reject(Frame.Replace("\"fresh\":true,", ""), "missing required flag");
        Reject(new string(' ', 8193), "byte limit");
        Reject(Frame.Replace("\"version\":1", "\"version\":2"), "unsupported version");
        Reject(Frame.Replace("\"goldVisible\":true", "\"goldVisible\":1"), "wrong flag type");
        Reject(Frame.Replace("[0,20,-10,null,null]", "[0]"), "five differences required");
        Reject(Frame.Replace("\"allyTotal\":1800", "\"allyTotal\":-1"), "negative total rejected");
        Reject(Frame.Replace("\"allyTotal\":1800", "\"allyTotal\":1000000001"), "large total rejected");
        Reject(Frame.Replace("\"owned\":null", "\"owned\":\"yes\""), "wrong owned type");
        Reject(Frame.Replace("\"targetName\":null", "\"targetName\":\"line\\nfeed\""), "control text rejected");
        Reject(Frame.Replace("\"targetName\":null", "\"targetName\":\"" + new string('x',129) + "\""), "long text rejected");
        Reject(Frame.Replace("\"buffs\":[]", "\"buffs\":[{\"name\":\"Elder\",\"remainingSeconds\":151}]"), "buff duration bounded");
        Reject(Frame.Replace("\"buffs\":[]", "\"buffs\":[{\"name\":\"Baron\",\"remainingSeconds\":10},{\"name\":\"Baron\",\"remainingSeconds\":10}]"), "duplicate buff rejected");
        Reject(Frame.Replace("\"purchaseVisible\":false", "\"purchaseVisible\":true"), "purchase target required");
        Reject(Frame.Replace("\"statsVisible\":false", "\"statsVisible\":true"), "visible stats rows required");
        var buff = LiveFrame.Parse(Frame.Replace("\"buffs\":[]", "\"buffs\":[{\"name\":\"Baron\",\"remainingSeconds\":180}]"));
        Check(buff.Buffs.Length == 1 && buff.Buffs[0].Seconds == 180, "valid buff");
        Console.WriteLine(checks + " LiveFrame checks passed."); return 0;
    }
}
