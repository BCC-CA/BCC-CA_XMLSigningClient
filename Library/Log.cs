using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace XMLSigner.Library
{
    public enum LogLevel
    {
        _Low,
        Medium,
        High,
        Critical
    }

    class Log
    {
        static bool isErrorOccured = false;
        static string logLocation;// = @"C:\Log\Bcc-Signer\" + DateTime.Now.ToString("yyyy-MM-dd.log");

        internal static void Print(LogLevel level, string message)
        {
            logLocation = @"C:\Log\Bcc-Signer\" + DateTime.Now.ToString("yyyy-MM-dd")+ ".log";
            // If directory does not exist, create it. 
            if (!Directory.Exists(logLocation))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logLocation));
            }
            try
            {
                using (StreamWriter sw = File.AppendText(logLocation))
                {
                    sw.WriteLine(
                           DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK")
                           + "\t" + level
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                           + "\t" + new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                           + "\t" + ":" + "\t" + message
                       //+ Environment.NewLine
                       );
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if(!isErrorOccured)
                {
                    MessageBox.Show("Log is not writing");
                }
            }
        }
    }
}
