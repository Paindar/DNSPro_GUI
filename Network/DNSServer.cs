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
        private DiversionSystem diversion;
        private List<UdpClient> udpClients = new List<UdpClient>();
        public int GetConnectCount() => udpClients.Count;
        public DNSServer()
        {
            diversion = new DiversionSystem();
            if (!File.Exists("config.json"))
                File.WriteAllBytes("config.json", Resource.config);
            diversion.ReadConf("config.json");
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
        public void ReceiveMessage()
        {
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    // 关闭receiveUdpClient时此时会产生异常
                    IAsyncResult iar = udpClient.BeginReceive(null, null);
                    byte[] buf = udpClient.EndReceive(iar, ref remoteIpEndPoint);
                    DNSRequest req = new DNSRequest(buf);
                    try
                    {
                        UdpClient midManClient = new UdpClient();
                        
                        IPEndPoint server = diversion.Request(req.qname);
                        Logging.Info("analyse: " + req.qname + $" from {server}");
                        midManClient.BeginSend(buf, buf.Length, server, new AsyncCallback((IAsyncResult ar)=>
                        {
                            object[] state = (object[])ar.AsyncState;
                            UdpClient mmClient = (UdpClient)state[0];
                            IPEndPoint e = (IPEndPoint)state[1];
                            mmClient.EndSend(ar);
                            mmClient.BeginReceive((IAsyncResult ar1)=> 
                            {
                                IPEndPoint e1 = new IPEndPoint(0, 0);
                                byte[] buff = mmClient.EndReceive(ar1, ref e1);
                                mmClient.Close();
                                udpClient.BeginSend(buff, buff.Length, e, (IAsyncResult ar2) => { udpClient.EndSend(ar2); }, null);
                            }, state);
                        }),new object[] { midManClient, remoteIpEndPoint });
                    }
                    catch (Exception ex)
                    {
                        Logging.Error(ex.ToString());
                    }
                }
                catch(Exception e)
                {
                    Logging.Error(e.ToString());
                }
            }
        }
        public void Close()
        {
            if (tcpServer != null)
                tcpServer.Close();
            if (udpClient != null)
                udpClient.Close();
            lock(udpClients)
            {
                udpClients.ForEach(h => { h.Close(); });
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
            DNSRequest req = new DNSRequest(buf);
            try
            {
                UdpClient midManClient = new UdpClient();
                lock (udpClients)
                {
                    udpClients.Add(midManClient);
                }
                IPEndPoint server = diversion.Request(req.qname);
                midManClient.Connect(server);
                Logging.Info("analyse: " + req.qname+$" from {server}");
                Timer timer = new Timer((object obj) => 
                    {
                        try
                        {
                            midManClient.Close();

                        }
                        catch(ObjectDisposedException)
                        {
                            return;
                        }
                        Logging.Info($"connect to {server} close: time out.");
                    }
                , null, 10 * 1000, Timeout.Infinite);
                midManClient.BeginSend(buf, buf.Length, new AsyncCallback(UdpSendCallback),
                    new object[] { midManClient, e ,timer});
            }
            catch(Exception ex)
            {
                Logging.Error(ex.ToString());
            }
            

        }
        private void UdpSendCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;
            UdpClient mmClient = (UdpClient)state[0];
            IPEndPoint e = (IPEndPoint)state[1];
            Timer timer = (Timer)state[2];
            if (timer != null)
            {
                
                timer.Dispose();
            }
            if (mmClient == null)
                return;
            try
            {
                mmClient.EndSend(ar);
                mmClient.BeginReceive(new AsyncCallback(UdpReceiveResultCallback), new object[] { mmClient, e });
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
        private void UdpReceiveResultCallback(IAsyncResult ar)
        {
            IPEndPoint e1=new IPEndPoint(0,0);
            object[] state = (object[])ar.AsyncState;
            UdpClient mmClient = (UdpClient)state[0];
            IPEndPoint e = (IPEndPoint)state[1];
            if (mmClient == null)
                return;
            byte[] buf;
            try
            {
                buf = mmClient.EndReceive(ar, ref e1);
            }
            catch(ObjectDisposedException)
            {
                return;
            }
            try
            {
                DNSResponse response = new DNSResponse(buf);
                if(response.answers.Count>0)
                    Logging.Info($"{response.qname} = {response.answers[0].rdata}");
            }
            catch (Exception ex)
            {
                Logging.Error($"cannot analyse:  with Exception {ex}");
            }
            //TODO analyse pack and cache
            udpClient.BeginSend(buf, buf.Length, e, UdpSendResultCallback, mmClient);
            
        }
        private void UdpSendResultCallback(IAsyncResult ar)
        {
            UdpClient client = (UdpClient)ar.AsyncState;
            udpClient.EndSend(ar);
            client.Close();
            lock (udpClients)
            {
                udpClients.Remove(client);
            }
        }
        //响应连接请求
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Logging.Warn("Find a tcp connect.");
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
                        midManSocket.BeginConnect(diversion.DefaultServer, new AsyncCallback(ConnectCallback),
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
