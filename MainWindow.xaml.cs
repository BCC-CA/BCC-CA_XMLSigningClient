using RestSharp;
using System;
using System.IO;
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
        private static readonly String baseUrl = @"http://localhost:8080/file";

        public MainWindow()
        {
            InitializeComponent();
        }

        [Obsolete]
        private async void SignButtonClicked(object sender, RoutedEventArgs e)
        {
            //http://localhost:8080/file/download/831286aaff629434d9d4ddcbec679f8ecbe4afb0631c73e60d0968fbea2cccbea63aa8afb0a5bef528a3e2433c0d9994713da42fdadd62f5fab19d0365520e3cbig.xml
            String serverFileName = ServerFileName.Text.Trim();
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

            //Sign the File with key and save that to - fileLocation
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            // If you get compilation error after on X509Certificate2UI class, then Project->Add Reference -> Add System.Security 
            X509Certificate2Collection selectedCert = X509Certificate2UI.SelectFromCollection(store.Certificates, null, null, X509SelectionFlag.SingleSelection);
            X509Certificate2 cert = selectedCert[0];
            XmlDocument xmlDoc = GetEntryXmlDoc(response);

            //SignedXml signedPartOfXML = SignXml1(GetEntryXmlDoc(response), cert);
            String fileLocation = AppDomain.CurrentDomain.BaseDirectory + "signed.xml";
            //SaveSignedXmlToFile(xmlDoc, signedPartOfXML, fileLocation);

            SignXml2(GetEntryXmlDoc(response), cert).Save(fileLocation);

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

        private static XmlDocument SignXml2(XmlDocument xmlDoc, X509Certificate2 certificate)
        {
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

        private static SignedXml SignXml1(XmlDocument doc, X509Certificate2 cert)
        {
            SignedXml signedXml = new SignedXml(doc);
            signedXml.SigningKey = cert.PrivateKey;

            // Create a reference to be signed.
            Reference reference = new Reference();
            reference.Uri = "";

            // Add an enveloped transformation to the reference.            
            XmlDsigEnvelopedSignatureTransform env =
               new XmlDsigEnvelopedSignatureTransform(true);
            reference.AddTransform(env);

            //canonicalize
            XmlDsigC14NTransform c14t = new XmlDsigC14NTransform();
            reference.AddTransform(c14t);

            KeyInfo keyInfo = new KeyInfo();
            KeyInfoX509Data keyInfoData = new KeyInfoX509Data(cert);
            KeyInfoName kin = new KeyInfoName();
            kin.Value = "Public key of certificate";
            RSACryptoServiceProvider rsaprovider = (RSACryptoServiceProvider)cert.GetRSAPrivateKey();
            RSAKeyValue rkv = new RSAKeyValue(rsaprovider);
            keyInfo.AddClause(kin);
            keyInfo.AddClause(rkv);
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Compute the signature.
            signedXml.ComputeSignature();

            return signedXml;
        }
    }
}
