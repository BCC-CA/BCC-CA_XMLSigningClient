using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using XMLSigner.Library;

namespace XMLSigner
{
    public partial class App : Application
    {
        private static TaskbarIcon tbi;

        protected override void OnStartup(StartupEventArgs e)
        {
            Process proc = Process.GetCurrentProcess();
            int count = Process.GetProcesses().Where(p =>
                p.ProcessName == proc.ProcessName).Count();

            if(count > 1)
            {
                MessageBox.Show("Already an instance is running...");
                Current.Shutdown();
            }
            AddTaskbarIcon();
            base.OnStartup(e);
            RegisterApplicationToRunOnStartup();

#pragma warning disable CS0612 // Type or member is obsolete
            StartServer();
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Obsolete]
        private void StartServer()
        {
            int port = NetworkPort.CheckIfPortAvailable(5050) ? 5050:8088;
            //ThreadPool.QueueUserWorkItem(_ => new HttpServer(port));
            new HttpServer(port);
        }

        internal static void ShowTaskbarNotificationAfterUpload(string message) //"Signed XML File Uploaded Successfully"
        {
            tbi.ToolTipText = "BCC-CA XML Signing Client";
            tbi.ShowBalloonTip("XML Signing Client", message, BalloonIcon.Warning);
        }

        private void AddTaskbarIcon()
        {
            tbi = (TaskbarIcon)FindResource("NotifyIcon");
            tbi.Icon = XmlSign.BytesToIcon(XMLSigner.Properties.Resources.Logo);
            tbi.ToolTipText = "BCC-CA XML Signing Client";
            tbi.Visibility = Visibility.Visible;
            tbi.ShowBalloonTip("XML Signing Client", "BCC-CA XML Signing Client is running in background", BalloonIcon.Info);
        }

        private void RegisterApplicationToRunOnStartup()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                registryKey.SetValue(Application.ResourceAssembly.GetName().Name, Process.GetCurrentProcess().MainModule.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error - Application should be launched as Administrator, please run again.", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                Environment.Exit(0);
            }
        }

        void radioButton_CustomTSASelect(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Selected");
        }

        void radioButton_CustomTSAUnSelect(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Un-Selected");
        }
    }
}
