using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace DNSPro_GUI
{
    class JsonConfig
    {
        private JToken cfg = null;
        public JsonConfig(string filePath)
        {
            cfg = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(filePath));
        }

        public int GetInteger(string path, int defVal)
        {
            if (cfg == null)
                return defVal;
            try
            {
                string[] paths = path.Split('.');
                JToken curNode = cfg;
                foreach(string p in paths)
                {
                    curNode = cfg[p];
                }
                //todo
            }
            catch(Exception e)
            {
                Logging.Info(e);
            }
            return defVal;
        }
    
    }
}
