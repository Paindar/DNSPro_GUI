
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace DNSPro_GUI
{
    class DiversionSystem
    {
        private static readonly object locker = new object();
        public static DiversionSystem uniqueInstance;
        public static DiversionSystem GetInstance()
        {
            lock (locker)
            {
                if (uniqueInstance == null)
                {
                    uniqueInstance = new DiversionSystem();
                    if (!File.Exists("config.json"))
                        File.WriteAllBytes("config.json", Resource.config);
                    uniqueInstance.ReadConf("config.json");
                }

            }

            return uniqueInstance;
        }

        List<KeyValuePair<string, IPEndPoint>> rules = new List<KeyValuePair<string, IPEndPoint>>();
        Dictionary<string, IPEndPoint> regions = new Dictionary<string, IPEndPoint>();
        IPEndPoint defaultServer;
        //IPUtils ipRegion;

        public IPEndPoint DefaultServer { get => defaultServer;  }

        private DiversionSystem()
        {
            if(!File.Exists("regions.json"))
            {
                File.WriteAllText("regions.json", Utils.UnGzip(Resource.IpToCountry_json));
            }
            //ipRegion = new IPUtils("regions.json");
        }
        /// <summary>
        /// 根据规则返回
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public IPEndPoint Request(string addr, out bool isEncrypted)
        {
            foreach(var i in rules)
            {
                if(Regex.IsMatch(addr, i.Key))
                {
                    isEncrypted = (i.Value.Port != 53);
                    return i.Value;
                }
            }
            //TODO support ipv6
            /*string ctry = ipRegion.GetIPCtry((uint)IPAddress.Parse(addr).Address);
            if(regions.ContainsKey(ctry))
            {
                return regions[ctry];
            }*/
            isEncrypted = false;
            return defaultServer;
        }

        public void ReadConf(string filePath)
        {
            dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(filePath));
            int port = 20;
            foreach (dynamic item in obj.regex)
            {
                string server = item.server.ToString();
                port = 53;
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
            port = 53;
            try
            {
                port = obj["port"];
            }
            catch (System.Exception)
            {
                ;
            }
            defaultServer = new IPEndPoint(IPAddress.Parse(obj["default"].ToString()),port);
        }
    }
}
