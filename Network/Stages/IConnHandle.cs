using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNSPro_GUI.Network.Stages
{
    abstract class IConnHandle
    {
        protected UdpConn conn;
        public IConnHandle(UdpConn conn)
        {
            this.conn = conn;
        }

        public abstract void Invoke(byte[] bytes);

        public abstract void End();
    }
}
