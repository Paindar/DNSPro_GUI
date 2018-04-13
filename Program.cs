using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DNSPro_GUI
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Logging.OpenLogFile();
            LogForm form = new LogForm
            {
                BackColor = Color.Gray,
                TransparencyKey = Color.White,
                Opacity = 0.7,
                Visible = false
            };
            ToolBarForm toolBar = new ToolBarForm(form);

            DNSServer server = new DNSServer();
            ServerThread serverThread = new ServerThread(() => { server.Start(); });
            serverThread.Start();
            Application.Run();
            toolBar.Close();
            form.Close();
            serverThread.Abort();
            server.Close();
        }
    }
}
