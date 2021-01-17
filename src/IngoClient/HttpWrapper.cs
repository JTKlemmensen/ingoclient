using IngoClient.Interfaces;
using IngoClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace IngoClient
{
    public class HttpWrapper : IHttpWrapper
    {
        private HttpClient client = new HttpClient();
        public void AddDefaultHeader(string name, string value)
        {
            client.DefaultRequestHeaders.Remove(name);
            client.DefaultRequestHeaders.Add(name, value);
        }

        public HttpResponse Post(string url, HttpContent content)
        {
            var result = client.PostAsync(url, content).Result;

            return new HttpResponse
            {
                StatusCode = (int)result.StatusCode,
                ResponseBody = result.Content.ReadAsStringAsync().Result,
                Headers = result.Headers.ToDictionary(a => a.Key, a => a.Value)
            };
        }

        public HttpResponse Patch(string url, HttpContent content)
        {
            var result = client.PatchAsync(url, content).Result;

            return new HttpResponse
            {
                StatusCode = (int)result.StatusCode,
                ResponseBody = result.Content.ReadAsStringAsync().Result,
                Headers = result.Headers.ToDictionary(a => a.Key, a => a.Value)
            };
        }

        public HttpResponse Get(string url)
        {
            var result = client.GetAsync(url).Result;
            return new HttpResponse
            {
                StatusCode = (int)result.StatusCode,
                ResponseBody = result.Content.ReadAsStringAsync().Result,
                Headers = result.Headers.ToDictionary(a => a.Key, a => a.Value)
        };
        }

        public void RemoveDefaultHeader(string name)
        {
            client.DefaultRequestHeaders.Remove(name);
        }
    }
}