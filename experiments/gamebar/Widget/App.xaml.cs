using System;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Windows.Storage;

namespace RiftReady.GameBar
{
    sealed partial class App : Application
    {
        // Keep the SDK connection alive for the widget's entire window lifetime.
        private XboxGameBarWidget widget;
        private WidgetPage page;
        private AppServiceConnection connection;
        private BackgroundTaskDeferral serviceDeferral;
        private IBackgroundTaskInstance serviceTask;
        private readonly object serviceGate = new object();
        public App()
        {
            InitializeComponent();
            Suspending += async (s, e) =>
            {
                var hold = e.SuspendingOperation.GetDeferral();
                try { var target = page; if (target != null) await target.PauseAsync(); }
                finally { hold.Complete(); }
            };
            Resuming += async (s, e) => { var target = page; if (target != null) await target.ResumeAsync(); };
            UnhandledException += (s, e) => LogError("unhandled", e.Exception);
        }
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            var activation = args as XboxGameBarWidgetActivatedEventArgs;
            if (activation == null) return;
            if (!activation.IsLaunchActivation)
            {
                await LaunchBridge();
                return;
            }
            var window = Window.Current;
            var dispatcher = window.Dispatcher;
            await CleanupAsync();
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var frame = new Frame();
                window.Content = frame;
                widget = new XboxGameBarWidget(activation, window.CoreWindow, frame);
                frame.Navigate(typeof(WidgetPage), widget);
                page = frame.Content as WidgetPage;
                var createdPage = page;
                window.Closed += async (sender, closed) =>
                {
                    await createdPage.DisposeAsync();
                    if (ReferenceEquals(page, createdPage)) { page = null; widget = null; CloseService(); }
                };
                window.Activate();
            });
            await LaunchBridge();
        }
        private async Task LaunchBridge()
        {
            string status;
            try { await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(); status = "Desktop helper launch requested"; }
            catch (Exception error) { status = "Desktop helper launch failed: " + error.GetType().Name + " " + error.HResult.ToString("X8"); }
            try { var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("widget-status.log", CreationCollisionOption.ReplaceExisting); await FileIO.WriteTextAsync(file, DateTime.UtcNow.ToString("o") + " " + status); } catch (Exception) { }
        }
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var details = args.TaskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (details == null || details.Name != "RiftReadyDisplayFeed" || details.CallerPackageFamilyName != Package.Current.Id.FamilyName) return;
            CloseService();
            lock (serviceGate)
            {
                serviceTask = args.TaskInstance;
                serviceDeferral = serviceTask.GetDeferral();
                serviceTask.Canceled += ServiceCanceled;
                connection = details.AppServiceConnection;
                connection.RequestReceived += Receive;
                connection.ServiceClosed += ServiceClosed;
            }
        }
        private async void Receive(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var hold = args.GetDeferral();
            try
            {
                lock (serviceGate) { if (sender != connection) return; }
                object value;
                var json = args.Request.Message.TryGetValue("json", out value) ? value as string : null;
                if (json != null && System.Text.Encoding.UTF8.GetByteCount(json) > 8192) json = null;
                var target = page;
                if (target != null) await target.ApplyLiveJsonAsync(json);
                await args.Request.SendResponseAsync(new ValueSet { { "continue", target != null } });
            }
            catch (System.Exception error) { LogError("receive", error); }
            finally { hold.Complete(); }
        }
        private void ServiceCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) { CloseService(null, sender); }
        private void ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args) { CloseService(sender); }
        private void CloseService(AppServiceConnection expected = null, IBackgroundTaskInstance expectedTask = null)
        {
            AppServiceConnection old;
            IBackgroundTaskInstance task;
            BackgroundTaskDeferral deferral;
            lock (serviceGate)
            {
                if ((expected != null && expected != connection) || (expectedTask != null && expectedTask != serviceTask)) return;
                old = connection; task = serviceTask; deferral = serviceDeferral;
                connection = null; serviceTask = null; serviceDeferral = null;
            }
            if (old != null) { old.RequestReceived -= Receive; old.ServiceClosed -= ServiceClosed; old.Dispose(); }
            if (task != null) task.Canceled -= ServiceCanceled;
            if (deferral != null) deferral.Complete();
        }
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (args.PrelaunchActivated) return;
            var frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                frame = new Frame();
                Window.Current.Content = frame;
                frame.Navigate(typeof(WidgetPage));
                // A normal setup window is a separate XAML view. It must never become
                // the live feed target or dispose a widget owned by another UI thread.
                var setupPage = frame.Content as WidgetPage;
                Window.Current.Closed += async (sender, closed) => { if (setupPage != null) await setupPage.DisposeAsync(); };
            }
            Window.Current.Activate();
            await LaunchBridge();
        }
        private async Task CleanupAsync()
        {
            var oldPage = page;
            page = null;
            widget = null;
            if (oldPage != null) await oldPage.DisposeAsync();
        }
        private static async void LogError(string operation, Exception error)
        {
            try
            {
                string text = DateTime.UtcNow.ToString("o") + " " + operation + " " + error.GetType().Name + " " + error.HResult.ToString("X8") + "\n" + error.StackTrace;
                ApplicationData.Current.LocalSettings.Values["app-error"] = text;
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("app-error.txt", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, text);
            }
            catch (Exception) { }
        }
    }
}
