using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DNSPro_GUI.Network.Stages
{
    class UdpConn
    {
        private UdpClient client;
        private byte[] buf;
        private IConnHandle handle;
        private IPEndPoint endPoint;
        private bool isClose = false;

        public UdpConn(UdpClient c, byte[] buf, IPEndPoint endPoint)
        {
            this.client = c;
            this.buf = buf;
            this.endPoint = endPoint;
        }

        public void Invoke(IConnHandle h)
        {
            handle = h;
        }

        public void Close()
        {
            handle.End();
            isClose = true;
        }
        public bool IsClose() { return isClose; }

        public void SendBack(byte[] bytes)
        {
            client.BeginSend(bytes, bytes.Length, endPoint, (IAsyncResult ar) => { client.EndSend(ar); }, null);
        }


    }
}
