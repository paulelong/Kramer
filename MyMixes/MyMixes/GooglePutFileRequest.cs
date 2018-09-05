using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

using Xamarin.Auth;

namespace MyMixes
{
    internal class GooglePutFileRequest : OAuth2Request
    {
        public GooglePutFileRequest(string u, Uri uri, IDictionary<string,string> dict, Account a) : base(u, uri, dict, a)
        {

        }

        public virtual void SetRequestBody(byte[] b)
        {
            var request = this.GetPreparedWebRequest();

            request.Content = new System.Net.Http.ByteArrayContent(b);
        }

        public virtual void SetRequestBody(string s, string contenttype = null)
        {
            var request = this.GetPreparedWebRequest();
            //request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            //byte[] bytes = new byte[s.Length * sizeof(char)];
            //System.Buffer.BlockCopy(s.ToCharArray(), 0, bytes, 0, bytes.Length);

            //request.Content = new System.Net.Http.ByteArrayContent(bytes);
            request.Content = new StringContent(s, Encoding.UTF8, "application/json");
        }

    }
}
