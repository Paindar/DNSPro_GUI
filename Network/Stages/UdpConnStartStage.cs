using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNSPro_GUI.Network.Stages
{
    class UdpConnStartStage : IConnHandle
    {
        public UdpConnStartStage(UdpConn conn):base(conn)
        {

        }
        public override void End()
        {
        }

        public override void Invoke(byte[] bytes)
        {
            byte[] header = new byte[12];
            byte[] question = new byte[bytes.Length - 12];
            for(int i=0;i<bytes.Length;i++)
            {
                if(i<12)
                {
                    header[i] = bytes[i];
                }
                else
                {
                    question[i - 12] = bytes[i];
                }
            }
            Logging.Info("Get request id " + header[0] + header[1]);
            IConnHandle handle = new UdpConnForwardStage(conn, header, question);
            base.conn.Invoke(handle);
            handle.Invoke(bytes);
            
        }
    }
}
