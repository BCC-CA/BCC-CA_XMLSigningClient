using RestSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using DataObject = System.Security.Cryptography.Xml.DataObject;

namespace XMLSigner.Library
{
    class XmlSign
    {
        internal static Icon BytesToIcon(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return new Icon(ms);
            }
        }

        [Obsolete]
        internal static async Task<Tuple<XmlDocument, string>> DownloadFileWithIdAsync(string downloadUrl)
        {
            RestClient client = new RestClient(downloadUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = await client.ExecuteTaskAsync(request);

            if(response.StatusCode == HttpStatusCode.OK)
            {
                string description = response.Headers.ToList()
                                .Find(x => x.Name == "Content-Disposition")
                                .Value.ToString();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(response.Content);
                string fileNameContainerString = description.Split(";").ToList()
                                                    .Find(x => x.Contains("filename="))
                                                    .Split("=").ToList()
                                                    .Find(x => !x.Contains("filename"))
                                                    .Trim();
                return new Tuple<XmlDocument, string>(doc, fileNameContainerString);
            }
            else
            {
                return null;
            }
        }

        [Obsolete]
        internal static async Task<long?> UploadFileAsync(Tuple<XmlDocument, string> uploadFileToupleData, string token, long? previousSignedFileId, string uploadUrl)
        {
            RestClient client = new RestClient(uploadUrl);
            RestRequest uploadRequest = new RestRequest("", Method.POST);
            if(previousSignedFileId != null)
            {
                uploadRequest.AddParameter("previousFileId", previousSignedFileId);
                uploadRequest.AddParameter("token", token);
            }
            uploadRequest.AddFile("xmlFile", Encoding.UTF8.GetBytes(uploadFileToupleData.Item1.OuterXml), uploadFileToupleData.Item2);

            IRestResponse uploadResponse = await client.ExecutePostTaskAsync(uploadRequest);
            if (uploadResponse.StatusCode.CompareTo(HttpStatusCode.OK) == 0)
            {
                return long.Parse(uploadResponse.Content);
            }
            else
            {
                return null;
            }
        }

        private static bool CheckIfDocumentPreviouslySigned(XmlDocument xmlDocument)
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

        internal static bool? VerifyAllSign(XmlDocument xmlDocument)
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

        private static bool? VerifyLastSign(XmlDocument xmlDocument)
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

                //////////////////////////////////Extract key - Start
                X509Certificate2 x509 = GetLastSignerCertificate(xmlDocument);
                //////////////////////////////////Extract key - End

                AsymmetricAlgorithm key;
                bool signatureCheckStatus = signedXml.CheckSignatureReturningKey(out key);
                if(signatureCheckStatus) {
                    XmlElement metaElement = (XmlElement)nodeList[nodeList.Count - 1].LastChild;
                    return VerifyMetaDataObjectSignature(metaElement, key);
                } else {
                    return false;
                }
                //return signedXml.CheckSignature(key);
                //return signedXml.CheckSignature(certificate, true);
            } catch (Exception exception) {
                Console.Write("Error: " + exception);
                throw exception;
            }
        }

        private static X509Certificate2 GetLastSignerCertificate(XmlDocument xmlDocument)
        {
            if (!CheckIfDocumentPreviouslySigned(xmlDocument))
            {
                return null;
            }
            XmlDocument document = new XmlDocument();

            // Find the "Signature" node and create a new
            // XmlNodeList object.
            XmlNodeList nodeList = xmlDocument.GetElementsByTagName("Signature");

            // Load the signature node.

            document.LoadXml(((XmlElement)nodeList[nodeList.Count - 1]).OuterXml);
            string certString = document.GetElementsByTagName("X509Data")[0].InnerText;
            /*...Decode text in cert here (may need to use Encoding, Base64, UrlEncode, etc) ending with 'data' being a byte array...*/
            return new X509Certificate2(Encoding.ASCII.GetBytes(certString));
        }

        private static bool VerifyMetaDataObjectSignature(XmlElement metaXmlElement, AsymmetricAlgorithm ExtractedKey)
        {
            return true;
            throw new NotImplementedException();
        }

        //tutorial - https://www.asptricks.net/2015/09/sign-xmldocument-with-x509certificate2.html
        internal static XmlDocument GetSignedXMLDocument(XmlDocument xmlDocument, X509Certificate2 certificate, long procedureSerial = -1, string reason = "")
        {
            /*
            //Check certificate velidity from server and certificate varification here first - not implemented yet
            if(!certificate.Verify()) {
                return null;    //Certificate Not Verified
            }
            */

            //Check if local time is OK
            if(!CheckIfLocalTimeIsOk()) {
                MessageBox.Show("PC Time is not updated !!");
                return null;    //Last Sign Not Verified
            }

            //Before signing, should check if current document sign is valid or not, if current document is invalid, then new sign should not be added - not implemented yet, but should be
            if (CheckIfDocumentPreviouslySigned(xmlDocument))
            {
                bool? isLastSignVerified = VerifyLastSign(xmlDocument);
                if (isLastSignVerified == false)
                {
                    MessageBox.Show("File Tempered after last sign !!");
                    return null;    //Last Sign Not Verified
                }
            }
            //Then sign the xml
            try
            {
                //MessageBox.Show(certificate.Subject);
                SignedXml signedXml = new SignedXml(xmlDocument);
                signedXml.SigningKey = certificate.PrivateKey;

                // Create a reference to be signed
                Reference reference = new Reference();
                /////////////////////
                reference.Uri = "";//"#" + procedureSerial;
                //reference.Type = reason;
                //reference.Id = DateTime.UtcNow.Ticks.ToString();
                reference.Id = Base64EncodedCurrentTime();
                //reference.TransformChain = ;
                /////////////////////
                // Add an enveloped transformation to the reference.            
                XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform(true);
                reference.AddTransform(env);

                // Add the reference to the SignedXml object.
                signedXml.AddReference(reference);

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

                //////////////////////////////////////////Add Other Data as we need////
                // Add the data object to the signature.
                //CreateMetaDataObject("Name", GetNetworkTime());
                signedXml.AddObject(CreateMetaDataObject(procedureSerial, reason));
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
                MessageBox.Show("Internal System Error");
                throw exception;
            }
            return xmlDocument;
        }

        private static string Base64EncodedCurrentTime()
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString());
            return Convert.ToBase64String(plainTextBytes);
        }

        private static DateTime Base64DecodTime(string encodedTimeString)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(encodedTimeString);
            return DateTime.Parse(Encoding.UTF8.GetString(base64EncodedBytes));
        }

        private static DataObject CreateMetaDataObject(long uniqueId, string reason)
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.import?view=netframework-4.8
            //Should add a sign with it also so that it can be proven that data is not tempered and should add verifire for it also
            DataObject dataObject = new DataObject();
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode root = xmlDoc.AppendChild(xmlDoc.CreateElement("meta", "meta-data"));

            XmlNode child1 = root.AppendChild(xmlDoc.CreateElement("unique", "unique-id"));
            XmlAttribute childAtt1 = child1.Attributes.Append(xmlDoc.CreateAttribute("server-unique"));
            //childAtt1.InnerText = uniqueId.ToString();
            child1.InnerText = uniqueId.ToString();

            XmlNode child2 = root.AppendChild(xmlDoc.CreateElement("signing-reason", "signing-local-time"));
            XmlAttribute childAtt2 = child2.Attributes.Append(xmlDoc.CreateAttribute("local-time"));
            childAtt2.InnerText = DateTime.Now.ToString();          //Local Time
            child2.InnerText = reason;    //Server Time

            //Sign Meta Data and store without key
            //xmlDoc = GetSignedMetaData(xmlDoc, certificate);

            dataObject.Data = xmlDoc.ChildNodes;
            dataObject.Id = new Random().Next().ToString();
            return dataObject;
        }

        private static XmlDocument GetSignedMetaData(XmlDocument xmlDocument, X509Certificate2 certificate)
        {
            SignedXml signedXml = new SignedXml(xmlDocument);
            signedXml.SigningKey = certificate.PrivateKey;

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

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature and save 
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            xmlDocument.DocumentElement.AppendChild(
                xmlDocument.ImportNode(xmlDigitalSignature, true));

            return xmlDocument;
        }

        internal static X509Certificate2 GetX509Certificate2FromDongle()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            // If you get compilation error after on X509Certificate2UI class, then Project->Add Reference -> Add System.Security 
            X509Certificate2Collection selectedCert = X509Certificate2UI.SelectFromCollection(store.Certificates, null, null, X509SelectionFlag.SingleSelection);
            return selectedCert[0];
        }

        internal static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private static DateTime GetNetworkTime()
        {
#if DEBUG
            if (!CheckForInternetConnection())
            {
                return DateTime.UtcNow;
            }
#endif
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

        private static bool CheckIfLocalTimeIsOk(int allowedMaxMinuiteDiff = 5)
        {
            DateTime ntpTime = GetNetworkTime();
            DateTime localTime = DateTime.UtcNow;
            TimeSpan timeDiff = ntpTime - localTime;
            if (Math.Abs(timeDiff.TotalMinutes) <= allowedMaxMinuiteDiff)
                return true;
            else
                return false;
        }
    }
}
