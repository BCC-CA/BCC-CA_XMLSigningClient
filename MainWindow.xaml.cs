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
using DataObject = System.Security.Cryptography.Xml.DataObject;

namespace XMLSigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private static readonly String baseUrl = Properties.Resources.ApiUrl;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                //ServerFileName.Text = File.ReadAllText(openFileDialog.FileName);
                SelectedFileName.Text = openFileDialog.FileName;
            }
        }

        private void SignButtonClicked(object sender, RoutedEventArgs e)
        {
            //http://localhost:8080/file/download/831286aaff629434d9d4ddcbec679f8ecbe4afb0631c73e60d0968fbea2cccbea63aa8afb0a5bef528a3e2433c0d9994713da42fdadd62f5fab19d0365520e3cbig.xml
            String fileName = SelectedFileName.Text.Trim();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = false;/////////////////////////////////
            xmlDoc.Load(fileName);
            X509Certificate2 cert = GetX509Certificate2();
            GetSignedXMLDocument(xmlDoc, cert).Save(fileName + "_signed.xml");

            //Verify
            bool result = VerifyXmlFileWithoutCertificateVerification(fileName + "_signed.xml", cert);
            if(result) {
                MessageBox.Show("Verified");
            } else {
                MessageBox.Show("Failed Verification");
            }
        }

        private bool VerifyXmlFileWithoutCertificateVerification(String fileName, X509Certificate2 certificate)
        {
            try
            {
                // Create a new XML document.
                XmlDocument xmlDocument = new XmlDocument();

                // Format using white spaces.
                xmlDocument.PreserveWhitespace = false;

                // Load the passed XML file into the document. 
                xmlDocument.Load(fileName);

                // Create a new SignedXml object and pass it
                // the XML document class.
                SignedXml signedXml = new SignedXml(xmlDocument);

                // Find the "Signature" node and create a new
                // XmlNodeList object.
                XmlNodeList nodeList = xmlDocument.GetElementsByTagName("Signature");

                // Load the signature node.
                signedXml.LoadXml((XmlElement)nodeList[0]);

                // Check the signature and return the result.
                return signedXml.CheckSignature(certificate, true);
            }
            catch (Exception exc)
            {
                Console.Write("Error:" + exc);
                return false;
            }
        }

        //tutorial - https://www.asptricks.net/2015/09/sign-xmldocument-with-x509certificate2.html
        private XmlDocument GetSignedXMLDocument(XmlDocument xmlDocument, X509Certificate2 certificate)
        {
            //Check certificate velidity from server and certificate varification here first - not implemented yet

            //Then sign the xml
            try
            {
                //MessageBox.Show(certificate.Subject);
                SignedXml signedXml = new SignedXml(xmlDocument);
                signedXml.SigningKey = certificate.PrivateKey;

                // Create a reference to be signed.
                Reference reference = new Reference();
                reference.Uri = "";

                // Add an enveloped transformation to the reference.            
                XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform(true);
                reference.AddTransform(env);

                //canonicalize
                XmlDsigC14NTransform c14t = new XmlDsigC14NTransform();
                reference.AddTransform(c14t);

                KeyInfo keyInfo = new KeyInfo();
                KeyInfoX509Data keyInfoData = new KeyInfoX509Data(certificate);
                KeyInfoName kin = new KeyInfoName();
                //kin.Value = "Public key of certificate";
                kin.Value = certificate.FriendlyName;

                RSA rsa = (RSA)certificate.PublicKey.Key;
                RSAKeyValue rkv = new RSAKeyValue(rsa);
                keyInfo.AddClause(rkv);

                keyInfo.AddClause(kin);
                keyInfo.AddClause(keyInfoData);
                signedXml.KeyInfo = keyInfo;

                // Add the reference to the SignedXml object.
                signedXml.AddReference(reference);

                //////////////////////////////////////////Add Other Data as we need////
                // Add the data object to the signature.
                //CreateMetaDataObject("Name", GetNetworkTime());
                signedXml.AddObject(CreateMetaDataObject("Name", GetNetworkTime()));
                ///////////////////////////////////////////////////////////////////////
                // Compute the signature.
                signedXml.ComputeSignature();

                // Get the XML representation of the signature and save 
                // it to an XmlElement object.
                XmlElement xmlDigitalSignature = signedXml.GetXml();

                xmlDocument.DocumentElement.AppendChild(
                        xmlDocument.ImportNode(xmlDigitalSignature, true)
                    );
                /////////////////////
            } catch (Exception exception) {
                throw exception;
            }
            return xmlDocument;
        }

        private DataObject CreateMetaDataObject(string signerUniqueId, DateTime signingTimeFromServer)
        {
            DataObject dataObject = new DataObject();
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode root = xmlDoc.AppendChild(xmlDoc.CreateElement("meta", "meta-data"));

            XmlNode child1 = root.AppendChild(xmlDoc.CreateElement("unique", "unique-id"));
            XmlAttribute childAtt1 = child1.Attributes.Append(xmlDoc.CreateAttribute("server-unique"));
            childAtt1.InnerText = signerUniqueId;
            child1.InnerText = signerUniqueId + " - test";

            XmlNode child2 = root.AppendChild(xmlDoc.CreateElement("time", "signing-time"));
            XmlAttribute childAtt2 = child2.Attributes.Append(xmlDoc.CreateAttribute("local"));
            childAtt2.InnerText = DateTime.UtcNow.ToString();
            child2.InnerText = signingTimeFromServer.ToString();

            dataObject.Data = xmlDoc.ChildNodes;
            dataObject.Id = "MyObjectId";
            return dataObject;
        }

        private X509Certificate2 GetX509Certificate2()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            // If you get compilation error after on X509Certificate2UI class, then Project->Add Reference -> Add System.Security 
            X509Certificate2Collection selectedCert = X509Certificate2UI.SelectFromCollection(store.Certificates, null, null, X509SelectionFlag.SingleSelection);
            return selectedCert[0];
        }

        public static DateTime GetNetworkTime()
        {
            //Should check time server by certificate, not added now
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

            IPAddress[] addresses = Dns.GetHostEntry(Properties.Resources.NtpServerUrl).AddressList;
            IPEndPoint ipEndPoint = new IPEndPoint(addresses[0], 123);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
            ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

            ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            DateTime networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }
   }
}
