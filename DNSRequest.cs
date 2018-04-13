using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNSPro_GUI
{
    class Flag
    {
        public short qr, opcode, aa, tc, rd, ra, rcode;
        public Flag(byte b0,byte b1)
        {
            qr = (short)((b0 >> 7) & 1);
            opcode = (short)((b0 >> 3) & 0xf);
            aa = (short)((b0 >> 2) & 1);
            tc = (short)((b0>>1) & 1);
            rd = (short)(b0 & 1);
            ra = (short)((b1 >> 7) & 1);
            rcode = (short)((b1) & 0xf);
        }
        public override string ToString()
        {
            return $"{qr} {opcode} {aa} {tc} {rd} {ra}  Z   {rcode}";
        }
    }
    class DNSRequest
    {
        public int id { get; }//2 bytes
        public Flag flag { get; }//2bytes;
        public int qdCount { get; }//2bytes
        public int anCount { get; }//2bytes
        public int nsCount { get; }//2bytes
        public int arCount { get; }//2bytes
        public string qname { get; }
        public int qtype { get; }
        public int qclass { get; }

        public DNSRequest(byte[] pack)
        {
            id = pack[0] * 256 + pack[1];
            flag = new Flag(pack[2], pack[3]);
            qdCount = pack[4] * 256 + pack[5];
            anCount = pack[6] * 256 + pack[7];
            nsCount = pack[8] * 256 + pack[9];
            arCount = pack[10] * 256 + pack[11];
            StringBuilder sb = new StringBuilder();
            int i = 0;
            for(i=12;i<pack.Length-5;i++)
            {
                if (pack[i] == 0)
                    break;
                if (sb.Length != 0)
                    sb.Append('.');
                for(int j=0;j<pack[i];j++)
                {
                    sb.Append((char)pack[i + 1 + j]);
                }
                
                i += pack[i];
                
            }
            qname = sb.ToString();
            i++;
            qtype = pack[i] * 256 + pack[i+1];
            i += 2;
            qclass = pack[i] * 256 + pack[i+1];
            i += 2;
        }
        public override string ToString()
        {
            return $"{id}\n{flag}\n{qdCount}\n{anCount}\n{nsCount}\n{arCount}\n{qname}\n{qtype}\n{qclass}";
        }
    }
}
