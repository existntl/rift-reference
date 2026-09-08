using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace RiftReady.GameBar
{
    public sealed partial class WidgetPage : Page, IDisposable
    {
        private XboxGameBarWidget widget;
        private readonly CoreDispatcher uiDispatcher;
        private readonly CoreWindow ownerCoreWindow;
        private readonly DisplayInformation ownerDisplay;
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        private readonly Stopwatch receiveAge = new Stopwatch();
        private bool disposed, paused, resizing, resizeAccepted;
        private LiveFrame current;
        private string lastJson;
        private string lastDiagnostic;
        private long lastDiagnosticTick;
        private double requestedWidth, requestedHeight, pixelsPerDip = 1;
        public WidgetPage() { ownerCoreWindow = Window.Current.CoreWindow; uiDispatcher = ownerCoreWindow.Dispatcher; ownerDisplay = DisplayInformation.GetForCurrentView(); InitializeComponent(); timer.Tick += Tick; SizeChanged += PageSizeChanged; }
        private Rect ContentBounds { get { return widget != null ? widget.WindowBounds : ownerCoreWindow.Bounds; } }
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            widget = args.Parameter as XboxGameBarWidget;
            if (widget == null) return;
            widget.RequestedOpacityChanged += StateChanged;
            widget.PinnedChanged += StateChanged;
            widget.VisibleChanged += StateChanged;
            widget.GameBarDisplayModeChanged += DisplayModeChanged;
            widget.WindowBoundsChanged += StateChanged;
            ApplyState();
        }
        public async Task ApplyLiveJsonAsync(string json)
        {
            LiveFrame frame = null;
            try { frame = LiveFrame.Parse(json); } catch (ArgumentException) { } catch (System.Runtime.InteropServices.COMException) { }
            await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (disposed) return;
                bool same = current != null && frame != null && json == lastJson;
                current = frame; lastJson = json;
                if (frame != null && frame.Fresh) receiveAge.Restart(); else receiveAge.Reset();
                if (frame != null && frame.HasGeometry) EnsureViewport(frame, false);
                if (!same) UpdateContent();
            });
        }
        private bool FitsViewport { get { return resizeAccepted && Math.Abs(ActualWidth - requestedWidth) <= 4 && Math.Abs(ActualHeight - requestedHeight) <= 4; } }
        private async void EnsureViewport(LiveFrame frame, bool retry)
        {
            if (disposed || widget == null || !frame.HasGeometry || resizing) return;
            double density = ownerDisplay.RawPixelsPerViewPixel;
            if (density <= 0 || Double.IsNaN(density)) density = 1;
            double width = frame.ViewportWidth / density, height = frame.ViewportHeight / density;
            if (!retry && requestedWidth == width && requestedHeight == height) return;
            requestedWidth = width; requestedHeight = height; pixelsPerDip = density; resizing = true; resizeAccepted = false;
            try
            {
                widget.MaxWindowSize = new Size(Math.Max(width, widget.MinWindowSize.Width), Math.Max(height, widget.MinWindowSize.Height));
                resizeAccepted = await ResizeOnUiAsync(width, height);
                if (!resizeAccepted)
                {
                    await Task.Delay(1000);
                    await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (!disposed && widget != null)
                            resizeAccepted = Math.Abs(ActualWidth - width) <= 4 && Math.Abs(ActualHeight - height) <= 4 && Math.Abs(widget.WindowBounds.Width - width) <= 4 && Math.Abs(widget.WindowBounds.Height - height) <= 4;
                    });
                }
                // Game Bar reserves space for its toolbar and screen edges. A bounded
                // smaller host clips panels; it never scales their game coordinates.
                if (!resizeAccepted) resizeAccepted = await ResizeOnUiAsync(Math.Max(360, width - 40), Math.Max(214, height - 100));
                if (!resizeAccepted) resizeAccepted = await ResizeOnUiAsync(Math.Max(360, width * .90), Math.Max(214, height * .85));
                if (resizeAccepted)
                {
                    IAsyncAction center = null;
                    await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { if (!disposed && widget != null) center = widget.CenterWindowAsync(); });
                    if (center != null) await center;
                }
            }
            catch (Exception error)
            {
                ApplicationData.Current.LocalSettings.Values["viewport-error"] = error.GetType().Name + " " + error.HResult.ToString("X8");
            }
            finally
            {
                await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    resizing = false;
                    if (disposed || widget == null) return;
                    string status = "accepted=" + resizeAccepted + ";requested=" + width + "x" + height + ";actual=" + ActualWidth + "x" + ActualHeight + ";dpi=" + density + ";core=" + ownerCoreWindow.Bounds + ";widget=" + widget.WindowBounds;
                    ApplicationData.Current.LocalSettings.Values["viewport-status"] = status;
                    WriteDiagnostic("viewport-status.txt", status);
                    UpdateContent();
                });
            }
        }
        private async Task<bool> ResizeOnUiAsync(double width, double height)
        {
            IAsyncOperation<bool> operation = null;
            await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { if (!disposed && widget != null) operation = widget.TryResizeWindowAsync(new Size(width, height)); });
            return operation != null && await operation;
        }
        private void PageSizeChanged(object sender, SizeChangedEventArgs args)
        {
            if (disposed || widget == null) return;
            WriteDiagnostic("viewport-size.txt", "actual=" + ActualWidth + "x" + ActualHeight + ";bounds=" + widget.WindowBounds + ";core=" + ownerCoreWindow.Bounds);
            UpdateContent();
        }
        private void Tick(object sender, object args)
        {
            if (!disposed && receiveAge.IsRunning && receiveAge.Elapsed.TotalSeconds >= 4) { current = null; receiveAge.Reset(); UpdateContent(); }
        }
        private void UpdateContent()
        {
            LiveCanvas.Children.Clear();
            bool fresh = !paused && current != null && current.Fresh && receiveAge.IsRunning && receiveAge.Elapsed.TotalSeconds < 4;
            bool editing = widget != null && widget.GameBarDisplayMode == XboxGameBarDisplayMode.Foreground;
            HelpCard.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            if (editing)
                Help.Text = !fresh ? "Waiting for Rift Ready. Open the app and return to League. Pin this widget and enable Game Bar click-through." : !FitsViewport ? "Live feed connected. Pin Rift Ready and enable Game Bar click-through. Game Bar limited the canvas size; only panels that fit can appear. Return to League after pinning." : "Live feed connected. Pin Rift Ready, enable Game Bar click-through, then return to League. Hold Tab for item-value comparisons. Move panels with Rift Ready's overlay settings; keep this canvas centered.";
            if (!fresh || !current.Visible || !current.HasGeometry || resizing) { RecordCanvas(false); return; }
            LiveCanvas.Width = current.ViewportWidth; LiveCanvas.Height = current.ViewportHeight;
            Rect bounds = ContentBounds;
            LiveCanvas.RenderTransform = new CompositeTransform { ScaleX = 1 / pixelsPerDip, ScaleY = 1 / pixelsPerDip, TranslateX = current.LocalX(0, bounds.X, pixelsPerDip), TranslateY = current.LocalY(0, bounds.Y, pixelsPerDip) };
            OverlayRoot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, Math.Max(0, ActualWidth), Math.Max(0, ActualHeight)) };
            Render(current);
            RecordCanvas(current.Gold);
        }
        private void RecordCanvas(bool gold)
        {
            string diagnostic = "panels=" + LiveCanvas.Children.Count + ";gold=" + gold + ";bounds=" + ContentBounds + ";actual=" + ActualWidth + "x" + ActualHeight + ";dpi=" + pixelsPerDip;
            if (gold && current != null) diagnostic += ";centerX=" + (current.BoardX + current.BoardWidth / 2) + ";firstRowY=" + current.RowStart + ";lastRowY=" + (current.RowStart + 4 * current.RowGap);
            long tick = Stopwatch.GetTimestamp();
            if (diagnostic != lastDiagnostic && (tick - lastDiagnosticTick) / (double)Stopwatch.Frequency >= 1)
            { lastDiagnostic = diagnostic; lastDiagnosticTick = tick; WriteDiagnostic("canvas-status.txt", diagnostic); if (LiveCanvas.Children.Count > 0) WriteDiagnostic("last-visible-canvas.txt", diagnostic); }
        }
        private static async void WriteDiagnostic(string name, string text)
        {
            try { var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting); await FileIO.WriteTextAsync(file, text); } catch (Exception) { }
        }
        private void Render(LiveFrame frame)
        {
            double scale = frame.BoardWidth / 440.0;
            if (frame.Gold)
            {
                var totals = new Grid { Width = 320, Height = 64 };
                var left = Text((frame.AlliesLeft ? "ALLIES" : "ENEMIES") + "  " + Number(frame.AlliesLeft ? frame.AllyTotal : frame.EnemyTotal), frame.AlliesLeft ? Accent : Enemy, 13);
                left.Margin = new Thickness(10, 7, 0, 0);
                var right = Text((frame.AlliesLeft ? "ENEMIES" : "ALLIES") + "  " + Number(frame.AlliesLeft ? frame.EnemyTotal : frame.AllyTotal), frame.AlliesLeft ? Enemy : Accent, 13);
                right.HorizontalAlignment = HorizontalAlignment.Right; right.Margin = new Thickness(0, 7, 10, 0);
                var caption = Text("ITEM VALUE · ESTIMATE", Muted, 10); caption.HorizontalAlignment = HorizontalAlignment.Center; caption.VerticalAlignment = VerticalAlignment.Bottom; caption.Margin = new Thickness(0, 0, 0, 8);
                totals.Children.Add(left); totals.Children.Add(right); totals.Children.Add(caption);
                Put(totals, frame.BoardX + frame.BoardWidth / 2 - 160 * scale, frame.BoardY, 320, 64, scale, 1);
                for (int i = 0; i < 5; i++)
                {
                    double? difference = frame.Differences[i];
                    string value = !difference.HasValue ? "—" : difference.Value == 0 ? "0" : (difference.Value > 0 ? "◀ " : "▶ ") + Math.Abs(difference.Value).ToString("0", CultureInfo.InvariantCulture);
                    bool allyAhead = difference.HasValue && (frame.AlliesLeft ? difference.Value > 0 : difference.Value < 0);
                    var text = Text(value, difference.HasValue && difference.Value != 0 ? (allyAhead ? Accent : Enemy) : Muted, 11);
                    text.HorizontalAlignment = HorizontalAlignment.Center; text.VerticalAlignment = VerticalAlignment.Center; text.FontWeight = Windows.UI.Text.FontWeights.Bold;
                    Put(text, frame.BoardX + frame.BoardWidth / 2 - 32 * scale, frame.RowStart + i * frame.RowGap - 12 * scale, 64, 24, scale, 1);
                }
            }
            if (frame.Purchase)
            {
                var content = new StackPanel { Margin = new Thickness(12, 9, 12, 9), Spacing = 4 };
                content.Children.Add(Text("NEXT ITEM", Muted, 10)); content.Children.Add(Text(frame.TargetName, ForegroundColor, 13));
                content.Children.Add(Text(frame.Owned == true ? "Owned" : !frame.NeededGold.HasValue ? "Gold unavailable" : frame.NeededGold == 0 ? "Ready to buy" : Number(frame.NeededGold) + " gold needed", Accent, 12));
                Put(content, frame.PurchaseX, frame.PurchaseY, frame.PurchaseWidth, frame.PurchaseHeight, 1, frame.PurchaseOpacity);
            }
            if (frame.StatsVisible)
            {
                var rows = new StackPanel { Margin = new Thickness(12, 9, 12, 9), Spacing = 5 };
                foreach (var row in frame.Stats) rows.Children.Add(Row(row[0], row[1], ForegroundColor));
                Put(rows, frame.StatsX, frame.StatsY, frame.StatsWidth, frame.StatsHeight, 1, frame.StatsOpacity);
            }
            if (frame.Buffs.Length > 0)
            {
                var rows = new StackPanel { Margin = new Thickness(12, 9, 12, 9), Spacing = 5 };
                foreach (var buff in frame.Buffs) rows.Children.Add(Row(buff.Name + " · estimate", Time(buff.Seconds), Accent));
                Put(rows, frame.BuffsX, frame.BuffsY, frame.BuffsWidth, frame.BuffsHeight, 1, frame.BuffsOpacity);
            }
        }
        private void Put(UIElement content, double x, double y, double width, double height, double scale, double opacity)
        {
            Rect bounds = ContentBounds;
            double localX = current.LocalX(x, bounds.X, pixelsPerDip), localY = current.LocalY(y, bounds.Y, pixelsPerDip);
            if (localX < 0 || localY < 0 || localX + width * scale / pixelsPerDip > ActualWidth || localY + height * scale / pixelsPerDip > ActualHeight) return;
            double alpha = Math.Max(0, Math.Min(1, opacity * (widget == null ? 1 : widget.RequestedOpacity / 100.0)));
            var border = new Border { Width = width, Height = height, Background = new SolidColorBrush(Color.FromArgb((byte)Math.Round(245 * alpha), 21, 29, 36)), BorderBrush = new SolidColorBrush(Color.FromArgb((byte)Math.Round(255 * alpha), 49, 93, 96)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(2), Child = content, RenderTransform = new ScaleTransform { ScaleX = scale, ScaleY = scale } };
            border.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
            Canvas.SetLeft(border, Math.Round(Math.Max(0, Math.Min(x, current.ViewportWidth - width * scale))));
            Canvas.SetTop(border, Math.Round(Math.Max(0, Math.Min(y, current.ViewportHeight - height * scale))));
            LiveCanvas.Children.Add(border);
        }
        private static readonly Color Accent = Color.FromArgb(255, 66, 205, 198), Enemy = Color.FromArgb(255, 240, 134, 155), Muted = Color.FromArgb(255, 149, 166, 181), ForegroundColor = Color.FromArgb(255, 241, 245, 247);
        private static TextBlock Text(string text, Color color, double size) { return new TextBlock { Text = text ?? "", Foreground = new SolidColorBrush(color), FontSize = size, TextTrimming = TextTrimming.CharacterEllipsis }; }
        private static Grid Row(string label, string value, Color color)
        {
            var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var name = Text(label, Muted, 12); name.Margin = new Thickness(0, 0, 8, 0); var amount = Text(value, color, 12); Grid.SetColumn(amount, 1); grid.Children.Add(name); grid.Children.Add(amount); return grid;
        }
        private static string Number(double? value) { return value.HasValue ? value.Value.ToString("N0", CultureInfo.InvariantCulture) : "—"; }
        private static string Time(double seconds) { return ((int)seconds / 60).ToString(CultureInfo.InvariantCulture) + ":" + ((int)seconds % 60).ToString("00", CultureInfo.InvariantCulture); }
        private async void StateChanged(XboxGameBarWidget sender, object args) { if (disposed) return; try { await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, ApplyState); } catch (Exception error) { LogFailure("state", error); } }
        private async void DisplayModeChanged(XboxGameBarWidget sender, object args)
        {
            if (disposed) return;
            try { await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { if (current != null && !resizeAccepted) EnsureViewport(current, true); ApplyState(); }); } catch (Exception error) { LogFailure("display-mode", error); }
        }
        private void ApplyState() { if (disposed || widget == null) return; Tick(null, null); UpdateContent(); if (widget.Visible && !paused) timer.Start(); else timer.Stop(); }
        public Task PauseAsync() { return OnUiAsync(() => { if (disposed) return; paused = true; timer.Stop(); current = null; receiveAge.Reset(); UpdateContent(); }, "pause"); }
        public Task ResumeAsync() { return OnUiAsync(() => { if (disposed) return; paused = false; ApplyState(); }, "resume"); }
        private async Task OnUiAsync(Action action, string operation)
        {
            try { if (uiDispatcher.HasThreadAccess) action(); else await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action()); }
            catch (Exception error) { LogFailure(operation, error); }
        }
        private static void LogFailure(string operation, Exception error) { WriteDiagnostic("widget-error.txt", DateTime.UtcNow.ToString("o") + " " + operation + " " + error.GetType().Name + " " + error.HResult.ToString("X8") + "\n" + error.StackTrace); }
        protected override void OnNavigatedFrom(NavigationEventArgs args) { Dispose(); }
        public async void Dispose() { await DisposeAsync(); }
        public Task DisposeAsync() { return OnUiAsync(DisposeCore, "dispose"); }
        private void DisposeCore()
        {
            if (disposed) return; disposed = true; timer.Stop(); timer.Tick -= Tick; SizeChanged -= PageSizeChanged;
            if (widget != null) { widget.RequestedOpacityChanged -= StateChanged; widget.PinnedChanged -= StateChanged; widget.VisibleChanged -= StateChanged; widget.GameBarDisplayModeChanged -= DisplayModeChanged; widget.WindowBoundsChanged -= StateChanged; widget = null; }
        }
    }
    // Reject a corrupt frame in full, so stale or partial values cannot masquerade as live data.
    internal sealed class LiveFrame
    {
        public bool Fresh, Visible, Gold, AlliesLeft, Purchase, StatsVisible;
        public double? MatchSeconds, AllyTotal, EnemyTotal, NeededGold;
        public bool? Owned;
        public double ViewportX, ViewportY, ViewportWidth, ViewportHeight, BoardX, BoardY, BoardWidth, BoardHeight, RowStart, RowGap;
        public double PurchaseX, PurchaseY, StatsX, StatsY, BuffsX, BuffsY, PurchaseOpacity, StatsOpacity, BuffsOpacity;
        public double PurchaseWidth, PurchaseHeight, StatsWidth, StatsHeight, BuffsWidth, BuffsHeight;
        public bool HasGeometry { get { return ViewportWidth > 0 && ViewportHeight > 0; } }
        public double LocalX(double gameX, double contentLeftDip, double rawPixelsPerDip) { return (ViewportX + gameX) / rawPixelsPerDip - contentLeftDip; }
        public double LocalY(double gameY, double contentTopDip, double rawPixelsPerDip) { return (ViewportY + gameY) / rawPixelsPerDip - contentTopDip; }
        public string TargetName;
        public double?[] Differences;
        public string[][] Stats;
        public Buff[] Buffs;
        internal sealed class Buff { public string Name; public double Seconds; }
        public static LiveFrame Parse(string json)
        {
            if (String.IsNullOrEmpty(json) || json.Length > 8192 || Encoding.UTF8.GetByteCount(json) > 8192) throw new ArgumentException("Invalid frame size.");
            JsonObject obj;
            if (!JsonObject.TryParse(json, out obj) || Num(obj, "version", false) != 1) throw new ArgumentException("Invalid frame version.");
            var f = new LiveFrame { Fresh = Bool(obj, "fresh"), Visible = Bool(obj, "visible"), Gold = Bool(obj, "goldVisible"), AlliesLeft = Bool(obj, "alliesLeft"), Purchase = Bool(obj, "purchaseVisible"), StatsVisible = Bool(obj, "statsVisible"), MatchSeconds = Num(obj, "matchSeconds", false), AllyTotal = Num(obj, "allyTotal", false), EnemyTotal = Num(obj, "enemyTotal", false), NeededGold = Num(obj, "neededGold", false), TargetName = Str(obj, "targetName", true) };
            f.ViewportX = Geo(obj, "viewportX", -32768, 32768); f.ViewportY = Geo(obj, "viewportY", -32768, 32768);
            f.ViewportWidth = Geo(obj, "viewportWidth", 0, 16384); f.ViewportHeight = Geo(obj, "viewportHeight", 0, 16384);
            if ((f.ViewportWidth == 0) != (f.ViewportHeight == 0) || (f.Visible && !f.HasGeometry)) throw new ArgumentException("Missing viewport.");
            f.BoardX = Geo(obj, "boardX", 0, f.ViewportWidth); f.BoardY = Geo(obj, "boardY", 0, f.ViewportHeight);
            f.BoardWidth = Geo(obj, "boardWidth", 0, f.ViewportWidth); f.BoardHeight = Geo(obj, "boardHeight", 0, f.ViewportHeight);
            f.RowStart = Geo(obj, "rowStart", 0, f.ViewportHeight); f.RowGap = Geo(obj, "rowGap", 0, f.ViewportHeight);
            f.PurchaseX = Geo(obj, "purchaseX", 0, f.ViewportWidth); f.PurchaseY = Geo(obj, "purchaseY", 0, f.ViewportHeight);
            f.StatsX = Geo(obj, "statsX", 0, f.ViewportWidth); f.StatsY = Geo(obj, "statsY", 0, f.ViewportHeight);
            f.BuffsX = Geo(obj, "buffsX", 0, f.ViewportWidth); f.BuffsY = Geo(obj, "buffsY", 0, f.ViewportHeight);
            f.PurchaseOpacity = Geo(obj, "purchaseOpacity", 0, 1); f.StatsOpacity = Geo(obj, "statsOpacity", 0, 1); f.BuffsOpacity = Geo(obj, "buffsOpacity", 0, 1);
            f.PurchaseWidth = Geo(obj, "purchaseWidth", 1, 16384); f.PurchaseHeight = Geo(obj, "purchaseHeight", 1, 16384);
            f.StatsWidth = Geo(obj, "statsWidth", 1, 16384); f.StatsHeight = Geo(obj, "statsHeight", 1, 16384);
            f.BuffsWidth = Geo(obj, "buffsWidth", 1, 16384); f.BuffsHeight = Geo(obj, "buffsHeight", 1, 16384);
            if (f.HasGeometry && (f.ViewportWidth < 640 || f.ViewportHeight < 480 || f.BoardWidth < 100 || f.BoardHeight < 100 || f.BoardX + f.BoardWidth > f.ViewportWidth + 1 || f.BoardY + f.BoardHeight > f.ViewportHeight + 1 || f.RowGap < 10 || f.RowStart + f.RowGap * 4 > f.ViewportHeight)) throw new ArgumentException("Invalid viewport geometry.");
            var owned = Required(obj, "owned");
            if (owned.ValueType != JsonValueType.Null && owned.ValueType != JsonValueType.Boolean) throw new ArgumentException("Invalid owned state.");
            f.Owned = owned.ValueType == JsonValueType.Null ? (bool?)null : owned.GetBoolean();
            var differences = Array(obj, "differences");
            if (differences.Count != 5) throw new ArgumentException("Invalid differences.");
            f.Differences = new double?[5];
            for (int i = 0; i < 5; i++) f.Differences[i] = NumValue(differences[i], true);
            var stats = Array(obj, "stats"); var buffs = Array(obj, "buffs");
            if (stats.Count > 5 || buffs.Count > 2) throw new ArgumentException("Too many rows.");
            f.Stats = new string[stats.Count][];
            for (int i = 0; i < stats.Count; i++) { var row = Object(stats[i]); f.Stats[i] = new[] { Str(row, "label", false), Str(row, "value", false) }; }
            f.Buffs = new Buff[buffs.Count];
            for (int i = 0; i < buffs.Count; i++)
            {
                var row = Object(buffs[i]); string name = Str(row, "name", false); double? remaining = Num(row, "remainingSeconds", false);
                if ((name != "Baron" && name != "Elder") || !remaining.HasValue || remaining <= 0 || remaining > (name == "Baron" ? 180 : 150) || (i > 0 && f.Buffs[0].Name == name)) throw new ArgumentException("Invalid buff.");
                f.Buffs[i] = new Buff { Name = name, Seconds = remaining.Value };
            }
            if ((f.Purchase && f.TargetName == null) || (f.Visible && (!f.Fresh || !f.MatchSeconds.HasValue || !(f.Gold || f.Purchase || f.StatsVisible || f.Buffs.Length > 0))) || (f.StatsVisible && f.Stats.Length == 0)) throw new ArgumentException("Inconsistent frame.");
            return f;
        }
        private static IJsonValue Required(JsonObject o, string key) { IJsonValue value; if (!o.TryGetValue(key, out value)) throw new ArgumentException("Missing frame field."); return value; }
        private static double Geo(JsonObject o, string key, double minimum, double maximum) { double? value = Num(o, key, true); if (!value.HasValue || value.Value < minimum || value.Value > maximum) throw new ArgumentException("Invalid geometry."); return value.Value; }
        private static JsonArray Array(JsonObject o, string key) { var v = Required(o, key); if (v.ValueType != JsonValueType.Array) throw new ArgumentException("Invalid array."); return v.GetArray(); }
        private static JsonObject Object(IJsonValue v) { if (v.ValueType != JsonValueType.Object) throw new ArgumentException("Invalid row."); return v.GetObject(); }
        private static bool Bool(JsonObject o, string key) { var v = Required(o, key); if (v.ValueType != JsonValueType.Boolean) throw new ArgumentException("Invalid flag."); return v.GetBoolean(); }
        private static double? Num(JsonObject o, string key, bool signed) { return NumValue(Required(o, key), signed); }
        private static double? NumValue(IJsonValue v, bool signed)
        {
            if (v.ValueType == JsonValueType.Null) return null;
            if (v.ValueType != JsonValueType.Number) throw new ArgumentException("Invalid number.");
            double n = v.GetNumber(); if (Double.IsNaN(n) || Double.IsInfinity(n) || (!signed && n < 0) || Math.Abs(n) > 1000000000) throw new ArgumentException("Number out of range."); return n;
        }
        private static string Str(JsonObject o, string key, bool nullable)
        {
            var v = Required(o, key); if (nullable && v.ValueType == JsonValueType.Null) return null;
            if (v.ValueType != JsonValueType.String) throw new ArgumentException("Invalid text.");
            string s = v.GetString(); if (String.IsNullOrWhiteSpace(s) || s.Length > 128) throw new ArgumentException("Invalid text length.");
            foreach (char c in s) if (Char.IsControl(c)) throw new ArgumentException("Invalid text character."); return s;
        }
    }
}
