using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DNSPro_GUI.Network.Stages
{
    class UdpConnEncryptedForwardStage : IConnHandle
    {
        TcpClient client;
        IPEndPoint point;
        byte[] header;
        public UdpConnEncryptedForwardStage(UdpConn conn, IPEndPoint point, byte[] header):base(conn)
        {
            this.point = point;
            this.header = header;
        }
        public override void End()
        {
            if(client!=null)
            {
                client.Close();
            }
        }

        public override void Invoke(byte[] bytes)
        {
            client = new TcpClient(point.Address.ToString(), point.Port);
            using (SslStream sslStream = new SslStream(client.GetStream(), false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                {
                    sslStream.AuthenticateAsClient(point.Address.ToString());
                    sslStream.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(onSendEnd), null);
                        // This is where you read and send data
                }
            
        }

        private void onSendEnd(IAsyncResult ar)
        {
            client.GetStream().EndWrite(ar);
            byte[] buffer = new byte[2048]; // read in chunks of 2KB
            int bytesRead;
            while ((bytesRead = client.GetStream().Read(buffer, 0, buffer.Length)) > 0)
            {
                conn.SendBack(buffer);
            }
            Logging.Info("End request id " + header[0] + header[1]);
            conn.Close();
        }
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
