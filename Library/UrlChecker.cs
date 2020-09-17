using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace XMLSigner.Library
{
    class UrlChecker
    {
        private static List<string> _availableLinks;

        [Obsolete]
        internal static async Task<bool> CheckIfUrlApprovedAsync(string downloadUrl, string uploadUrl)
        {
            if ((new Uri(downloadUrl)).Host != (new Uri(uploadUrl)).Host)
            {
                return false;
            }
            else if (!await CheckIfUrlExistInRepo(downloadUrl, "https://raw.githubusercontent.com/AbrarJahin/BCC-CA_XMLSigningClient/master/.doc/approved_url_list.json"))
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
            if (_availableLinks != null)
            {
                return CheckIfUrlExistInServerList(urlToCheck);
            }
            RestClient client = new RestClient(apiUrl);
            RestRequest request = new RestRequest(Method.GET);
            IRestResponse response = await client.ExecuteTaskAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _availableLinks = JsonConvert.DeserializeObject<List<string>>(response.Content);
                return CheckIfUrlExistInServerList(urlToCheck);
            }
            MessageBox.Show("URL Checker API Blocked");
            return false;
        }

        private static bool CheckIfUrlExistInServerList(string urlToCheck)
        {
            foreach (string url in _availableLinks)
            {
                if ((new Uri(urlToCheck)).Host == url)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
