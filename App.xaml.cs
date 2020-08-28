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

            if (count > 1)
            {
                MessageBox.Show("Already an instance is running...");
                Current.Shutdown();
            }

            RegisterApplicationToRunOnStartup();
            AddTaskbarIcon();
            new HttpServer(5050);

            base.OnStartup(e);
        }

        internal static void ShowTaskbarNotificationAfterUpload(string message) //"Signed XML File Uploaded Successfully"
        {
            tbi.ToolTipText = "BCC-CA XML Signing Client";
            tbi.ShowBalloonTip("XML Signing Client", message, BalloonIcon.Warning);
        }

        private void AddTaskbarIcon()
        {
            tbi = new TaskbarIcon();
            tbi.Icon = XmlSign.BytesToIcon(XMLSigner.Properties.Resources.Logo);
            tbi.ToolTipText = "BCC-CA XML Signing Client";
            //tbi.Visibility = Visibility.Visible;
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
                MessageBox.Show(ex.ToString(), "Error Registering Application with Browser", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
