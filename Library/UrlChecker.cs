using RestSharp;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace XMLSigner.Library
{
    class UrlChecker
    {
        [Obsolete]
        internal static async Task<bool> CheckIfUrlApprovedAsync(string downloadUrl, string uploadUrl)
        {
            if ((new Uri(downloadUrl, UriKind.Absolute)) != (new Uri(uploadUrl, UriKind.Absolute)))
            {
                return false;
            }
            else if(!await CheckIfUrlExistInRepo(downloadUrl, "https://github.com/AbrarJahin/BCC-CA_XMLSigningClient/blob/master/.doc/approved_url_list.json"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        [Obsolete]
        private static async Task<bool> CheckIfUrlExistInRepo(string urlToCheck, string apiUrl)
        {
            RestClient client = new RestClient(apiUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = await client.ExecuteTaskAsync(request);
            throw new NotImplementedException();
        }

        [Obsolete]
        internal static async Task<Tuple<XmlDocument, string>> DownloadFileWithIdAsync(string downloadUrl)
        {
            RestClient client = new RestClient(downloadUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = await client.ExecuteTaskAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
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
    }
}
