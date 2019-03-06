using DNSPro_GUI.Network.Stages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DNSPro_GUI
{
    class DNSServer
    {
        private Socket tcpServer;
        private UdpClient udpClient;
        //TODO Close all sub-socket
        private Dictionary<string, byte[]> cache = new Dictionary<string, byte[]>();
        private List<UdpConn> conns = new List<UdpConn>();
        public int GetConnectCount() => conns.Count;
        public DNSServer()
        {

        }
        public bool Start(bool localOnly = true)
        {
            try
            {
                tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //udpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                udpClient = new UdpClient(53, AddressFamily.InterNetwork);
                const uint IOC_IN = 0x80000000;
                int IOC_VENDOR = 0x18000000;
                int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                udpClient.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                
                //Thread receiveThread = new Thread(ReceiveMessage);
                //receiveThread.Start();
                try
                {
                    tcpServer.Bind(new IPEndPoint(localOnly ? IPAddress.Parse("127.0.0.1") : IPAddress.Any, 53));
                    tcpServer.Listen(1024);
                    udpClient.BeginReceive(new AsyncCallback(UdpReceiveCallback), null);
                    //udpServer.Bind(new IPEndPoint(localOnly ? IPAddress.Parse("127.0.0.1") : IPAddress.Any, 53));
                    //udpServer.BeginReceive(buf, 0,buf.Length, 0, new AsyncCallback(UdpReceiveCallback), buf);
                }
                catch (Exception e)
                {
                    Logging.Error(e);
                    return false;
                }
                tcpServer.BeginAccept(new AsyncCallback(AcceptCallback), tcpServer);
                return true;
            }
            catch (Exception e)
            {
                Logging.Error(e);
                return false;
            }
        }
        public void Close()
        {
            if (tcpServer != null)
                tcpServer.Close();
            if (udpClient != null)
                udpClient.Close();
            lock(conns)
            {
                conns.ForEach(h => { h.Close(); });
            }
        }
        private void UdpReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint e = new IPEndPoint(0,0);
            byte[] buf=null;
            try
            { 
                buf = udpClient.EndReceive(ar, ref e);
            }
            catch(ObjectDisposedException ex)
            {
                return;
            }
            catch(Exception ex)
            {
                Logging.Error(ex.ToString()+" "+e.ToString());
            }
            udpClient.BeginReceive(new AsyncCallback(UdpReceiveCallback), null);//keep main client listening
            if (buf == null)
                return;
            
            UdpConn conn = new UdpConn(udpClient, buf, e);
            IConnHandle handle = new UdpConnStartStage(conn);
            conn.Invoke(handle);
            handle.Invoke(buf);
            lock(conns)
            {
                conns.Add(conn);
            }
        }
        //响应连接请求
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Logging.Warn("Find a tcp connect.");
            return;//TODO TCP check
            try
            {
                Socket conn = listener.EndAccept(ar);
                byte[] buf = new byte[4096];
                object[] state = new object[] {
                    conn,
                    buf
                };

                conn.BeginReceive(buf, 0, buf.Length, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Logging.Error(e.Message);
            }
        }
        //包接收完毕
        private void ReceiveCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;

            Socket conn = (Socket)state[0];
            byte[] buf = (byte[])state[1];
            try
            {
                int bytesRead = conn.EndReceive(ar);
                if (bytesRead > 0)
                {
                    Console.WriteLine(Encoding.UTF8.GetString(buf));
                }
                //TODO
                // no service found for this
                if (conn.ProtocolType == ProtocolType.Tcp)
                {
                    try
                    { 
                        Socket midManSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        midManSocket.BeginConnect(DiversionSystem.GetInstance().DefaultServer, new AsyncCallback(ConnectCallback),
                            new object[] { midManSocket, buf, conn });
                    }
                    catch (Exception e)
                    {
                        Logging.Error(e.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Receive a Udp Request.");
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.Message);
                conn.Close();
            }
        }
        //与上游连接达成
        private void ConnectCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;

            Socket conn = (Socket)state[0];
            Socket conn1= (Socket)state[2];
            byte[] buf = (byte[])state[1];
            try
            {
                //TODO
                // no service found for this
                if (conn.ProtocolType == ProtocolType.Tcp)
                {
                    conn.Close();
                }
                else
                { 
                    conn.BeginSend(buf, 0, buf.Length, SocketFlags.None,
                            SendCallback, new object[] { conn, conn1 });
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.Message);
                conn.Close();
                conn1.Close();
            }
        }
        //向上游发送包完毕
        private void SendCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;
            Socket connToBottom = (Socket)state[0],
                connToAbove = (Socket)state[1];
            byte[] buf = new byte[4096];
            try
            {
                //TODO
                // no service found for this
                if (connToAbove.ProtocolType == ProtocolType.Tcp)
                {
                    connToAbove.Close();
                }
                else
                {
                    connToAbove.BeginReceive(buf, 0, buf.Length, SocketFlags.None,
                       ReceiveResultCallback, new object[] { connToAbove, connToBottom, buf});
                }

            }
            catch (Exception e)
            {
                Logging.Error(e.Message);
                connToAbove.Close();
                connToBottom.Close();
            }
        }
        private void ReceiveResultCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;
            Socket connToBottom = (Socket)state[0],
                connToAbove = (Socket)state[1];
            byte[] buf = (byte[])state[2];
            connToAbove.Close();
            try
            {
                connToBottom.BeginSend(buf, 0, buf.Length, SocketFlags.None,
                       ReceiveCloseCallback, connToBottom);
            }
            catch(Exception e)
            {
                Logging.Error(e);
            }
        }
        private void ReceiveCloseCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            conn.Close();
        }
    }
}
