using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DNSPro_GUI.Network.Stages
{
    class UdpConnForwardRecv : IConnHandle
    {
        private IPEndPoint endPoint;
        private byte[] header;
        private UdpClient forwardClient = new UdpClient();
        private Timer timer;

        public UdpConnForwardRecv(UdpConn conn, IPEndPoint endPoint, byte[] header) : base(conn)
        {
            this.endPoint = endPoint;
            this.header = header;
        }
        public override void End()
        {
            if(forwardClient!=null)
                forwardClient.Close();
        }

        public override void Invoke(byte[] bytes)
        {
            forwardClient.BeginSend(bytes, bytes.Length, endPoint, new AsyncCallback(onSendEnd), null);
        }

        private void onSendEnd(IAsyncResult ar)
        {
            try
            { 
                forwardClient.EndSend(ar);
            }
            catch(ObjectDisposedException)
            {
                ;
            }
            catch(SocketException e)
            {
                return;
            }
            forwardClient.BeginReceive(new AsyncCallback(onRecvEnd), null);
            timer = new Timer(6000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void onRecvEnd(IAsyncResult ar)
        {
            try
            {
                IPEndPoint e1 = new IPEndPoint(0, 0);
                byte[] buff = forwardClient.EndReceive(ar, ref e1);
                Logging.Info("End request id " + header[0] + header[1]);
                timer.Elapsed -= Timer_Elapsed;
                timer.Stop();
                conn.SendBack(buff);
                conn.Close();
            }
            catch(ObjectDisposedException)
            {
                ;
            }
            
        }
    }
}
