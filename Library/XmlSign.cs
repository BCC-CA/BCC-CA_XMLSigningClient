using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using DataObject = System.Security.Cryptography.Xml.DataObject;

namespace XMLSigner.Library
{
    class XmlSign
    {
        public static bool CheckIfDocumentPreviouslySigned(XmlDocument xmlDocument)
        {
            int signCount = DocumentSignCount(xmlDocument);
            if (signCount > 0) {
                return true;
            } else {
                return false;
            }
        }

        public static int DocumentSignCount(XmlDocument xmlDocument)
        {
            XmlNodeList nodeList = xmlDocument.GetElementsByTagName("Signature");
            return nodeList.Count;
        }

        public static bool? VerifyAllSign(XmlDocument xmlDocument)
        {
            if (!CheckIfDocumentPreviouslySigned(xmlDocument))
                return null;    //File has no sign
            while(CheckIfDocumentPreviouslySigned(xmlDocument)) {
                bool? lastSignVerificationStatus = VerifyLastSign(xmlDocument);
                if (lastSignVerificationStatus == false) {
                    return false;   //Not counting all sign, find first invalid sign and tell that file is invalid
                }
                //Update xmlDocument by removing last sign tag
                xmlDocument = RemoveLastSign(xmlDocument);
            }
            return true;
        }

        private static XmlDocument RemoveLastSign(XmlDocument xmlDocument)
        {
            //nodes[i].ParentNode.RemoveChild(nodes[i]);
            XmlNodeList signList = xmlDocument.GetElementsByTagName("Signature");
            int indexToRemove = signList.Count - 1;
            signList[indexToRemove].ParentNode.RemoveChild(signList[indexToRemove]);
            return xmlDocument;
        }

        public static bool? VerifyLastSign(XmlDocument xmlDocument)
        {
            if (!CheckIfDocumentPreviouslySigned(xmlDocument)) {
                return null;    //File has no sign
            }
            if (VerifySignedXmlLastSignWithoutCertificateVerification(xmlDocument)) {
                //return VerifyMetaDataObjectSignature(xmlDocument);
                return true;
            }
            else {
                return false;
            }
        }

        //Should get data from XmlDocument, not file
        private static bool VerifySignedXmlLastSignWithoutCertificateVerification(XmlDocument xmlDocument)
        {
            try {
                // Create a new SignedXml object and pass it
                SignedXml signedXml = new SignedXml(xmlDocument);

                // Find the "Signature" node and create a new
                // XmlNodeList object.
                XmlNodeList nodeList = xmlDocument.GetElementsByTagName("Signature");

                // Load the signature node.
                signedXml.LoadXml((XmlElement)nodeList[nodeList.Count-1]);

                AsymmetricAlgorithm key;
                var a = signedXml.CheckSignatureReturningKey(out key);
                return a;
                //return signedXml.CheckSignature(key);
                //return signedXml.CheckSignature(certificate, true);
            } catch (Exception exception) {
                Console.Write("Error: " + exception);
                throw exception;
            }
        }

        //tutorial - https://www.asptricks.net/2015/09/sign-xmldocument-with-x509certificate2.html
        public static XmlDocument GetSignedXMLDocument(XmlDocument xmlDocument, X509Certificate2 certificate)
        {
            /*
            //Check certificate velidity from server and certificate varification here first - not implemented yet
            if(!certificate.Verify()) {
                return null;    //Certificate Not Verified
            }
            */
            //Before signing, should check if current document sign is valid or not, if current document is invalid, then new sign should not be added - not implemented yet, but should be
            if(CheckIfDocumentPreviouslySigned(xmlDocument)) {
                bool? isLastSignVerified = VerifyLastSign(xmlDocument);
                if (isLastSignVerified == false) {
                    Console.WriteLine("File Tempered after last sign !!");
                    return null;    //Last Sign Not Verified
                }
            }
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
                signedXml.AddObject(CreateMetaDataObject(certificate, GetNetworkTime()));
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

        public static DataObject CreateMetaDataObject(X509Certificate2 certificate, DateTime signingTimeFromServer)
        {
            //Should add a sign with it also so that it can be proven that data is not tempered and should add verifire for it also
            DataObject dataObject = new DataObject();
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode root = xmlDoc.AppendChild(xmlDoc.CreateElement("meta", "meta-data"));

            XmlNode child1 = root.AppendChild(xmlDoc.CreateElement("unique", "unique-id"));
            XmlAttribute childAtt1 = child1.Attributes.Append(xmlDoc.CreateAttribute("server-unique"));
            childAtt1.InnerText = certificate.Thumbprint.ToString();
            child1.InnerText = certificate.Subject.ToString();

            XmlNode child2 = root.AppendChild(xmlDoc.CreateElement("time", "signing-time"));
            XmlAttribute childAtt2 = child2.Attributes.Append(xmlDoc.CreateAttribute("local"));
            childAtt2.InnerText = DateTime.Now.ToString();          //Local Time
            child2.InnerText = signingTimeFromServer.ToString();    //Server Time

            dataObject.Data = xmlDoc.ChildNodes;
            dataObject.Id = certificate.SerialNumber.ToString();
            return dataObject;
        }

        private static bool VerifyMetaDataObjectSignature(XmlDocument xmlDocument)
        {
            throw new NotImplementedException();
        }

        public static X509Certificate2 GetX509Certificate2FromDongle()
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
