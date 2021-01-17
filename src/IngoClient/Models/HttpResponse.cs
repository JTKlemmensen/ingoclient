using System;
using System.Collections.Generic;
using System.Text;

namespace IngoClient.Models
{
    public class HttpResponse
    {
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; }
        public IDictionary<string, IEnumerable<string>> Headers { get; set; }
    }
}