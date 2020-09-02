using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace XMLSigner.Library
{
    class UrlChecker
    {
        [Obsolete]
        internal static async Task<bool> CheckIfUrlApprovedAsync(string downloadUrl, string uploadUrl)
        {
            if ((new Uri(downloadUrl)).Host != (new Uri(uploadUrl)).Host)
            {
                return false;
            }
            else if(!await CheckIfUrlExistInRepo(downloadUrl, "https://raw.githubusercontent.com/AbrarJahin/BCC-CA_XMLSigningClient/master/.doc/approved_url_list.json"))
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
            if (response.StatusCode == HttpStatusCode.OK)
            {
                foreach(string url in JsonConvert.DeserializeObject<List<string>>(response.Content))
                {
                    if ((new Uri(urlToCheck)).Host == (new Uri(url)).Host)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
