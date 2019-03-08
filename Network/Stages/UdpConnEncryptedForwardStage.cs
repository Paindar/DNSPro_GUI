using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;

namespace DNSPro_GUI.Network.Stages
{
    class UdpConnEncryptedForwardStage : IConnHandle
    {
        WebSocket ws;
        DiversionSystem.RemotePoint point;
        byte[] header;
        public UdpConnEncryptedForwardStage(UdpConn conn, DiversionSystem.RemotePoint point, byte[] header):base(conn)
        {
            this.point = point;
            this.header = header;
            ws = new WebSocket(point.addr);
        }
        public override void End()
        {
            Logging.Info("Execute end");
            if(ws!=null)
            {
                Logging.Info("Close " + ws);
                Logging.Info(string.Format("id:{1} End time: {0:MM/dd/yyy HH:mm:ss.fff}", DateTime.Now, header[0] + header[1]));
                ws.Close();
                ws = null;
            }
        }

        public override void Invoke(byte[] bytes)
        {
            ws = new WebSocket(point.addr);
            ws.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
            
            ws.OnMessage += (sender, e) =>
            {
                byte[] buffer = e.RawData; // read in chunks of 2KB
                Logging.Info(string.Format("id:{1} Recv time: {0:MM/dd/yyy HH:mm:ss.fff}", DateTime.Now, header[0] + header[1]));
                conn.SendBack(buffer);
            };
            ws.OnClose += (sender, e) => 
            {
                Logging.Info("Found close event."+header[0]+header[1]);
                ws = null;
            };

            ws.Connect();
            ws.Send(bytes);
            Logging.Info("Send data to remote.");
        }
        
    }
}
