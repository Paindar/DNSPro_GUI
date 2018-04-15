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
            int len = GetStringFromBytes(buf, str, out string tmp);
            name = tmp;
            i += len + 1;
            type = buf[i] * 256 + buf[i + 1];//2
            i += 2;
            klass = buf[i] * 256 + buf[i + 1];//2
            i += 2;
            ttl = (((buf[i] * 256 + buf[i + 1]) * 256) + buf[i + 2]) * 256 + buf[i + 3];//4
            i += 4;
            rdlength = buf[i] * 256 + buf[i + 1];//2
            i += 2;
            if (type == 12)//ipaddress to hostname
            {
                for (int j = 0; j < rdlength; j++)
                {
                    rdata += (char)(buf[i + j]);
                }
            }
            else if (type == 1)
            {
                rdata = "";
                for (int j = 0; j < rdlength; j++)
                {
                    if (rdata.Length != 0)
                        rdata += '.';
                    rdata += (int)(buf[i + j]);
                }
            }
            else if (type == 28)
            {
                rdata = "";
                for (int j = 0; j < rdlength; j += 4)
                {
                    if (rdata.Length != 0)
                        rdata += ':';
                    rdata += (((buf[i] * 256 + buf[i + 1]) * 256 + buf[i + 2]) * 256 + buf[3]).ToString("X");
                }
            }
            else if (type == 2)
            {
                len = GetStringFromBytes(buf, i, out tmp, rdlength);
                rdata = tmp;
            }
            else
            {
                Console.WriteLine("Cannot analyse with type " + type);
            }
            i = i + rdlength;
            allSize = i - str;
        }
        public static int GetStringFromBytes(byte[] buf, int str, out string result, int maxLen = -1)
        {
            StringBuilder sb = new StringBuilder();
            Stack<int> stack = new Stack<int>();
            int len = (maxLen == -1 ? buf.Length - 1 : maxLen);
            for (int j = str; j < (maxLen == -1 ? buf.Length : str + maxLen); j++)
            {
                if ((buf[j] & 0xC0) == 0xC0)//found ptr, jump to address
                {
                    stack.Push(j + 1);
                    j = ((buf[j] * 256 + buf[j + 1]) & 16383) - 1;
                }
                else if (buf[j] != 0x00)// not ptr, common reading
                {
                    if (sb.Length != 0)
                        sb.Append('.');
                    for (int k = 0; k < buf[j]; k++)
                    {
                        sb.Append((char)buf[j + k + 1]);
                    }
                    j += buf[j];
                }
                else//found 0, end reading.
                {
                    while (stack.Count != 0)
                    {
                        j = stack.Pop();
                    }
                    len = j - str;
                    break;
                }
            }
            result = sb.ToString();
            return len;
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

        public DNSResponse(byte[] buf)
        {
            id = buf[0] * 256 + buf[1];
            flag = new Flag(buf[2], buf[3]);
            qdCount = buf[4] * 256 + buf[5];
            anCount = buf[6] * 256 + buf[7];
            nsCount = buf[8] * 256 + buf[9];
            arCount = buf[10] * 256 + buf[11];
            int len = Answer.GetStringFromBytes(buf, 12, out string tmp);
            qname = tmp;
            int i = 12 + len + 1;
            qtype = buf[i] * 256 + buf[i + 1];
            i += 2;
            qclass = buf[i] * 256 + buf[i + 1];
            i += 2;
            while (i < buf.Length)
            {
                Answer ans = new Answer(buf, i);
                answers.Add(ans);
                if (ans.allSize > 0)
                {
                    i += ans.allSize;
                }
                else
                    break;
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
