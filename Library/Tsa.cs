using Org.BouncyCastle.Math;
using Org.BouncyCastle.Tsp;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Xml;

namespace XMLSigner.Library
{
    class Tsa
    {
        //Rfc3161TimestampRequest request;// = Rfc3161TimestampRequest.CreateFromHash(byte[] , HashAlgorithmName.SHA1);
        private static string stampURI;

        internal Tsa(string tsa = "http://timestamp.globalsign.com/scripts/timstamp.dll")
        {
            stampURI = tsa;
            //"http://www.cryptopro.ru/tsp/tsp.srf"
            ///request = Rfc3161TimestampRequest.CreateFromHash(data, HashAlgorithmName.SHA1);
        }

        internal static async Task<bool> TrySettingNewTsaAsync(string newTsaUrl)
        {
            try
            {
                bool ifNewTsaOk = await CheckIfValidTsaAsync(newTsaUrl);
                if (ifNewTsaOk)
                {
                    stampURI = newTsaUrl;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        internal static bool TrySettingNewTsa(string newTsaUrl)
        {
            try
            {
                bool ifNewTsaOk = CheckIfValidTsa(newTsaUrl);
                if (ifNewTsaOk)
                {
                    stampURI = newTsaUrl;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private static async Task<bool> CheckIfValidTsaAsync(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Timeout = 15000;    //15 second
            request.Method = "HEAD";
            HttpWebResponse response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(
                                        request.BeginGetResponse,
                                        request.EndGetResponse,
                                        null
                                        );
            return response.StatusCode == HttpStatusCode.OK;
        }

        private static bool CheckIfValidTsa(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 15000;    //15 second
            request.Method = "HEAD";
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException)
            {
                return false;
            }
            //throw new NotImplementedException();
        }

        private TimeStampResponse GetSignedHashFromTsa(byte[] hash)
        {
            /*
            if(!CheckIfUrlReachable(stampURI))
            {
                return null;
            }
            */

            TimeStampRequestGenerator reqGen = new TimeStampRequestGenerator();
            // Dummy request
            TimeStampRequest request = reqGen.Generate(
                        TspAlgorithms.Sha1,
                        hash,
                        BigInteger.ValueOf(100)
                    );
            byte[] reqData = request.GetEncoded();

            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(stampURI);
            httpReq.Method = "POST";
            httpReq.ContentType = "application/timestamp-query";
            httpReq.ContentLength = reqData.Length;

            //Configure Timeout
            //httpReq.Timeout = 5000;
            //httpReq.ReadWriteTimeout = 32000;


            // Write the request content
            Stream reqStream = httpReq.GetRequestStream();
            reqStream.Write(reqData, 0, reqData.Length);
            reqStream.Close();

            HttpWebResponse httpResp = (HttpWebResponse)httpReq.GetResponse();

            // Read the response
            Stream respStream = new BufferedStream(httpResp.GetResponseStream());
            TimeStampResponse response = new TimeStampResponse(respStream);
            respStream.Close();

            return response;
        }

        private static bool ValidateTimestamp(TimeStampResponse tr, byte[] hash)
        {
            try
            {
                TimeStampRequestGenerator reqGen = new TimeStampRequestGenerator();
                TimeStampRequest request = reqGen.Generate(
                        TspAlgorithms.Sha1,
                        hash,
                        BigInteger.ValueOf(100)
                    );

                tr.Validate(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return tr.GetFailInfo() == null;
        }

        internal static bool ValidateTimestamp(XmlDocument xmlDocument, string tsaSignedHashString)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(tsaSignedHashString);
                TimeStampResponse timeStampResponse = new TimeStampResponse(bytes);
                byte[] hash = GetXmlHashByteStream(xmlDocument);
                return ValidateTimestamp(timeStampResponse, hash);
            }
            catch(Exception)
            {
                return false;
            }
        }

        internal static DateTime? GetTsaTimeFromSignedHash(string tsaSignedHashString)
        {
            try {
                byte[] bytes = Convert.FromBase64String(tsaSignedHashString);
                TimeStampResponse timeStampResponse = new TimeStampResponse(bytes);
                return timeStampResponse.TimeStampToken.TimeStampInfo.GenTime;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw ex;
                return null;
            }
        }

        internal string GetSignedHashFromTsa(XmlDocument xmlDxocument)
        {
            byte[] hash = GetXmlHashByteStream(xmlDxocument);
            TimeStampResponse timeStampResponse = GetSignedHashFromTsa(hash);
            byte[] signedEncodedByteStream = timeStampResponse.GetEncoded();
            return Convert.ToBase64String(signedEncodedByteStream);
        }

        private static string GetXmlHash(XmlDocument xmlDoc)
        {
            return Convert.ToBase64String(GetXmlHashByteStream(xmlDoc));
        }

        private static byte[] GetXmlHashByteStream(XmlDocument xmlDoc)
        {
            byte[] hash;
            XmlDsigC14NTransform transformer = new XmlDsigC14NTransform();
            transformer.LoadInput(xmlDoc);
            using (Stream stream = (Stream)transformer.GetOutput(typeof(Stream)))
            {
                SHA1 sha1 = SHA1.Create();
                hash = sha1.ComputeHash(stream);
                stream.Close();
            }
            return hash;
        }
    }
}
