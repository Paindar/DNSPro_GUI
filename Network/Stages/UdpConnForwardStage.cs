using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DNSPro_GUI.Network.Stages
{
    class UdpConnForwardStage : IConnHandle
    {
        byte[] header;
        byte[] question;
        public UdpConnForwardStage(UdpConn conn, byte[] header, byte[] question) : base(conn)
        {
            this.header = header;
            this.question = question;

        }

        public override void End()
        {
            
        }
        

        public override void Invoke(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            for (i = 0; i < question.Length - 5; i++)
            {
                if (question[i] == 0)
                    break;
                if (sb.Length != 0)
                    sb.Append('.');
                for (int j = 0; j < question[i]; j++)
                {
                    sb.Append((char)question[i + 1 + j]);
                }
                i += question[i];
            }
            string hostName = sb.ToString();

            try
            {
                bool isEncrypted = false;
                DiversionSystem.RemotePoint serverAddress = DiversionSystem.GetInstance().Request(hostName, out isEncrypted);
                Logging.Info("analyse: " + hostName + $" forward to {serverAddress.addr}, enc: {isEncrypted}");
                if(isEncrypted)
                {
                    IConnHandle handle = new UdpConnEncryptedForwardStage(conn, serverAddress, header);
                    base.conn.Invoke(handle);
                    handle.Invoke(bytes);
                }
                else
                {
                    IPEndPoint point = serverAddress.ToIPEndPoint();
                    if(point.Address==IPAddress.None)
                    {
                        Logging.Warn("Cannot reach host " + serverAddress.addr);
                        return;
                    }
                    IConnHandle handle = new UdpConnForwardRecv(conn, point, header);
                    base.conn.Invoke(handle);
                    handle.Invoke(bytes);
                }
            }
            catch (Exception ex)
            {
                Logging.Error(ex.ToString());
            }
        }
    }
}
