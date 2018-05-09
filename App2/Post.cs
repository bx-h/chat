using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace App2
{
    class Post
    {
        public static async Task<string> Get_Response_Str(string uri, string json)
        {
            var myClient = new HttpClient();
            var myRequest = new HttpRequestMessage(HttpMethod.Post, uri);
            HttpContent content = new StringContent(json);
            
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            myRequest.Content = content;
            var response = await myClient.SendAsync(myRequest);

            string result = await response.Content.ReadAsStringAsync();

            return result;
        }
    }
}
