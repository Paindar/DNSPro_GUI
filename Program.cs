using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DNSPro_GUI
{
    static class Program
    {
        private static ServerThread serverThread;
        private static LogForm form;
        private static ToolBarForm toolBar;
        private static DNSServer server;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // handle UI exceptions
            Application.ThreadException += Application_ThreadException;
            // handle non-UI exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ApplicationExit += Application_ApplicationExit;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Logging.OpenLogFile();
            form = new LogForm
            {
                BackColor = Color.Gray,
                TransparencyKey = Color.White,
                Opacity = 0.7,
                Visible = false
            };
            toolBar = new ToolBarForm(form);
            
            server = new DNSServer();
            serverThread = new ServerThread(() => { server.Start(); });
            serverThread.Start();
            
            Application.Run();
        }
        private static int exited = 0;
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref exited) == 1)
            {
                string errMsg = e.ExceptionObject.ToString();
                Logging.Error(errMsg);
                MessageBox.Show(
                    $"Unexpected error, DNSPro will exit. Please send this to author.",
                    "DNS_Pro console Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref exited) == 1)
            {
                string errorMsg = $"Exception Detail: {Environment.NewLine}{e.Exception}";
                Logging.Error(errorMsg);
                MessageBox.Show(
                    $"Unexpected error, shadowsocks will exit. Please send this report  to  author ",
                    "DNS_Pro GUI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Logging.Info("os wake up");
                    if (serverThread != null)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(10 * 1000);
                            try
                            {
                                serverThread.Start();
                                Logging.Info("Started");
                            }
                            catch (Exception ex)
                            {
                                Logging.LogUsefulException(ex);
                            }
                        });
                    }
                    break;
                case PowerModes.Suspend:
                    if (serverThread != null)
                    {
                        server.Close();
                        serverThread.Abort();
                        Logging.Info("Stopped");
                    }
                    Logging.Info("os suspend");
                    break;
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            // detach static event handlers
            Application.ApplicationExit -= Application_ApplicationExit;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            Application.ThreadException -= Application_ThreadException;
            if (serverThread != null)
            {
                toolBar.Close();
                form.Close();
                serverThread.Abort();
                server.Close();
            }
        }
    }
}
