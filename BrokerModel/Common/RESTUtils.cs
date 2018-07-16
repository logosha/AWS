using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Utils
{
    public static class REST
    {
        public static async Task<string> SendRequest(string request, string httpMethod)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            HttpWebRequest wr = WebRequest.Create(request) as HttpWebRequest;
            wr.Method = httpMethod;
            wr.ContentType = "application/json";

            string response = null;
            try
            {
                HttpWebResponse resp = await wr.GetResponseAsync() as HttpWebResponse;
                StreamReader sr = new StreamReader(resp.GetResponseStream());
                response = sr.ReadToEnd();
                sr.Close();
            }
            catch (WebException ex)
            {
                StreamReader sr = new StreamReader(ex.Response.GetResponseStream());
                response = sr.ReadToEnd();
                sr.Close();
            }
            return response;
        }
    }
}
