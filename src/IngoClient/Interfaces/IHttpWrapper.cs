using IngoClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace IngoClient.Interfaces
{
    public interface IHttpWrapper
    {
        void AddDefaultHeader(string name, string value);
        void RemoveDefaultHeader(string name);
        HttpResponse Post(string url, HttpContent content);
        HttpResponse Patch(string url, HttpContent content);
        HttpResponse Get(string url);
    }
}