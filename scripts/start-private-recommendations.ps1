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
    readonly Timer timer = new Timer();
    readonly string python, pipeline, data, output;
    readonly int budget;
    Process worker;
    DateTime started;
    bool cancelled;

    public PrivateRecommendationWindow(string pythonPath, string pipelinePath, string dataPath, string outputPath, int callBudget)
    {
        python = pythonPath; pipeline = pipelinePath; data = dataPath; output = outputPath; budget = callBudget;
        Text = "Rift Ready - private Riot collection";
        ClientSize = new Size(580, 285);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        Label explanation = new Label { Left = 22, Top = 18, Width = 534, Height = 65,
            Text = "Paste your Riot development key below. It stays in memory for this collection only. Private NA, EUW and Korea sampling; no public upload." };
        Controls.Add(explanation);
        key.SetBounds(22, 91, 534, 29);
        key.UseSystemPasswordChar = true; key.MaxLength = 100;
        Controls.Add(key);
        start.Text = "Start collection"; start.SetBounds(22, 137, 160, 34);
        start.Click += delegate { BeginCollection(); }; Controls.Add(start);
        stop.Text = "Stop"; stop.SetBounds(194, 137, 95, 34); stop.Enabled = false;
        stop.Click += delegate { StopWorker(); }; Controls.Add(stop);
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
            info = new ProcessStartInfo(python, "-u " + Quote(pipeline) + " --data " + Quote(data) + " --output " + Quote(output) + " --budget " + budget);
            info.UseShellExecute = false; info.CreateNoWindow = true;
            info.RedirectStandardOutput = true; info.RedirectStandardError = true;
            info.WorkingDirectory = System.IO.Path.GetDirectoryName(pipeline);
            info.EnvironmentVariables["RIOT_API_KEY"] = secret;
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            worker = new Process(); worker.StartInfo = info;
            // Drain both pipes without displaying, retaining or writing raw responses/errors.
            worker.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) { };
            worker.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { };
            worker.Start();
            worker.BeginOutputReadLine(); worker.BeginErrorReadLine();
            started = DateTime.UtcNow; cancelled = false;
            key.Enabled = false; start.Enabled = false; stop.Enabled = true;
            status.Text = "Collecting privately across NA, EUW and Korea...";
            timer.Start();
        }
        catch
        {
            StopWorker(); status.Text = "The collector could not start. Check the local Python runtime and collector files.";
            key.Enabled = true; start.Enabled = true;
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
            status.Text = "Collecting privately - " + (int)(DateTime.UtcNow - started).TotalSeconds + " seconds elapsed. Riot rate limits may pause requests. Maximum " + budget + " requests.";
            return;
        }
        timer.Stop(); worker.WaitForExit(); stop.Enabled = false; key.Enabled = true; start.Enabled = true;
        status.Text = cancelled ? "Stopped. Committed private samples are retained; the existing feed is unchanged. Paste a key to run again." :
            worker.ExitCode == 0 ? "Collection finished. The local anonymous feed was updated. Alternatives appear only after 30 games and 10 players; a first sample may have no eligible choices." :
            "Collection did not finish. The existing feed is unchanged. Check key expiry, network access and Riot service availability, then paste a valid key to retry.";
    }
}
'@
if (!$ValidateOnly) {
    [System.Windows.Forms.Application]::EnableVisualStyles()
    $window = New-Object PrivateRecommendationWindow($python, $pipeline, $data, $output, $Budget)
    try { [void]$window.ShowDialog() } finally { $window.Dispose() }
}
