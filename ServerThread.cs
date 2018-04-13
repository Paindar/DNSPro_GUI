using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DNSPro_GUI
{
    public delegate void StartServerDelegate();
    class ServerThread
    {
        private Thread _thread;
        public ServerThread(StartServerDelegate method)
        {
            _thread = new Thread(() => { method(); });
            _thread.IsBackground = true;
        }
        public void Start() => _thread.Start();
        public void Abort() => _thread.Abort();
    }
}
