using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DNSPro_GUI
{
    class IPUtils
    {
        internal class IPRange
        {
            public uint ipfrom { get; set; }
            public uint ipto { get; set; }
            public string registry { get; set; }
            public string ctry { get; set; }
            public string cntcy { get; set; }
            public string country { get; set; }
            public override string ToString() => $"{ipfrom} {ipto} {registry} {ctry} {cntcy} {country}";
        }
        List<IPRange> list = new List<IPRange>();
        public IPUtils(string filePath)
        {        //Create User object.  
            IPRange ir = new IPRange();

            FileStream fileStream = new FileStream(filePath, FileMode.Open);
            StreamReader sr = new StreamReader(fileStream);
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                if (str[0] != '#')
                {
                    IPRange obj = JsonConvert.DeserializeObject<IPRange>(str);
                    if(list.Count==0)
                    {
                        list.Add(obj);
                        continue;
                    }
                    IPRange lastObj = list[list.Count - 1];
                    if (lastObj.ipto==obj.ipfrom-1 &&
                        lastObj.cntcy == obj.cntcy &&
                        lastObj.ctry == obj.ctry &&
                        lastObj.country == obj.country)
                    {
                        list[list.Count - 1].ipto = obj.ipto;
                        //Console.WriteLine($"list[list.Count - 1] = {list[list.Count - 1]} lastObj = {lastObj}");
                    }
                    else
                        list.Add(obj);
                }
              
            }
        }
        private IPRange GetIPRange(uint addr)
        {
            foreach (var ip in list)
            {
                    if (ip.ipfrom <= addr && ip.ipto > addr)
                        return ip;
            }
                return null;
        }
        public string GetIPCtry(uint addr)
        {
            var ip = GetIPRange(addr);
            return ip != null ? ip.ctry : "";
        }
        public string GetIPCntcy(uint addr)
        {
            var ip = GetIPRange(addr);
            return ip != null ? ip.cntcy : "";
        }
        public string GetIPCountry(uint addr)
        {
            var ip = GetIPRange(addr);  
            return ip != null ? ip.country : "";
        }
        public string GetIPRegistry(uint addr)
        {
            var ip = GetIPRange(addr);
            return ip != null ? ip.registry : "";
        }
        private string Uint2IPStr(uint u)
        {
            ushort a = (ushort)((u >> 24) & 0xff);
            ushort b = (ushort)((u >> 16) & 0xff);
            ushort c = (ushort)((u >> 8) & 0xff);
            ushort d = (ushort)(u & 0xff);
            return $"{a}.{b}.{c}.{d}";
        }
        public string GetIPsByCtry(string ct)
        {
            StringBuilder sb = new StringBuilder();
            foreach(var ir in list)
            {
                if(ir.ctry.Equals(ct))
                {
                    sb.Append($"{Uint2IPStr(ir.ipfrom)}-{Uint2IPStr(ir.ipto)}; ");
                }
            }
            return sb.ToString();
        }
    }
}
