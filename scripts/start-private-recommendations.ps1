param(
    [ValidateRange(150, 5000)][int]$Budget = 500,
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$python = Join-Path $repo 'cache/runtime/python.exe'
$pipeline = Join-Path $repo 'services/recommendations/pipeline.py'
$data = Join-Path $repo 'cache/data'
$output = Join-Path $repo 'services/recommendations/output'
foreach ($required in @($python, $pipeline, (Join-Path $data 'version.txt'), (Join-Path $data 'item.json'))) {
    if (!(Test-Path -LiteralPath $required)) { throw "Required local collector file is missing: $required" }
}
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Windows.Forms,System.Drawing -TypeDefinition @'
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

public sealed class PrivateRecommendationWindow : Form
{
    readonly TextBox key = new TextBox();
    readonly Label status = new Label();
    readonly Button start = new Button();
    readonly Button stop = new Button();
    readonly Button test = new Button();
    readonly Timer timer = new Timer();
    readonly string python, pipeline, data, output;
    readonly int budget;
    Process worker;
    DateTime started;
    bool cancelled;
    string failureDetail = "";
    bool checkingAccess;
    string accessResult = "";
    string collectionProgress = "", collectionResult = "";

    public PrivateRecommendationWindow(string pythonPath, string pipelinePath, string dataPath, string outputPath, int callBudget)
    {
        python = pythonPath; pipeline = pipelinePath; data = dataPath; output = outputPath; budget = callBudget;
        Text = "Rift Ready - private Riot collection";
        BackColor = Color.FromArgb(15,16,20); ForeColor = Color.FromArgb(238,240,244);
        ClientSize = new Size(580, 285);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        Label explanation = new Label { Left = 22, Top = 18, Width = 534, Height = 65,
            Text = "Paste your Riot development key below. It stays in memory for this collection only. Private NA, EUW and Korea sampling; no public upload." };
        Controls.Add(explanation);
        key.SetBounds(22, 91, 534, 29);
        key.UseSystemPasswordChar = true; key.MaxLength = 100;
        key.BackColor = Color.FromArgb(25,27,33); key.ForeColor = ForeColor;
        Controls.Add(key);
        start.Text = "Start collection"; start.SetBounds(22, 137, 160, 34);
        start.Click += delegate { checkingAccess = false; BeginCollection(); }; Controls.Add(start);
        stop.Text = "Stop"; stop.SetBounds(194, 137, 95, 34); stop.Enabled = false;
        stop.Click += delegate { StopWorker(); }; Controls.Add(stop);
        test.Text = "Test API access"; test.SetBounds(307, 137, 175, 34);
        test.Click += delegate { checkingAccess = true; BeginCollection(); }; Controls.Add(test);
        foreach(var button in new[]{start,stop,test}){button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(25,27,33);button.ForeColor=ForeColor;button.FlatAppearance.BorderColor=Color.FromArgb(66,205,198);}
        status.SetBounds(22, 188, 534, 78);
        status.Text = "At most " + budget + " API requests. Collection can take several minutes. You can stop or close this window at any time.";
        Controls.Add(status);
        timer.Interval = 500; timer.Tick += delegate { Poll(); };
        FormClosing += delegate { StopWorker(); };
        FormClosed += delegate { timer.Dispose(); key.Clear(); if (worker != null) worker.Dispose(); };
    }

    static string Quote(string value)
    {
        // Paths are generated locally, never populated from credential input.
        if (value.IndexOf('"') >= 0 || value.EndsWith("\\")) throw new ArgumentException("Invalid local path.");
        return "\"" + value + "\"";
    }

    void BeginCollection()
    {
        string secret = key.Text.Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(secret, @"^RGAPI-[A-Za-z0-9-]{20,80}$"))
        {
            secret = null; status.Text = "Paste the complete development key from Riot's portal into the masked field."; return;
        }
        ProcessStartInfo info = null;
        try
        {
            if (worker != null) { worker.Dispose(); worker = null; }
            info = new ProcessStartInfo(python, "-u " + Quote(pipeline) + " --data " + Quote(data) + " --output " + Quote(output) + " --budget " + budget + (checkingAccess ? " --check-access" : " --seeds 200"));
            info.UseShellExecute = false; info.CreateNoWindow = true;
            info.RedirectStandardOutput = true; info.RedirectStandardError = true;
            info.WorkingDirectory = System.IO.Path.GetDirectoryName(pipeline);
            info.EnvironmentVariables["RIOT_API_KEY"] = secret;
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            worker = new Process(); worker.StartInfo = info;
            // Drain both pipes without displaying, retaining or writing raw responses/errors.
            accessResult = "";
            collectionProgress = ""; collectionResult = "";
            worker.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                var result = System.Text.RegularExpressions.Regex.Match(e.Data ?? "", @"^Access check: NA1 (status|ranked) (passed|not-found|request-failed|HTTP-[0-9]{3})$");
                if(result.Success) accessResult = "NA1 " + result.Groups[1].Value + ": " + result.Groups[2].Value + ".";
                var progress = System.Text.RegularExpressions.Regex.Match(e.Data ?? "", @"^Collection progress: (NA1|EUW1|KR) ([0-9]{1,9}) samples$");
                if(progress.Success) collectionProgress = progress.Groups[1].Value + " complete; " + progress.Groups[2].Value + " player samples saved. ";
                var done = System.Text.RegularExpressions.Regex.Match(e.Data ?? "", @"^Collection result: ([0-9]{1,6}) builds ([0-9]{1,6}) rune pages$");
                if(done.Success) collectionResult = done.Groups[1].Value + " qualifying builds, " + done.Groups[2].Value + " rune pages. ";
            };
            failureDetail = "";
            worker.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                // Capture only fixed diagnostic categories, never arbitrary stderr or URLs.
                string line = e.Data ?? "";
                var http = System.Text.RegularExpressions.Regex.Match(line, @"^Riot HTTP ([0-9]{3});");
                if (http.Success) failureDetail = "Riot returned HTTP " + http.Groups[1].Value + ". " +
                    (http.Groups[1].Value == "403" ? "Riot refused access; expiry is only one possible cause. Use Test API access." : "Check key, API access and service availability.");
                else if (line.StartsWith("Riot network request failed")) failureDetail = "Could not reach Riot. Check the network connection and retry.";
                else if (line.StartsWith("Riot rate limit")) failureDetail = "Riot rate limit reached. Wait before retrying.";
                else if (line.StartsWith("Collection request budget exhausted")) failureDetail = "Request budget reached. Saved samples are retained; run collection again to continue.";
                else if (line.Contains("response lacks PUUIDs") || line.StartsWith("Unexpected Riot")) failureDetail = "Riot returned a response the collector does not support. The collector needs an update.";
                else if (line.StartsWith("ModuleNotFoundError:") || line.StartsWith("ImportError:")) failureDetail = "A required Python module could not load. The private runtime needs repair.";
            };
            worker.Start();
            worker.BeginOutputReadLine(); worker.BeginErrorReadLine();
            started = DateTime.UtcNow; cancelled = false;
            key.Enabled = false; start.Enabled = false; test.Enabled = false; stop.Enabled = true;
            status.Text = checkingAccess ? "Testing NA API access without collecting matches..." : "Collecting privately across NA, EUW and Korea...";
            timer.Start();
        }
        catch
        {
            StopWorker(); status.Text = "The collector could not start. Check the local Python runtime and collector files.";
            key.Enabled = true; start.Enabled = true; test.Enabled = true; checkingAccess = false;
        }
        finally
        {
            key.Clear(); secret = null;
            if (info != null) info.EnvironmentVariables.Remove("RIOT_API_KEY");
        }
    }

    void StopWorker()
    {
        cancelled = true;
        if (worker != null)
        {
            try { if (!worker.HasExited) worker.Kill(); } catch (InvalidOperationException) { }
        }
    }

    void Poll()
    {
        if (worker == null) return;
        if (!worker.HasExited)
        {
            status.Text = checkingAccess ? "Testing API access... " + accessResult : collectionProgress + "Collecting privately - " + (int)(DateTime.UtcNow - started).TotalSeconds + " seconds elapsed. Riot rate limits may pause requests. Maximum " + budget + " requests.";
            return;
        }
        timer.Stop(); worker.WaitForExit(); stop.Enabled = false; key.Enabled = true; start.Enabled = true; test.Enabled = true;
        key.Focus();
        if(checkingAccess){status.Text = "Access test: " + accessResult + (worker.ExitCode == 0 ? " Both checks passed. Paste key again to start collection." : " Stopped; no matches collected. Share this result, not your key.");checkingAccess=false;return;}
        status.Text = cancelled ? "Stopped. Committed private samples are retained; the existing feed is unchanged. Paste a key to run again." :
            worker.ExitCode == 0 ? "Collection finished. " + collectionResult + "Local feed updated; refresh Runes / builds. Choices require 30 games and 10 players. Run again to grow the sample." :
            "Collection stopped. " + (failureDetail == "" ? "An unrecognized local error occurred. The existing feed is unchanged." : failureDetail);
    }
}
'@
if (!$ValidateOnly) {
    [System.Windows.Forms.Application]::EnableVisualStyles()
    $window = New-Object PrivateRecommendationWindow($python, $pipeline, $data, $output, $Budget)
    try { [void]$window.ShowDialog() } finally { $window.Dispose() }
}
