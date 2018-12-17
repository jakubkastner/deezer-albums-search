using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace deezer
{
    class Uzivatel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string lastname { get; set; }
        public string firstname { get; set; }
        public string picture { get; set; }
        public string link { get; set; }
        public string country { get; set; }
    }
}
