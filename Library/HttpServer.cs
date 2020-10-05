using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Xml;
using XMLSigner.Dialog.WysiwysDialog;

namespace XMLSigner.Library
{
    class HttpServer
    {
        private static HttpListener _httpListener;
        private static readonly Timer _timer = new Timer(100);
        //private static readonly System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();

        private static int _portNo;

        [Obsolete]
        private async void StartNonThreadedServerAsync()
        {
            _httpListener.Start();
            while (true)
            {
                await ServerHandlerAsync();
            }
        }

        [Obsolete]
        internal HttpServer(int port, bool isThreaded = true)
        {
            _portNo = port;
            if (_httpListener != null)  //Making things singleton
            {
                return;
                //throw new Exception("Already Initiated");
            }
            _httpListener = new HttpListener();
            Log.Print(LogLevel.High, "Started with Port " + port);
            _httpListener.Prefixes.Add("http://127.0.0.1:" + port + "/");

            if(isThreaded)
            {
                _timer.Elapsed += async (sender, e) => await ServerHandlerAsync();
                _ = StartServerAsync();
            }
            else
            {
                StartNonThreadedServerAsync();
            }
        }

        internal static void RestartServer()
        {
            StopServer();
            _ = StartServerAsync();
        }

        internal static void StopServer()
        {
            _timer.Stop();
            _httpListener.Stop();
        }

        internal static async Task StartServerAsync()
        {
            /*
            if(!Tsa.CheckIfLocalTimeIsOk())
            {
                MessageBox.Show("Please update your PC time to server time before signing!");
                System.Diagnostics.Process.Start("https://answers.microsoft.com/en-us/windows/forum/windows_10-other_settings/how-to-force-windows-10-time-to-synch-with-a-time/20f3b546-af38-42fb-a2d0-d4df13cc8f43");
            }
            */
            _httpListener.Start();
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                App.ShowTaskbarNotificationAfterUpload("Server Started in port: "+ _portNo, BalloonIcon.Info);
            }));
            _timer.Start();
        }

        [Obsolete]
        private async Task ServerHandlerAsync()
        {
            try {
                HttpListenerContext httpListenerContext = _httpListener.GetContext();
                httpListenerContext.Response.ContentType = "application/json";

                httpListenerContext.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                httpListenerContext.Response.AppendHeader("Access-Control-Allow-Methods", "*");
                httpListenerContext.Response.AppendHeader("Access-Control-Allow-Credentials", "true");
                httpListenerContext.Response.AppendHeader("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
                httpListenerContext.Response.AppendHeader("Access-Control-Max-Age", "1728000");

                //https://stackoverflow.com/questions/25437405/cors-access-for-httplistener

                httpListenerContext = await SendResponseAsync(httpListenerContext);
                httpListenerContext.Response.OutputStream.Flush();
                httpListenerContext.Response.OutputStream.Close();
                httpListenerContext.Response.Close();
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Obsolete]
        private async Task<HttpListenerContext> SendResponseAsync(HttpListenerContext httpListenerContext)
        {
            httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
            string returnJsonString;

            try
            {
                string uploadUrl = httpListenerContext.Request.QueryString["uploadUrl"];
                string downloadUrl = httpListenerContext.Request.QueryString["downloadUrl"];
                long id = long.Parse(httpListenerContext.Request.QueryString["id"]);
                string token = httpListenerContext.Request.QueryString["token"];
                long procedureSerial = long.Parse(httpListenerContext.Request.QueryString["procedureSerial"]);
                string reason = httpListenerContext.Request.QueryString["reason"];
                //long procedureSerial = -1, string reason = ""
                //bool urlCheckResult = ;
                if (await UrlChecker.CheckIfUrlApprovedAsync(downloadUrl, uploadUrl))
                {
                    //////////////////////////////////Download, sign, upload - Start
                    long? uploadId = await SignFileAsync(id, token, downloadUrl, uploadUrl, procedureSerial, reason);
                    //////////////////////////////////Download, sign, upload - End
                    returnJsonString = JsonConvert.SerializeObject(
                        Tuple.Create(
                                id,
                                token,
                                downloadUrl,
                                uploadUrl,
                                uploadId
                            )
                        );
                }
                else
                {
                    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                    returnJsonString = JsonConvert.SerializeObject(
                        Tuple.Create(
                                "Error",
                                "URL not Allowed"
                            )
                        );
                }
            }
            catch (Exception ex)
            {
                if (Environment.MachineName.ToString() == "Abrar")
                    returnJsonString = ex.Message.Trim();
                else
                    returnJsonString = "BCC-CA Is Running";
            }

            httpListenerContext.Response.ContentLength64 = Encoding.UTF8.GetByteCount(returnJsonString);
            using (Stream stream = httpListenerContext.Response.OutputStream)
            {
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    sw.Write(returnJsonString);
                }
            }
            Log.Print(LogLevel._Low, "Response JSON - " + returnJsonString);
            return httpListenerContext;
        }

        [Obsolete]
        private async Task<long?> SignFileAsync(long previouSigningFileId, string token, string downloadUrl, string uploadUrl, long procedureSerial = -1, string reason = "")
        {
            Tuple<XmlDocument, string> downloadedFile = await XmlSign.DownloadFileWithIdAsync(downloadUrl);

            //Open Dialog Popup
            if (downloadedFile == null)
            {
                return null;
            }
            try {
                bool isRejected = false;
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                     using (WysiwysDialog inputDialog = new WysiwysDialog(downloadedFile.Item1.OuterXml))
                     {
                         if (inputDialog.ShowDialog() == false)
                         {
                             isRejected = true;
                         }
                     }
                }));
                if(isRejected)
                {
                    return null;
                }
            }
            catch(Exception ex)
            {
                Log.Print(LogLevel.Critical, ex.ToString());
            }

            XmlDocument signedXmldDoc = XmlSign.GetSignedXMLDocument(downloadedFile.Item1, XmlSign.GetX509Certificate2FromDongle(), procedureSerial, reason);
            
            Tuple<XmlDocument, string> uploadFile = new Tuple<XmlDocument, string>(signedXmldDoc, downloadedFile.Item2);
            long? uploadFileID = await XmlSign.UploadFileAsync(uploadFile, token, previouSigningFileId, uploadUrl);
            Log.Print(LogLevel._Low, "Uploaded File ID - " + uploadFileID);
            try
            {
                if (uploadFileID != null)
                {
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        App.ShowTaskbarNotificationAfterUpload("Signed XML File Uploaded Successfully");
                    }));
                }
            }
            catch(Exception ex)
            {
                Log.Print(LogLevel.Critical, ex.Message.ToString());
            }
            /*
            //Verify - No Need
            bool? ifSignVerified = XmlSign.VerifyAllSign(signedXmldDoc);
            if (ifSignVerified == true)
            {
                MessageBox.Show("Verified");
            }
            else if (ifSignVerified == false)
            {
                MessageBox.Show("Failed Verification");
            }
            else
            {
                MessageBox.Show("File Has No Sign");
            }*/
            return uploadFileID;
        }
    }
}
