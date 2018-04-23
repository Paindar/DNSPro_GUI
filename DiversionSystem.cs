
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace DNSPro_GUI
{
    class DiversionSystem
    {
        List<KeyValuePair<string, IPEndPoint>> rules = new List<KeyValuePair<string, IPEndPoint>>();
        Dictionary<string, IPEndPoint> regions = new Dictionary<string, IPEndPoint>();
        IPEndPoint defaultServer;
        //IPUtils ipRegion;

        public IPEndPoint DefaultServer { get => defaultServer;  }

        public DiversionSystem()
        {
            if(!File.Exists("regions.json"))
            {
                File.WriteAllText("regions.json", Utils.UnGzip(Resource.IpToCountry_json));
            }
            //ipRegion = new IPUtils("regions.json");
        }
        public IPEndPoint Request(string addr)
        {
            foreach(var i in rules)
            {
                if(Regex.IsMatch(addr, i.Key))
                {
                    return i.Value;
                }
            }
            //TODO support ipv6
            /*string ctry = ipRegion.GetIPCtry((uint)IPAddress.Parse(addr).Address);
            if(regions.ContainsKey(ctry))
            {
                return regions[ctry];
            }*/
            return defaultServer;
        }

        public void ReadConf(string filePath)
        {
            dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(filePath));
            foreach (dynamic item in obj.regex)
            {
                string server = item.server.ToString();
                int port = 53;
                try
                {
                    port = item.port;
                }
                catch(System.Exception)
                {
                    ;
                }
                IPEndPoint ip = new IPEndPoint(IPAddress.Parse(server), port);
                foreach(dynamic rule in item.rule)
                {
                    rules.Add(new KeyValuePair<string, IPEndPoint>(rule.ToString(), ip));
                }
            }
            /*foreach(dynamic item in obj.region)
            {
                string server = item.server.ToString();
                IPEndPoint ip = new IPEndPoint(IPAddress.Parse(server), 53);
                foreach (dynamic reg in item.ctry)
                {
                    regions.Add(reg.ToString(), ip);
                }
            }*/
            defaultServer = new IPEndPoint(IPAddress.Parse(obj["default"].ToString()),53);
        }
    }
}
