﻿using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
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

            StartServer();
        }

        private void StartServer()
        {
            if (CheckIfPortAvailable(5050))
            {
                Log.Print(LogLevel.High, "Started with Port 5050");
#pragma warning disable CS0612 // Type or member is obsolete
                ThreadPool.QueueUserWorkItem(_ => new HttpServer(5050));
                //new HttpServer(5050);
#pragma warning restore CS0612 // Type or member is obsolete
            }
            else
            {
                Log.Print(LogLevel.High, "Started with Port 8088");
#pragma warning disable CS0612 // Type or member is obsolete
                //new HttpServer(8088);
                ThreadPool.QueueUserWorkItem(_ => new HttpServer(8088));
#pragma warning restore CS0612 // Type or member is obsolete
            }
        }

        private bool CheckIfPortAvailable(int port)
        {
            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            //Can be checked other with ways by trying opening a TCP port into that port address
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                Log.Print(LogLevel._Low, "Port Checking - " + tcpi.LocalEndPoint.Port.ToString());
                if (tcpi.LocalEndPoint.Port == port)
                {
                    return false;
                }
            }
            return true;
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
                MessageBox.Show(ex.ToString(), "Error Registering Application with Browser", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        void radioButton_CustomTSASelect(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Selected");
        }

        void radioButton_CustomTSAUnSelect(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("UnselectedK");
        }
        
    }
}
