using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace App2
{
    class Get
    {
        public static async Task<string> Get_Response_Str(string uri)
        {
            HttpClientHandler myHandler = new HttpClientHandler();
            myHandler.AllowAutoRedirect = false;
            HttpClient myClient = new HttpClient(myHandler);
            var myRequest = new HttpRequestMessage(HttpMethod.Get, uri);

            var response = await myClient.SendAsync(myRequest);

            string result = await response.Content.ReadAsStringAsync();

            return result;
        }
    }
}
