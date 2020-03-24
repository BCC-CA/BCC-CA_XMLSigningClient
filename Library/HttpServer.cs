﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace XMLSigner.Library
{
    class HttpServer
    {
        HttpListener httpListener;
        public HttpServer(int port)
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://127.0.0.1:" + port + "/");

            _ = StartServerAsync();
            //StopServer();
        }

        internal void StopServer()
        {
            httpListener.Stop();
        }

        private async Task StartServerAsync()
        {
            httpListener.Start();
            while (true)
            {
                await ServerHandlerAsync();
            }
        }

        private async Task ServerHandlerAsync()
        {
            try {
                HttpListenerContext httpListenerContext = httpListener.GetContext();
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

            ////////////
            try {
                string uploadUrl = httpListenerContext.Request.QueryString["uploadUrl"];
                string downloadUrl = httpListenerContext.Request.QueryString["downloadUrl"];
                long id = long.Parse(httpListenerContext.Request.QueryString["id"]);
                string token = httpListenerContext.Request.QueryString["token"];
                long procedureSerial = long.Parse(httpListenerContext.Request.QueryString["procedureSerial"]);
                string reason = httpListenerContext.Request.QueryString["reason"];
                //long procedureSerial = -1, string reason = ""
                //////////////////////////////////Download, sign, upload - Start
                long? uploadId = await SignFileAsync(id, token, downloadUrl, uploadUrl, procedureSerial, reason);
                //////////////////////////////////Download, sign, upload - End

                var returnTuple = Tuple.Create(id, token, downloadUrl, uploadUrl, uploadId);
                returnJsonString = JsonConvert.SerializeObject(returnTuple);
            }
            catch(Exception ex)
            {
                if (Environment.MachineName.ToString() == "Abrar")
                    returnJsonString = ex.Message.Trim();
                else
                    returnJsonString = "BCC-CA Is Running";
            }
            ////////////

            httpListenerContext.Response.ContentLength64 = Encoding.UTF8.GetByteCount(returnJsonString);
            using (Stream stream = httpListenerContext.Response.OutputStream)
            {
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    sw.Write(returnJsonString);
                }
            }
            return httpListenerContext;
        }

        [Obsolete]
        private async Task<long?> SignFileAsync(long previouSigningFileId, string token, string downloadUrl, string uploadUrl, long procedureSerial = -1, string reason = "")
        {
            Tuple<XmlDocument, string> downloadedFile = await XmlSign.DownloadFileWithIdAsync(downloadUrl);
            XmlDocument signedXmldDoc = XmlSign.GetSignedXMLDocument(downloadedFile.Item1, XmlSign.GetX509Certificate2FromDongle(), procedureSerial, reason);
            Tuple<XmlDocument, string> uploadFile = new Tuple<XmlDocument, string>(signedXmldDoc, downloadedFile.Item2);
            long? uploadFileID = await XmlSign.UploadFileAsync(uploadFile, token, previouSigningFileId, uploadUrl);

            if (uploadFileID != null)
            {
                //MessageBox.Show("Sign File Uploaded");
                App.ShowTaskbarNotificationAfterUpload("Signed XML File Uploaded Successfully");
                return uploadFileID;
            }
            else return null;
            /*//Verify
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
        }
    }
}
