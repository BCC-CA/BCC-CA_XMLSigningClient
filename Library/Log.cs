using System;
using System.Diagnostics;
using System.IO;

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
        static string logLocation;// = @"C:\Log\Bcc-Signer\" + DateTime.Now.ToString("yyyy-MM-dd.log");

        internal static void Print(LogLevel level, string message)
        {
            logLocation = @"C:\Log\Bcc-Signer\" + DateTime.Now.ToString("yyyy-MM-dd")+ ".log";
            // If directory does not exist, create it. 
            if (!Directory.Exists(logLocation))
            {
                //Directory.CreateDirectory(logLocation);
                Directory.CreateDirectory(Path.GetDirectoryName(logLocation));
            }

            using (StreamWriter sw = File.AppendText(logLocation))
            {
                sw.WriteLine(
                       DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK")
                       + "\t" + level
                       + "\t" + (new StackTrace().GetFrame(1).GetMethod()).ReflectedType.Name
                       + "\t" + ":" + "\t" + message
                       //+ Environment.NewLine
                   );
            }
        }
    }
}
