using Microsoft.Win32;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Windows;
using System.Xml;

namespace XMLSigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private static readonly String baseUrl = @"http://localhost:8080/file";
        private static readonly String NtpServerUrl = @"time.windows.com";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SignButtonClicked(object sender, RoutedEventArgs e)
        {
            //http://localhost:8080/file/download/831286aaff629434d9d4ddcbec679f8ecbe4afb0631c73e60d0968fbea2cccbea63aa8afb0a5bef528a3e2433c0d9994713da42fdadd62f5fab19d0365520e3cbig.xml
            String serverFileName = ServerFileName.Text.Trim();
            /*
            if (serverFileName.Length < 128)
            {
                MessageBox.Show("File Name Invalid");
                return;
            }

            //Download file from API
            RestClient client = new RestClient(baseUrl);
            //var request = new RestRequest("/resource/5", Method.GET);
            RestRequest request = new RestRequest("download/" + serverFileName);
            byte[] response = client.DownloadData(request);
            XmlDocument xmlDoc = GetEntryXmlDoc(response);
            */
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(serverFileName);


            //Sign the File with key and save that to - fileLocation
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            // If you get compilation error after on X509Certificate2UI class, then Project->Add Reference -> Add System.Security 
            X509Certificate2Collection selectedCert = X509Certificate2UI.SelectFromCollection(store.Certificates, null, null, X509SelectionFlag.SingleSelection);
            X509Certificate2 cert = selectedCert[0];

            //////////////////
            ///Certificate Verification

            //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.verify?view=netframework-4.8
            /*if (cert.Verify()) {
                MessageBox.Show("Cert Verified");
            } else {
                MessageBox.Show("Cert Not Verified");
            }*/

            //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.notafter?view=netframework-4.8
            DateTime currentTime = GetNetworkTime();
            /*if(currentTime> cert.NotAfter) {
                MessageBox.Show("Expired");
                return;
            } else {
                MessageBox.Show("Valid");
            }*/
            //////////////////

            SignXml(xmlDoc, cert).Save(serverFileName + "_signed.xml");

            XmlDocument xmlDocTrue = new XmlDocument();
            xmlDocTrue.PreserveWhitespace = true;
            xmlDocTrue.Load(serverFileName);
            SignXml(xmlDocTrue, cert).Save(serverFileName + "_true.xml");

            XmlDocument xmlDocFalse = new XmlDocument();
            xmlDocFalse.PreserveWhitespace = false;
            xmlDocFalse.Load(serverFileName);
            SignXml(xmlDocFalse, cert).Save(serverFileName + "_false.xml");

            XmlDocument signedXmlDoc = new XmlDocument();
            signedXmlDoc.Load(serverFileName + "_false.xml");
            SignedXml signedXml = new SignedXml(signedXmlDoc);
            if(signedXml.CheckSignature()) {
                MessageBox.Show("Valid Signature");
            } else {
                MessageBox.Show("Invalid Sig");
            }
            /*
            //SignedXml signedPartOfXML = SignXml1(GetEntryXmlDoc(response), cert);
            String fileLocation = AppDomain.CurrentDomain.BaseDirectory + "signed.xml";
            //SaveSignedXmlToFile(xmlDoc, signedPartOfXML, fileLocation);

            SignXml2(xmlDoc, cert).Save(fileLocation);

            
            //Upload signed file with API
            RestRequest uploadRequest = new RestRequest("upload_signed");
            uploadRequest.AddParameter("file_name", serverFileName);
            uploadRequest.AddParameter("file", "upload file");
            uploadRequest.AddFile("file", fileLocation);

            IRestResponse uploadResponse = await client.ExecutePostTaskAsync(uploadRequest);
            if (uploadResponse.StatusCode.CompareTo(System.Net.HttpStatusCode.OK) == 0) {
                MessageBox.Show("Uploaded");
            } else {
                MessageBox.Show("Upload Failed");
            }
            */
        }

        public static DateTime GetNetworkTime()
        {
            //Should check time server by certificate, not added now
            var ntpData = new byte[48];
            ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(NtpServerUrl).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
            ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }

        private void SaveSignedXmlToFile(XmlDocument xmlDoc, SignedXml signedXml, string fileLocation)
        {
            // Get the XML representation of the signature and save
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));

            if (xmlDoc.FirstChild is XmlDeclaration)
            {
                xmlDoc.RemoveChild(xmlDoc.FirstChild);
            }
            xmlDoc.Save(fileLocation);
        }

        public XmlDocument GetEntryXmlDoc(byte[] Bytes)
        {
            XmlDocument xmlDoc = new XmlDocument();
            using (MemoryStream ms = new MemoryStream(Bytes))
            {
                xmlDoc.Load(ms);
            }
            return xmlDoc;
        }

        private static XmlDocument SignXml(XmlDocument xmlDoc, X509Certificate2 certificate)
        {
            //https://stackoverflow.com/questions/23394654/signing-a-xml-document-with-x509-certificate
            //xmlDoc.PreserveWhitespace = false;

            RSA key = certificate.GetRSAPrivateKey();
            // Check arguments.
            if (xmlDoc == null)
                throw new ArgumentException("xmlDoc");
            if (key == null)
                throw new ArgumentException("Key");

            // Create a SignedXml object.
            SignedXml signedXml = new SignedXml(xmlDoc);

            // Add the key to the SignedXml document.
            signedXml.SigningKey = key;

            KeyInfo keyInfo = new KeyInfo();
            KeyInfoX509Data keyInfoData = new KeyInfoX509Data(certificate);

            //KeyInfoX509Data keyInfoData = new KeyInfoX509Data(Key);
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;

            // Create a reference to be signed.
            Reference reference = new Reference();
            reference.Uri = "";

            // Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature and save
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));
            return xmlDoc;
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true) {
                //ServerFileName.Text = File.ReadAllText(openFileDialog.FileName);
                ServerFileName.Text = openFileDialog.FileName;
            }
        }
    }
}
