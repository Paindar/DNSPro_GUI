using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNSPro_GUI
{
    class Answer
    {
        public string name { get; set; }
        public int type { get; set; }
        public int klass { get; set; }
        public int ttl { get; set; }
        public int rdlength { get; set; }
        public string rdata { get; set; }
        public bool similar = false;
        public int allSize = 0;
        public Answer(byte[] buf, int str)
        {
            int i = str;
            if (buf[i] == 0xC0)
            {
                if(buf[i + 1] == 0xC)
                { 
                    similar = true;
                    i += 2;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    for (int j=buf[i+1]; j < buf.Length; j++)
                    {
                        if (buf[j] == 0)
                            break;
                        if (sb.Length != 0)
                            sb.Append('.');
                        for (int k = 0; k < buf[j]; k++)
                        {
                            sb.Append((char)buf[j+k]);
                        }
                    }
                    name = sb.ToString();
                    i+=2;
                }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for (; i < buf.Length; i++)
                {
                    if (buf[i] == 0)
                        break;
                    if (sb.Length != 0)
                        sb.Append('.');
                    for (int j = 0; j < buf[i]; j++)
                    {
                        sb.Append((char)buf[i + 1 + j]);
                    }

                    i += buf[i];
                }
                name = sb.ToString();
                i++;
            }
            type = buf[i] * 256 + buf[i + 1];
            i += 2;
            klass = buf[i] * 256 + buf[i + 1];
            i += 2;
            ttl = (((buf[i] * 256 + buf[i + 1]) * 256) + buf[i + 2]) * 256 + buf[i + 3];
            i += 4;
            rdlength = buf[i] * 256 + buf[i + 1];
            i += 2;
            if(type ==12)//ipaddress to hostname
            {
                for (int j = 0; j < rdlength; j++)
                {
                    rdata += (char)(buf[i + j]);
                }
            }
            else if(type==1)
            {
                rdata = "";
                for (int j = 0; j < rdlength; j++)
                {
                    if (rdata.Length != 0)
                        rdata += '.';
                    rdata += (int)(buf[i + j]);
                }
            }
            else if(type==28)
            {
                rdata = "";
                for (int j = 0; j < rdlength; j+=4)
                {
                    if (rdata.Length != 0)
                        rdata += ':';
                    rdata += (((buf[i]*256+buf[i+1])*256+buf[i+2])*256+buf[3]).ToString("X");
                }
            }
            i = i + rdlength;
            allSize = i - str;
        }
    }
    class DNSResponse
    {
        public int id { get; set; }//2 bytes
        public Flag flag { get; set; }//2bytes;
        public int qdCount { get; set; }//2bytes
        public int anCount { get; set; }//2bytes
        public int nsCount { get; set; }//2bytes
        public int arCount { get; set; }//2bytes
        public string qname { get; set; }
        public int qtype { get; set; }
        public int qclass { get; set; }
        public List<Answer> answers = new List<Answer>();

        public DNSResponse(byte[] pack)
        {
            id = pack[0] * 256 + pack[1];
            flag = new Flag(pack[2], pack[3]);
            qdCount = pack[4] * 256 + pack[5];
            anCount = pack[6] * 256 + pack[7];
            nsCount = pack[8] * 256 + pack[9];
            arCount = pack[10] * 256 + pack[11];
            StringBuilder sb = new StringBuilder();
            int i = 0;
            for (i = 12; i < pack.Length; i++)
            {
                if (pack[i] == 0)
                    break;
                if (sb.Length != 0)
                    sb.Append('.');
                for (int j = 0; j < pack[i]; j++)
                {
                    sb.Append((char)pack[i + 1 + j]);
                }

                i += pack[i];

            }
            qname = sb.ToString();
            i++;
            qtype = pack[i] * 256 + pack[i + 1];
            i += 2;
            qclass = pack[i] * 256 + pack[i + 1];
            i += 2;
            while(i<pack.Length)
            {
                Answer ans = new Answer(pack, i);
                answers.Add(ans);
                i += ans.allSize;
            }
        }
        public void FillFromPequest(DNSRequest req)
        {
            id = req.id;
            flag = req.flag;
            qdCount = req.qdCount;
            anCount = req.anCount;
            nsCount = req.anCount;
            arCount = req.arCount;
            qname = req.qname;
            qtype = req.qtype;
            qclass = req.qclass;
        }
        
    }
}
